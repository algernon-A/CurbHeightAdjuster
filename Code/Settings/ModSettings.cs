using System;
using System.IO;
using System.Xml.Serialization;


namespace CurbHeightAdjuster
{
    /// <summary>
    /// Global mod settings.
    /// </summary>
    /// 
    [XmlRoot("CurbHeightAdjuster")]
    public class ModSettings
    {
        // Settings file name.
        [XmlIgnore]
        private static readonly string SettingsFilePath = Path.Combine(ColossalFramework.IO.DataLocation.localApplicationData, "CurbHeightAdjuster.xml");

        // What's new notification version.
        [XmlIgnore]
        internal static string whatsNewVersion = "0.0";


        // File version.
        [XmlAttribute("Version")]
        public int version = 0;

        // What's new notification version.
        [XmlElement("WhatsNewVersion")]
        public string XMLWhatsNewVersion { get => whatsNewVersion; set => whatsNewVersion = value; }


        /// <summary>
        /// Language setting.
        /// </summary>
        [XmlElement("Language")]
        public string XMLLanguage { get => Translations.CurrentLanguage; set => Translations.CurrentLanguage = value; }

        /// <summary>
        /// Detailed logging enabled.
        /// </summary>
        [XmlElement("DetailedLogging")]
        public bool XMDetailedLogging { get => Logging.detailLogging; set => Logging.detailLogging = value; }


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
        /// Load settings from XML file.
        /// </summary>
        internal static void Load()
        {
            try
            {
                // Check to see if configuration file exists.
                if (File.Exists(SettingsFilePath))
                {
                    // Read it.
                    using (StreamReader reader = new StreamReader(SettingsFilePath))
                    {
                        XmlSerializer xmlSerializer = new XmlSerializer(typeof(ModSettings));
                        if (!(xmlSerializer.Deserialize(reader) is ModSettings settingsFile))
                        {
                            Logging.Error("couldn't deserialize settings file");
                        }
                    }
                }
                else
                {
                    Logging.Message("no settings file found");
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception reading XML settings file");
            }
        }


        /// <summary>
        /// Save settings to XML file.
        /// </summary>
        internal static void Save()
        {
            try
            {
                // Pretty straightforward.  Serialisation is within GBRSettingsFile class.
                using (StreamWriter writer = new StreamWriter(SettingsFilePath))
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(ModSettings));
                    xmlSerializer.Serialize(writer, new ModSettings());
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception saving XML settings file");
            }
        }
    }
}