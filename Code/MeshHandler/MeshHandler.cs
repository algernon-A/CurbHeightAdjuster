// <copyright file="MeshHandler.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace CurbHeightAdjuster
{
    using System;
    using System.IO;
    using AlgernonCommons;
    using UnityEngine;

    /// <summary>
    /// Class for handling meshes and serialization/deserialization.
    /// </summary>
    internal static class MeshHandler
    {
        // Mesh data location.
        private static string pathName = Path.Combine(AssemblyUtils.AssemblyPath, "Data");

        /// <summary>
        /// Loads a mesh from binary file.
        /// </summary>
        /// <param name="meshName">Mesh name.</param>
        /// <returns>New mesh from binary data (null if error).</returns>
        internal static Mesh LoadMesh(string meshName)
        {
            // Read mesh as <meshname>.dat.
            string fileName = meshName + ".dat";
            string filePath = Path.Combine(pathName, fileName);

            try
            {
                // Deserialize mesh.
                SerializableMeshInfo serializedMesh = new SerializableMeshInfo(filePath);
                Mesh mesh = serializedMesh.GetMesh();
                return mesh;
            }
            catch (Exception e)
            {
                Logging.LogException(e, "error reading meshfile ", fileName);
            }

            // If we got here, we didn't get a mesh; return null.
            return null;
        }
    }
}