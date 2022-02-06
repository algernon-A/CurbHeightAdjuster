using UnityEngine;
using ColossalFramework.UI;


namespace CurbHeightAdjuster
{
    /// <summary>
    /// Gerneral mod options panel.
    /// </summary>
    internal class GeneralOptions
    {
        /// <summary>
        /// Adds general options tab to tabstrip.
        /// </summary>
        /// <param name="tabStrip">Tab strip to add to</param>
        /// <param name="tabIndex">Index number of tab</param>
        internal GeneralOptions(UITabstrip tabStrip, int tabIndex)
        {
            // Add tab and helper.
            UIPanel panel = OptionsPanelUtils.AddTab(tabStrip, Translations.Translate("CHA_OPT_GEN"), tabIndex, false);

            // Add controls.
            // Y position indicator.
            float currentY = OptionsPanelUtils.Margin;

            // Language choice.
            UIDropDown languageDropDown = UIControls.AddPlainDropDown(panel, Translations.Translate("TRN_CHOICE"), Translations.LanguageList, Translations.Index);
            languageDropDown.eventSelectedIndexChanged += (control, index) =>
            {
                Translations.Index = index;
            };
            languageDropDown.parent.relativePosition = new Vector2(OptionsPanelUtils.LeftMargin, currentY);
            currentY += languageDropDown.parent.height + OptionsPanelUtils.GroupMargin;

            // LOD checkbox.
            UICheckBox lodCheck = UIControls.AddPlainCheckBox(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("CHA_LOD"));
            lodCheck.isChecked = CurbHeight.DoLODs;
            lodCheck.eventCheckChanged += (control, isChecked) => { CurbHeight.DoLODs = isChecked; };
            currentY += lodCheck.height + OptionsPanelUtils.GroupMargin;
        }
    }
}