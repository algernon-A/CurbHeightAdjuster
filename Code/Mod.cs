﻿namespace CurbHeightAdjuster
{
    using AlgernonCommons.Notifications;
    using AlgernonCommons.Patching;
    using AlgernonCommons.Translation;
    using ICities;

    /// <summary>
    /// The base mod class for instantiation by the game.
    /// </summary>
    public sealed class Mod : PatcherMod<OptionsPanel, PatcherBase>, IUserMod
    {
        /// <summary>
        /// Gets the mod's base display name (name only).
        /// </summary>
        public override string BaseName => "Curb Height Adjuster";

        /// <summary>
        /// Gets the mod's unique Harmony identfier.
        /// </summary>
        public override string HarmonyID => "com.github.algernon-A.csl.cha";

        /// <summary>
        /// Gets the mod's description for display in the content manager.
        /// </summary>
        public string Description => Translations.Translate("CHA_DESC");

        /// <summary>
        /// Gets the mod's what's new message array.
        /// </summary>
        public override WhatsNewMessage[] WhatsNewMessages => new WhatsNewMessageListing().Messages;

        /// <summary>
        /// Saves settings file.
        /// </summary>
        public override void SaveSettings() => ModSettings.Save();

        /// <summary>
        /// Loads settings file.
        /// </summary>
        public override void LoadSettings() => ModSettings.Load();
    }
}