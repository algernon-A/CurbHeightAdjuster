using UnityEngine;


namespace CurbHeightAdjuster
{
    /// <summary>
    /// Harmony patch to change curb hights on net load.
    /// </summary>
    public static class CurbHeight
    {
        // Original and default new curb heights.
        private const float OriginalCurbHeight = -0.30f;
        private const float DefaultNewCurbHeight = -0.15f;

        // Maximum bounds.
        internal const float MinCurbHeight = 0.01f;
        internal const float MaxCurbHeight = 0.29f;

        // Curb height multiiplier.
        private static float newCurbMultiplier = DefaultNewCurbHeight / OriginalCurbHeight;

        
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
                    // Raise network surface level.
                    if (network.m_surfaceLevel == -0.3f)
                    {
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
                                // Eligibile target; raise vertices.
                                RaiseMesh(segment.m_segmentMesh);
                                RaiseMesh(segment.m_lodMesh);
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
                                // Eligibile target; raise vertices.
                                RaiseMesh(node.m_nodeMesh);
                                RaiseMesh(node.m_lodMesh);
                            }
                            else
                            {
                                Logging.Message("node vertices filter failed with count15 ", count15);
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Raises 'parking lot roads' parking lot 'buildings' from -30cm to -15cm.
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

                // Check for parking lot road prefab prefixes.
                int periodIndex = building.name.IndexOf(".");
                if (periodIndex > 0)
                {
                    string steamID = building.name.Substring(0, periodIndex);
                    if (steamID.Equals("1285201733") || steamID.Equals("1293870311") || steamID.Equals("1293869603"))
                    {
                        // Found a match - raise the mesh (including a 5mm adjustment to ensure we're clear of raised road surface and to avoid z-fighting, especially at oblique angles).
                        Logging.Message("raising parking lot ", building.name);
                        RaiseMesh(building.m_mesh, 0.005f);
                        RaiseMesh(building.m_lodMesh, 0.005f);

                        // Raise props in building.
                        foreach (BuildingInfo.Prop prop in building.m_props)
                        {
                            prop.m_position.y -= (OriginalCurbHeight - newCurbHeight);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Raises the vertices of the given mesh in line with curb height adjustment.
        /// </summary>
        /// <param name="mesh">Mesh to modify</param>
        /// <param name="adjustment">Upwards adjustment to final y height (if any)</param>
        private static void RaiseMesh(Mesh mesh, float adjustment = 0f)
        {
            // Create new vertices array (changing individual elements within the existing array won't work with locked meshes).
            Vector3[] newVertices = new Vector3[mesh.vertices.Length];
            mesh.vertices.CopyTo(newVertices, 0);

            // Raise verticies; anything below ground level (but above -31cm - allow for bridges etc.) has its y-value adjusted.
            for (int j = 0; j < newVertices.Length; ++j)
            {
                if (newVertices[j].y < 0.0f && newVertices[j].y > -0.31f)
                {
                    newVertices[j].y = (newVertices[j].y * newCurbMultiplier) + adjustment;
                }
            }

            // Assign new vertices to mesh.
            mesh.vertices = newVertices;
        }
    }
}