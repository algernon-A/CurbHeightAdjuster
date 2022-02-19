using System;
using UnityEngine;
using ICities;
using ColossalFramework.UI;
using ColossalFramework.Globalization;


namespace CurbHeightAdjuster
{
    /// <summary>
    /// Class to handle the mod's options panel.
    /// </summary>
    internal static class OptionsPanel
    {
        // Layout constants.
        internal const float Margin = 5f;
        internal const float LeftMargin = 24f;
        internal const float GroupMargin = 40f;
        internal const float ButtonWidth = 400f;


        // Parent UI panel reference.
        internal static UIScrollablePanel optionsPanel;
        private static UIPanel gameOptionsPanel;

        // Instance references.
        private static GameObject optionsGameObject;


        /// <summary>
        /// Options panel setup.
        /// </summary>
        /// <param name="helper">UIHelperBase parent</param>
        internal static void Setup(UIHelperBase helper)
        {
            // Set up tab strip and containers.
            optionsPanel = ((UIHelper)helper).self as UIScrollablePanel;
            optionsPanel.autoLayout = false;
        }


        /// <summary>
        /// Attaches an event hook to options panel visibility, to enable/disable mod hokey when the panel is open.
        /// </summary>
        internal static void OptionsEventHook()
        {
            // Get options panel instance.
            gameOptionsPanel = UIView.library.Get<UIPanel>("OptionsPanel");

            if (gameOptionsPanel == null)
            {
                Logging.Error("couldn't find OptionsPanel");
            }
            else
            {
                // Simple event hook to create/destroy GameObject based on appropriate visibility.
                gameOptionsPanel.eventVisibilityChanged += (control, isVisible) =>
                {
                    // Create/destroy based on whether or not we're now visible.
                    if (isVisible)
                    {
                        Create();
                    }
                    else
                    {
                        Close();

                        // Save settings on close.
                        ModSettings.Save();
                    }
                };

                // Recreate panel on system locale change.
                LocaleManager.eventLocaleChanged += LocaleChanged;
            }
        }


        /// <summary>
        /// Refreshes the options panel (destroys and rebuilds) on a locale change when the options panel is open.
        /// </summary>
        internal static void LocaleChanged()
        {
            if (gameOptionsPanel != null && gameOptionsPanel.isVisible)
            {
                Logging.KeyMessage("changing locale");

                Close();
                Create();
            }
        }


        /// <summary>
        /// Creates the panel object in-game and displays it.
        /// </summary>
        private static void Create()
        {
            try
            {
                Logging.KeyMessage("creating options panel");

                // We're now visible - create our gameobject, and give it a unique name for easy finding with ModTools.
                optionsGameObject = new GameObject("CHAOptionsPanel");

                // Attach to game options panel.
                optionsGameObject.transform.parent = optionsPanel.transform;

                // Create a base panel attached to our game object, perfectly overlaying the game options panel.
                UIPanel panel = optionsGameObject.AddComponent<UIPanel>();
                panel.absolutePosition = optionsPanel.absolutePosition;
                panel.width = optionsPanel.width;
                panel.height = 744f;

                // Add controls.
                // Y position indicator.
                float currentY = Margin;

                // Language choice.
                UIDropDown languageDropDown = UIControls.AddPlainDropDown(panel, Translations.Translate("TRN_CHOICE"), Translations.LanguageList, Translations.Index);
                languageDropDown.eventSelectedIndexChanged += (control, index) =>
                {
                    Translations.Index = index;
                };
                languageDropDown.parent.relativePosition = new Vector2(LeftMargin, currentY);
                currentY += languageDropDown.parent.height + GroupMargin;

                // LOD checkbox.
                UICheckBox lodCheck = UIControls.AddPlainCheckBox(panel, Margin, currentY, Translations.Translate("CHA_LOD"));
                lodCheck.isChecked = NetHandler.DoLODs;
                lodCheck.eventCheckChanged += (control, isChecked) => { NetHandler.DoLODs = isChecked; };
                currentY += lodCheck.height + GroupMargin;

                // Curb depth slider.
                UISlider depthSlider = AddDepthSlider(panel, ref currentY, "CHA_HEIGHT", NetHandler.MinCurbHeight, NetHandler.MaxCurbHeight, NetHandler.NewCurbHeight);
                depthSlider.eventValueChanged += (control, value) => NetHandler.NewCurbHeight = value;

                // Enable bridge mesh checkbox.
                UICheckBox enableCheck = UIControls.AddPlainCheckBox(panel, Margin, currentY, Translations.Translate("CHA_BRI_ENA"));
                enableCheck.isChecked = NetHandler.DoLODs;
                enableCheck.eventCheckChanged += (control, isChecked) => { NetHandler.EnableBridges = isChecked; };
                currentY += enableCheck.height + GroupMargin;

                // Brige min threshold slider.
                UISlider threshholdSlider = AddDepthSlider(panel, ref currentY, "CHA_BRI_THR", NetHandler.MinBridgeThreshold, NetHandler.MaxBridgeThreshold, NetHandler.BridgeHeightThreshold);
                threshholdSlider.eventValueChanged += (control, value) => NetHandler.BridgeHeightThreshold = value;

                // Bridge depth multiplier slider.
                UISlider multiplierSlider = AddPercentageSlider(panel, ref currentY, "CHA_BRI_SCA", NetHandler.MinBridgeScale, NetHandler.MaxBridgeScale, NetHandler.BridgeHeightScale);
                multiplierSlider.eventValueChanged += (control, value) => NetHandler.BridgeHeightScale = value;

                // Apply button.
                UIButton applyButton = UIControls.AddButton(panel, Margin, currentY, Translations.Translate("CHA_APPLY"), ButtonWidth);
                applyButton.eventClicked += (control, clickEvent) => NetHandler.Apply();
                currentY += applyButton.height + Margin;

                // Undo button.
                UIButton undoButton = UIControls.AddButton(panel, Margin, currentY, Translations.Translate("CHA_REVERT"), ButtonWidth);
                undoButton.eventClicked += (control, clickEvent) => NetHandler.Revert();
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception creating options panel");
            }
        }


        /// <summary>
        /// Closes the panel by destroying the object (removing any ongoing UI overhead).
        /// </summary>
        private static void Close()
        {
            // Save settings on close.
            ModSettings.Save();

            // We're no longer visible - destroy our game object.
            if (optionsGameObject != null)
            {
                GameObject.Destroy(optionsGameObject);
                optionsGameObject = null;
            }
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
        private static UISlider AddDepthSlider(UIComponent parent, ref float yPos, string labelKey, float min, float max, float initialValue)
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
        private static UISlider AddPercentageSlider(UIComponent parent, ref float yPos, string labelKey, float min, float max, float initialValue)
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