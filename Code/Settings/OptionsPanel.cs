using System;
using UnityEngine;
using ColossalFramework.UI;


namespace CurbHeightAdjuster
{
    /// <summary>
    /// The mod's options panel.
    /// </summary>
    internal class OptionsPanel : UIPanel
    {
        // Layout constants.
        internal const float Margin = 5f;
        internal const float LeftMargin = 24f;
        internal const float GroupMargin = 40f;
        internal const float ButtonWidth = 400f;


        // Panel components.
        UISlider thresholdSlider, multiplierSlider;


        /// <summary>
        /// Setus up the panel.
        /// </summary>
        internal void Setup()
        {
            try
            {
                // Add controls.
                // Y position indicator.
                float currentY = Margin;

                // Language choice.
                UIDropDown languageDropDown = UIControls.AddPlainDropDown(this, Translations.Translate("TRN_CHOICE"), Translations.LanguageList, Translations.Index);
                languageDropDown.eventSelectedIndexChanged += (control, index) =>
                {
                    Translations.Index = index;
                };
                languageDropDown.parent.relativePosition = new Vector2(LeftMargin, currentY);
                currentY += languageDropDown.parent.height + GroupMargin;

                // LOD checkbox.
                UICheckBox lodCheck = UIControls.AddPlainCheckBox(this, Margin, currentY, Translations.Translate("CHA_LOD"));
                lodCheck.isChecked = NetHandler.DoLODs;
                lodCheck.eventCheckChanged += (control, isChecked) => { NetHandler.DoLODs = isChecked; };
                currentY += lodCheck.height + GroupMargin;

                // Curb depth slider.
                UISlider depthSlider = AddDepthSlider(this, ref currentY, "CHA_HEIGHT", NetHandler.MinCurbHeight, NetHandler.MaxCurbHeight, NetHandler.NewCurbHeight);
                depthSlider.eventValueChanged += (control, value) => NetHandler.NewCurbHeight = value;

                // Spacer.
                UIControls.OptionsSpacer(this, Margin, currentY, this.width - (Margin * 2f));
                currentY += GroupMargin;

                // Enable bridge mesh checkbox.
                UICheckBox bridgeCheck = UIControls.AddPlainCheckBox(this, Margin, currentY, Translations.Translate("CHA_BRI_ENA"));
                bridgeCheck.isChecked = NetHandler.EnableBridges;
                bridgeCheck.eventCheckChanged += BridgeCheckChanged;
                currentY += bridgeCheck.height + Margin;

                // Adjust exiting bridge pillars checkbox.
                UICheckBox pillarCheck = UIControls.AddPlainCheckBox(this, Margin, currentY, Translations.Translate("CHA_OPT_PIL"));
                pillarCheck.isChecked = Pillars.AutoUpdate;
                pillarCheck.eventCheckChanged += (control, isChecked) => { Pillars.AutoUpdate = isChecked; };
                currentY += pillarCheck.height + GroupMargin;

                // Brige min threshold slider.
                thresholdSlider = AddDepthSlider(this, ref currentY, "CHA_BRI_THR", NetHandler.MinBridgeThreshold, NetHandler.MaxBridgeThreshold, NetHandler.BridgeHeightThreshold);
                thresholdSlider.eventValueChanged += (control, value) => NetHandler.BridgeHeightThreshold = value;

                // Bridge depth multiplier slider.
                multiplierSlider = AddPercentageSlider(this, ref currentY, "CHA_BRI_SCA", NetHandler.MinBridgeScale, NetHandler.MaxBridgeScale, NetHandler.BridgeHeightScale);
                multiplierSlider.eventValueChanged += (control, value) => NetHandler.BridgeHeightScale = value;

                // Reset to deafults button.
                UIButton defaultsButton = UIControls.AddButton(this, Margin, currentY, Translations.Translate("CHA_DEFAULT"), ButtonWidth);
                defaultsButton.eventClicked += (control, clickEvent) =>
                {
                    // Set controls to default settings.
                    depthSlider.value = NetHandler.DefaultNewCurbHeight;
                    thresholdSlider.value = -NetHandler.DefaultBridgeThreshold;
                    multiplierSlider.value = NetHandler.DefaultBridgeMultiplier;

                    // Apply defaults.
                    NetHandler.Apply();
                };
                currentY += defaultsButton.height + Margin;

                // Apply button.
                UIButton applyButton = UIControls.AddButton(this, Margin, currentY, Translations.Translate("CHA_APPLY"), ButtonWidth);
                applyButton.eventClicked += (control, clickEvent) => NetHandler.Apply();
                currentY += applyButton.height + Margin;

                // Undo button.
                UIButton undoButton = UIControls.AddButton(this, Margin, currentY, Translations.Translate("CHA_REVERT"), ButtonWidth);
                undoButton.eventClicked += (control, clickEvent) => NetHandler.Revert();
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception creating options panel");
            }
        }


        /// <summary>
        /// Event handler for enable bridge checkbox changes.
        /// </summary>
        /// <param name="control">Calling component</param>
        /// <param name="isChecked">New checked state</param>
        private void BridgeCheckChanged(UIComponent component, bool isChecked)
        {
            // Update settings.
            NetHandler.EnableBridges = isChecked;

            // Adjust component visibility.
            thresholdSlider.parent.isVisible = isChecked;
            multiplierSlider.parent.isVisible = isChecked;
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
        private UISlider AddDepthSlider(UIComponent parent, ref float yPos, string labelKey, float min, float max, float initialValue)
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
        private UISlider AddPercentageSlider(UIComponent parent, ref float yPos, string labelKey, float min, float max, float initialValue)
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
        private UISlider AddSlider(UIComponent parent, ref float yPos, string labelKey, float min, float max, float initialValue)
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
        private void SetDepthLabel(UIComponent control, float value)
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
        private void SetPercentageLabel(UIComponent control, float value)
        {

            // Ensure that there's a valid label attached to the slider.
            if (control.objectUserData is UILabel label)
            {
                label.text = (value * 100).ToString("N0") + "%";
            }
        }
    }
}