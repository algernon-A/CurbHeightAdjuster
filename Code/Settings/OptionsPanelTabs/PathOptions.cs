using ColossalFramework.UI;


namespace CurbHeightAdjuster
{
    /// <summary>
    /// Options panel for setting pedestrian path-related options.
    /// </summary>
    internal class PathOptionsPanel
    {
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
            float currentY = OptionsPanelUtils.Margin;

            // Curb depth slider.
            UISlider depthSlider = OptionsPanelUtils.AddDepthSlider(panel, ref currentY, "CHA_HEIGHT", PathHandler.MinPathHeight, PathHandler.MaxPathHeight, PathHandler.NewPathHeight);
            depthSlider.eventValueChanged += (control, value) => PathHandler.NewPathHeight = value;

            // LOD checkbox.
            UICheckBox lodCheck = UIControls.AddPlainCheckBox(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("CHA_LOD"));
            lodCheck.isChecked = PathHandler.DoLODs;
            lodCheck.eventCheckChanged += (control, isChecked) => { PathHandler.DoLODs = isChecked; };
            currentY += lodCheck.height + OptionsPanelUtils.GroupMargin;

            // Reset to deafults button.
            UIButton defaultsButton = UIControls.AddButton(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("CHA_DEFAULT"), OptionsPanelUtils.ButtonWidth);
            defaultsButton.eventClicked += (control, clickEvent) =>
            {
                // Set controls to default settings.
                depthSlider.value = PathHandler.DefaultNewPathHeight;

                // Apply defaults.
                PathHandler.Apply();
            };
            currentY += defaultsButton.height + OptionsPanelUtils.Margin;

            // Apply button.
            UIButton applyButton = UIControls.AddButton(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("CHA_APPLY"), OptionsPanelUtils.ButtonWidth);
            applyButton.eventClicked += (control, clickEvent) => PathHandler.Apply();
            currentY += applyButton.height + OptionsPanelUtils.Margin;

            // Undo button.
            UIButton undoButton = UIControls.AddButton(panel, OptionsPanelUtils.Margin, currentY, Translations.Translate("CHA_REVERT"), OptionsPanelUtils.ButtonWidth);
            undoButton.eventClicked += (control, clickEvent) => PathHandler.Revert();

        }
    }
}