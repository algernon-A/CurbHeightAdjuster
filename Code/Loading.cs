namespace CurbHeightAdjuster
{
    using System.Collections.Generic;
    using AlgernonCommons.Patching;
    using ICities;

    /// <summary>
    /// Main loading class: the mod runs from here.
    /// </summary>
    public sealed class Loading : PatcherLoadingBase<OptionsPanel, PatcherBase>
    {
        /// <summary>
        /// Performs any actions upon successful level loading completion.
        /// </summary>
        /// <param name="mode">Loading mode (e.g. game, editor, scenario, etc.).</param>
        protected override void LoadedActions(LoadMode mode)
        {
            base.LoadedActions(mode);

            // Adjust existing pillars, if we're doing so.
            if (RoadHandler.EnableBridges)
            {
                // Iterate through all network records in dictionary.
                foreach (KeyValuePair<NetInfo, NetRecord> netEntry in RoadHandler.netRecords)
                {
                    // Local references.
                    NetInfo netInfo = netEntry.Key;
                    NetRecord netRecord = netEntry.Value;

                    // Reset any recorded pillar offset.
                    netRecord.bridgePillarOffset = 0;

                    // Adjust pillar heights to match net adjustment.
                    if (netRecord.adjustPillars && netInfo.m_netAI is RoadBridgeAI bridgeAI)
                    {
                        bridgeAI.m_bridgePillarOffset = RoadHandler.BridgeAdjustment(netRecord.bridgePillarOffset);
                        bridgeAI.m_middlePillarOffset = RoadHandler.BridgeAdjustment(netRecord.middlePillarOffset);
                    }
                }

                // Apply adjustments.
                Pillars.AdjustPillars();
            }
        }
    }
}