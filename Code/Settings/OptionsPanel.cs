﻿// <copyright file="OptionsPanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace CurbHeightAdjuster
{
    using AlgernonCommons.UI;
    using ColossalFramework.UI;

    /// <summary>
    /// The mod's settings options panel.
    /// </summary>
    public class OptionsPanel : UIPanel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OptionsPanel"/> class.
        /// </summary>
        internal OptionsPanel()
        {
            // Add tabstrip.
            // Add tabstrip.
            UITabstrip tabstrip = UITabstrips.AddTabstrip(this, 0f, 0f, OptionsPanelManager<OptionsPanel>.PanelWidth, OptionsPanelManager<OptionsPanel>.PanelHeight, out _);

            // Add tabs and panels.
            new GeneralOptionsPanel(tabstrip, 0);
            new RoadOptionsPanel(tabstrip, 1);
            new PathOptionsPanel(tabstrip, 2);

            // Ensure initial selected tab (doing a 'quickstep' to ensure proper events are triggered).
            tabstrip.selectedIndex = -1;
            tabstrip.selectedIndex = 0;
        }
    }
}