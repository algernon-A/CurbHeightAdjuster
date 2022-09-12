// <copyright file="GeneralOptionsPanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace CurbHeightAdjuster
{
    using AlgernonCommons;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;

    /// <summary>
    /// Options panel for setting general mod options.
    /// </summary>
    internal class GeneralOptionsPanel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GeneralOptionsPanel"/> class.
        /// </summary>
        /// <param name="tabStrip">Tab strip to add to.</param>
        /// <param name="tabIndex">Index number of tab.</param>
        internal GeneralOptionsPanel(UITabstrip tabStrip, int tabIndex)
        {
            // Add tab and helper.
            UIPanel panel = UITabstrips.AddTextTab(tabStrip, Translations.Translate("CHA_OPT_GEN"), tabIndex, out UIButton _);

            // Y position indicator.
            float currentY = OptionsPanelUtils.GroupMargin;

            // Language choice.
            UIDropDown languageDropDown = UIDropDowns.AddPlainDropDown(panel, OptionsPanelUtils.LeftMargin, currentY, Translations.Translate("TRN_CHOICE"), Translations.LanguageList, Translations.Index);
            languageDropDown.eventSelectedIndexChanged += (control, index) =>
            {
                Translations.Index = index;
                OptionsPanelManager<OptionsPanel>.LocaleChanged();
            };
            currentY += languageDropDown.parent.height + OptionsPanelUtils.GroupMargin;

            // LOD checkbox.
            UICheckBox logCheck = UICheckBoxes.AddPlainCheckBox(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("CHA_OPT_LOG"));
            logCheck.isChecked = Logging.DetailLogging;
            logCheck.eventCheckChanged += (control, isChecked) => { Logging.DetailLogging = isChecked; };
        }
    }
}