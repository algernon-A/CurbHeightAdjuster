using ColossalFramework.UI;


namespace CurbHeightAdjuster
{
    /// <summary>
    /// Options panel for setting pedestrian path-related options.
    /// </summary>
    internal class PathOptionsPanel
    {
        // Panel components.
        private UISlider baseSlider, curbSlider;
        private UICheckBox lodCheck;


        /// <summary>
        /// Adds mod options tab to tabstrip.
        /// </summary>
        /// <param name="tabStrip">Tab strip to add to</param>
        /// <param name="tabIndex">Index number of tab</param>
        internal PathOptionsPanel(UITabstrip tabStrip, int tabIndex)
        {
            // Add tab and helper.
            UIPanel panel = OptionsPanelUtils.AddTab(tabStrip, Translations.Translate("CHA_OPT_PAT"), tabIndex);

            // Y position indicator.
            float currentY = OptionsPanelUtils.GroupMargin;

            // Enable path manipulations checkbox.
            UICheckBox pathCheck = UIControls.AddPlainCheckBox(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("CHA_PAT_ENA"));
            pathCheck.isChecked = PathHandler.customizePaths;
            pathCheck.eventCheckChanged += PathCheckChanged;
            currentY += pathCheck.height + OptionsPanelUtils.GroupMargin;

            // Base level slider slider.
            baseSlider = OptionsPanelUtils.AddDepthSlider(panel, ref currentY, "CHA_PAT_BAS", PathHandler.MinBaseHeight, PathHandler.MaxBaseHeight, PathHandler.BaseHeight);
            baseSlider.eventValueChanged += (control, value) => PathHandler.BaseHeight = value;

            // Curb height slider.
            curbSlider = OptionsPanelUtils.AddDepthSlider(panel, ref currentY, "CHA_PAT_CUR", PathHandler.MinCurbHeight, PathHandler.MaxCurbHeight, PathHandler.CurbHeight);
            curbSlider.eventValueChanged += (control, value) => PathHandler.CurbHeight = value;

            // LOD checkbox.
            lodCheck = UIControls.AddPlainCheckBox(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("CHA_LOD"));
            lodCheck.isChecked = PathHandler.DoLODs;
            lodCheck.eventCheckChanged += (control, isChecked) => { PathHandler.DoLODs = isChecked; };
            currentY += lodCheck.height + OptionsPanelUtils.GroupMargin;

            // Reset to deafults button.
            UIButton defaultsButton = UIControls.AddButton(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("CHA_DEFAULT"), OptionsPanelUtils.ButtonWidth);
            defaultsButton.eventClicked += (control, clickEvent) =>
            {
                // Set controls to default settings.
                baseSlider.value = PathHandler.DefaultBaseHeight;
                curbSlider.value = PathHandler.DefaultCurbHeight;

                // Apply defaults.
                PathHandler.Apply();
            };
            currentY += defaultsButton.height + OptionsPanelUtils.Margin;

            // Only show undo and apply buttons if in-game.
            if (ColossalFramework.Singleton<LoadingManager>.instance.m_loadingComplete)
            {
                // Apply button.
                UIButton applyButton = UIControls.AddButton(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("CHA_APPLY"), OptionsPanelUtils.ButtonWidth);
                applyButton.eventClicked += (control, clickEvent) => PathHandler.Apply();
                currentY += applyButton.height + OptionsPanelUtils.Margin;

                // Undo button.
                UIButton undoButton = UIControls.AddButton(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("CHA_REVERT"), OptionsPanelUtils.ButtonWidth);
                undoButton.eventClicked += (control, clickEvent) => PathHandler.Revert();
            }

            // Set initial control states.
            UpdateControlStates(pathCheck.isChecked);

        }


        /// <summary>
        /// Enable path customizations checkbox event handler
        /// </summary>
        /// <param name="control">Calling component (unused)</param>
        /// <param name="isChecked">New checked state</param>
        private void PathCheckChanged(UIComponent control, bool isChecked)
        {
            // Update state.
            PathHandler.customizePaths = isChecked;

            // Refresh control state.
            UpdateControlStates(isChecked);
        }


        /// <summary>
        /// Updates control visibility according to current settings.
        /// </summary>
        /// <param name="pathsEnabled">True if path customizations are enabled, false if disabled</param>
        private void UpdateControlStates(bool pathsEnabled)
        {
            baseSlider.parent.isVisible = pathsEnabled;
            curbSlider.parent.isVisible = pathsEnabled;
            lodCheck.isVisible = pathsEnabled;
        }
    }
}