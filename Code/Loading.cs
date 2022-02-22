using ICities;


namespace CurbHeightAdjuster
{
    /// <summary>
    /// Main loading class: the mod runs from here.
    /// </summary>
    public class Loading : LoadingExtensionBase
    {
        /// <summary>
        /// Called by the game when level loading is complete.
        /// </summary>
        /// <param name="mode">Loading mode (e.g. game, editor, scenario, etc.)</param>
        public override void OnLevelLoaded(LoadMode mode)
        {
            Logging.Message("loading");

            base.OnLevelLoaded(mode);

            // Set up options panel event handler (need to redo this now that options panel has been reset after loading into game).
            OptionsPanelManager.OptionsEventHook();

            // Display update notification.
            WhatsNew.ShowWhatsNew();

            Logging.Message("loading complete");
        }
    }
}