using System;
using System.Collections.Generic;
using UnityEngine;


namespace CurbHeightAdjuster
{
    /// <summary>
    /// Class to manage changes to pedestrian paths.
    /// </summary>
    public static class PathHandler
    {
        // Original curb heights.
        internal const float OriginalPathHeight = 0.20f;

        // Default mod settings.
        internal const float DefaultNewPathHeight = 0.05f;

        // Maximum bounds.
        internal const float MinPathHeight = 0.01f;
        internal const float MaxPathHeight = 0.19f;

        // Path height multiiplier.
        private static float newPathMultiplier = DefaultNewPathHeight / OriginalPathHeight;

        // Dictionaris of altered nets.
        internal readonly static Dictionary<NetInfo, NetRecord> netRecords = new Dictionary<NetInfo, NetRecord>();

        // Hashset of currently processed network meshes, with calculated adjustment offsets.
        private readonly static HashSet<Mesh> processedMeshes = new HashSet<Mesh>();


        /// <summary>
        /// New curb height to apply (positive figure, in cm).
        /// </summary>
        internal static float NewPathHeight
        {
            get => newPathHeight;

            set
            {
                // Update multiplier with change in value.
                newPathHeight = Mathf.Clamp(value, MinPathHeight, MaxPathHeight);
                newPathMultiplier = NewPathHeight / OriginalPathHeight;
            }
        }
        private static float newPathHeight = DefaultNewPathHeight;


        /// <summary>
        /// Determines if lods are also adjusted.
        /// </summary>
        internal static bool DoLODs { get; set; } = false;


        /// <summary>
        /// Called on load to scan through all loaded NetInfos, build the database, and apply network manipulations (meshes and lanes).
        /// </summary>
        public static void OnLoad()
        {
            // List of meshes that we've already checked.
            HashSet<Mesh> checkedMeshes = new HashSet<Mesh>();

            Logging.KeyMessage("starting path load processing");

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

                    // Only looking at pedestrian way prefabs.
                    if (network.m_netAI is PedestrianWayAI netAI)
                    {
                        // Dirty flag.
                        bool netAltered = false;

                        // Network record for this prefab.
                        NetRecord netRecord = new NetRecord();

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
                            if (shaderName != "Custom/Net/Road")
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

                                // Eligibile target; record original value.
                                netAltered = true;
                                netRecord.segmentDict.Add(segment, new NetComponentRecord
                                {
                                    netInfo = network,
                                    mainVerts = segmentMesh.vertices,
                                    lodVerts = segment.m_lodMesh.vertices
                                });

                                AdjustMesh(segment.m_segmentMesh);
                                if (DoLODs)
                                {
                                    AdjustMesh(segment.m_lodMesh);
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
                            if (shaderName != "Custom/Net/Road")
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

                                // Eligibile target; record original value.
                                netAltered = true;
                                netRecord.nodeDict.Add(node, new NetComponentRecord
                                {
                                    netInfo = network,
                                    mainVerts = nodeMesh.vertices,
                                    lodVerts = node.m_lodMesh.vertices
                                });

                                // Adjust vertices.
                                AdjustMesh(node.m_nodeMesh);
                                if (DoLODs)
                                {
                                    AdjustMesh(node.m_lodMesh);
                                }
                            }
                        }

                        // If the net was altered, record the created netRecord.
                        if (netAltered)
                        {
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

            // Clear processed mesh list once done.
            processedMeshes.Clear();

            Logging.KeyMessage("finished load processing");
        }


        /// <summary>
        /// Reverts changes (back to original).
        /// </summary>
        internal static void Revert()
        {
            Logging.Message("PathHandler.Revert");
            Logging.KeyMessage("reverting changes");

            // Iterate through all network records in dictionary.
            foreach (KeyValuePair<NetInfo, NetRecord> netEntry in netRecords)
            {
                Logging.Message("reverting ", netEntry.Key.name);

                // Local references.
                NetInfo netInfo = netEntry.Key;
                NetRecord netRecord = netEntry.Value;

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
            }
        }


        /// <summary>
        /// Applies updated settings.
        /// </summary>
        internal static void Apply()
        {
            Logging.Message("PathHandler.Apply");

            // Ensure processed mesh list is clear, just in case.
            processedMeshes.Clear();

            // Iterate through all network records in dictionary.
            foreach (KeyValuePair<NetInfo, NetRecord> netEntry in netRecords)
            {
                // Local references.
                NetInfo netInfo = netEntry.Key;
                NetRecord netRecord = netEntry.Value;

                // Update segment vertices.
                foreach (KeyValuePair<NetInfo.Segment, NetComponentRecord> segmentEntry in netRecord.segmentDict)
                {
                    // Restore original vertices and then raise mesh.
                    NetInfo.Segment segment = segmentEntry.Key;
                    segment.m_segmentMesh.vertices = segmentEntry.Value.mainVerts;
                    segment.m_lodMesh.vertices = segmentEntry.Value.lodVerts;
                    AdjustMesh(segment.m_segmentMesh);
                    if (DoLODs)
                    {
                        AdjustMesh(segment.m_lodMesh);
                    }
                }

                // Update node vertices.
                foreach (KeyValuePair<NetInfo.Node, NetComponentRecord> nodeEntry in netRecord.nodeDict)
                {
                    // Restore original vertices and then raise mesh.
                    NetInfo.Node node = nodeEntry.Key;
                    node.m_nodeMesh.vertices = nodeEntry.Value.mainVerts;
                    node.m_lodMesh.vertices = nodeEntry.Value.lodVerts;
                    AdjustMesh(node.m_nodeMesh);

                    // Update LODs if set to do so.
                    if (DoLODs)
                    {
                        AdjustMesh(node.m_lodMesh);
                    }
                }
            }

            // Clear processed mesh list once done.
            processedMeshes.Clear();
        }


        /// <summary>
        /// Adjusts the given mesh in line with current settings (curb height).
        /// </summary>
        /// <param name="mesh">Mesh to modify</param>
        private static void AdjustMesh(Mesh mesh)
        {
            // Check if we've already done this one.
            if (processedMeshes.Contains(mesh))
            {
                // Already processed this mesh - do nothing.
                return;
            }

            // Create new vertices array (changing individual elements within the existing array won't work with locked meshes).
            Vector3[] newVertices = new Vector3[mesh.vertices.Length];
            mesh.vertices.CopyTo(newVertices, 0);

            // Raise verticies; anything below ground level (but above the maximum depth trigger - allow for bridges etc.) has its y-value multiplied for proportional adjustment.
            for (int i = 0; i < newVertices.Length; ++i)
            {
                // Adjust any eligible curb vertices.
                newVertices[i].y *= newPathMultiplier;
            }

            mesh.vertices = newVertices;

            // Record mesh as being altered.
            processedMeshes.Add(mesh);
        }
    }
}