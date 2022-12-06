// <copyright file="PathOptionsPanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace CurbHeightAdjuster
{
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;

    /// <summary>
    /// Options panel for setting pedestrian path-related options.
    /// </summary>
    internal class PathOptionsPanel
    {
        // Panel components.
        private readonly UISlider _baseSlider;
        private readonly UISlider _curbSlider;
        private readonly UICheckBox _lodCheck;

        /// <summary>
        /// Initializes a new instance of the <see cref="PathOptionsPanel"/> class.
        /// </summary>
        /// <param name="tabStrip">Tab strip to add to.</param>
        /// <param name="tabIndex">Index number of tab.</param>
        internal PathOptionsPanel(UITabstrip tabStrip, int tabIndex)
        {
            // Add tab and helper.
            UIPanel panel = UITabstrips.AddTextTab(tabStrip, Translations.Translate("CHA_OPT_PAT"), tabIndex, out UIButton _);

            // Y position indicator.
            float currentY = OptionsPanelUtils.GroupMargin;

            // Enable path manipulations checkbox.
            UICheckBox pathCheck = UICheckBoxes.AddPlainCheckBox(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("CHA_PAT_ENA"));
            pathCheck.isChecked = PathHandler.CustomizePaths;
            pathCheck.eventCheckChanged += PathCheckChanged;
            currentY += pathCheck.height + OptionsPanelUtils.GroupMargin;

            // Base level slider slider.
            _baseSlider = OptionsPanelUtils.AddDepthSlider(panel, ref currentY, "CHA_PAT_BAS", PathHandler.MinBaseHeight, PathHandler.MaxBaseHeight, PathHandler.BaseHeight);
            _baseSlider.eventValueChanged += (c, value) => PathHandler.BaseHeight = value;

            // Curb height slider.
            _curbSlider = OptionsPanelUtils.AddDepthSlider(panel, ref currentY, "CHA_PAT_CUR", PathHandler.MinCurbHeight, PathHandler.MaxCurbHeight, PathHandler.CurbHeight);
            _curbSlider.eventValueChanged += (c, value) => PathHandler.CurbHeight = value;

            // LOD checkbox.
            _lodCheck = UICheckBoxes.AddPlainCheckBox(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("CHA_LOD"));
            _lodCheck.isChecked = PathHandler.DoLODs;
            _lodCheck.eventCheckChanged += (c, isChecked) => { PathHandler.DoLODs = isChecked; };
            currentY += _lodCheck.height + OptionsPanelUtils.GroupMargin;

            // Reset to deafults button.
            UIButton defaultsButton = UIButtons.AddButton(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("CHA_DEFAULT"), OptionsPanelUtils.ButtonWidth);
            defaultsButton.eventClicked += (c, clickEvent) =>
            {
                // Set controls to default settings.
                _baseSlider.value = PathHandler.DefaultBaseHeight;
                _curbSlider.value = PathHandler.DefaultCurbHeight;

                // Apply defaults.
                NetHandler.PathHandler.Apply();
            };
            currentY += defaultsButton.height + OptionsPanelUtils.Margin;

            // Only show undo and apply buttons if in-game.
            if (ColossalFramework.Singleton<LoadingManager>.instance.m_loadingComplete)
            {
                // Apply button.
                UIButton applyButton = UIButtons.AddButton(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("CHA_APPLY"), OptionsPanelUtils.ButtonWidth);
                applyButton.eventClicked += (control, clickEvent) => NetHandler.PathHandler.Apply();
                currentY += applyButton.height + OptionsPanelUtils.Margin;

                // Undo button.
                UIButton undoButton = UIButtons.AddButton(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("CHA_REVERT"), OptionsPanelUtils.ButtonWidth);
                undoButton.eventClicked += (control, clickEvent) => NetHandler.PathHandler.Revert();
            }

            // Set initial control states.
            UpdateControlStates(pathCheck.isChecked);
        }

        /// <summary>
        /// Enable path customizations checkbox event handler.
        /// </summary>
        /// <param name="c">Calling component.</param>
        /// <param name="isChecked">New checked state.</param>
        private void PathCheckChanged(UIComponent c, bool isChecked)
        {
            // Update state.
            PathHandler.CustomizePaths = isChecked;

            // Refresh control state.
            UpdateControlStates(isChecked);
        }

        /// <summary>
        /// Updates control visibility according to current settings.
        /// </summary>
        /// <param name="pathsEnabled">True if path customizations are enabled, false if disabled.</param>
        private void UpdateControlStates(bool pathsEnabled)
        {
            _baseSlider.parent.isVisible = pathsEnabled;
            _curbSlider.parent.isVisible = pathsEnabled;
            _lodCheck.isVisible = pathsEnabled;
        }
    }
}