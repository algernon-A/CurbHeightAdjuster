// <copyright file="NetRecord.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace CurbHeightAdjuster
{
    using System.Collections.Generic;

    /// <summary>
    /// Class to hold original data for networks (prior to curb height alteration).
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:Accessible fields should begin with upper-case letter", Justification = "Internal data class")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Internal data class")]
    internal class NetRecord
    {
        /// <summary>
        /// Network surface level.
        /// </summary>
        public float m_surfaceLevel;

        /// <summary>
        /// Network segment vertices.
        /// </summary>
        public Dictionary<NetInfo.Segment, NetComponentRecord> m_segmentDict = new Dictionary<NetInfo.Segment, NetComponentRecord>();

        /// <summary>
        /// Network node vertices.
        /// </summary>
        public Dictionary<NetInfo.Node, NetComponentRecord> m_nodeDict = new Dictionary<NetInfo.Node, NetComponentRecord>();

        /// <summary>
        /// Network lane vertical offsets.
        /// </summary>
        public Dictionary<NetInfo.Lane, float> m_laneDict = new Dictionary<NetInfo.Lane, float>();

        /// <summary>
        /// Whether or not to adjust pillars.
        /// </summary>
        public bool m_adjustPillars = false;

        /// <summary>
        /// Whether or not to adjust catenary wires.
        /// </summary>
        public bool m_adjustWires = false;

        /// <summary>
        /// Bridge pillar vertical offsets.
        /// </summary>
        public float m_bridgePillarOffset = 0f;

        /// <summary>
        /// Bridge middle pillar vertical offsets.
        /// </summary>
        public float m_middlePillarOffset = 0f;
    }
}