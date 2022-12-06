// <copyright file="RoadOptionsPanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace CurbHeightAdjuster
{
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;

    /// <summary>
    /// Options panel for setting road-related mod options.
    /// </summary>
    internal class RoadOptionsPanel
    {
        // Panel components.
        private readonly UISlider _thresholdSlider;
        private readonly UISlider _multiplierSlider;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoadOptionsPanel"/> class.
        /// </summary>
        /// <param name="tabStrip">Tab strip to add to.</param>
        /// <param name="tabIndex">Index number of tab.</param>
        internal RoadOptionsPanel(UITabstrip tabStrip, int tabIndex)
        {
            // Add tab and helper.
            UIPanel panel = UITabstrips.AddTextTab(tabStrip, Translations.Translate("OPTIONS_ROADS"), tabIndex, out UIButton _);

            // Y position indicator.
            float currentY = OptionsPanelUtils.GroupMargin;

            // Curb depth slider.
            UISlider depthSlider = OptionsPanelUtils.AddDepthSlider(panel, ref currentY, "CURB_HEIGHT", RoadHandler.MinCurbHeight, RoadHandler.MaxCurbHeight, RoadHandler.NewCurbHeight);
            depthSlider.eventValueChanged += (control, value) => RoadHandler.NewCurbHeight = value;

            // Spacer.
            UISpacers.AddOptionsSpacer(panel, OptionsPanelUtils.Margin, currentY, panel.width - (OptionsPanelUtils.Margin * 2f));
            currentY += OptionsPanelUtils.GroupMargin;

            // Enable bridge mesh checkbox.
            UICheckBox bridgeCheck = UICheckBoxes.AddPlainCheckBox(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("DO_BRIDGES"));
            bridgeCheck.isChecked = RoadHandler.EnableBridges;
            bridgeCheck.eventCheckChanged += BridgeCheckChanged;
            currentY += bridgeCheck.height + OptionsPanelUtils.Margin;

            // Adjust exiting bridge pillars checkbox.
            UICheckBox pillarCheck = UICheckBoxes.AddPlainCheckBox(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("ADJUST_PILLARS"));
            pillarCheck.isChecked = Pillars.AutoUpdate;
            pillarCheck.eventCheckChanged += (c, isChecked) => { Pillars.AutoUpdate = isChecked; };
            currentY += pillarCheck.height + OptionsPanelUtils.GroupMargin;

            // Brige min threshold slider.
            _thresholdSlider = OptionsPanelUtils.AddDepthSlider(panel, ref currentY, "BRIDGE_THRESHOLD", RoadHandler.MinBridgeThreshold, RoadHandler.MaxBridgeThreshold, RoadHandler.BridgeHeightThreshold);
            _thresholdSlider.eventValueChanged += (c, value) => RoadHandler.BridgeHeightThreshold = value;

            // Bridge depth multiplier slider.
            _multiplierSlider = OptionsPanelUtils.AddPercentageSlider(panel, ref currentY, "BRIDGE_SCALE", RoadHandler.MinBridgeScale, RoadHandler.MaxBridgeScale, RoadHandler.BridgeHeightScale);
            _multiplierSlider.eventValueChanged += (c, value) => RoadHandler.BridgeHeightScale = value;

            // LOD checkbox.
            UICheckBox lodCheck = UICheckBoxes.AddPlainCheckBox(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("DO_LODS"));
            lodCheck.isChecked = RoadHandler.DoLODs;
            lodCheck.eventCheckChanged += (c, isChecked) => { RoadHandler.DoLODs = isChecked; };
            currentY += lodCheck.height + OptionsPanelUtils.Margin;

            // Catenary checkbox.
            UICheckBox tramCatCheck = UICheckBoxes.AddPlainCheckBox(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("DO_TRAM_WIRES"));
            tramCatCheck.isChecked = RoadHandler.DoTramCatenaries;
            tramCatCheck.eventCheckChanged += (c, isChecked) => { RoadHandler.DoTramCatenaries = isChecked; };
            currentY += tramCatCheck.height + OptionsPanelUtils.GroupMargin;

            // Reset to defaults button.
            UIButton defaultsButton = UIButtons.AddButton(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("DEFAULTS"), OptionsPanelUtils.ButtonWidth);
            defaultsButton.eventClicked += (c, p) =>
            {
                // Set controls to default settings.
                depthSlider.value = RoadHandler.DefaultNewCurbHeight;
                _thresholdSlider.value = -RoadHandler.DefaultBridgeThreshold;
                _multiplierSlider.value = RoadHandler.DefaultBridgeMultiplier;

                // Apply defaults.
                NetHandler.RoadHandler.Apply();
            };
            currentY += defaultsButton.height + OptionsPanelUtils.Margin;

            // Only show undo and apply buttons if in-game.
            if (ColossalFramework.Singleton<LoadingManager>.instance.m_loadingComplete)
            {
                // Apply button.
                UIButton applyButton = UIButtons.AddButton(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("APPLY"), OptionsPanelUtils.ButtonWidth);
                applyButton.eventClicked += (c, p) => NetHandler.RoadHandler.Apply();
                currentY += applyButton.height + OptionsPanelUtils.Margin;

                // Undo button.
                UIButton undoButton = UIButtons.AddButton(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("REVERT"), OptionsPanelUtils.ButtonWidth);
                undoButton.eventClicked += (c, p) => NetHandler.RoadHandler.Revert();
            }
        }

        /// <summary>
        /// Event handler for enable bridge checkbox changes.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="isChecked">New checked state.</param>
        private void BridgeCheckChanged(UIComponent c, bool isChecked)
        {
            // Update settings.
            RoadHandler.EnableBridges = isChecked;

            // Adjust component visibility.
            _thresholdSlider.parent.isVisible = isChecked;
            _multiplierSlider.parent.isVisible = isChecked;
        }
    }
}