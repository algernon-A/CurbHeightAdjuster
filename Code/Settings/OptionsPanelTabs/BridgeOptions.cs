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

            // Enable bridge mesh checkbox.
            UICheckBox enableCheck = UIControls.AddPlainCheckBox(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("CHA_BRI_ENA"));
            enableCheck.isChecked = NetHandler.DoLODs;
            enableCheck.eventCheckChanged += (control, isChecked) => { NetHandler.EnableBridges = isChecked; };
            currentY += enableCheck.height + OptionsPanelUtils.GroupMargin;

            // Brige min threshold slider.
            UISlider threshholdSlider = OptionsPanelUtils.AddDepthSlider(panel, ref currentY, "CHA_BRI_THR", NetHandler.MinBridgeThreshold, NetHandler.MaxBridgeThreshold, NetHandler.BridgeHeightThreshold);
            threshholdSlider.eventValueChanged += (control, value) => NetHandler.BridgeHeightThreshold = value;

            // Bridge depth multiplier slider.
            UISlider multiplierSlider = OptionsPanelUtils.AddPercentageSlider(panel, ref currentY, "CHA_BRI_SCA", NetHandler.MinBridgeScale, NetHandler.MaxBridgeScale, NetHandler.BridgeHeightScale);
            multiplierSlider.eventValueChanged += (control, value) => NetHandler.BridgeHeightScale = value;

            // Apply button.
            UIButton applyButton = UIControls.AddButton(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("CHA_APPLY"), OptionsPanelUtils.ButtonWidth);
            applyButton.eventClicked += (control, clickEvent) => NetHandler.Apply();
            currentY += applyButton.height + OptionsPanelUtils.Margin;

            // Undo button.
            UIButton undoButton = UIControls.AddButton(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("CHA_REVERT"), OptionsPanelUtils.ButtonWidth);
            undoButton.eventClicked += (control, clickEvent) => NetHandler.Revert();
        }
    }
}