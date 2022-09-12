// <copyright file="NetComponentRecord.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace CurbHeightAdjuster
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Struct to hold references to original vertex arrays (main and LOD) and flags to indicate eligibility.
    /// </summary>
    internal struct NetComponentRecord
    {
        /// <summary>
        /// Net prefab instance.
        /// </summary>
        public NetInfo NetInfo;

        /// <summary>
        /// Indicates whether this network is eligible for curb height manipulation.
        /// </summary>
        public bool EligibleCurbs;

        /// <summary>
        /// Indicates whether this network is eligible for bridge manipulation.
        /// </summary>
        public bool EligibleBridge;

        /// <summary>
        /// Original main mesh vertices.
        /// </summary>
        public Vector3[] MainVerts;

        /// <summary>
        /// Original LOD mesh vertices.
        /// </summary>
        public Vector3[] LodVerts;
    }
}