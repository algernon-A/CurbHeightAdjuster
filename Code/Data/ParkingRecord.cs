// <copyright file="ParkingRecord.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace CurbHeightAdjuster
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Class to hold original data for parking assets (prior to curb height alteration).
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:Accessible fields should begin with upper-case letter", Justification = "Internal data class")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Internal data class")]
    internal class ParkingRecord
    {
        /// <summary>
        /// Original building mesh vertices.
        /// </summary>
        public Vector3[] m_vertices;

        /// <summary>
        /// Original building prop heights.
        /// </summary>
        public Dictionary<BuildingInfo.Prop, float> m_propHeights = new Dictionary<BuildingInfo.Prop, float>();
    }
}