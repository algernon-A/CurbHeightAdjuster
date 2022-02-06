using UnityEngine;
using ColossalFramework.UI;


namespace CurbHeightAdjuster
{
    /// <summary>
    /// Bridge options panel.
    /// </summary>
    internal class BridgeOptions
    {
        /// <summary>
        /// Adds curb options tab to tabstrip.
        /// </summary>
        /// <param name="tabStrip">Tab strip to add to</param>
        /// <param name="tabIndex">Index number of tab</param>
        internal BridgeOptions(UITabstrip tabStrip, int tabIndex)
        {
            // Add tab and helper.
            UIPanel panel = OptionsPanelUtils.AddTab(tabStrip, Translations.Translate("CHA_OPT_BRI"), tabIndex, false);

            // Add controls.
            // Y position indicator.
            float currentY = OptionsPanelUtils.Margin;

            // Brige min threshold slider.
            UISlider threshholdSlider = OptionsPanelUtils.AddDepthSlider(panel, ref currentY, "CHA_BRI_THR", CurbHeight.MinBridgeThreshold, CurbHeight.MaxBridgeThreshold, CurbHeight.BridgeHeightThreshold);
            threshholdSlider.eventValueChanged += (control, value) => CurbHeight.BridgeHeightThreshold = value;

            // Bridge depth multiplier slider.
            UISlider multiplierSlider = OptionsPanelUtils.AddPercentageSlider(panel, ref currentY, "CHA_BRI_SCA", CurbHeight.MinBridgeScale, CurbHeight.MaxBridgeScale, CurbHeight.BridgeHeightScale);
            multiplierSlider.eventValueChanged += (control, value) => CurbHeight.BridgeHeightScale = value;

            // Apply button.
            UIButton applyButton = UIControls.AddButton(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("CHA_APPLY"), OptionsPanelUtils.ButtonWidth);
            applyButton.eventClicked += (control, clickEvent) => CurbHeight.Apply();
            currentY += applyButton.height + OptionsPanelUtils.Margin;

            // Undo button.
            UIButton undoButton = UIControls.AddButton(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("CHA_REVERT"), OptionsPanelUtils.ButtonWidth);
            undoButton.eventClicked += (control, clickEvent) => CurbHeight.Revert();
        }
    }
}