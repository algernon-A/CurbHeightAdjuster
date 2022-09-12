namespace CurbHeightAdjuster
{
    using AlgernonCommons.UI;
    using ColossalFramework.UI;

    /// <summary>
    /// The mod's settings options panel.
    /// </summary>
    public class OptionsPanel : UIPanel
    {
        // Layout constants.
        internal const float Margin = 5f;
        internal const float LeftMargin = 24f;
        internal const float GroupMargin = 40f;
        internal const float ButtonWidth = 400f;

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionsPanel"/> class.
        /// </summary>
        internal OptionsPanel()
        {
            // Add tabstrip.
            // Add tabstrip.
            UITabstrip tabstrip = UITabstrips.AddTabStrip(this, 0f, 0f, OptionsPanelManager<OptionsPanel>.PanelWidth, OptionsPanelManager<OptionsPanel>.PanelHeight, out _);

            // Add tabs and panels.
            new GeneralOptionsPanel(tabstrip, 0);
            new RoadOptionsPanel(tabstrip, 1);
            new PathOptionsPanel(tabstrip, 2);

            // Ensure initial selected tab (doing a 'quickstep' to ensure proper events are triggered).
            tabstrip.selectedIndex = -1;
            tabstrip.selectedIndex = 0;
        }
    }
}