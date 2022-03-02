using System;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework;


namespace CurbHeightAdjuster
{
    /// <summary>
    /// Class to manage changes to networks.
    /// </summary>
    public static class NetHandler
    {
        /// Original script by Ronyx69 to adjust curb heights from -30cm to -15cm, adapted to mod form by krzychu124.
        /// Redesigned, rewritten, optimised, and extended by algernon.
        /// Bridge deck and parking lot manipulations added by algernon.
        /// Thanks to Ronyx69 (especially, for the original concept and implementation) and krzychu124 for their prior work!


        // List of excluded networks (Steam IDs).
        private readonly static HashSet<string> excludedNets = new HashSet<string>
        {
            // Prague curb roads.
            "2643462494",
            "2643463468",
            "2643464569",
            "2643465478",
            "2643466243",
            "2643467084",
            "2643468733",
            "2643467962",
            "2643469133",
            "2643470190",
            "2643469678",
            "2643470754",
            "2643471147"
        };


        // Original curb heights.
        internal const float OriginalCurbHeight = -0.30f;

        // Default mod settings.
        internal const float DefaultNewCurbHeight = -0.15f;
        internal const float DefaultBridgeThreshold = -0.5f;
        internal const float DefaultBridgeMultiplier = 0.25f;

        // Depth trigger - segments/nets need to have depths within these bounds to be adjusted.
        // Vanilla tram rails have tops at -0.225.
        // LRT tram rails have bases at -0.5.
        private const float MinCurbDepthTrigger = -0.21f;
        private const float MaxCurbDepthTrigger = -0.32f;

        // Maximum bounds.
        internal const float MinCurbHeight = 0.07f;
        internal const float MaxCurbHeight = 0.29f;
        internal const float MinBridgeThreshold = 0.35f;
        internal const float MaxBridgeThreshold = 2.0f;
        internal const float MinBridgeScale = 0.1f;
        internal const float MaxBridgeScale = 1f;
        internal const float MinBridgeCutoff = -3.1f;
        internal const float BridgeDepthCutoff = -5f;

        // Curb height multiiplier.
        private static float newCurbMultiplier = DefaultNewCurbHeight / OriginalCurbHeight;

        // Dictionary of altered nets.
        internal readonly static Dictionary<NetInfo, NetRecord> netRecords = new Dictionary<NetInfo, NetRecord>();

        // Hashset of currently processed network meshes, with calculated adjustment offsets.
        private readonly static HashSet<Mesh> processedMeshes = new HashSet<Mesh>();

        // Hashset of processed bridge meshes with previously calculated pillar offsets.
        private readonly static Dictionary<Mesh, float> processedOffsets = new Dictionary<Mesh, float>();


        /// <summary>
        /// New curb height to apply (positive figure, in cm).
        /// </summary>
        internal static float NewCurbHeight
        {
            get => -newCurbHeight;

            set
            {
                // Update multiplier with change in value.
                newCurbHeight = -Mathf.Clamp(value, MinCurbHeight, MaxCurbHeight);
                newCurbMultiplier = newCurbHeight / OriginalCurbHeight;
            }
        }
        private static float newCurbHeight = DefaultNewCurbHeight;


        /// <summary>
        /// Bridge height threshold to apply (positive figure, in cm).
        /// </summary>
        internal static float BridgeHeightThreshold
        {
            get => -bridgeHeightThreshold;

            set
            {
                bridgeHeightThreshold = -Mathf.Clamp(value, MinBridgeThreshold, MaxBridgeThreshold);
            }
        }
        private static float bridgeHeightThreshold = DefaultBridgeThreshold;


        /// <summary>
        /// Bridge height multiplier.
        /// </summary>
        internal static float BridgeHeightScale
        {
            get => bridgeHeightScale;

            set
            {
                bridgeHeightScale = Mathf.Clamp(value, MinBridgeScale, MaxBridgeScale);
            }
        }
        private static float bridgeHeightScale = DefaultBridgeMultiplier;


        /// <summary>
        /// Determines if lods are also adjusted.
        /// </summary>
        internal static bool DoLODs { get; set; } = false;

        /// <summary>
        /// Enables bridge deck manipulation.
        /// </summary>
        internal static bool EnableBridges { get; set; } = true;


