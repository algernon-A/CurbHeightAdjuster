// <copyright file="PathHandler.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace CurbHeightAdjuster
{
    using System;
    using System.Collections.Generic;
    using AlgernonCommons;
    using UnityEngine;

    /// <summary>
    /// Class to manage changes to pedestrian paths.
    /// </summary>
    public static class PathHandler
    {
        /// <summary>
        /// Default new path base height.
        /// </summary>
        internal const float DefaultBaseHeight = 0.05f;

        /// <summary>
        /// Default new path curb height.
        /// </summary>
        internal const float DefaultCurbHeight = 0.05f;

        /// <summary>
        /// Minimum permissible path base height.
        /// </summary>
        internal const float MinBaseHeight = 0.01f;

        /// <summary>
        /// Maximum permissible path base height.
        /// </summary>
        internal const float MaxBaseHeight = 0.10f;

        /// <summary>
        /// Minimum permissible path curb height.
        /// </summary>
        internal const float MinCurbHeight = 0.01f;

        /// <summary>
        /// Maximum permissible path curb height.
        /// </summary>
        internal const float MaxCurbHeight = 0.10f;

        // Original heights.
        private const float OriginalBaseHeight = 0.10f;
        private const float OriginalCurbHeight = 0.10f;

        // Trigger threshold.
        private const float MaxBaseThreshold = 0.11f;

        // Dictionary of altered nets.
        private static readonly Dictionary<NetInfo, NetRecord> NetRecords = new Dictionary<NetInfo, NetRecord>();

        // Hashset of currently processed network meshes, with calculated adjustment offsets.
        private static readonly HashSet<Mesh> ProcessedMeshes = new HashSet<Mesh>();

        // Path height multiiplier.
        private static float baseMultiplier = DefaultBaseHeight / OriginalBaseHeight;

        // New heights to apply.
        private static float s_baseHeight = DefaultBaseHeight;
        private static float s_curbHeight = DefaultCurbHeight;

        /// <summary>
        /// Gets or sets a value indicating whether custom path manipulations are enabled.
        /// </summary>
        internal static bool CustomizePaths { get; set; }

        /// <summary>
        /// Gets or sets the new base height to apply (positive figure, in cm).
        /// </summary>
        internal static float BaseHeight
        {
            get => s_baseHeight;

            set
            {
                // Update multiplier with change in value.
                s_baseHeight = Mathf.Clamp(value, MinBaseHeight, MaxBaseHeight);
                baseMultiplier = s_baseHeight / OriginalBaseHeight;
            }
        }

        /// <summary>
        /// Gets or sets the new curb height to apply (positive figure, in cm).
        /// </summary>
        internal static float CurbHeight
        {
            get => s_curbHeight;

            set
            {
                // Update multiplier with change in value.
                s_curbHeight = Mathf.Clamp(value, MinCurbHeight, MaxCurbHeight);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether lods are also adjusted.
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
                                netRecord.m_segmentDict.Add(segment, new NetComponentRecord
                                {
                                    NetInfo = network,
                                    MainVerts = segmentMesh.vertices,
                                    LodVerts = segment.m_lodMesh.vertices,
                                });

                                // Apply adjustments, if we're doing so.
                                if (CustomizePaths)
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
                                netRecord.m_nodeDict.Add(node, new NetComponentRecord
                                {
                                    NetInfo = network,
                                    MainVerts = nodeMesh.vertices,
                                    LodVerts = node.m_lodMesh.vertices,
                                });

                                // Apply adjustments, if we're doing so.
                                if (CustomizePaths)
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
                            NetRecords.Add(network, netRecord);
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
            ProcessedMeshes.Clear();

            Logging.KeyMessage("finished load processing");
        }

        /// <summary>
        /// Reverts changes (back to original).
        /// </summary>
        internal static void Revert()
        {
            // Iterate through all network records in dictionary.
            foreach (KeyValuePair<NetInfo, NetRecord> netEntry in NetRecords)
            {
                // Local references.
                NetInfo netInfo = netEntry.Key;
                NetRecord netRecord = netEntry.Value;

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
            }
        }

        /// <summary>
        /// Applies updated settings.
        /// </summary>
        internal static void Apply()
        {
            // Ensure processed mesh list is clear, just in case.
            ProcessedMeshes.Clear();

            // Iterate through all network records in dictionary.
            foreach (KeyValuePair<NetInfo, NetRecord> netEntry in NetRecords)
            {
                // Local references.
                NetInfo netInfo = netEntry.Key;
                NetRecord netRecord = netEntry.Value;

                // Whether or not to raise zero-level.
                bool raiseZero = netInfo.m_netAI is PedestrianBridgeAI || netInfo.m_netAI is RoadBridgeAI;

                // Update segment vertices.
                foreach (KeyValuePair<NetInfo.Segment, NetComponentRecord> segmentEntry in netRecord.m_segmentDict)
                {
                    // Restore original vertices.
                    NetInfo.Segment segment = segmentEntry.Key;
                    segment.m_segmentMesh.vertices = segmentEntry.Value.MainVerts;
                    segment.m_lodMesh.vertices = segmentEntry.Value.LodVerts;

                    // Apply adjustments, if we're doing so.
                    if (CustomizePaths)
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
                foreach (KeyValuePair<NetInfo.Node, NetComponentRecord> nodeEntry in netRecord.m_nodeDict)
                {
                    // Restore original vertices.
                    NetInfo.Node node = nodeEntry.Key;
                    node.m_nodeMesh.vertices = nodeEntry.Value.MainVerts;
                    node.m_lodMesh.vertices = nodeEntry.Value.LodVerts;

                    // Apply adjustments, if we're doing so.
                    if (CustomizePaths)
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
            ProcessedMeshes.Clear();
        }

        /// <summary>
        /// Adjusts the given mesh in line with current settings (pavement and curb height).
        /// </summary>
        /// <param name="mesh">Mesh to modify.</param>
        /// <param name="raiseZero">Set to true to raise zero-level vertices to the new pavement height (typically for elevated segments to match ground pavement height).</param>
        private static void AdjustMesh(Mesh mesh, bool raiseZero)
        {
            // Create new vertices array (changing individual elements within the existing array won't work with locked meshes).
            Vector3[] newVertices = new Vector3[mesh.vertices.Length];
            mesh.vertices.CopyTo(newVertices, 0);

            // Check if we've already done this one.
            if (ProcessedMeshes.Contains(mesh))
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
                    newVertices[i].y += s_baseHeight;
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
                    newVertices[i].y = thisY - OriginalBaseHeight - OriginalCurbHeight + s_baseHeight + s_curbHeight;
                }
            }

            mesh.vertices = newVertices;

            // Record mesh as being altered.
            ProcessedMeshes.Add(mesh);
        }
    }
}