using System;
using System.Runtime.CompilerServices;
using ColossalFramework;
using HarmonyLib;


namespace CurbHeightAdjuster
{
    /// <summary>
    /// Class to adjust network pillars
    /// </summary>
    /// 
    [HarmonyPatch]
    public static class Pillars
    {
        /// <summary>
        /// Enables automatic update of bridge pillars when true.
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

            // Iterate through all networks  in list.
            NetNode[] nodes = Singleton<NetManager>.instance.m_nodes.m_buffer;
            for (uint i = 0; i < nodes.Length; ++i)
            {
                // Skip uncreated nodes or nodes with  no building attached.
                if ((nodes[i].m_flags & NetNode.Flags.Created) == NetNode.Flags.None || nodes[i].m_building == 0)
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
                if (info.m_netAI is RoadBridgeAI)
                {
                    // Only deal with networks with a valid adjustment.
                    if (NetHandler.netRecords.ContainsKey(info))
                    {
                        Logging.Message("adjusting pillars for node ", i, ": ", info.name);

                        // Reset pillar height via reverse patch call to NetNode.CheckHeightOffset.
                        CheckHeightOffset(ref nodes[i], (ushort)i);
                    }
                }
            }

            Logging.Message("finished adjusting pillars");
        }

        
        /// <summary>
        /// Harmony reverse patch to access private method NetNode.CheckHeightOffset.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="nodeID"></param>
        /// <exception cref="NotImplementedException"></exception>
        [HarmonyReversePatch]
        [HarmonyPatch((typeof(NetNode)), "CheckHeightOffset")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CheckHeightOffset(ref NetNode instance, ushort nodeID)
        {
            string message = "CheckHeightOffset reverse Harmony patch wasn't applied ";
            Logging.Error(message, instance, nodeID);
            throw new NotImplementedException(message);
        }
    }
}