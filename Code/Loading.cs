using System.Collections.Generic;
using ICities;


namespace CurbHeightAdjuster
{
    /// <summary>
    /// Main loading class: the mod runs from here.
    /// </summary>
    public class Loading : LoadingExtensionBase
    {
        /// <summary>
        /// Called by the game when level loading is complete.
        /// </summary>
        /// <param name="mode">Loading mode (e.g. game, editor, scenario, etc.)</param>
        public override void OnLevelLoaded(LoadMode mode)
        {
            Logging.Message("loading");

            base.OnLevelLoaded(mode);

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

            // Set up options panel event handler (need to redo this now that options panel has been reset after loading into game).
            OptionsPanel.OptionsEventHook();

            // Display update notification.
            WhatsNew.ShowWhatsNew();

            Logging.Message("loading complete");
        }
    }
}