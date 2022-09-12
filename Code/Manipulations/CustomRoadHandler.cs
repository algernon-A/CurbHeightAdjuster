// <copyright file="CustomRoadHandler.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace CurbHeightAdjuster
{
    using System.Collections.Generic;
    using AlgernonCommons;
    using ColossalFramework;
    using UnityEngine;

    /// <summary>
    /// Customized road mesh manipulations for special cases.
    /// </summary>
    internal static class CustomRoadHandler
    {
        // Dictionary of custom roads requiring individualised settings.
        private static readonly Dictionary<string, CustomRoadParams> CustomRoads = new Dictionary<string, CustomRoadParams>
        {
            // Paris cobblestone roads.
            { "1729876865", new CustomRoadParams { SurfaceLevel = -0.15f, SurfaceTopBound = -0.06f, SurfaceBottomBound = -0.31f } },

            // Cobblestone Lane (1-tile wide).
            { "1521824617", new CustomRoadParams { SurfaceLevel = -0.14f, SurfaceTopBound = -0.06f, SurfaceBottomBound = -0.31f } },
        };

        // List of 10cm curb roads.
        private static readonly HashSet<string> Curbs10cm = new HashSet<string>
        {
            // BIG suburbs 2 lane.
            "2211907342",

            // BIG suburbs 2 lane worn.
            "2211898750",

            // 0.1m sunken + Prague Curbs.
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
            "2643471147",

            // Tiny Narrow Alley
            "2270053832",
        };

        /*
        private static readonly HashSet<string> Curbs15cm = new HashSet<string>
        {
            // Paris cobblestone roads.
            "1729876865",
        };
        */

        // Dictionary of altered nets.
        private static readonly Dictionary<NetInfo, CustomNetRecord> NetRecords = new Dictionary<NetInfo, CustomNetRecord>();

        // Hashset of currently processed network meshes, with calculated adjustment offsets.
        private static readonly HashSet<Mesh> ProcessedMeshes = new HashSet<Mesh>();

        // List of meshes that we've already checked.
        private static readonly HashSet<Mesh> CheckedMeshes = new HashSet<Mesh>();

        /// <summary>
        /// Checks to see if the given network is a custom network, and if so, performs custom net manipulations.
        /// </summary>
        /// <param name="network">Network prefab.</param>
        /// <returns>True if this was processed as a custom network, false otherwise.</returns>
        internal static bool IsCustomNet(NetInfo network)
        {
            // Try to find steam ID (anything without this isn't custom).
            int periodIndex = network.name.IndexOf(".");
            if (periodIndex > 0)
            {
                // Steam ID found; check for any custom roads.
                string steamID = network.name.Substring(0, periodIndex);
                if (CustomRoads.TryGetValue(steamID, out CustomRoadParams customRoad))
                {
                    Logging.KeyMessage("processing custom road ", network.name, " with Steam ID ", steamID);
                    CustomNetManipulation(network, customRoad);
                    return true;
                }
                else if (Curbs10cm.Contains(steamID))
                {
                    Logging.KeyMessage("processing 10cm curb road ", network.name, " with Steam ID ", steamID);
                    CustomNetManipulation(network, new CustomRoadParams { SurfaceLevel = -0.10f, SurfaceTopBound = -0.06f, SurfaceBottomBound = -0.31f });
                    return true;
                }
            }

            // If we got here, it wasn't a custom network; return false.
            return false;
        }

        /// <summary>
        /// Reverts changes (back to original).
        /// </summary>
        internal static void Revert()
        {
            // Iterate through all network records in dictionary.
            foreach (KeyValuePair<NetInfo, CustomNetRecord> netEntry in NetRecords)
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
            }

            // Recalulate lanes on map with new height.
            RecalculateLanes();
        }

        /// <summary>
        /// Applies updated settings.
        /// </summary>
        internal static void Apply()
        {
            // Ensure processed mesh list is clear, just in case.
            ProcessedMeshes.Clear();

            // Iterate through all network records in dictionary.
            foreach (KeyValuePair<NetInfo, CustomNetRecord> netEntry in NetRecords)
            {
                // Local references.
                NetInfo netInfo = netEntry.Key;
                NetRecord netRecord = netEntry.Value;

                // Change net surface level.
                netInfo.m_surfaceLevel = -RoadHandler.NewCurbHeight;

                // Update segment vertices.
                foreach (KeyValuePair<NetInfo.Segment, NetComponentRecord> segmentEntry in netRecord.m_segmentDict)
                {
                    // Restore original vertices and then raise mesh.
                    NetInfo.Segment segment = segmentEntry.Key;
                    segment.m_segmentMesh.vertices = segmentEntry.Value.MainVerts;
                    segment.m_lodMesh.vertices = segmentEntry.Value.LodVerts;
                    AdjustMesh(segment.m_segmentMesh, netEntry.Value.m_customParams);
                    if (RoadHandler.DoLODs)
                    {
                        AdjustMesh(segment.m_lodMesh, netEntry.Value.m_customParams);
                    }
                }

                // Update node vertices.
                foreach (KeyValuePair<NetInfo.Node, NetComponentRecord> nodeEntry in netRecord.m_nodeDict)
                {
                    // Restore original vertices and then raise mesh.
                    NetInfo.Node node = nodeEntry.Key;
                    node.m_nodeMesh.vertices = nodeEntry.Value.MainVerts;
                    node.m_lodMesh.vertices = nodeEntry.Value.LodVerts;
                    AdjustMesh(node.m_nodeMesh, netEntry.Value.m_customParams);

                    // Update LODs if set to do so.
                    if (RoadHandler.DoLODs)
                    {
                        AdjustMesh(node.m_lodMesh, netEntry.Value.m_customParams);
                    }
                }

                // Change lanes.
                foreach (KeyValuePair<NetInfo.Lane, float> laneEntry in netRecord.m_laneDict)
                {
                    laneEntry.Key.m_verticalOffset = -RoadHandler.NewCurbHeight;
                }
            }

            // Recalulate lanes on map with new height.
            RecalculateLanes();

            // Clear processed mesh list once done.
            ProcessedMeshes.Clear();
        }

        /// <summary>
        /// Perform manipulations on specially defined meshes.
        /// </summary>
        private static void CustomNetManipulation(NetInfo network, CustomRoadParams customParams)
        {
            // Dirty flag.
            bool netAltered = false;

            // Network record for this prefab.
            CustomNetRecord netRecord = new CustomNetRecord();

            // Raise network surface level.
            if (network.m_surfaceLevel == customParams.SurfaceLevel)
            {
                // Record original value.
                netAltered = true;
                netRecord.m_surfaceLevel = network.m_surfaceLevel;

                // Set new value.
                network.m_surfaceLevel = -RoadHandler.NewCurbHeight;
            }

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

                // Skip any meshes that we've already checked.
                Mesh segmentMesh = segment.m_segmentMesh;
                if (!CheckedMeshes.Contains(segmentMesh))
                {
                    // Not checked yet - add to list.
                    CheckedMeshes.Add(segmentMesh);

                    // Record original value.
                    netAltered = true;
                    netRecord.m_segmentDict.Add(segment, new NetComponentRecord
                    {
                        NetInfo = network,
                        EligibleCurbs = true,
                        EligibleBridge = false,
                        MainVerts = segmentMesh.vertices,
                        LodVerts = segment.m_lodMesh.vertices,
                    });

                    // Adjust vertices.
                    AdjustMesh(segment.m_segmentMesh, customParams);
                    if (RoadHandler.DoLODs)
                    {
                        AdjustMesh(segment.m_lodMesh, customParams);
                    }
                }
            }

            // Check lanes.
            if (network.m_lanes != null)
            {
                // Iterate through each lane in network, replacing ~30cm depths with our new curb height.
                foreach (NetInfo.Lane lane in network.m_lanes)
                {
                    if (lane.m_verticalOffset == customParams.SurfaceLevel)
                    {
                        // Record original value.
                        netAltered = true;
                        netRecord.m_laneDict.Add(lane, lane.m_verticalOffset);

                        // Apply new curb height.
                        lane.m_verticalOffset = -RoadHandler.NewCurbHeight;
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

                // Skip any meshes that we've already checked.
                Mesh nodeMesh = node.m_nodeMesh;
                if (!CheckedMeshes.Contains(nodeMesh))
                {
                    // Not checked yet - add to list.
                    CheckedMeshes.Add(nodeMesh);

                    // Record original value.
                    netAltered = true;
                    netRecord.m_nodeDict.Add(node, new NetComponentRecord
                    {
                        NetInfo = network,
                        EligibleCurbs = true,
                        EligibleBridge = false,
                        MainVerts = nodeMesh.vertices,
                        LodVerts = node.m_lodMesh.vertices,
                    });

                    // Adjust vertices.
                    AdjustMesh(node.m_nodeMesh, customParams);
                    if (RoadHandler.DoLODs)
                    {
                        AdjustMesh(node.m_lodMesh, customParams);
                    }
                }
            }

            // If the net was altered, record the created netRecord.
            if (netAltered)
            {
                netRecord.m_customParams = customParams;
                NetRecords.Add(network, netRecord);
            }
        }

        /// <summary>
        /// Adjusts the given mesh in line with current settings (curb heights and bridge deck depths).
        /// Includes filters to exclude meshes with fewer than four vertices, or full-height bridges.
        /// </summary>
        /// <param name="mesh">Mesh to modify.</param>
        /// <param name="customParams">Custom parameters for this manipulation.</param>
        private static void AdjustMesh(Mesh mesh, CustomRoadParams customParams)
        {
            // Check if we've already done this one.
            if (ProcessedMeshes.Contains(mesh))
            {
                // Already processed this mesh - do nothing.
                return;
            }

            // Adjusted vertex counters.
            int curbChangedVertices = 0;

            // Create new vertices array (changing individual elements within the existing array won't work with locked meshes).
            Vector3[] newVertices = new Vector3[mesh.vertices.Length];
            mesh.vertices.CopyTo(newVertices, 0);

            // Raise verticies; anything below ground level (but above the maximum depth trigger - allow for bridges etc.) has its y-value multiplied for proportional adjustment.
            for (int i = 0; i < newVertices.Length; ++i)
            {
                float thisY = newVertices[i].y;

                // Adjust any eligible curb vertices.
                if (thisY < customParams.SurfaceTopBound && thisY > customParams.SurfaceBottomBound)
                {
                    newVertices[i].y = -RoadHandler.NewCurbHeight;
                    ++curbChangedVertices;
                }
            }

            // If we changed at least four vertices, assign new vertices to mesh.
            // Don't change the mesh if we didn't get at least one quad, to avoid minor rendering glitches with flat LODs.
            if (curbChangedVertices > 3)
            {
                mesh.vertices = newVertices;

                // Record mesh as being altered.
                ProcessedMeshes.Add(mesh);
            }
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
                    if (netInfo != null && NetRecords.ContainsKey(netInfo))
                    {
                        // Update lanes in this segment.
                        segments[i].Info.m_netAI.UpdateLanes(i, ref segments[i], loading: false);
                    }
                }
            });
        }

        /// <summary>
        /// Struct to hold custom parameters for mesh manipulation.
        /// </summary>
        internal struct CustomRoadParams
        {
            /// <summary>
            /// Nominal original surface level.
            /// </summary>
            public float SurfaceLevel;

            /// <summary>
            /// Top Y-bound for a vertex to be considered a surface vertex.
            /// </summary>
            public float SurfaceTopBound;

            /// <summary>
            /// Bottom Y-bound for a vertex to be considered a surface vertex.
            /// </summary>
            public float SurfaceBottomBound;
        }

        /// <summary>
        /// Class to hold original data for networks (prior to curb height alteration).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:Accessible fields should begin with upper-case letter", Justification = "Internal data field")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Internal data field")]
        internal sealed class CustomNetRecord : NetRecord
        {
            /// <summary>
            /// Custom manipulation parameters for this network.
            /// </summary>
            internal CustomRoadParams m_customParams;
        }
    }
}
