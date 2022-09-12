// <copyright file="Pillars.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace CurbHeightAdjuster
{
    using System;
    using System.Runtime.CompilerServices;
    using AlgernonCommons;
    using ColossalFramework;
    using HarmonyLib;

    /// <summary>
    /// Class to adjust network pillars.
    /// </summary>
    [HarmonyPatch]
    public static class Pillars
    {
        /// <summary>
        /// Gets or sets a value indicating whether automatic update of bridge pillars is enabled.
        /// </summary>
        internal static bool AutoUpdate { get; set; } = true;

        /// <summary>
        /// Adjusts pillar height on existing bridge nodes to match current settings.
        /// Called via transpiler insert.
        /// </summary>
        internal static void AdjustPillars()
        {
            // Don't do anything if auto-update is disabled.
            if (!AutoUpdate)
            {
                return;
            }

            Logging.KeyMessage("adjusting existing pillars");

            // Perform action via simulation thread.
            Singleton<SimulationManager>.instance.AddAction(() =>
            {
                // Local reference.
                NetManager netManager = Singleton<NetManager>.instance;

                // Iterate through all networks  in list.
                NetNode[] nodes = Singleton<NetManager>.instance.m_nodes.m_buffer;
                Building[] buildings = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
                for (uint i = 0; i < nodes.Length; ++i)
                {
                    // Skip uncreated nodes or nodes with  no building attached.
                    if ((nodes[i].m_flags & NetNode.Flags.Created) == NetNode.Flags.None || nodes[i].m_building == 0 || (buildings[nodes[i].m_building].m_flags & Building.Flags.Created) == 0)
                    {
                        continue;
                    }

                    // Skip any nodes with null infos.
                    NetInfo info = nodes[i].Info;
                    if (info?.name == null)
                    {
                        continue;
                    }

                    // Check for road bridge AI.
                    if (info.m_netAI is RoadBridgeAI bridgeAI)
                    {
                        // Only deal with networks with a valid adjustment.
                        if (RoadHandler.NetRecords.ContainsKey(info))
                        {
                            Logging.Message("adjusting pillars for node ", i, ": ", info.name);

                            // Reset pillar height via reverse patch call to NetNode.CheckHeightOffset.
                            CheckHeightOffset(ref nodes[i], (ushort)i);

                            // Additional manual pillar adjustment if needed.
                            info.m_netAI.GetNodeBuilding((ushort)i, ref Singleton<NetManager>.instance.m_nodes.m_buffer[i], out BuildingInfo building, out float heightOffset);
                            netManager.m_nodes.m_buffer[i].UpdateBuilding((ushort)i, building, heightOffset);
                            netManager.UpdateNodeFlags((ushort)i);
                            netManager.UpdateNodeRenderer((ushort)i, updateGroup: true);
                            for (int j = 0; j < 8; j++)
                            {
                                ushort segment = netManager.m_nodes.m_buffer[i].GetSegment(j);
                                if (segment != 0)
                                {
                                    netManager.m_segments.m_buffer[segment].UpdateLanes(segment, loading: false);
                                    netManager.UpdateSegmentFlags(segment);
                                    netManager.UpdateSegmentRenderer(segment, updateGroup: true);
                                }
                            }
                        }
                    }
                }

                Logging.Message("finished adjusting pillars");
            });
        }

        /// <summary>
        /// Harmony reverse patch to access private method NetNode.CheckHeightOffset.
        /// </summary>
        /// <param name="instance">NetNode instance.</param>
        /// <param name="nodeID">NetNode ID.</param>
        /// <exception cref="NotImplementedException">Reverse patch wasn't applied.</exception>
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(NetNode), "CheckHeightOffset")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CheckHeightOffset(ref NetNode instance, ushort nodeID)
        {
            string message = "CheckHeightOffset reverse Harmony patch wasn't applied ";
            Logging.Error(message, instance, nodeID);
            throw new NotImplementedException(message);
        }
    }
}