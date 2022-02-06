using UnityEngine;
using ColossalFramework.UI;


namespace CurbHeightAdjuster
{
    /// <summary>
    /// Curb options panel.
    /// </summary>
    internal class CurbOptions
    {
        /// <summary>
        /// Adds curb options tab to tabstrip.
        /// </summary>
        /// <param name="tabStrip">Tab strip to add to</param>
        /// <param name="tabIndex">Index number of tab</param>
        internal CurbOptions(UITabstrip tabStrip, int tabIndex)
        {
            // Add tab and helper.
            UIPanel panel = OptionsPanelUtils.AddTab(tabStrip, Translations.Translate("CHA_OPT_CUR"), tabIndex, false);

            // Add controls.
            // Y position indicator.
            float currentY = OptionsPanelUtils.Margin;

            // Curb depth slider.
            UISlider depthSlider = OptionsPanelUtils.AddDepthSlider(panel, ref currentY, "CHA_HEIGHT", CurbHeight.MinCurbHeight, CurbHeight.MaxCurbHeight, CurbHeight.NewCurbHeight);

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