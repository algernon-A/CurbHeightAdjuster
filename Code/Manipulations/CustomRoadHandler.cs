using System.Collections.Generic;
using UnityEngine;
using ColossalFramework;


namespace CurbHeightAdjuster
{
    internal static class CustomRoadHandler
    {
        // Dictionary of custom roads requiring individualised settings.
        internal static Dictionary<string, CustomRoadParams> customRoads = new Dictionary<string, CustomRoadParams>
        {
            // Paris cobblestone roads.
            { "1729876865", new CustomRoadParams { surfaceLevel = -0.15f, surfaceTopBound = -0.06f, surfaceBottomBound = -0.2f } }
        };

        // List of 10cm curb roads.
        private static HashSet<string> curbs10cm = new HashSet<string>
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
            "2643471147"
        };
        private static HashSet<string> curbs15cm = new HashSet<string>
        {
            // Paris cobblestone roads.
            "1729876865"
        };


        // Dictionary of altered nets.
        internal readonly static Dictionary<NetInfo, CustomNetRecord> netRecords = new Dictionary<NetInfo, CustomNetRecord>();

        // Hashset of currently processed network meshes, with calculated adjustment offsets.
        private readonly static HashSet<Mesh> processedMeshes = new HashSet<Mesh>();

        // List of meshes that we've already checked.
        private readonly static HashSet<Mesh> checkedMeshes = new HashSet<Mesh>();