        /// <summary>
        /// Called on load to scan through all loaded NetInfos, build the database, and apply network manipulations (meshes and lanes).
        /// </summary>
        public static void OnLoad()
        {
            // List of meshes that we've already checked.
            HashSet<Mesh> checkedMeshes = new HashSet<Mesh>();

            Logging.KeyMessage("starting load processing");

            // Iterate through all networks in list.
            for (uint i = 0; i < PrefabCollection<NetInfo>.LoadedCount(); ++i)
            {
                NetInfo network = PrefabCollection<NetInfo>.GetLoaded(i);

                try
                {
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
                        // Skip excluded networks.
                        int periodIndex = network.name.IndexOf(".");
                        if (periodIndex > 0)
                        {
                            string steamID = network.name.Substring(0, periodIndex);
                            if (excludedNets.Contains(steamID))
                            {
                                continue;
                            }
                        }

                        // Dirty flag.
                        bool netAltered = false;

                        // Network record for this prefab.
                        NetRecord netRecord = new NetRecord();

                        // Record original pillar height.
                        if (isBridge)
                        {
                            netRecord.bridgePillarOffset = bridgeAI.m_bridgePillarOffset;
                            netRecord.middlePillarOffset = bridgeAI.m_middlePillarOffset;
                        }

                        // Raise network surface level.
                        if (network.m_surfaceLevel == -0.3f)
                        {
                            // Record original value.
                            netAltered = true;
                            netRecord.surfaceLevel = network.m_surfaceLevel;

                            // Set new value.
                            network.m_surfaceLevel = newCurbHeight;
                        }

                        // Bridge adjustment status.
                        bool eligibleBridgeInfo = false;
                        float maxAdjustment = 0f;

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
                                if (IsEligibleMesh(vertices, 9, isBridge, out bool eligibleCurbs, out bool eligibleBridgeMesh))
                                {
                                    // Eligibile target; record original value.
                                    netAltered = true;
                                    netRecord.segmentDict.Add(segment, new NetComponentRecord
                                    {
                                        netInfo = network,
                                        eligibleCurbs = eligibleCurbs,
                                        eligibleBridge = eligibleBridgeMesh,
                                        mainVerts = vertices,
                                        lodVerts = segment.m_lodMesh.vertices
                                    });

                                    // If this segment is an eligible bridge segment, mark the 
                                    eligibleBridgeInfo |= eligibleBridgeMesh;

                                    // Adjust vertices.
                                    float adjustment = AdjustMesh(segment.m_segmentMesh, eligibleBridgeMesh);
                                    if (DoLODs)
                                    {
                                        AdjustMesh(segment.m_lodMesh, eligibleBridgeMesh);
                                    }

                                    // Update maximum bridge adjustment, if applicable.
                                    if (eligibleBridgeMesh && adjustment < maxAdjustment)
                                    {
                                        maxAdjustment = adjustment;
                                    }
                                }
                            }
                        }

                        // Check lanes, if we've passed checks.
                        if (network.m_lanes != null)
                        {
                            // Iterate through each lane in network, replacing ~30cm depths with our new curb height.
                            foreach (NetInfo.Lane lane in network.m_lanes)
                            {
                                if (lane.m_verticalOffset < MinCurbDepthTrigger && lane.m_verticalOffset > MaxCurbDepthTrigger)
                                {
                                    // Record original value.
                                    netAltered = true;
                                    netRecord.laneDict.Add(lane, lane.m_verticalOffset);

                                    // Apply new curb height.
                                    lane.m_verticalOffset *= newCurbMultiplier;
                                }
                            }
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
                                if (IsEligibleMesh(vertices, 5, isBridge, out bool eligibleCurbs, out bool eligibleBridgeMesh))
                                {
                                    // Eligibile target; record original value.
                                    netAltered = true;
                                    netRecord.nodeDict.Add(node, new NetComponentRecord
                                    {
                                        netInfo = network,
                                        eligibleCurbs = eligibleCurbs,
                                        eligibleBridge = eligibleBridgeMesh,
                                        mainVerts = vertices,
                                        lodVerts = node.m_lodMesh.vertices
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
                                netRecord.adjustPillars = true;

                                // Apply adjustment if set.
                                if (EnableBridges)
                                {
                                    bridgeAI.m_bridgePillarOffset -= maxAdjustment;
                                    bridgeAI.m_middlePillarOffset -= maxAdjustment;
                                    Logging.Message("adjusted pillars by ", maxAdjustment, " for net ", network.name);
                                }
                            }

                            netRecords.Add(network, netRecord);
                        }
                    }
                }
                catch (Exception e)
                {
                    // Don't let one exception stop everything - skip this network and carry on.
                    Logging.LogException(e, "exception reading network ", network?.name ?? "null");
                    continue;
                }
            }

            // Adjust existing pillars.
            Pillars.AdjustPillars();

            // Clear processed mesh lists once done.
            processedMeshes.Clear();
            processedOffsets.Clear();

            Logging.KeyMessage("finished load processing");
        }


