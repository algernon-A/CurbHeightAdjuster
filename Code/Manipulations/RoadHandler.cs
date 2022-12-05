// <copyright file="RoadHandler.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace CurbHeightAdjuster
{
    using System;
    using System.Collections.Generic;
    using AlgernonCommons;
    using ColossalFramework;
    using UnityEngine;

    /// <summary>
    /// Class to manage changes to roads.
    /// </summary>
    internal class RoadHandler
    {
        // Original script by Ronyx69 to adjust curb heights from -30cm to -15cm, adapted to mod form by krzychu124.
        // Redesigned, rewritten, optimised, and extended by algernon.
        // Bridge deck and parking lot manipulations added by algernon.
        // Thanks to Ronyx69 (especially, for the original concept and implementation) and krzychu124 for their prior work!

        /// <summary>
        /// Standard vanilla curb heights.
        /// </summary>
        internal const float OriginalCurbHeight = -0.30f;

        /// <summary>
        /// Default new curb height.
        /// </summary>
        internal const float DefaultNewCurbHeight = -0.15f;

        /// <summary>
        /// Default new bridge trigger threshold.
        /// </summary>
        internal const float DefaultBridgeThreshold = -0.5f;

        /// <summary>
        /// Default new bridge deck thickness multiplier.
        /// </summary>
        internal const float DefaultBridgeMultiplier = 0.25f;

        /// <summary>
        /// Minimum permissible road curb height.
        /// </summary>
        internal const float MinCurbHeight = 0.07f;

        /// <summary>
        /// Maximum permissible road curb height.
        /// </summary>
        internal const float MaxCurbHeight = 0.29f;

        /// <summary>
        /// Minimum permissible bridge deck thickness trigger.
        /// </summary>
        internal const float MinBridgeThreshold = 0.55f;

        /// <summary>
        /// Maximum permissible bridge deck thickness trigger.
        /// </summary>
        internal const float MaxBridgeThreshold = 2.0f;

        /// <summary>
        /// Minimum permissible bridge deck thickness multiplier.
        /// </summary>
        internal const float MinBridgeScale = 0.1f;

        /// <summary>
        /// Maximum permissible bridge deck thickness multiplier.
        /// </summary>
        internal const float MaxBridgeScale = 1f;

        /// <summary>
        /// Minimum permissible bridge deck manipulation cutoff.
        /// </summary>
        internal const float MinBridgeCutoff = -3.1f;

        /// <summary>
        /// Maximum permissible bridge deck manipulation cutoff.
        /// </summary>
        internal const float BridgeDepthCutoff = -5f;

        // Depth triggers - segments/nets need to have depths within these bounds to be adjusted.
        // Vanilla tram rails have tops at -0.225.
        // LRT tram rails have bases at -0.5.
        private const float MinCurbDepthTrigger = -0.21f;
        private const float MaxCurbDepthTrigger = -0.32f;
        private const float MaxSubDepthTrigger = -0.55f;

        // Manipulation settings.
        private static float s_newCurbHeight = DefaultNewCurbHeight;
        private static float s_bridgeHeightThreshold = DefaultBridgeThreshold;
        private static float s_bridgeHeightScale = DefaultBridgeMultiplier;

        // Curb height multiplier.
        private static float s_newCurbMultiplier = DefaultNewCurbHeight / OriginalCurbHeight;

        // Dictionary of altered networks.
        private readonly Dictionary<NetInfo, NetRecord> _netRecords = new Dictionary<NetInfo, NetRecord>();

        // Hashset of currently processed network meshes, with calculated adjustment offsets.
        private readonly HashSet<Mesh> _processedMeshes = new HashSet<Mesh>();

        // Dictionary of catenary wire meshes, with orignal vertices.
        private readonly Dictionary<Mesh, Vector3[]> _catenaryMeshes = new Dictionary<Mesh, Vector3[]>();

        // Custom road handler.
        private readonly CustomRoadHandler _customRoadHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoadHandler"/> class.
        /// Scans through all loaded NetInfos, builds the database, and applies network manipulations (meshes and lanes).
        /// </summary>
        internal RoadHandler()
        {
            // List of meshes that we've already checked.
            HashSet<Mesh> checkedMeshes = new HashSet<Mesh>();

            // Initialise custom road handler.
            _customRoadHandler = new CustomRoadHandler();

            Logging.KeyMessage("starting load processing");

            // Iterate through all networks in list.
            for (uint i = 0; i < PrefabCollection<NetInfo>.LoadedCount(); ++i)
            {
                try
                {
                    NetInfo network = PrefabCollection<NetInfo>.GetLoaded(i);

                    // Skip any null prefabs.
                    if (network?.m_netAI == null || network.name == null || network.m_segments == null || network.m_nodes == null)
                    {
                        continue;
                    }

                    // Only looking at road prefabs.
                    NetAI netAI = network.m_netAI;
                    RoadBridgeAI bridgeAI = netAI as RoadBridgeAI;
                    bool isBridge = bridgeAI != null;
                    if (isBridge || netAI is RoadAI || netAI is RoadTunnelAI || netAI is DamAI)
                    {
                        // Check for any custom roads.
                        if (_customRoadHandler.IsCustomNet(network))
                        {
                            continue;
                        }

                        // Dirty flag.
                        bool netAltered = false;

                        // Network record for this prefab.
                        NetRecord netRecord = new NetRecord();

                        // Record original pillar height.
                        if (isBridge)
                        {
                            netRecord.m_bridgePillarOffset = bridgeAI.m_bridgePillarOffset;
                            netRecord.m_middlePillarOffset = bridgeAI.m_middlePillarOffset;
                        }

                        // Raise network surface level.
                        if (network.m_surfaceLevel == -0.3f)
                        {
                            // Record original value.
                            netAltered = true;
                            netRecord.m_surfaceLevel = network.m_surfaceLevel;

                            // Set new value.
                            network.m_surfaceLevel = s_newCurbHeight;
                        }

                        // Bridge adjustment status.
                        bool eligibleBridgeInfo = false;

                        // Raise segments - iterate through each segment in net.
                        foreach (NetInfo.Segment segment in network.m_segments)
                        {
                            // Skip segments with no mesh or material.
                            if (segment?.m_segmentMesh?.name == null || segment.m_segmentMaterial?.shader?.name == null)
                            {
                                continue;
                            }

                            // Only interested in segments using road shaders.
                            string shaderName = segment.m_segmentMaterial.shader.name;
                            if (shaderName != "Custom/Net/Road" && shaderName != "Custom/Net/RoadBridge" && shaderName != "Custom/Net/TrainBridge")
                            {
                                continue;
                            }

                            // Is mesh readable?
                            if (!segment.m_segmentMesh.isReadable)
                            {
                                // Unreadable mesh - see if we've got a replacement serialized mesh.
                                Logging.Message("unreadable segment mesh for network ", network.name);
                                Mesh replacementMesh = MeshHandler.LoadMesh(segment.m_segmentMesh.name);
                                if (replacementMesh == null)
                                {
                                    // No replacement mesh found - skip this segment, as we can't do anything more.
                                    Logging.Message("skipping unreadable segment mesh for network ", network.name);
                                    continue;
                                }

                                // Replace unreadable segment mesh with replacment mesh.
                                Logging.Message("substituting unreadable segment mesh for network ", network.name);
                                segment.m_segmentMesh = replacementMesh;
                            }

                            // Skip any meshes that we've already checked.
                            Mesh segmentMesh = segment.m_segmentMesh;
                            if (!checkedMeshes.Contains(segmentMesh))
                            {
                                // Not checked yet - add to list.
                                checkedMeshes.Add(segmentMesh);

                                // Check to see if this segment is a viable target.
                                Vector3[] vertices = segmentMesh.vertices;
                                if (IsEligibleMesh(vertices, 9, isBridge, out bool eligibleCurbs, out bool eligibleBridgeMesh, out bool eligbleSubMesh))
                                {
                                    // Eligibile target; record original value.
                                    netAltered = true;
                                    netRecord.m_segmentDict.Add(segment, new NetComponentRecord
                                    {
                                        NetInfo = network,
                                        EligibleCurbs = eligibleCurbs,
                                        EligibleBridge = eligibleBridgeMesh,
                                        MainVerts = vertices,
                                        LodVerts = segment.m_lodMesh.vertices,
                                    });

                                    // If this segment is an eligible bridge segment, mark it as such.
                                    eligibleBridgeInfo |= eligibleBridgeMesh;

                                    // Adjust vertices.
                                    AdjustMesh(segment.m_segmentMesh, eligibleBridgeMesh);
                                    if (DoLODs)
                                    {
                                        AdjustMesh(segment.m_lodMesh, eligibleBridgeMesh);
                                    }
                                }
                            }
                        }

                        // Check lanes.
                        bool hasTramLanes = false;
                        if (network.m_lanes != null)
                        {
                            // Iterate through each lane in network, replacing ~30cm depths with our new curb height.
                            foreach (NetInfo.Lane lane in network.m_lanes)
                            {
                                if (lane.m_verticalOffset < MinCurbDepthTrigger && lane.m_verticalOffset > MaxCurbDepthTrigger)
                                {
                                    // Record original value.
                                    netAltered = true;
                                    netRecord.m_laneDict.Add(lane, lane.m_verticalOffset);

                                    // Apply new curb height.
                                    lane.m_verticalOffset *= s_newCurbMultiplier;
                                }

                                // Record if any tram lines in this network.
                                hasTramLanes |= (lane.m_vehicleType & VehicleInfo.VehicleType.Tram) != 0;
                            }

                            // Record tram lanes
                            netRecord.m_hasWires = hasTramLanes;
                        }

                        // Update nodes.
                        foreach (NetInfo.Node node in network.m_nodes)
                        {
                            // Skip nodes with no mesh or material.
                            if (node?.m_nodeMesh?.name == null || node.m_nodeMaterial?.shader?.name == null)
                            {
                                continue;
                            }

                            // Only interested in nodes using road shaders.
                            string shaderName = node.m_nodeMaterial.shader.name;
                            if (shaderName != "Custom/Net/Road" && shaderName != "Custom/Net/RoadBridge" && shaderName != "Custom/Net/TrainBridge")
                            {
                                continue;
                            }

                            // Is mesh readable?
                            if (!node.m_nodeMesh.isReadable)
                            {
                                // Unreadable mesh - see if we've got a replacement serialized mesh.
                                Mesh replacementMesh = MeshHandler.LoadMesh(node.m_nodeMesh.name);
                                if (replacementMesh == null)
                                {
                                    // No replacement mesh found - skip this segment, as we can't do anything more.
                                    Logging.Message("skipping unreadable node mesh for network ", network.name);
                                    continue;
                                }

                                Logging.Message("substituting unreadable node mesh for network ", network.name);
                                node.m_nodeMesh = replacementMesh;
                            }

                            // Skip any meshes that we've already checked.
                            Mesh nodeMesh = node.m_nodeMesh;
                            if (!checkedMeshes.Contains(nodeMesh))
                            {
                                // Not checked yet - add to list.
                                checkedMeshes.Add(nodeMesh);

                                // Check to see if this node is a viable target.
                                Vector3[] vertices = nodeMesh.vertices;
                                if (IsEligibleMesh(vertices, 5, isBridge, out bool eligibleCurbs, out bool eligibleBridgeMesh, out bool eligibleSub))
                                {
                                    // Eligibile target; record original value.
                                    netAltered = true;
                                    netRecord.m_nodeDict.Add(node, new NetComponentRecord
                                    {
                                        NetInfo = network,
                                        EligibleCurbs = eligibleCurbs,
                                        EligibleBridge = eligibleBridgeMesh,
                                        MainVerts = vertices,
                                        LodVerts = node.m_lodMesh.vertices,
                                    });

                                    // Adjust vertices.
                                    AdjustMesh(node.m_nodeMesh, eligibleBridgeMesh);
                                    if (DoLODs)
                                    {
                                        AdjustMesh(node.m_lodMesh, eligibleBridgeMesh);
                                    }
                                }
                            }
                        }

                        // If the net was altered, record the created netRecord.
                        if (netAltered)
                        {
                            // Handle pillar heights for bridges.
                            if (isBridge && eligibleBridgeInfo)
                            {
                                // Record network as having adjustable pillars.
                                netRecord.m_adjustPillars = true;

                                // Apply adjustment if set.
                                if (EnableBridges)
                                {
                                    bridgeAI.m_bridgePillarOffset = BridgeAdjustment(netRecord.m_bridgePillarOffset);
                                    bridgeAI.m_middlePillarOffset = BridgeAdjustment(netRecord.m_middlePillarOffset);
                                }
                            }

                            // Handle tram wires.
                            if (hasTramLanes && DoTramCatenaries)
                            {
                                AdjustWires(network);
                            }

                            // Add network to list.
                            _netRecords.Add(network, netRecord);
                        }
                    }
                }
                catch (Exception e)
                {
                    // Don't let one exception stop everything - skip this network and carry on.
                    Logging.LogException(e, "exception reading network");
                    continue;
                }
            }

            // Clear processed mesh list once done.
            _processedMeshes.Clear();

            Logging.KeyMessage("finished load processing");
        }

        /// <summary>
        /// Gets or sets the new curb height to apply (positive figure, in cm).
        /// </summary>
        internal static float NewCurbHeight
        {
            get => -s_newCurbHeight;

            set
            {
                // Update multiplier with change in value.
                s_newCurbHeight = -Mathf.Clamp(value, MinCurbHeight, MaxCurbHeight);
                s_newCurbMultiplier = s_newCurbHeight / OriginalCurbHeight;
            }
        }

        /// <summary>
        /// Gets or sets the bridge height threshold to apply (positive figure, in cm).
        /// </summary>
        internal static float BridgeHeightThreshold
        {
            get => -s_bridgeHeightThreshold;

            set
            {
                s_bridgeHeightThreshold = -Mathf.Clamp(value, MinBridgeThreshold, MaxBridgeThreshold);
            }
        }

        /// <summary>
        /// Gets or sets the bridge deck thickness multiplier.
        /// </summary>
        internal static float BridgeHeightScale
        {
            get => s_bridgeHeightScale;

            set
            {
                s_bridgeHeightScale = Mathf.Clamp(value, MinBridgeScale, MaxBridgeScale);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether bridge deck manipulation is active.
        /// </summary>
        internal static bool EnableBridges { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether lods are also adjusted.
        /// </summary>
        internal static bool DoLODs { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether tram catenary wires are also adjusted.
        /// </summary>
        internal static bool DoTramCatenaries { get; set; } = true;

        /// <summary>
        /// Gets the dictionary of altered networks.
        /// </summary>
        internal Dictionary<NetInfo, NetRecord> NetRecords => _netRecords;

        /// <summary>
        /// Reverts changes (back to original).
        /// </summary>
        internal void Revert()
        {
            // Iterate through all network records in dictionary.
            foreach (KeyValuePair<NetInfo, NetRecord> netEntry in _netRecords)
            {
                Logging.Message("reverting ", netEntry.Key.name);

                // Local references.
                NetInfo netInfo = netEntry.Key;
                NetRecord netRecord = netEntry.Value;

                // Restore net surface level.
                netInfo.m_surfaceLevel = netRecord.m_surfaceLevel;

                // Restore segment vertices.
                foreach (KeyValuePair<NetInfo.Segment, NetComponentRecord> segmentEntry in netRecord.m_segmentDict)
                {
                    segmentEntry.Key.m_segmentMesh.vertices = segmentEntry.Value.MainVerts;
                    segmentEntry.Key.m_lodMesh.vertices = segmentEntry.Value.LodVerts;
                }

                // Restore node vertices.
                foreach (KeyValuePair<NetInfo.Node, NetComponentRecord> nodeEntry in netRecord.m_nodeDict)
                {
                    nodeEntry.Key.m_nodeMesh.vertices = nodeEntry.Value.MainVerts;
                    nodeEntry.Key.m_lodMesh.vertices = nodeEntry.Value.LodVerts;
                }

                // Restore lanes.
                foreach (KeyValuePair<NetInfo.Lane, float> laneEntry in netRecord.m_laneDict)
                {
                    laneEntry.Key.m_verticalOffset = laneEntry.Value;
                }

                // Restore bridge pillar offsets.
                if (netRecord.m_adjustPillars && netInfo.m_netAI is RoadBridgeAI bridgeAI)
                {
                    bridgeAI.m_bridgePillarOffset = netRecord.m_bridgePillarOffset;
                    bridgeAI.m_middlePillarOffset = netRecord.m_middlePillarOffset;
                }

                // Reset any recorded pillar offset.
                netRecord.m_bridgePillarOffset = 0;

                // Restore tram catenary wire original values.
                foreach (KeyValuePair<Mesh, Vector3[]> catenary in _catenaryMeshes)
                {
                    catenary.Key.vertices = catenary.Value;
                }

                // Clear adjusted cantenary values.
                _catenaryMeshes.Clear();
            }

            // Revert custom networks.
            _customRoadHandler.Revert();

            // Revert parking records.
            ParkingLots.Revert();

            // Adjust existing pillars.
            Pillars.AdjustPillars();

            // Recalulate lanes on map with new height.
            RecalculateLanes();
        }

        /// <summary>
        /// Applies updated settings.
        /// </summary>
        internal void Apply()
        {
            // Ensure processed mesh list is clear, just in case.
            _processedMeshes.Clear();

            // Iterate through all network records in dictionary.
            foreach (KeyValuePair<NetInfo, NetRecord> netEntry in _netRecords)
            {
                // Local references.
                NetInfo netInfo = netEntry.Key;
                NetRecord netRecord = netEntry.Value;

                // Change net surface level.
                netInfo.m_surfaceLevel = s_newCurbHeight;

                // Update segment vertices.
                foreach (KeyValuePair<NetInfo.Segment, NetComponentRecord> segmentEntry in netRecord.m_segmentDict)
                {
                    // Restore original vertices and then raise mesh.
                    NetInfo.Segment segment = segmentEntry.Key;
                    segment.m_segmentMesh.vertices = segmentEntry.Value.MainVerts;
                    segment.m_lodMesh.vertices = segmentEntry.Value.LodVerts;
                    AdjustMesh(segment.m_segmentMesh, segmentEntry.Value.EligibleBridge);
                    if (DoLODs)
                    {
                        AdjustMesh(segment.m_lodMesh, segmentEntry.Value.EligibleBridge);
                    }
                }

                // Update node vertices.
                foreach (KeyValuePair<NetInfo.Node, NetComponentRecord> nodeEntry in netRecord.m_nodeDict)
                {
                    // Restore original vertices and then raise mesh.
                    NetInfo.Node node = nodeEntry.Key;
                    node.m_nodeMesh.vertices = nodeEntry.Value.MainVerts;
                    node.m_lodMesh.vertices = nodeEntry.Value.LodVerts;
                    AdjustMesh(node.m_nodeMesh, nodeEntry.Value.EligibleBridge);

                    // Update LODs if set to do so.
                    if (DoLODs)
                    {
                        AdjustMesh(node.m_lodMesh, nodeEntry.Value.EligibleBridge);
                    }
                }

                // Adjust pillar heights to match net adjustment.
                if (netRecord.m_adjustPillars && netInfo.m_netAI is RoadBridgeAI bridgeAI && EnableBridges)
                {
                    bridgeAI.m_bridgePillarOffset = BridgeAdjustment(netRecord.m_bridgePillarOffset);
                    bridgeAI.m_middlePillarOffset = BridgeAdjustment(netRecord.m_middlePillarOffset);
                }

                // Change lanes.
                foreach (KeyValuePair<NetInfo.Lane, float> laneEntry in netRecord.m_laneDict)
                {
                    laneEntry.Key.m_verticalOffset = s_newCurbHeight;
                }
            }

            // Apply changes to custom roads.
            _customRoadHandler.Apply();

            // Apply changes to buildings.
            ParkingLots.Apply();

            // Adjust existing pillars.
            Pillars.AdjustPillars();

            // Recalulate lanes on map with new height.
            RecalculateLanes();

            // Clear processed mesh list once done.
            _processedMeshes.Clear();

            // Adjust catenary wires.
            if (DoTramCatenaries)
            {
                foreach (KeyValuePair<NetInfo, NetRecord> network in _netRecords)
                {
                    if (network.Value.m_hasWires)
                    {
                        AdjustWires(network.Key);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the adjusted height of the given bridge vertex according to current settings.
        /// </summary>
        /// <param name="originalHeight">Original vertex height.</param>
        /// <returns>Adjusted vertex height.</returns>
        internal float BridgeAdjustment(float originalHeight)
        {
            // Check for threshold trigger.
            if (originalHeight < s_bridgeHeightThreshold)
            {
                // Threshold met; calculate new height.
                return ((originalHeight - s_bridgeHeightThreshold) * s_bridgeHeightScale) + s_bridgeHeightThreshold;
            }

            // If we got here, the vertex didn't meet the threshold; return original value.
            return originalHeight;
        }

        /// <summary>
        /// Adjusts the given mesh in line with current settings (curb heights and bridge deck depths).
        /// Includes filters to exclude meshes with fewer than four vertices, or full-height bridges.
        /// </summary>
        /// <param name="mesh">Mesh to modify.</param>
        /// <param name="isBridge">True if this is an eligible bridge mesh, false otherwise.</param>
        private void AdjustMesh(Mesh mesh, bool isBridge)
        {
            // Check if we've already done this one.
            if (_processedMeshes.Contains(mesh))
            {
                // Already processed this mesh - do nothing.
                return;
            }

            // Disable bridge manipulation if setting isn't set.
            bool bridge = isBridge & EnableBridges;

            // Adjusted vertex counters.
            int curbChangedVertices = 0, bridgeChangedVertices = 0;

            // Create new vertices array (changing individual elements within the existing array won't work with locked meshes).
            Vector3[] newVertices = new Vector3[mesh.vertices.Length];
            mesh.vertices.CopyTo(newVertices, 0);

            // Raise verticies; anything below ground level (but above the maximum depth trigger - allow for bridges etc.) has its y-value multiplied for proportional adjustment.
            for (int i = 0; i < newVertices.Length; ++i)
            {
                float thisY = newVertices[i].y;

                // Adjust any eligible curb vertices.
                if (thisY < 0.0f)
                {
                    if (thisY > MaxCurbDepthTrigger)
                    {
                        // Standard vertices, road surface and above - scale.
                        newVertices[i].y = thisY * s_newCurbMultiplier;
                        ++curbChangedVertices;
                    }
                    else if (thisY > MaxSubDepthTrigger)
                    {
                        // Sub-verticies below road surface, e.g. LRT tracks.
                        // Shift up by total adjustment difference, not scaled.
                        newVertices[i].y = thisY - (OriginalCurbHeight - s_newCurbHeight);
                        ++curbChangedVertices;
                    }

                    // Adjust any eligible bride vertices.
                    else if (bridge && thisY < s_bridgeHeightThreshold && thisY >= BridgeDepthCutoff)
                    {
                        float newHeight = BridgeAdjustment(thisY);
                        newVertices[i].y = newHeight;
                        ++bridgeChangedVertices;
                    }
                }
            }

            // If we changed at least four vertices, assign new vertices to mesh.
            // Don't change the mesh if we didn't get at least one quad, to avoid minor rendering glitches with flat LODs.
            bool bridgeChanged = bridge && bridgeChangedVertices > 3;
            if (curbChangedVertices > 3 || bridgeChanged)
            {
                mesh.vertices = newVertices;

                // Record mesh as being altered.
                _processedMeshes.Add(mesh);
            }
        }

        /// <summary>
        /// Checks vertices for eligibility for adjustment.
        /// </summary>
        /// <param name="vertices">Vertices to check.</param>
        /// <param name="minVertices">Minimum number of eligible vertices to be valid.</param>
        /// <param name="isBridge">If this mesh from a valid bridge prefab.</param>
        /// <param name="eligibleCurbs">Set to true if this mesh is eligible for curb height adjustment, false otherwise.</param>
        /// <param name="eligibleBridge">Set to true if this mesh is eligbile for bridge deck adjustment, false otherwise.</param>
        /// <param name="eligbleSub">Set to true if this mesh has eligible vertices below road surface height (e.g. LRT tracks), false otherwise.</param>
        /// <returns>True if the mesh is eligible for adjustment (brigde or curb), false otherwise.</returns>
        private bool IsEligibleMesh(Vector3[] vertices, int minVertices, bool isBridge, out bool eligibleCurbs, out bool eligibleBridge, out bool eligbleSub)
        {
            // Status flags.
            int curbVertices = 0, bridgeVertices = 0, subVertices = 0;
            bool fullDepthMesh = false;

            // Iterate through each vertex in segment mesh, counting how many meet our trigger height ranges.
            for (int i = 0; i < vertices.Length; ++i)
            {
                float thisY = vertices[i].y;
                if (thisY < MinCurbDepthTrigger && thisY > MaxCurbDepthTrigger)
                {
                    // Eligible curb vertex.
                    ++curbVertices;
                }
                else if (thisY < MinCurbDepthTrigger && thisY > MaxSubDepthTrigger)
                {
                    // Eligible vertex below road surface height, e.g. LRT tracks.
                    ++subVertices;
                }
                else if (thisY < MinBridgeCutoff && thisY >= BridgeDepthCutoff)
                {
                    // Eligible bridge vertex.
                    ++bridgeVertices;
                }
                else if (thisY < BridgeDepthCutoff)
                {
                    // Assume full-depth bridge mesh.
                    fullDepthMesh = true;
                }
            }

            // Set return bools.
            eligibleCurbs = curbVertices >= minVertices;
            eligbleSub = subVertices >= minVertices;

            // Bridge vertex count is only valid if this isn't a full-height bridge.
            eligibleBridge = isBridge && (!fullDepthMesh && bridgeVertices >= minVertices);

            return eligibleCurbs | eligibleBridge | eligbleSub;
        }

        /// <summary>
        /// Recalculates network segment lanes after a height change (via simulation thread action).
        /// </summary>
        private void RecalculateLanes() => Singleton<SimulationManager>.instance.AddAction(RecalculateLaneAction);

        /// <summary>
        /// Recalculates network segment lanes after a height change.
        /// Should only be called via simulation thread action.
        /// </summary>
        private void RecalculateLaneAction()
        {
            // Local references.
            NetManager netManager = Singleton<NetManager>.instance;
            NetSegment[] segments = netManager.m_segments.m_buffer;

            // Add action via simulation thread.
            Singleton<SimulationManager>.instance.AddAction(() =>
            {
                // Iterate through all segments on map.
                for (ushort i = 0; i < segments.Length; ++i)
                {
                    // Skip empty segments or segments with null infos.
                    if ((segments[i].m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None)
                    {
                        continue;
                    }

                    // Only look at nets that we've altered.
                    NetInfo netInfo = segments[i].Info;
                    if (netInfo != null && _netRecords.ContainsKey(netInfo))
                    {
                        // Update lanes in this segment.
                        segments[i].Info.m_netAI.UpdateLanes(i, ref segments[i], loading: false);
                    }
                }
            });
        }

        /// <summary>
        /// Adjusts catenary wires for the given network.
        /// </summary>
        /// <param name="network">Network prefab.</param>
        private void AdjustWires(NetInfo network)
        {
            // Iterate though each segment.
            foreach (NetInfo.Segment segment in network.m_segments)
            {
                AdjustCatenaryMesh(segment?.m_segmentMaterial, segment?.m_segmentMesh);
            }

            // Iterate though each node.
            foreach (NetInfo.Node node in network.m_nodes)
            {
               AdjustCatenaryMesh(node?.m_nodeMaterial, node?.m_nodeMesh);
            }
        }

        /// <summary>
        /// Adjusts a catenary waire mesh.
        /// </summary>
        /// <param name="material">Wire candidate material.</param>
        /// <param name="mesh">Wire mesh.</param>
        private void AdjustCatenaryMesh(Material material, Mesh mesh)
        {
            // Null checks.
            if (material?.name == null || mesh == null)
            {
                return;
            }

            // Skip already-processed meshes, and only interested in materials using electricity shaders.
            if (!_catenaryMeshes.ContainsKey(mesh) && material.shader.name.Equals("Custom/Net/Electricity"))
            {
                // New vertex array.
                int vertexCount = mesh.vertices.Length;
                Vector3[] newVertices = new Vector3[vertexCount];

                // Calculate adjustment.
                float adjustment = s_newCurbHeight - OriginalCurbHeight;

                // Iterate through each vertex in mesh and adjust upwards in our new mesh.
                for (int i = 0; i < vertexCount; ++i)
                {
                    // Increment vertex y position in our new array.
                    Vector3 newVector = mesh.vertices[i];
                    newVector.y += adjustment;
                    newVertices[i] = newVector;
                }

                // Record original vertices.
                _catenaryMeshes.Add(mesh, mesh.vertices);

                // Apply updated vertices.
                mesh.vertices = newVertices;

                Logging.KeyMessage("adjusted catenary mesh ", mesh.name);

                return;
            }
        }
    }
}