        /// <summary>
        /// Checks to see if the given network is a custom network, and if so, performs custom net manipulations
        /// </summary>
        /// <param name="network">Network prefab</param>
        /// <returns>True if this was processed as a custom network, false otherwise</returns>
        internal static bool IsCustomNet(NetInfo network)
        {
            // Try to find steam ID (anything without this isn't custom).
            int periodIndex = network.name.IndexOf(".");
            if (periodIndex > 0)
            {
                // Steam ID found; check for any custom roads.
                string steamID = network.name.Substring(0, periodIndex);
                if (customRoads.TryGetValue(steamID, out CustomRoadParams customRoad))
                {
                    Logging.KeyMessage("processing custom road ", network.name, " with Steam ID ", steamID);
                    CustomNetManipulation(network, customRoad);
                    return true;
                }
                else if (curbs10cm.Contains(steamID))
                {
                    Logging.KeyMessage("processing 10cm curb road ", network.name, " with Steam ID ", steamID);
                    CustomNetManipulation(network, new CustomRoadParams { surfaceLevel = -0.10f, surfaceTopBound = -0.06f, surfaceBottomBound = -0.31f });
                    return true;
                }
            }

            // If we got here, it wasn't a custom network; return false.
            return false;
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
            if (network.m_surfaceLevel == customParams.surfaceLevel)
            {
                // Record original value.
                netAltered = true;
                netRecord.surfaceLevel = network.m_surfaceLevel;

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
                if (!checkedMeshes.Contains(segmentMesh))
                {
                    // Not checked yet - add to list.
                    checkedMeshes.Add(segmentMesh);

                    // Record original value.
                    netAltered = true;
                    netRecord.segmentDict.Add(segment, new NetComponentRecord
                    {
                        netInfo = network,
                        eligibleCurbs = true,
                        eligibleBridge = false,
                        mainVerts = segmentMesh.vertices,
                        lodVerts = segment.m_lodMesh.vertices
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
                    if (lane.m_verticalOffset == customParams.surfaceLevel)
                    {
                        // Record original value.
                        netAltered = true;
                        netRecord.laneDict.Add(lane, lane.m_verticalOffset);

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
                if (!checkedMeshes.Contains(nodeMesh))
                {
                    PrintVertices(network, nodeMesh);

                    // Not checked yet - add to list.
                    checkedMeshes.Add(nodeMesh);

                    // Record original value.
                    netAltered = true;
                    netRecord.nodeDict.Add(node, new NetComponentRecord
                    {
                        netInfo = network,
                        eligibleCurbs = true,
                        eligibleBridge = false,
                        mainVerts = nodeMesh.vertices,
                        lodVerts = node.m_lodMesh.vertices
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
                netRecord.customParams = customParams;
                netRecords.Add(network, netRecord);
            }
        }


        /// <summary>
        /// Reverts changes (back to original).
        /// </summary>
        internal static void Revert()
        {
            Logging.KeyMessage("CustomRoadRevert");

            // Iterate through all network records in dictionary.
            foreach (KeyValuePair<NetInfo, CustomNetRecord> netEntry in netRecords)
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
                    PrintVertices(netInfo, nodeEntry.Key.m_nodeMesh);
                    PrintVertices(netInfo, nodeEntry.Value.mainVerts);
                }

                // Restore lanes.
                foreach (KeyValuePair<NetInfo.Lane, float> laneEntry in netRecord.laneDict)
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
            processedMeshes.Clear();

            // Iterate through all network records in dictionary.
            foreach (KeyValuePair<NetInfo, CustomNetRecord> netEntry in netRecords)
            {
                // Local references.
                NetInfo netInfo = netEntry.Key;
                NetRecord netRecord = netEntry.Value;

                // Change net surface level.
                netInfo.m_surfaceLevel = -RoadHandler.NewCurbHeight;

                // Update segment vertices.
                foreach (KeyValuePair<NetInfo.Segment, NetComponentRecord> segmentEntry in netRecord.segmentDict)
                {
                    // Restore original vertices and then raise mesh.
                    NetInfo.Segment segment = segmentEntry.Key;
                    segment.m_segmentMesh.vertices = segmentEntry.Value.mainVerts;
                    segment.m_lodMesh.vertices = segmentEntry.Value.lodVerts;
                    AdjustMesh(segment.m_segmentMesh, netEntry.Value.customParams);
                    if (RoadHandler.DoLODs)
                    {
                        AdjustMesh(segment.m_lodMesh, netEntry.Value.customParams);
                    }
                }

                // Update node vertices.
                foreach (KeyValuePair<NetInfo.Node, NetComponentRecord> nodeEntry in netRecord.nodeDict)
                {
                    // Restore original vertices and then raise mesh.
                    NetInfo.Node node = nodeEntry.Key;
                    node.m_nodeMesh.vertices = nodeEntry.Value.mainVerts;
                    node.m_lodMesh.vertices = nodeEntry.Value.lodVerts;
                    AdjustMesh(node.m_nodeMesh, netEntry.Value.customParams);

                    // Update LODs if set to do so.
                    if (RoadHandler.DoLODs)
                    {
                        AdjustMesh(node.m_lodMesh, netEntry.Value.customParams);
                    }
                }

                // Change lanes.
                foreach (KeyValuePair<NetInfo.Lane, float> laneEntry in netRecord.laneDict)
                {
                    laneEntry.Key.m_verticalOffset = -RoadHandler.NewCurbHeight;
                }
            }

            // Recalulate lanes on map with new height.
            RecalculateLanes();

            // Clear processed mesh list once done.
            processedMeshes.Clear();
        }


        /// <summary>
        /// Adjusts the given mesh in line with current settings (curb heights and bridge deck depths).
        /// Includes filters to exclude meshes with fewer than four vertices, or full-height bridges.
        /// </summary>
        /// <param name="mesh">Mesh to modify</param>
        private static void AdjustMesh(Mesh mesh, CustomRoadParams customParams)
        {
            // Check if we've already done this one.
            if (processedMeshes.Contains(mesh))
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
                if (thisY < customParams.surfaceTopBound && thisY > customParams.surfaceBottomBound)
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
                processedMeshes.Add(mesh);
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

        private static void PrintVertices(NetInfo network, Mesh mesh)
        {
            Logging.Message("PrintVertices mesh: ", mesh.name);
            PrintVertices(network, mesh.vertices);
        }

        private static void PrintVertices(NetInfo network, Vector3[] vertices)
        {
            Logging.Message("Network: ", network.name);
            for (int i = 0; i < vertices.Length; ++i)
            {
                float vertext = vertices[i].y;
                if (vertext < -0.2f && vertext > -0.31f)
                {
                    Logging.KeyMessage("found vertext ", i, ":" , vertext);
                }
            }
        }
    }

    /// <summary>
    /// Class to hold original data for networks (prior to curb height alteration).
    /// </summary>
    public class CustomNetRecord : NetRecord
    {
        public CustomRoadParams customParams;
    }


    public struct CustomRoadParams
    {
        public float surfaceLevel;
        public float surfaceTopBound;
        public float surfaceBottomBound;
    }
}
