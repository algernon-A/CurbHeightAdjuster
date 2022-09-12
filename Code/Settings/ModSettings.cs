namespace CurbHeightAdjuster
{
    using System.IO;
    using System.Xml.Serialization;
    using AlgernonCommons;
    using AlgernonCommons.XML;

    /// <summary>
    /// Global mod settings.
    /// </summary>
    /// 
    [XmlRoot("CurbHeightAdjuster")]
    public class ModSettings : SettingsXMLBase
    {
        // Settings file name.
        [XmlIgnore]
        private static readonly string SettingsFileName = "CurbHeightAdjuster.xml";

        // User settings directory.
        [XmlIgnore]
        private static readonly string UserSettingsDir = ColossalFramework.IO.DataLocation.localApplicationData;

        // Full userdir settings file name.
        [XmlIgnore]
        private static readonly string SettingsFile = Path.Combine(UserSettingsDir, SettingsFileName);

        /// <summary>
        /// Settings file version.
        /// </summary>
        [XmlAttribute("Version")]
        public int Version = 0;

        /// <summary>
        /// Detailed logging enabled.
        /// </summary>
        [XmlElement("DetailedLogging")]
        public bool XMDetailedLogging { get => Logging.DetailLogging; set => Logging.DetailLogging = value; }

        /// <summary>
        /// New curb height.
        /// </summary>
        [XmlElement("CurbHeight")]
        public float XMLCurbHeight { get => RoadHandler.NewCurbHeight; set => RoadHandler.NewCurbHeight = value; }

        /// <summary>
        /// Whether or not road LODs are changed as well.
        /// </summary>
        [XmlElement("RaiseLODs")]
        public bool XMLUpdateRoadLods { get => RoadHandler.DoLODs; set => RoadHandler.DoLODs = value; }

        /// <summary>
        /// Whether or not bridge manipulations are applied.
        /// </summary>
        [XmlElement("EnableBridges")]
        public bool XMLEnableBridges { get => RoadHandler.EnableBridges; set => RoadHandler.EnableBridges = value; }

        /// <summary>
        /// Whether or not bridge manipulations are applied.
        /// </summary>
        [XmlElement("UpdatePillars")]
        public bool XMLUpdatePillars { get => Pillars.AutoUpdate; set => Pillars.AutoUpdate = value; }

        /// <summary>
        /// Bridge deck threshold.
        /// </summary>
        [XmlElement("BridgeHeightThreshold")]
        public float XMLBridgeHeightThreshold { get => RoadHandler.BridgeHeightThreshold; set => RoadHandler.BridgeHeightThreshold = value; }

        /// <summary>
        /// Bridge deck multiplier.
        /// </summary>
        [XmlElement("BridgeHeightScale")]
        public float XMLBridgeHeightScale { get => RoadHandler.BridgeHeightScale; set => RoadHandler.BridgeHeightScale = value; }

        /// <summary>
        /// Enable path manipulations.
        /// </summary>
        [XmlElement("EnablePaths")]
        public bool XMLEnablePaths { get => PathHandler.customizePaths; set => PathHandler.customizePaths = value; }

        /// <summary>
        /// New path base height.
        /// </summary>
        [XmlElement("PathBaseHeight")]
        public float XMLPathBaseHeight { get => PathHandler.BaseHeight; set => PathHandler.BaseHeight = value; }

        /// <summary>
        /// New path curb height.
        /// </summary>
        [XmlElement("PathCurbHeight")]
        public float XMLPathCurbHeight { get => PathHandler.CurbHeight; set => PathHandler.CurbHeight = value; }

        /// <summary>
        /// Whether or not path LODs are changed as well.
        /// </summary>
        [XmlElement("PathLods")]
        public bool XMLUpdatePathLods { get => PathHandler.DoLODs; set => PathHandler.DoLODs = value; }

        /// <summary>
        /// Loads settings from file.
        /// </summary>
        internal static void Load() => XMLFileUtils.Load<ModSettings>(SettingsFileName);

        /// <summary>
        /// Saves settings to file.
        /// </summary>
        internal static void Save() => XMLFileUtils.Save<ModSettings>(SettingsFileName);
    }
}