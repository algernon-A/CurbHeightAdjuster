// <copyright file="ModSettings.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace CurbHeightAdjuster
{
    using System.IO;
    using System.Xml.Serialization;
    using AlgernonCommons;
    using AlgernonCommons.XML;

    /// <summary>
    /// Global mod settings.
    /// </summary>
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
        /// Gets or sets the settings file version.
        /// </summary>
        [XmlAttribute("Version")]
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether detailed logging is enabled.
        /// </summary>
        [XmlElement("DetailedLogging")]
        public bool XMDetailedLogging { get => Logging.DetailLogging; set => Logging.DetailLogging = value; }

        /// <summary>
        /// Gets or sets the new curb height to apply (positive figure, in cm).
        /// </summary>
        [XmlElement("CurbHeight")]
        public float XMLCurbHeight { get => RoadHandler.NewCurbHeight; set => RoadHandler.NewCurbHeight = value; }

        /// <summary>
        /// Gets or sets a value indicating whether LODs are changed as well.
        /// </summary>
        [XmlElement("RaiseLODs")]
        public bool XMLUpdateRoadLods { get => RoadHandler.DoLODs; set => RoadHandler.DoLODs = value; }

        /// <summary>
        /// Gets or sets a value indicating whether bridge manipulations are applied.
        /// </summary>
        [XmlElement("EnableBridges")]
        public bool XMLEnableBridges { get => RoadHandler.EnableBridges; set => RoadHandler.EnableBridges = value; }

        /// <summary>
        /// Gets or sets a value indicating whether bridge manipulations are applied.
        /// </summary>
        [XmlElement("UpdatePillars")]
        public bool XMLUpdatePillars { get => Pillars.AutoUpdate; set => Pillars.AutoUpdate = value; }

        /// <summary>
        /// Gets or sets the bridge height threshold to apply (positive figure, in cm).
        /// </summary>
        [XmlElement("BridgeHeightThreshold")]
        public float XMLBridgeHeightThreshold { get => RoadHandler.BridgeHeightThreshold; set => RoadHandler.BridgeHeightThreshold = value; }

        /// <summary>
        /// Gets or sets the bridge deck thickness multiplier.
        /// </summary>
        [XmlElement("BridgeHeightScale")]
        public float XMLBridgeHeightScale { get => RoadHandler.BridgeHeightScale; set => RoadHandler.BridgeHeightScale = value; }

        /// <summary>
        /// Gets or sets a value indicating whether custom path manipulations are enabled.
        /// </summary>
        [XmlElement("EnablePaths")]
        public bool XMLEnablePaths { get => PathHandler.CustomizePaths; set => PathHandler.CustomizePaths = value; }

        /// <summary>
        /// Gets or sets the new base height to apply (positive figure, in cm).
        /// </summary>
        [XmlElement("PathBaseHeight")]
        public float XMLPathBaseHeight { get => PathHandler.BaseHeight; set => PathHandler.BaseHeight = value; }

        /// <summary>
        /// Gets or sets the new curb height to apply (positive figure, in cm).
        /// </summary>
        [XmlElement("PathCurbHeight")]
        public float XMLPathCurbHeight { get => PathHandler.CurbHeight; set => PathHandler.CurbHeight = value; }

        /// <summary>
        /// Gets or sets a value indicating whether path lods are also adjusted.
        /// </summary>
        [XmlElement("PathLods")]
        public bool XMLUpdatePathLods { get => PathHandler.DoLODs; set => PathHandler.DoLODs = value; }

        /// <summary>
        /// Loads settings from file.
        /// </summary>
        internal static void Load() => XMLFileUtils.Load<ModSettings>(SettingsFile);

        /// <summary>
        /// Saves settings to file.
        /// </summary>
        internal static void Save() => XMLFileUtils.Save<ModSettings>(SettingsFile);
    }
}