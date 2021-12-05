using UnityEngine;
using ColossalFramework.UI;


namespace CurbHeightAdjuster
{
    /// <summary>
    /// CameraMod options panel.
    /// </summary>
    public class CHAOptionsPanel : UIPanel
    {
        // Layout constants.
        private const float Margin = 5f;
        private const float LeftMargin = 24f;
        private const float GroupMargin = 40f;
        private const float ButtonWidth = 350f;


        /// <summary>
        /// Performs initial setup for the panel; we don't use Start() as that's not sufficiently reliable (race conditions), and is not needed with the dynamic create/destroy process.
        /// </summary>
        internal void Setup(float width, float height)
        {
            // Size and placement.
            this.width = width - (this.relativePosition.x * 2);
            this.height = height - (this.relativePosition.y * 2);
            this.autoLayout = false;

            // Add controls.

            // Y position indicator.
            float currentY = Margin;

            // Language choice.
            UIDropDown languageDropDown = UIControls.AddPlainDropDown(this, Translations.Translate("TRN_CHOICE"), Translations.LanguageList, Translations.Index);
            languageDropDown.eventSelectedIndexChanged += (control, index) =>
            {
                Translations.Index = index;
                ModSettings.Save();
            };
            languageDropDown.parent.relativePosition = new Vector2(LeftMargin, currentY);
            currentY += languageDropDown.parent.height + GroupMargin;

            // Curb depth slider.
            UISlider depthSlider = AddDepthSlider(ref currentY, "CHA_HEIGHT", CurbHeight.NewCurbHeight);
            depthSlider.eventValueChanged += (control, value) => CurbHeight.NewCurbHeight = value;

            UICheckBox lodCheck = UIControls.AddPlainCheckBox(this, Margin, currentY, Translations.Translate("CHA_LOD"));
            lodCheck.eventCheckChanged += (control, isChecked) => { CurbHeight.RaiseLods = isChecked; };
            currentY += lodCheck.height + GroupMargin;

            // Apply button.
            UIButton applyButton = UIControls.AddButton(this, Margin, currentY, Translations.Translate("CHA_APPLY"), ButtonWidth);
            applyButton.eventClicked += (control, clickEcvent) => CurbHeight.Apply();
            currentY += applyButton.height + Margin;

            // Undo button.
            UIButton undoButton = UIControls.AddButton(this, Margin, currentY, Translations.Translate("CHA_REVERT"), ButtonWidth);
            undoButton.eventClicked += (control, clickEcvent) => CurbHeight.Revert();
        }


        /// <summary>
        /// Adds a depth slider.
        /// </summary>
        /// <param name="yPos">Relative y-position indicator (will be incremented with slider height</param>
        /// <param name="labelKey">Translation key for slider label</param>
        /// <param name="initialValue">Initial slider value</param>
        /// <returns>New delay slider with attached game-time label</returns>
        private UISlider AddDepthSlider(ref float yPos, string labelKey, float initialValue)
        {
            // Create new slider.
            UISlider newSlider = UIControls.AddSlider(this, Translations.Translate(labelKey), CurbHeight.MinCurbHeight, CurbHeight.MaxCurbHeight, 0.01f, initialValue);
            newSlider.parent.relativePosition = new Vector2(Margin, yPos);

            // Game-distanceLabel label.
            UILabel depthLabel = UIControls.AddLabel(newSlider.parent, Margin, newSlider.parent.height - Margin, string.Empty);
            newSlider.objectUserData = depthLabel;

            // Force set slider value to populate initial time label and add event handler.
            SetDepthLabel(newSlider, initialValue);
            newSlider.eventValueChanged += SetDepthLabel;

            // Increment y position indicator.
            yPos += newSlider.parent.height + depthLabel.height + GroupMargin;

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
    }
}