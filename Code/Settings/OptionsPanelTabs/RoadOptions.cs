using ColossalFramework.UI;


namespace CurbHeightAdjuster
{
    /// <summary>
    /// Options panel for setting road-related mod options.
    /// </summary>
    internal class RoadOptionsPanel
    {
        // Panel components.
        UISlider thresholdSlider, multiplierSlider;


        /// <summary>
        /// Adds mod options tab to tabstrip.
        /// </summary>
        /// <param name="tabStrip">Tab strip to add to</param>
        /// <param name="tabIndex">Index number of tab</param>
        internal RoadOptionsPanel(UITabstrip tabStrip, int tabIndex)
        {
            // Add tab and helper.
            UIPanel panel = OptionsPanelUtils.AddTab(tabStrip, Translations.Translate("CHA_OPT_ROA"), tabIndex);
            
            // Y position indicator.
            float currentY = OptionsPanelUtils.GroupMargin;

            // Curb depth slider.
            UISlider depthSlider = OptionsPanelUtils.AddDepthSlider(panel, ref currentY, "CHA_HEIGHT", RoadHandler.MinCurbHeight, RoadHandler.MaxCurbHeight, RoadHandler.NewCurbHeight);
            depthSlider.eventValueChanged += (control, value) => RoadHandler.NewCurbHeight = value;

            // Spacer.
            UIControls.OptionsSpacer(panel, OptionsPanelUtils.Margin, currentY, panel.width - (OptionsPanelUtils.Margin * 2f));
            currentY += OptionsPanelUtils.GroupMargin;

            // Enable bridge mesh checkbox.
            UICheckBox bridgeCheck = UIControls.AddPlainCheckBox(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("CHA_BRI_ENA"));
            bridgeCheck.isChecked = RoadHandler.EnableBridges;
            bridgeCheck.eventCheckChanged += BridgeCheckChanged;
            currentY += bridgeCheck.height + OptionsPanelUtils.Margin;

            // Adjust exiting bridge pillars checkbox.
            UICheckBox pillarCheck = UIControls.AddPlainCheckBox(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("CHA_OPT_PIL"));
            pillarCheck.isChecked = Pillars.AutoUpdate;
            pillarCheck.eventCheckChanged += (control, isChecked) => { Pillars.AutoUpdate = isChecked; };
            currentY += pillarCheck.height + OptionsPanelUtils.GroupMargin;

            // Brige min threshold slider.
            thresholdSlider = OptionsPanelUtils.AddDepthSlider(panel, ref currentY, "CHA_BRI_THR", RoadHandler.MinBridgeThreshold, RoadHandler.MaxBridgeThreshold, RoadHandler.BridgeHeightThreshold);
            thresholdSlider.eventValueChanged += (control, value) => RoadHandler.BridgeHeightThreshold = value;

            // Bridge depth multiplier slider.
            multiplierSlider = OptionsPanelUtils.AddPercentageSlider(panel, ref currentY, "CHA_BRI_SCA", RoadHandler.MinBridgeScale, RoadHandler.MaxBridgeScale, RoadHandler.BridgeHeightScale);
            multiplierSlider.eventValueChanged += (control, value) => RoadHandler.BridgeHeightScale = value;

            // LOD checkbox.
            UICheckBox lodCheck = UIControls.AddPlainCheckBox(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("CHA_LOD"));
            lodCheck.isChecked = RoadHandler.DoLODs;
            lodCheck.eventCheckChanged += (control, isChecked) => { RoadHandler.DoLODs = isChecked; };
            currentY += lodCheck.height + OptionsPanelUtils.GroupMargin;


            // Reset to deafults button.
            UIButton defaultsButton = UIControls.AddButton(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("CHA_DEFAULT"), OptionsPanelUtils.ButtonWidth);
            defaultsButton.eventClicked += (control, clickEvent) =>
            {
                // Set controls to default settings.
                depthSlider.value = RoadHandler.DefaultNewCurbHeight;
                thresholdSlider.value = -RoadHandler.DefaultBridgeThreshold;
                multiplierSlider.value = RoadHandler.DefaultBridgeMultiplier;

                // Apply defaults.
                RoadHandler.Apply();
            };
            currentY += defaultsButton.height + OptionsPanelUtils.Margin;

            // Only show undo and apply buttons if in-game.
            if (ColossalFramework.Singleton<LoadingManager>.instance.m_loadingComplete)
            {
                // Apply button.
                UIButton applyButton = UIControls.AddButton(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("CHA_APPLY"), OptionsPanelUtils.ButtonWidth);
                applyButton.eventClicked += (control, clickEvent) => RoadHandler.Apply();
                currentY += applyButton.height + OptionsPanelUtils.Margin;

                // Undo button.
                UIButton undoButton = UIControls.AddButton(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("CHA_REVERT"), OptionsPanelUtils.ButtonWidth);
                undoButton.eventClicked += (control, clickEvent) => RoadHandler.Revert();
            }
        }


        /// <summary>
        /// Event handler for enable bridge checkbox changes.
        /// </summary>
        /// <param name="control">Calling component</param>
        /// <param name="isChecked">New checked state</param>
        private void BridgeCheckChanged(UIComponent component, bool isChecked)
        {
            // Update settings.
            RoadHandler.EnableBridges = isChecked;

            // Adjust component visibility.
            thresholdSlider.parent.isVisible = isChecked;
            multiplierSlider.parent.isVisible = isChecked;
        }

    }
}