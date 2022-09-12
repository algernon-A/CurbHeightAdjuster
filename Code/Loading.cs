// <copyright file="Loading.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

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
                foreach (KeyValuePair<NetInfo, NetRecord> netEntry in RoadHandler.NetRecords)
                {
                    // Local references.
                    NetInfo netInfo = netEntry.Key;
                    NetRecord netRecord = netEntry.Value;

                    // Reset any recorded pillar offset.
                    netRecord.m_bridgePillarOffset = 0;

                    // Adjust pillar heights to match net adjustment.
                    if (netRecord.m_adjustPillars && netInfo.m_netAI is RoadBridgeAI bridgeAI)
                    {
                        bridgeAI.m_bridgePillarOffset = RoadHandler.BridgeAdjustment(netRecord.m_bridgePillarOffset);
                        bridgeAI.m_middlePillarOffset = RoadHandler.BridgeAdjustment(netRecord.m_middlePillarOffset);
                    }
                }

                // Apply adjustments.
                Pillars.AdjustPillars();
            }
        }
    }
}