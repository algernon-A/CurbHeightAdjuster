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
        private const float OriginalBaseHeight = 0.10f;
        private const float OriginalCurbHeight = 0.10f;

        // Default mod settings.
        internal const float DefaultBaseHeight = 0.05f;
        internal const float DefaultCurbHeight = 0.05f;

        // Thresholds.
        private const float MaxBaseThreshold = 0.11f;

        // Maximum bounds.
        internal const float MinBaseHeight = 0.01f;
        internal const float MaxBaseHeight = 0.10f;
        internal const float MinCurbHeight = 0.01f;
        internal const float MaxCurbHeight = 0.10f;


        // Activation flag.
        internal static bool customizePaths = true;

        // Path height multiiplier.
        private static float baseMultiplier = DefaultBaseHeight / OriginalBaseHeight;

        // Dictionary of altered nets.
        internal readonly static Dictionary<NetInfo, NetRecord> netRecords = new Dictionary<NetInfo, NetRecord>();

        // Hashset of currently processed network meshes, with calculated adjustment offsets.
        private readonly static HashSet<Mesh> processedMeshes = new HashSet<Mesh>();


        /// <summary>
        /// New base height to apply (positive figure, in cm).
        /// </summary>
        internal static float BaseHeight
        {
            get => baseHeight;

            set
            {
                // Update multiplier with change in value.
                baseHeight = Mathf.Clamp(value, MinBaseHeight, MaxBaseHeight);
                baseMultiplier = baseHeight / OriginalBaseHeight;
            }
        }
        private static float baseHeight = DefaultBaseHeight;


        /// <summary>
        /// New curb height to apply (positive figure, in cm).
        /// </summary>
        internal static float CurbHeight
        {
            get => curbHeight;

            set
            {
                // Update multiplier with change in value.
                curbHeight = Mathf.Clamp(value, MinCurbHeight, MaxCurbHeight);
            }
        }
        private static float curbHeight = DefaultCurbHeight;


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
                    if (network.m_netAI is PedestrianWayAI || network.m_netAI is PedestrianBridgeAI || network.name.StartsWith("2212198462"))
                    {
                        // Skip nature reserve paths.
                        if (network.name.StartsWith("Nature Reserve") || network.name.StartsWith("2212198462.Natural Park"))
                        {
                            continue;
                        }

                        // Dirty flag.
                        bool netAltered = false;

                        // Network record for this prefab.
                        NetRecord netRecord = new NetRecord();

                        // Whether or not to raise zero-level.
                        bool raiseZero = network.m_netAI is PedestrianBridgeAI || network.m_netAI is RoadBridgeAI;

                        // Raise segments - iterate through each segment in net.
                        foreach (NetInfo.Segment segment in network.m_segments)
                        {
                            // Skip segments with no mesh or material.
                            if (segment?.m_segmentMesh?.name == null || segment.m_segmentMaterial?.shader?.name == null)
                            {
                                continue;
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

                                // Apply adjustments, if we're doing so.
                                if (customizePaths)
                                {
                                    AdjustMesh(segment.m_segmentMesh, raiseZero);
                                    if (DoLODs)
                                    {
                                        AdjustMesh(segment.m_lodMesh, false);
                                    }
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

                                // Apply adjustments, if we're doing so.
                                if (customizePaths)
                                {
                                    AdjustMesh(node.m_nodeMesh, raiseZero);
                                    if (DoLODs)
                                    {
                                        AdjustMesh(node.m_lodMesh, false);
                                    }
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

                // Whether or not to raise zero-level.
                bool raiseZero = netInfo.m_netAI is PedestrianBridgeAI || netInfo.m_netAI is RoadBridgeAI;

                // Update segment vertices.
                foreach (KeyValuePair<NetInfo.Segment, NetComponentRecord> segmentEntry in netRecord.segmentDict)
                {
                    // Restore original vertices.
                    NetInfo.Segment segment = segmentEntry.Key;
                    segment.m_segmentMesh.vertices = segmentEntry.Value.mainVerts;
                    segment.m_lodMesh.vertices = segmentEntry.Value.lodVerts;

                    // Apply adjustments, if we're doing so.
                    if (customizePaths)
                    {
                        AdjustMesh(segment.m_segmentMesh, raiseZero);

                        // Update LODs if set to do so.
                        if (DoLODs)
                        {
                            AdjustMesh(segment.m_lodMesh, false);
                        }
                    }
                }

                // Update node vertices.
                foreach (KeyValuePair<NetInfo.Node, NetComponentRecord> nodeEntry in netRecord.nodeDict)
                {
                    // Restore original vertices.
                    NetInfo.Node node = nodeEntry.Key;
                    node.m_nodeMesh.vertices = nodeEntry.Value.mainVerts;
                    node.m_lodMesh.vertices = nodeEntry.Value.lodVerts;


                    // Apply adjustments, if we're doing so.
                    if (customizePaths)
                    {
                        AdjustMesh(node.m_nodeMesh, raiseZero);

                        // Update LODs if set to do so.
                        if (DoLODs)
                        {
                            AdjustMesh(node.m_lodMesh, false);
                        }
                    }
                }
            }

            // Clear processed mesh list once done.
            processedMeshes.Clear();
        }


        /// <summary>
        /// Adjusts the given mesh in line with current settings (pavement and curb height).
        /// </summary>
        /// <param name="mesh">Mesh to modify</param>
        /// <param name="raiseZero">Set to true to raise zero-level vertices to the new pavement height (typically for elevated segments to match ground pavement height)</param>
        private static void AdjustMesh(Mesh mesh, bool raiseZero)
        {
            // Create new vertices array (changing individual elements within the existing array won't work with locked meshes).
            Vector3[] newVertices = new Vector3[mesh.vertices.Length];
            mesh.vertices.CopyTo(newVertices, 0);

            // Check if we've already done this one.
            if (processedMeshes.Contains(mesh))
            {
                // Already processed this mesh - do nothing.
                return;
            }

            // Watch out for mesh.name.Equals("ZooPath01Node_0") || mesh.name.Equals("ZooPath01Node_0_0").
            // Raise verticies; anything below ground level (but above the maximum depth trigger - allow for bridges etc.) has its y-value multiplied for proportional adjustment.
            for (int i = 0; i < newVertices.Length; ++i)
            {
                float thisY = newVertices[i].y;

                // Ignore anything less than 0.
                if (thisY < -0.01f)
                {
                    continue;
                }

                // Adjust pavement height if we're doing this.
                if (raiseZero && thisY < 0.01f)
                {
                    newVertices[i].y += baseHeight;
                    continue;
                }

                // Base or curb?
                if (thisY < MaxBaseThreshold)
                {
                    // Base - adjust with base multiplier.
                    newVertices[i].y = thisY * baseMultiplier;
                }
                else
                {
                    // Curb - adjust from new base.
                    newVertices[i].y = thisY - OriginalBaseHeight - OriginalCurbHeight + baseHeight + curbHeight;
                }
            }

            mesh.vertices = newVertices;

            // Record mesh as being altered.
            processedMeshes.Add(mesh);
        }
    }
}