        /// <summary>
        /// Reverts changes (back to original).
        /// </summary>
        internal static void Revert()
        {
            Logging.KeyMessage("reverting changes");

            // Iterate through all network records in dictionary.
            foreach (KeyValuePair<NetInfo, NetRecord> netEntry in netRecords)
            {
                Logging.Message("reverting ", netEntry.Key.name);

                // Local references.
                NetInfo netInfo = netEntry.Key;
                NetRecord netRecord = netEntry.Value;

                // Restore net surface level.
                netInfo.m_surfaceLevel = netRecord.surfaceLevel;

                // Restore segment vertices.
                foreach (KeyValuePair<NetInfo.Segment, NetComponentRecord> segmentEntry in netRecord.segmentDict)
                {
                    segmentEntry.Key.m_segmentMesh.vertices = segmentEntry.Value.mainVerts;
                    segmentEntry.Key.m_lodMesh.vertices = segmentEntry.Value.lodVerts;
                }

                // Restore node vertices.
                foreach (KeyValuePair<NetInfo.Node, NetComponentRecord> nodeEntry in netRecord.nodeDict)
                {
                    nodeEntry.Key.m_nodeMesh.vertices = nodeEntry.Value.mainVerts;
                    nodeEntry.Key.m_lodMesh.vertices = nodeEntry.Value.lodVerts;
                }

                // Restore lanes.
                foreach (KeyValuePair<NetInfo.Lane, float> laneEntry in netRecord.laneDict)
                {
                    laneEntry.Key.m_verticalOffset = laneEntry.Value;
                }

                // Restore bridge pillar offsets.
                if (netRecord.adjustPillars && netInfo.m_netAI is RoadBridgeAI bridgeAI)
                {
                    bridgeAI.m_bridgePillarOffset = netRecord.bridgePillarOffset;
                    bridgeAI.m_middlePillarOffset = netRecord.middlePillarOffset;
                }

                // Reset any recorded pillar offset.
                netRecord.bridgePillarOffset = 0;
            }

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
        internal static void Apply()
        {
            // Ensure processed mesh lists are clear, just in case.
            processedMeshes.Clear();
            processedOffsets.Clear();

            // Iterate through all network records in dictionary.
            foreach (KeyValuePair<NetInfo, NetRecord> netEntry in netRecords)
            {
                // Local references.
                NetInfo netInfo = netEntry.Key;
                NetRecord netRecord = netEntry.Value;

                // Change net surface level.
                netInfo.m_surfaceLevel = newCurbHeight;

                // Update segment vertices.
                foreach (KeyValuePair<NetInfo.Segment, NetComponentRecord> segmentEntry in netRecord.segmentDict)
                {
                    // Restore original vertices and then raise mesh.
                    NetInfo.Segment segment = segmentEntry.Key;
                    segment.m_segmentMesh.vertices = segmentEntry.Value.mainVerts;
                    segment.m_lodMesh.vertices = segmentEntry.Value.lodVerts;
                    AdjustMesh(segment.m_segmentMesh, segmentEntry.Value.eligibleBridge);
                    if (DoLODs)
                    {
                        AdjustMesh(segment.m_lodMesh, segmentEntry.Value.eligibleBridge);
                    }
                }

                // Update node vertices.
                float maxAdjustment = 0f;
                foreach (KeyValuePair<NetInfo.Node, NetComponentRecord> nodeEntry in netRecord.nodeDict)
                {
                    // Restore original vertices and then raise mesh.
                    NetInfo.Node node = nodeEntry.Key;
                    node.m_nodeMesh.vertices = nodeEntry.Value.mainVerts;
                    node.m_lodMesh.vertices = nodeEntry.Value.lodVerts;
                    float adjustment = AdjustMesh(node.m_nodeMesh, nodeEntry.Value.eligibleBridge);

                    // Update maximum adjustment value (for bridge pillar adjustment later).
                    if (nodeEntry.Value.eligibleBridge && adjustment > maxAdjustment)
                    {
                        maxAdjustment = adjustment;
                    }

                    // Update LODs if set to do so.
                    if (DoLODs)
                    {
                        AdjustMesh(node.m_lodMesh, nodeEntry.Value.eligibleBridge);
                    }
                }

                // Adjust pillar heights to match net adjustment.
                if (netRecord.adjustPillars && netInfo.m_netAI is RoadBridgeAI bridgeAI && EnableBridges)
                {
                    bridgeAI.m_bridgePillarOffset = netRecord.bridgePillarOffset - maxAdjustment;
                    bridgeAI.m_middlePillarOffset = netRecord.middlePillarOffset - maxAdjustment;
                }

                // Change lanes.
                foreach (KeyValuePair<NetInfo.Lane, float> laneEntry in netRecord.laneDict)
                {
                    laneEntry.Key.m_verticalOffset = newCurbHeight;
                }
            }

            // Apply changes to buildings.
            ParkingLots.Apply();

            // Adjust existing pillars.
            Pillars.AdjustPillars();

            // Recalulate lanes on map with new height.
            RecalculateLanes();

            // Clear processed mesh lists once done.
            processedMeshes.Clear();
            processedOffsets.Clear();
        }


        /// <summary>
        /// Adjusts the given mesh in line with current settings (curb heights and bridge deck depths).
        /// Includes filters to exclude meshes with fewer than four vertices, or full-height bridges.
        /// </summary>
        /// <param name="mesh">Mesh to modify</param>
        /// <param name="isBridge">True if this is an eligible bridge mesh, false otherwise</param>
        /// <returns>Maximum vertical bridge adjustment applied (0 if none)</returns>
        private static float AdjustMesh(Mesh mesh, bool isBridge)
        {
            float maxAdjusted = 0f;

            // Check if we've already done this one.
            if (processedMeshes.Contains(mesh))
            {
                // Already processed this mesh - try to get any stored bridge adjustment figure.
                if (processedOffsets.TryGetValue(mesh, out float adjustment))
                {
                    return adjustment;
                }

                // No stored bridge adjustment figure; return 0.
                return 0;
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
                if (thisY < 0.0f && thisY > MaxCurbDepthTrigger)
                {
                    newVertices[i].y = thisY * newCurbMultiplier;
                    ++curbChangedVertices;
                }
                // Adjust any eligible bride vertices.
                else if (bridge && thisY < bridgeHeightThreshold && thisY >= BridgeDepthCutoff)
                {
                    float newHeight = ((thisY - bridgeHeightThreshold) * bridgeHeightScale) + bridgeHeightThreshold;
                    newVertices[i].y = newHeight;
                    ++bridgeChangedVertices;

                    // Update maximum adjusted value if needed.
                    float adjustment = thisY - newHeight;
                    if (adjustment < maxAdjusted)
                    {
                        maxAdjusted = adjustment;
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
                processedMeshes.Add(mesh);

                // If this was an adjusted bridge with a calculated adjustment, add it to our dictionary.
                if (bridgeChanged && maxAdjusted != 0f)
                {
                    processedOffsets.Add(mesh, maxAdjusted);
                }
            }

            return maxAdjusted;
        }


        /// <summary>
        /// Checks vertices for eligibility for adjustment.
        /// </summary>
        /// <param name="vertices">Vertices to check</param>
        /// <param name="minVertices">Minimum number of eligible vertices to be valid</param>
        /// <param name="isBridge">If this mesh from a valid bridge prefab</param>
        /// <param name="eligibleCurbs">Set to true if this mesh is eligible for curb height adjustment, false otherwise</param>
        /// <param name="eligibleBridge">Set to true if this mesh is eligbile for bridge deck adjustment, false otherwise</param>
        /// <returns>True if the mesh is eligible for adjustment (brigde or curb), false otherwise</returns>
        private static bool IsEligibleMesh(Vector3[] vertices, int minVertices, bool isBridge, out bool eligibleCurbs, out bool eligibleBridge)
        {
            // Status flags.
            int curbVertices = 0, bridgeVertices = 0;
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

            eligibleCurbs = curbVertices >= minVertices;

            // Bridge vertex count is only valid if this isn't a full-height bridge.
            eligibleBridge = isBridge && (!fullDepthMesh && bridgeVertices >= minVertices);

            return eligibleCurbs || eligibleBridge;
        }


        /// <summary>
        /// Recalculates network segment lanes after a height change (via simulation thread action).
        /// </summary>
        private static void RecalculateLanes() => Singleton<SimulationManager>.instance.AddAction(RecalculateLaneAction);


        /// <summary>
        /// Recalculates network segment lanes after a height change.
        /// Should only be called via simulation thread action.
        /// </summary>
        private static void RecalculateLaneAction()
        {
            // Local references.
            NetManager netManager = Singleton<NetManager>.instance;
            NetSegment[] segments = netManager.m_segments.m_buffer;

            /// Add action via simulation thread.
            Singleton<SimulationManager>.instance.AddAction(delegate
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
                    if (netInfo != null && netRecords.ContainsKey(netInfo))
                    {
                        // Update lanes in this segment.
                        segments[i].Info.m_netAI.UpdateLanes(i, ref segments[i], loading: false);
                    }
                }
            });
        }
    }
}