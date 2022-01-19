using System;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework;


namespace CurbHeightAdjuster
{
    /// <summary>
    /// Class to hold original data for networks (prior to curb height alteration).
    /// </summary>
    public class CurbRecord
    {
        // Network surface level.
        public float surfaceLevel;

        // Network segment vertices.
        public Dictionary<NetInfo.Segment, OriginalVerts> segmentDict = new Dictionary<NetInfo.Segment, OriginalVerts>();

        // Network node vertices.
        public Dictionary<NetInfo.Node, OriginalVerts> nodeDict = new Dictionary<NetInfo.Node, OriginalVerts>();

        // Network lane vertical offsets.
        public Dictionary<NetInfo.Lane, float> laneDict = new Dictionary<NetInfo.Lane, float>();
    }


    /// <summary>
    /// Struct to hold references to original vertex arrays (main and LOD).
    /// </summary>
    public struct OriginalVerts
    {
        public Vector3[] mainVerts;
        public Vector3[] lodVerts;
    }


    /// <summary>
    /// Class to hold original data for parking assets (prior to curb height alteration).
    /// </summary>
    public class ParkingRecord
    {
        // Building mesh vertices.
        public Vector3[] vertices;

        // Prop heights.
        public Dictionary<BuildingInfo.Prop, float> propHeights = new Dictionary<BuildingInfo.Prop, float>();
    }


    /// <summary>
    /// Harmony patch to change curb hights on net load.
    /// </summary>
    public static class CurbHeight
    {
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


        // Original and default new curb heights.
        private const float OriginalCurbHeight = -0.30f;
        private const float DefaultNewCurbHeight = -0.15f;

        // Depth trigger - segments/nets need to have depths within these bounds to be adjusted.
        // Vanilla tram rails have tops at -0.225.
        // LRT tram rails have bases at -0.5.
        private const float MinDepthTrigger = -0.21f;
        private const float MaxDepthTrigger = -0.32f;

        // Maximum bounds.
        internal const float MinCurbHeight = 0.07f;
        internal const float MaxCurbHeight = 0.29f;

        // Curb height multiiplier.
        private static float newCurbMultiplier = DefaultNewCurbHeight / OriginalCurbHeight;

        // Dictionary of altered nets.
        private readonly static Dictionary<NetInfo, CurbRecord> curbRecords = new Dictionary<NetInfo, CurbRecord>();

        // Dictionary of altered parking buildings.
        private readonly static Dictionary<BuildingInfo, ParkingRecord> parkingRecords = new Dictionary<BuildingInfo, ParkingRecord>();

        // Hashset of currently processed network meshes.
        private readonly static HashSet<Mesh> processedMeshes = new HashSet<Mesh>();


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
        /// Determines if lods are also adjusted.
        /// </summary>
        internal static bool RaiseLods { get; set; } = false;


