using System;
using System.IO;
using UnityEngine;


namespace CurbHeightAdjuster
{
    /// <summary>
    /// Class for handling meshes and serialization/deserialization.
    /// </summary>
    internal static class MeshHandler
    {
        // Mesh data location.
        private static string pathName = Path.Combine(ModUtils.AssemblyPath, "Data");


        /// <summary>
        /// Loads a mesh from binary file.
        /// </summary>
        /// <param name="meshName"></param>
        /// <returns>New mesh from binary data (null if error)</returns>
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