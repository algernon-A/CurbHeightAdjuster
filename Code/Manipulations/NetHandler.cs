// <copyright file="NetHandler.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace CurbHeightAdjuster
{
    /// <summary>
    /// Class to manage changes to networks.
    /// </summary>
    public static class NetHandler
    {
        /// <summary>
        /// Called on load to scan through all loaded NetInfos, build the database, and apply network manipulations (meshes and lanes).
        /// </summary>
        public static void OnLoad()
        {
            RoadHandler.OnLoad();
            PathHandler.OnLoad();
        }
    }
}