        /// <summary>
        /// Iterates through all loaded NetInfos and tries to raise curbs from -30cm to -15cm.
        /// Original script by Ronyx69, adapted to mod form by krzychu124, redesigned, rewritten, optimised and extended by algernon.
        /// </summary>
        public static void RaiseCurbHeights()
        {
            Logging.KeyMessage("reducing curb heights");

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
                    if (netAI is RoadAI || netAI is RoadBridgeAI || netAI is RoadTunnelAI || netAI is DamAI)
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

                        // Curb record for this prefab.
                        CurbRecord curbRecord = new CurbRecord();


                        // Raise network surface level.
                        if (network.m_surfaceLevel == -0.3f)
                        {
                            // Record original value.
                            netAltered = true;
                            curbRecord.surfaceLevel = network.m_surfaceLevel;

                            // Set new value.
                            network.m_surfaceLevel = newCurbHeight;
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

                            // Check to see if this segment is a viable target.
                            // Iterate through each vertex in segment mesh, counting how many meet our trigger height range.
                            Vector3[] vertices = segment.m_segmentMesh.vertices;
                            int count30 = 0;
                            for (int j = 0; j < vertices.Length; ++j)
                            {
                                if (vertices[j].y < MinDepthTrigger && vertices[j].y > MaxDepthTrigger)
                                {
                                    count30++;
                                }
                            }

                            // Check final counts; more than eight eligible verticies means the segment passes our filter.
                            if (count30 > 8)
                            {
                                // Eligibile target; record original value.
                                netAltered = true;
                                curbRecord.segmentDict.Add(segment, new OriginalVerts { mainVerts = vertices, lodVerts = segment.m_lodMesh.vertices });

                                // Raise vertices.
                                AdjustMesh(segment.m_segmentMesh);
                                if (RaiseLods)
                                {
                                    AdjustMesh(segment.m_lodMesh);
                                }
                            }
                        }

                        // Check lanes, if we've passed checks.
                        if (network.m_lanes != null)
                        {
                            // Iterate through each lane in network, replacing 30cm depths with our new curb height.
                            foreach (NetInfo.Lane lane in network.m_lanes)
                            {
                                if (lane.m_verticalOffset < MinDepthTrigger && lane.m_verticalOffset > MaxDepthTrigger)
                                {
                                    // Record original value.
                                    netAltered = true;
                                    curbRecord.laneDict.Add(lane, lane.m_verticalOffset);

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

                            // Check to see if this segment is a viable target.
                            // Iterate through each vertex in segment mesh, counting how many meet our trigger height range.
                            Vector3[] vertices = node.m_nodeMesh.vertices;
                            int count30 = 0;
                            for (int j = 0; j < vertices.Length; ++j)
                            {
                                if (vertices[j].y < MinDepthTrigger && vertices[j].y > MaxDepthTrigger)
                                {
                                    count30++;
                                }
                            }

                            // Check final counts; more than four -30cm verticies means the node passes our filter.
                            if (count30 > 4)
                            {
                                // Eligibile target; record original value.
                                netAltered = true;
                                curbRecord.nodeDict.Add(node, new OriginalVerts { mainVerts = vertices, lodVerts = node.m_lodMesh.vertices });

                                // Raise vertices.
                                AdjustMesh(node.m_nodeMesh);
                                if (RaiseLods)
                                {
                                    AdjustMesh(node.m_lodMesh);
                                }
                            }
                        }

                        // If the net was altered, record the created curbRecord.
                        if (netAltered)
                        {
                            curbRecords.Add(network, curbRecord);
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

            // Clear processed mesh list once done.
            processedMeshes.Clear();


            Logging.KeyMessage("finished reducing curb heights");
        }


        /// <summary>
        /// Raises 'parking lot roads' parking lot 'buildings' from -30cm to -15cm.
        /// Called via transpiler insert.
        /// </summary>
        public static void RaiseParkingLots()
        {
            Logging.KeyMessage("raising parking lots");

            // Iterate through all networks in list.
            for (uint i = 0; i < PrefabCollection<BuildingInfo>.LoadedCount(); ++i)
            {
                BuildingInfo building = PrefabCollection<BuildingInfo>.GetLoaded(i);

                // Skip any null prefabs.
                if (building?.name == null)
                {
                    continue;
                }

                try
                {
                    // Check for parking lot road prefab prefixes.
                    int periodIndex = building.name.IndexOf(".");
                    if (periodIndex > 0)
                    {
                        string steamID = building.name.Substring(0, periodIndex);

                        // Parking lot roads.
                        if (steamID.Equals("1285201733") || steamID.Equals("1293870311") || steamID.Equals("1293869603"))
                        {
                            // Local reference.
                            Mesh mesh = building.m_mesh;
                            Vector3[] vertices = mesh.vertices;

                            // Found a match - raise the mesh.
                            Logging.Message("raising Parking Lot Road ", building.name);

                            // Record original vertices.
                            ParkingRecord parkingRecord = new ParkingRecord
                            {
                                vertices = vertices
                            };

                            // Raise mesh.
                            RaiseMesh(mesh);
                            if (RaiseLods)
                            {
                                Logging.Message("raising LOD");
                                RaiseMesh(building.m_lodMesh);
                            }

                            // Raise props in building.
                            foreach (BuildingInfo.Prop prop in building.m_props)
                            {
                                parkingRecord.propHeights.Add(prop, prop.m_position.y);
                                prop.m_position.y -= (OriginalCurbHeight - newCurbHeight);
                            }

                            // Add original data record to dictionary.
                            parkingRecords.Add(building, parkingRecord);
                        }
                        // Big Parking Lots.
                        else if ((steamID.Equals("2115188517") || steamID.Equals("2121900156") || steamID.Equals("2116510188")) && building.m_props != null)
                        {
                            // Create new parkingRecord with original vertices (which will remain unaltered).
                            ParkingRecord parkingRecord = new ParkingRecord
                            {
                                vertices = building.m_mesh?.vertices
                            };

                            // Raise invisible parking space markers in building.
                            foreach (BuildingInfo.Prop prop in building.m_props)
                            {
                                if (prop.m_prop.name.Equals("Invisible Parking Space"))
                                {
                                    parkingRecord.propHeights.Add(prop, prop.m_position.y);
                                    prop.m_position.y -= (OriginalCurbHeight - newCurbHeight);
                                }
                            }

                            // If we raised any invisible parking lot markers, add the parkingRecord to our list.
                            if (parkingRecord.propHeights.Count > 0)
                            {
                                // Found a match - raise the mesh.
                                Logging.Message("raised Big Parking Lot ", building.name);
                                parkingRecords.Add(building, parkingRecord);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    // Don't let a single failure stop us.
                    Logging.LogException(e, "exception checking building ", building.name);
                }
            }
        }


        /// <summary>
        /// Reverts curb height changes (back to original).
        /// </summary>
        internal static void Revert()
        {
            Logging.KeyMessage("reverting custom curbs");

            // Iterate through all curb records in dictionary.
            foreach (KeyValuePair<NetInfo, CurbRecord> netEntry in curbRecords)
            {
                CurbRecord curbRecord = netEntry.Value;

                // Restore net surface level.
                netEntry.Key.m_surfaceLevel = curbRecord.surfaceLevel;

                // Restore segment vertices.
                foreach (KeyValuePair<NetInfo.Segment, OriginalVerts> segmentEntry in curbRecord.segmentDict)
                {
                    segmentEntry.Key.m_segmentMesh.vertices = segmentEntry.Value.mainVerts;
                    segmentEntry.Key.m_lodMesh.vertices = segmentEntry.Value.lodVerts;
                }

                // Restore node vertices.
                foreach (KeyValuePair<NetInfo.Node, OriginalVerts> nodeEntry in curbRecord.nodeDict)
                {
                    nodeEntry.Key.m_nodeMesh.vertices = nodeEntry.Value.mainVerts;
                    nodeEntry.Key.m_lodMesh.vertices = nodeEntry.Value.lodVerts;
                }

                // Restore lanes.
                foreach (KeyValuePair<NetInfo.Lane, float> laneEntry in curbRecord.laneDict)
                {
                    laneEntry.Key.m_verticalOffset = laneEntry.Value;
                }
            }

            // Iterate through all parking records in dictionary.
            {

                foreach (KeyValuePair<BuildingInfo, ParkingRecord> buildingEntry in parkingRecords)
                {
                    // Restore building vertices.
                    buildingEntry.Key.m_mesh.vertices = buildingEntry.Value.vertices;

                    // Restore prop heights.
                    foreach (KeyValuePair<BuildingInfo.Prop, float> propEntry in buildingEntry.Value.propHeights)
                    {
                        propEntry.Key.m_position.y = propEntry.Value;
                    }
                }
            }

            // Recalulate lanes on map with new height.
            RecalculateLanes();
        }


        /// <summary>
        /// Applies updated curb height changes.
        /// </summary>
        internal static void Apply()
        {
            Logging.KeyMessage("applying custom curbs");

            // Ensure processed mesh list is clear, just in case.
            processedMeshes.Clear();

            // Iterate through all curb records in dictionary.
            foreach (KeyValuePair<NetInfo, CurbRecord> netEntry in curbRecords)
            {
                CurbRecord curbRecord = netEntry.Value;

                // Change net surface level.
                netEntry.Key.m_surfaceLevel = newCurbHeight;

                // Update segment vertices.
                foreach (KeyValuePair<NetInfo.Segment, OriginalVerts> segmentEntry in curbRecord.segmentDict)
                {
                    // Restore original vertices and then raise mesh.
                    segmentEntry.Key.m_segmentMesh.vertices = segmentEntry.Value.mainVerts;
                    segmentEntry.Key.m_lodMesh.vertices = segmentEntry.Value.lodVerts;
                    AdjustMesh(segmentEntry.Key.m_segmentMesh);
                    if (RaiseLods)
                    {
                        AdjustMesh(segmentEntry.Key.m_lodMesh);
                    }
                }

                // Update node vertices.
                foreach (KeyValuePair<NetInfo.Node, OriginalVerts> nodeEntry in curbRecord.nodeDict)
                {
                    // Restore original vertices and then raise mesh.
                    nodeEntry.Key.m_nodeMesh.vertices = nodeEntry.Value.mainVerts;
                    nodeEntry.Key.m_lodMesh.vertices = nodeEntry.Value.lodVerts;
                    AdjustMesh(nodeEntry.Key.m_nodeMesh);
                    if (RaiseLods)
                    {
                        AdjustMesh(nodeEntry.Key.m_lodMesh);
                    }
                }

                // Change lanes.
                foreach (KeyValuePair<NetInfo.Lane, float> laneEntry in curbRecord.laneDict)
                {
                    laneEntry.Key.m_verticalOffset = newCurbHeight;
                }
            }

            // Iterate through all parking records in dictionary.
            {
                foreach (KeyValuePair<BuildingInfo, ParkingRecord> buildingEntry in parkingRecords)
                {
                    // Restore building vertices and then re-adjust mesh.
                    if (buildingEntry.Key.m_mesh != null && buildingEntry.Value.vertices != null)
                    {
                        buildingEntry.Key.m_mesh.vertices = buildingEntry.Value.vertices;
                        RaiseMesh(buildingEntry.Key.m_mesh);
                    }

                    // Adjust prop heights.
                    foreach (KeyValuePair<BuildingInfo.Prop, float> propEntry in buildingEntry.Value.propHeights)
                    {
                        propEntry.Key.m_position.y = propEntry.Value - (OriginalCurbHeight - newCurbHeight);
                    }
                }
            }

            // Recalulate lanes on map with new height.
            RecalculateLanes();

            // Clear processed mesh list once done.
            processedMeshes.Clear();
        }


        /// <summary>
        /// Proportionally raises the vertices of the given mesh in line with curb height adjustment.
        /// Includes a filter to exclude meshes with fewer than four vertices.
        /// </summary>
        /// <param name="mesh">Mesh to modify</param>
        private static void AdjustMesh(Mesh mesh)
        {
            // Check if we've already done this one.
            if (processedMeshes.Contains(mesh))
            {
                Logging.Message("skipping duplicate network mesh ", mesh.name ?? "null");
                return;
            }

            // Adjusted vertex counter.
            int changedVertices = 0;

            // Create new vertices array (changing individual elements within the existing array won't work with locked meshes).
            Vector3[] newVertices = new Vector3[mesh.vertices.Length];
            mesh.vertices.CopyTo(newVertices, 0);

            // Raise verticies; anything below ground level (but above the maximum depth tirgger - allow for bridges etc.) has its y-value multiplied for proportional adjustment.
            for (int i = 0; i < newVertices.Length; ++i)
            {
                if (newVertices[i].y < 0.0f && newVertices[i].y > MaxDepthTrigger)
                {
                    newVertices[i].y *= newCurbMultiplier;
                    ++changedVertices;
                }
            }

            // If we changed at least four vertices, assign new vertices to mesh.
            // Don't change the mesh if we didn't get at least one quad, to avoid minor rendering glitches with flat LODs.
            if (changedVertices > 3)
            {
                mesh.vertices = newVertices;

                // Record mesh as being altered.
                processedMeshes.Add(mesh);

            }
        }


        /// <summary>
        /// Raises the vertices of the given mesh by the current curb height adjustment.
        /// </summary>
        /// <param name="mesh">Mesh to modify</param>
        private static void RaiseMesh(Mesh mesh)
        {
            // Check if we've already done this one.
            if (processedMeshes.Contains(mesh))
            {
                Logging.Message("skipping duplicate parking mesh ", mesh.name ?? "null");
                return;
            }

            // Amount to raise up from original height.
            float adjustment = OriginalCurbHeight - newCurbHeight;

            // Create new vertices array (changing individual elements within the existing array won't work with locked meshes).
            Vector3[] newVertices = new Vector3[mesh.vertices.Length];
            mesh.vertices.CopyTo(newVertices, 0);

            // Minimum height check, to avoid double-processing vertices.
            float minHeight = 0f;

            // Iterate through all vertices to raise them.
            for (int i = 0; i < newVertices.Length; ++i)
            {
                // If this vertex is lower than our current stored minimum, update the stored minimum height.
                if (newVertices[i].y < minHeight)
                {
                    minHeight = newVertices[i].y;
                }

                // Raise vertex.
                newVertices[i].y -= adjustment;
            }

            // Assign new vertices to mesh if minimum height check was passed (parking lot road parking lot minHeight will be -0.2794001).
            if (minHeight < 0.279)
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
                    if (netInfo != null && curbRecords.ContainsKey(netInfo))
                    {
                        // Update lanes in this segment.
                        segments[i].Info.m_netAI.UpdateLanes(i, ref segments[i], loading: false);
                    }
                }
            });
        }
    }
}