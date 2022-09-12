// <copyright file="OptionsPanelUtils.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace CurbHeightAdjuster
{
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;

    /// <summary>
    /// Utilities for Options Panel UI.
    /// </summary>
    internal static class OptionsPanelUtils
    {
        /// <summary>
        /// Layout margin.
        /// </summary>
        internal const float Margin = 5f;

        /// <summary>
        /// Left margin for options panel layout.
        /// </summary>
        internal const float LeftMargin = 24f;

        /// <summary>
        /// Vertical margin between control groups.
        /// </summary>
        internal const float GroupMargin = 40f;

        /// <summary>
        /// Button width.
        /// </summary>
        internal const float ButtonWidth = 400f;

        /// <summary>
        /// Adds a depth slider.
        /// </summary>
        /// <param name="parent">Parent component.</param>
        /// <param name="yPos">Relative y-position indicator (will be incremented with slider height).</param>
        /// <param name="labelKey">Translation key for slider label.</param>
        /// <param name="min">Minimum slider value.</param>
        /// <param name="max">Maximum slider value.</param>
        /// <param name="initialValue">Initial slider value.</param>
        /// <returns>New depth slider with attached depth label.</returns>
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
        /// <param name="parent">Parent component.</param>
        /// <param name="yPos">Relative y-position indicator (will be incremented with slider height).</param>
        /// <param name="labelKey">Translation key for slider label.</param>
        /// <param name="min">Minimum slider value.</param>
        /// <param name="max">Maximum slider value.</param>
        /// <param name="initialValue">Initial slider value.</param>
        /// <returns>New depth slider with attached depth label.</returns>
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
        /// Adds a value slider.
        /// </summary>
        /// <param name="parent">Parent component.</param>
        /// <param name="yPos">Relative y-position indicator (will be incremented with slider height).</param>
        /// <param name="labelKey">Translation key for slider label.</param>
        /// <param name="min">Minimum slider value.</param>
        /// <param name="max">Maximum slider value.</param>
        /// <param name="initialValue">Initial slider value.</param>
        /// <returns>New slider with attached label.</returns>
        private static UISlider AddSlider(UIComponent parent, ref float yPos, string labelKey, float min, float max, float initialValue)
        {
            // Create new slider.
            UISlider newSlider = UISliders.AddPlainSlider(parent, Margin, yPos, Translations.Translate(labelKey), min, max, 0.01f, initialValue);

            // Value label.
            UILabel valueLabel = UILabels.AddLabel(newSlider.parent, Margin, newSlider.parent.height - Margin, string.Empty);
            newSlider.objectUserData = valueLabel;

            // Increment y position indicator.
            yPos += newSlider.parent.height + valueLabel.height + GroupMargin;

            return newSlider;
        }

        /// <summary>
        /// Sets the depth value label text for a depth slider.
        /// </summary>
        /// <param name="control">Calling component.</param>
        /// <param name="value">New value.</param>
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
        /// <param name="c">Calling component.</param>
        /// <param name="value">New value.</param>
        private static void SetPercentageLabel(UIComponent c, float value)
        {
            // Ensure that there's a valid label attached to the slider.
            if (c.objectUserData is UILabel label)
            {
                label.text = (value * 100).ToString("N0") + "%";
            }
        }
    }
}