using UnityEngine;
using ColossalFramework.UI;


namespace CurbHeightAdjuster
{
    /// <summary>
    /// Options panel for setting general mod options.
    /// </summary>
    internal class GeneralOptionsPanel
    {
        /// <summary>
        /// Adds mod options tab to tabstrip.
        /// </summary>
        /// <param name="tabStrip">Tab strip to add to</param>
        /// <param name="tabIndex">Index number of tab</param>
        internal GeneralOptionsPanel(UITabstrip tabStrip, int tabIndex)
        {
            // Add tab and helper.
            UIPanel panel = OptionsPanelUtils.AddTab(tabStrip, Translations.Translate("CHA_OPT_GEN"), tabIndex);

            // Y position indicator.
            float currentY = OptionsPanelUtils.Margin;

            // Language choice.
            UIDropDown languageDropDown = UIControls.AddPlainDropDown(panel, Translations.Translate("TRN_CHOICE"), Translations.LanguageList, Translations.Index);
            languageDropDown.eventSelectedIndexChanged += (control, index) =>
            {
                Translations.Index = index;
                OptionsPanel.LocaleChanged();
            };
            languageDropDown.parent.relativePosition = new Vector2(OptionsPanelUtils.LeftMargin, currentY);
            currentY += languageDropDown.parent.height + OptionsPanelUtils.GroupMargin;
        }
    }
}