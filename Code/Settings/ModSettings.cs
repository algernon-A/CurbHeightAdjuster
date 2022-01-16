﻿using System;
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


        /// <summary>
        /// Language setting.
        /// </summary>
        [XmlElement("Language")]
        public string XMLLanguage { get => Translations.CurrentLanguage; set => Translations.CurrentLanguage = value; }


        /// <summary>
        /// New curb height.
        /// </summary>
        [XmlElement("CurbHeight")]
        public float XMLCurbHeight { get => CurbHeight.NewCurbHeight; set => CurbHeight.NewCurbHeight = value; }

        
        /// <summary>
        /// Whether or not LODs are raised as well.
        /// </summary>
        [XmlElement("RaiseLODs")]
        public bool XMLUpdateLods { get => CurbHeight.RaiseLods; set => CurbHeight.RaiseLods = value; }


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