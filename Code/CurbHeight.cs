using System.Collections.Generic;
using UnityEngine;


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
        public Dictionary<NetInfo.Segment, Vector3[]> segmentDict = new Dictionary<NetInfo.Segment, Vector3[]>();

        // Network node vertices.
        public Dictionary<NetInfo.Node, Vector3[]> nodeDict = new Dictionary<NetInfo.Node, Vector3[]>();

        // Network lane vertical offsets.
        public Dictionary<NetInfo.Lane, float> laneDict = new Dictionary<NetInfo.Lane, float>();
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
        // Original and default new curb heights.
        private const float OriginalCurbHeight = -0.30f;
        private const float DefaultNewCurbHeight = -0.15f;

        // Maximum bounds.
        internal const float MinCurbHeight = 0.07f;
        internal const float MaxCurbHeight = 0.29f;

        // Curb height multiiplier.
        private static float newCurbMultiplier = DefaultNewCurbHeight / OriginalCurbHeight;

        // Dictionary of altered nets.
        private readonly static Dictionary<NetInfo, CurbRecord> curbRecords = new Dictionary<NetInfo, CurbRecord>();

        // Dictionary of altered parking buildings.
        private readonly static Dictionary<BuildingInfo, ParkingRecord> parkingRecords = new Dictionary<BuildingInfo, ParkingRecord>();


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
        /// Original script by Ronyx69, adapted to mod form by krzychu124, rewritten and optimised by algernon.
        /// </summary>
        public static void RaiseCurbHeights()
        {
            Logging.KeyMessage("reducing curb heights");

            // Iterate through all networks in list.
            for (uint i = 0; i < PrefabCollection<NetInfo>.LoadedCount(); ++i)
            {
                NetInfo network = PrefabCollection<NetInfo>.GetLoaded(i);

                // Skip any null prefabs.
                if (network?.m_netAI == null)
                {
                    continue;
                }

                // Only looking at road prefabs.
                NetAI netAI = network.m_netAI;
                if (netAI is RoadAI || netAI is RoadBridgeAI || netAI is RoadTunnelAI)
                {
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
                        if (segment?.m_segmentMesh == null || segment.m_segmentMaterial?.shader == null)
                        {
                            continue;
                        }

                        // Only interested in segments using road shaders.
                        string shaderName = segment.m_segmentMaterial.shader.name;
                        if (shaderName != "Custom/Net/Road" && shaderName != "Custom/Net/RoadBridge" && shaderName != "Custom/Net/TrainBridge")
                        {
                            continue;
                        }

                        // Check to see if this segment is a viable target.
                        // Iterate through each vertex in segment mesh, counting how many are approx -30cm and how many are approx -15cm.
                        Vector3[] vertices = segment.m_segmentMesh.vertices;
                        int count15 = 0, count30 = 0;
                        for (int j = 0; j < vertices.Length; ++j)
                        {
                            if (vertices[j].y < -0.24f && vertices[j].y > -0.31f)
                            {
                                count30++;
                            }
                            else if (vertices[j].y < -0.145f && vertices[j].y > -0.155f)
                            {
                                count15++;
                            }
                        }

                        // Check final counts; less than eight -15cm vertices and more than eight -30cm verticies means the segment passes our filter.
                        // Eight or more -15cm verticies implies a primary -15cm curb height.
                        if (count30 > 8)
                        {
                            if (count15 < 8)
                            {
                                // Eligibile target; record original value.
                                netAltered = true;
                                curbRecord.segmentDict.Add(segment, vertices);
                                
                                // Raise vertices.
                                AdjustMesh(segment.m_segmentMesh);
                                if (RaiseLods)
                                {
                                    AdjustMesh(segment.m_lodMesh);
                                }
                            }
                            else
                            {
                                Logging.Message("segment vertices filter failed with count15 ", count15, " for network ", network.name);
                            }
                        }
                    }

                    // Check lanes, if we've passed checks.
                    if (network.m_lanes != null)
                    {
                        // Iterate through each lane in network, replacing 30cm depths with our new curb height.
                        foreach (NetInfo.Lane lane in network.m_lanes)
                        {
                            if (lane.m_verticalOffset == OriginalCurbHeight)
                            {
                                // Record original value.
                                netAltered = true;
                                curbRecord.laneDict.Add(lane, lane.m_verticalOffset);

                                // Apply new curb height.
                                lane.m_verticalOffset = newCurbHeight;
                            }
                        }
                    }

                    // Update nodes.
                    foreach (NetInfo.Node node in network.m_nodes)
                    {
                        // Skip nodes with no mesh or material.
                        if (node?.m_nodeMesh == null || node.m_nodeMaterial?.shader == null)
                        {
                            continue;
                        }

                        // Only interested in nodes using road shaders.
                        string shaderName = node.m_nodeMaterial.shader.name;
                        if (shaderName != "Custom/Net/Road" && shaderName != "Custom/Net/RoadBridge" && shaderName != "Custom/Net/TrainBridge")
                        {
                            continue;
                        }

                        // Check to see if this segment is a viable target.
                        // Iterate through each vertex in segment mesh, counting how many are approx -30cm and how many are approx -15cm.
                        Vector3[] vertices = node.m_nodeMesh.vertices;
                        int count15 = 0, count30 = 0;
                        for (int j = 0; j < vertices.Length; ++j)
                        {
                            if (vertices[j].y < -0.24f && vertices[j].y > -0.31f)
                            {
                                count30++;
                            }
                            else if (vertices[j].y < -0.145f && vertices[j].y > -0.155f)
                            {
                                count15++;
                            }
                        }

                        // Check final counts; less than twenty -15cm vertices and more than four -30cm verticies means the node passes our filter.
                        // Twenty or more -15cm verticies implies a primary -15cm curb height.
                        if (count30 > 4)
                        {
                            if (count15 < 20)
                            {
                                // Eligibile target; record original value.
                                netAltered = true;
                                curbRecord.nodeDict.Add(node, vertices);

                                // Raise vertices.
                                AdjustMesh(node.m_nodeMesh);
                                if (RaiseLods)
                                {
                                    AdjustMesh(node.m_lodMesh);
                                }
                            }
                            else
                            {
                                Logging.Message("node vertices filter failed with count15 ", count15);
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
        }


        /// <summary>
        /// Raises 'parking lot roads' parking lot 'buildings' from -30cm to -15cm.
        /// Called via transpiler insert.
        /// </summary>
        public static void RaiseParkingLots()
        {
            // Hashlist of already processed vertices (so we don't double-adjust a mesh due to Loading Screen Mod mesh sharing).
            HashSet<Vector3[]> processedMeshes = new HashSet<Vector3[]>();

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

                // Check for parking lot road prefab prefixes.
                int periodIndex = building.name.IndexOf(".");
                if (periodIndex > 0)
                {
                    string steamID = building.name.Substring(0, periodIndex);
                    if (steamID.Equals("1285201733") || steamID.Equals("1293870311") || steamID.Equals("1293869603"))
                    {
                        // Local reference.
                        Mesh mesh = building.m_mesh;
                        Vector3[] vertices = mesh.vertices;

                        // Found a match - check for any previously processed (duplicate, due to LSM mesh sharing) meshes.
                        if (processedMeshes.Contains(vertices))
                        {
                            Logging.Message("skipping already-processed mesh for ", building.name);
                            continue;
                        }

                        // New mesh - add it to processedMeshes.
                        processedMeshes.Add(vertices);

                        // Raise the mesh.
                        Logging.Message("raising parking lot ", building.name);

                        // Record original vertices.
                        ParkingRecord parkingRecord = new ParkingRecord
                        {
                            vertices = vertices
                        };

                        // Raise mesh.
                        RaiseMesh(mesh);
                        if (RaiseLods)
                        {
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
                foreach (KeyValuePair<NetInfo.Segment, Vector3[]> segmentEntry in curbRecord.segmentDict)
                {
                    segmentEntry.Key.m_segmentMesh.vertices = segmentEntry.Value;
                }

                // Restore node vertices.
                foreach (KeyValuePair<NetInfo.Node, Vector3[]> nodeEntry in curbRecord.nodeDict)
                {
                    nodeEntry.Key.m_nodeMesh.vertices = nodeEntry.Value;
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
        }


        /// <summary>
        /// Applies updated curb height changes.
        /// </summary>
        internal static void Apply()
        {
            Logging.KeyMessage("applying custom curbs");

            // Iterate through all curb records in dictionary.
            foreach (KeyValuePair<NetInfo, CurbRecord> netEntry in curbRecords)
            {
                CurbRecord curbRecord = netEntry.Value;

                // Change net surface level.
                netEntry.Key.m_surfaceLevel = newCurbHeight;

                // Update segment vertices.
                foreach (KeyValuePair<NetInfo.Segment, Vector3[]> segmentEntry in curbRecord.segmentDict)
                {
                    // Restore original vertices and then raise mesh.
                    segmentEntry.Key.m_segmentMesh.vertices = segmentEntry.Value;
                    AdjustMesh(segmentEntry.Key.m_segmentMesh);
                }

                // Update node vertices.
                foreach (KeyValuePair<NetInfo.Node, Vector3[]> nodeEntry in curbRecord.nodeDict)
                {
                    // Restore original vertices and then raise mesh.
                    nodeEntry.Key.m_nodeMesh.vertices = nodeEntry.Value;
                    AdjustMesh(nodeEntry.Key.m_nodeMesh);
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
                    buildingEntry.Key.m_mesh.vertices = buildingEntry.Value.vertices;
                    RaiseMesh(buildingEntry.Key.m_mesh);

                    // Adjust prop heights.
                    foreach (KeyValuePair<BuildingInfo.Prop, float> propEntry in buildingEntry.Value.propHeights)
                    {
                        propEntry.Key.m_position.y = propEntry.Value - (OriginalCurbHeight - newCurbHeight);
                    }
                }
            }
        }


        /// <summary>
        /// Proportionally raises the vertices of the given mesh in line with curb height adjustment.
        /// </summary>
        /// <param name="mesh">Mesh to modify</param>
        private static void AdjustMesh(Mesh mesh)
        {
            // Adjusted vertex counter.
            int changedVertices = 0;

            // Create new vertices array (changing individual elements within the existing array won't work with locked meshes).
            Vector3[] newVertices = new Vector3[mesh.vertices.Length];
            mesh.vertices.CopyTo(newVertices, 0);

            // Raise verticies; anything below ground level (but above -31cm - allow for bridges etc.) has its y-value multiplied for proportional adjustment.
            for (int i = 0; i < newVertices.Length; ++i)
            {
                if (newVertices[i].y < 0.0f && newVertices[i].y > -0.31f)
                {
                    newVertices[i].y = (newVertices[i].y * newCurbMultiplier);
                    ++changedVertices;
                }
            }

            // If we changed at least four vertices, assign new vertices to mesh.
            // Don't change the mesh if we didn't get at least one quad, to avoid minor rendering glitches with flat LODs.
            if (changedVertices > 3)
            {
                mesh.vertices = newVertices;
            }
        }


        /// <summary>
        /// Raises the vertices of the given mesh by the current curb height adjustment.
        /// </summary>
        /// <param name="mesh">Mesh to modify</param>
        private static void RaiseMesh(Mesh mesh)
        {
            // Amount to raise up from original height.
            float adjustment = OriginalCurbHeight - newCurbHeight;

            // Create new vertices array (changing individual elements within the existing array won't work with locked meshes).
            Vector3[] newVertices = new Vector3[mesh.vertices.Length];
            mesh.vertices.CopyTo(newVertices, 0);

            // Raise verticies.
            for (int i = 0; i < newVertices.Length; ++i)
            {
                newVertices[i].y -= adjustment;
            }

            // Assign new vertices to mesh.
            mesh.vertices = newVertices;
        }
    }
}