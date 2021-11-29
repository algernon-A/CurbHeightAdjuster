using ICities;
using CitiesHarmony.API;
using UnityEngine;
using ColossalFramework.UI;


namespace CurbHeightAdjuster
{
    /// <summary>
    /// The base mod class for instantiation by the game.
    /// </summary>
    public class CHAMod : IUserMod
    {
        public static string ModName => "Curb Height Adjuster";
        public static string Version => "0.1";

        public string Name => ModName + " " + Version;
        public string Description => Translations.Translate("CHA_DESC");

        // Curb depth label (for slider).
        private UILabel depthLabel;

        /// <summary>
        /// Called by the game when the mod is enabled.
        /// </summary>
        public void OnEnabled()
        {
            // Apply Harmony patches via Cities Harmony.
            // Called here instead of OnCreated to allow the auto-downloader to do its work prior to launch.
            HarmonyHelper.DoOnHarmonyReady(() => Patcher.PatchAll());

            // Load the settings file.
            ModSettings.Load();
        }


        /// <summary>
        /// Called by the game when the mod is disabled.
        /// </summary>
        public void OnDisabled()
        {
            // Unapply Harmony patches via Cities Harmony.
            if (HarmonyHelper.IsHarmonyInstalled)
            {
                Patcher.UnpatchAll();
            }
        }


        /// <summary>
        /// Called by the game when the mod options panel is setup.
        /// </summary>
        public void OnSettingsUI(UIHelperBase helper)
        {
            // Language.
            helper.AddDropdown (Translations.Translate("TRN_CHOICE"), Translations.LanguageList, Translations.Index, (index) =>
            {
                Translations.Index = index;
                ModSettings.Save();
            });

            // Curb depth slider.
            UISlider depthSlider = helper.AddSlider(Translations.Translate("CHA_HEIGHT"), CurbHeight.MinCurbHeight, CurbHeight.MaxCurbHeight, 0.01f, CurbHeight.NewCurbHeight, (value) => DepthSliderChange(value)) as UISlider;

            // Curb depth slider value label.
            depthLabel = depthSlider.parent.AddUIComponent<UILabel>();
            depthLabel.relativePosition = new Vector2(0f, depthSlider.parent.height);
            SetDepthLabel(CurbHeight.NewCurbHeight);
        }

        /// <summary>
        /// Curb depth slider event handler.
        /// </summary>
        /// <param name="value">New slider value</param>
        private void DepthSliderChange(float value)
        {
            CurbHeight.NewCurbHeight = value;
            SetDepthLabel(value);
            ModSettings.Save();
        }

        
        /// <summary>
        /// Sets the text of the curb depth slider value label.
        /// </summary>
        /// <param name="value">Value to display</param>
        private void SetDepthLabel(float value)
        {
            depthLabel.text = (value * 100).ToString("N0") + " cm";
        }
    }
}
