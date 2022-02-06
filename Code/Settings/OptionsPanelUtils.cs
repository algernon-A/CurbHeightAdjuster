using UnityEngine;
using ColossalFramework.UI;


namespace CurbHeightAdjuster
{
    /// <summary>
    /// Options panel utilities class.
    /// </summary>
    internal static class OptionsPanelUtils
    {
        // Layout constants.
        internal const float Margin = 5f;
        internal const float LeftMargin = 24f;
        internal const float GroupMargin = 40f;
        internal const float ButtonWidth = 400f;


        /// <summary>
        /// Adds a tab to a UI tabstrip.
        /// </summary>
        /// <param name="tabStrip">UIT tabstrip to add to</param>
        /// <param name="tabName">Name of this tab</param>
        /// <param name="tabIndex">Index number of this tab</param>
        /// <param name="autoLayout">Autolayout</param>
        /// <returns>UIHelper instance for the new tab panel</returns>
        internal static UIPanel AddTab(UITabstrip tabStrip, string tabName, int tabIndex, bool autoLayout)
        {
            // Create tab.
            UIButton tabButton = tabStrip.AddTab(tabName);

            // Sprites.
            tabButton.normalBgSprite = "SubBarButtonBase";
            tabButton.disabledBgSprite = "SubBarButtonBaseDisabled";
            tabButton.focusedBgSprite = "SubBarButtonBaseFocused";
            tabButton.hoveredBgSprite = "SubBarButtonBaseHovered";
            tabButton.pressedBgSprite = "SubBarButtonBasePressed";

            // Tooltip.
            tabButton.tooltip = tabName;

            tabStrip.selectedIndex = tabIndex;

            // Force width.
            tabButton.width = 200;

            // Get tab root panel.
            UIPanel rootPanel = tabStrip.tabContainer.components[tabIndex] as UIPanel;

            // Autolayout.
            rootPanel.autoLayout = autoLayout;

            if (autoLayout)
            {
                rootPanel.autoLayoutDirection = LayoutDirection.Vertical;
                rootPanel.autoLayoutPadding.top = 5;
                rootPanel.autoLayoutPadding.left = 10;
            }

            return rootPanel;
        }


        /// <summary>
        /// Adds a depth slider.
        /// </summary>
        /// <param name="parent">Parent component</param>
        /// <param name="yPos">Relative y-position indicator (will be incremented with slider height)</param>
        /// <param name="labelKey">Translation key for slider label</param>
        /// <param name="min">Minimum slider value</param>
        /// <param name="max">Maximum slider value</param>
        /// <param name="initialValue">Initial slider value</param>
        /// <returns>New depth slider with attached depth label</returns>
        internal static UISlider AddDepthSlider(UIComponent parent, ref float yPos, string labelKey, float min, float max, float initialValue)
        {
            // Create new slider.
            UISlider newSlider = AddSlider(parent, ref yPos, labelKey, min, max, initialValue);

            // Force set slider value to populate initial time label and add event handler.
            SetDepthLabel(newSlider, initialValue);
            newSlider.eventValueChanged += SetDepthLabel;

            return newSlider;
        }


        /// <summary>
        /// Adds a percentage slider.
        /// </summary>
        /// <param name="parent">Parent component</param>
        /// <param name="yPos">Relative y-position indicator (will be incremented with slider height)</param>
        /// <param name="labelKey">Translation key for slider label</param>
        /// <param name="min">Minimum slider value</param>
        /// <param name="max">Maximum slider value</param>
        /// <param name="initialValue">Initial slider value</param>
        /// <returns>New depth slider with attached depth label</returns>
        internal static UISlider AddPercentageSlider(UIComponent parent, ref float yPos, string labelKey, float min, float max, float initialValue)
        {
            // Create new slider.
            UISlider newSlider = AddSlider(parent, ref yPos, labelKey, min, max, initialValue);

            // Force set slider value to populate initial time label and add event handler.
            SetPercentageLabel(newSlider, initialValue);
            newSlider.eventValueChanged += SetPercentageLabel;

            return newSlider;
        }


        /// <summary>
        /// </summary>
        /// <param name="parent">Parent component</param>
        /// <param name="yPos">Relative y-position indicator (will be incremented with slider height)</param>
        /// <param name="labelKey">Translation key for slider label</param>
        /// <param name="min">Minimum slider value</param>
        /// <param name="max">Maximum slider value</param>
        /// <param name="initialValue">Initial slider value</param>
        /// <returns>New slider with attached label</returns>
        private static UISlider AddSlider(UIComponent parent, ref float yPos, string labelKey, float min, float max, float initialValue)
        {
            // Create new slider.
            UISlider newSlider = UIControls.AddSlider(parent, Translations.Translate(labelKey), min, max, 0.01f, initialValue);
            newSlider.parent.relativePosition = new Vector2(Margin, yPos);

            // Value label.
            UILabel valueLabel = UIControls.AddLabel(newSlider.parent, Margin, newSlider.parent.height - Margin, string.Empty);
            newSlider.objectUserData = valueLabel;

            // Increment y position indicator.
            yPos += newSlider.parent.height + valueLabel.height + GroupMargin;

            return newSlider;
        }


        /// <summary>
        /// Sets the depth value label text for a depth slider.
        /// </summary>
        /// <param name="control">Calling component</param>
        /// <param name="value">New value</param>
        private static void SetDepthLabel(UIComponent control, float value)
        {

            // Ensure that there's a valid label attached to the slider.
            if (control.objectUserData is UILabel label)
            {
                label.text = (value * 100).ToString("N0") + " cm";
            }
        }


        /// <summary>
        /// Sets the depth value label text for a percentage slider.
        /// </summary>
        /// <param name="control">Calling component</param>
        /// <param name="value">New value</param>
        private static void SetPercentageLabel(UIComponent control, float value)
        {

            // Ensure that there's a valid label attached to the slider.
            if (control.objectUserData is UILabel label)
            {
                label.text = (value * 100).ToString("N0") + "%";
            }
        }
    }
}