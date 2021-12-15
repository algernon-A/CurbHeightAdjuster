using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;


namespace CurbHeightAdjuster
{
    /// <summary>
    /// Class for handling meshes and serialization/deserialization.
    /// </summary>
    internal static class MeshHandler
    {
        // Location to store meshes.
        private static string pathName = Path.Combine(ModUtils.AssemblyPath, "Data");


        /// <summary>
        /// Loads a mesh from binary file.
        /// </summary>
        /// <param name="meshName"></param>
        /// <returns>New mesh from binary data (null if error)</returns>
        internal static Mesh LoadMesh(string meshName)
        {
            // Null check.
            if (string.IsNullOrEmpty(meshName))
            {
                return null;
            }    

            // Read mesh as <meshname>.dat.
            string fileName = meshName + ".dat";
            string filePath = Path.Combine(pathName, fileName);

            // Check file exists before doing anything.
            if (!File.Exists(filePath))
            {
                Logging.Message("meshFile ", fileName, " not found");
                return null;
            }

            try
            {
                // Open file for reading.
                FileStream filesStream = new FileStream(filePath, FileMode.Open);

                // Check for empty file.
                if (filesStream.Length == 0)
                {
                    Logging.Error("meshfile ", fileName, " had zero length");
                    filesStream.Close();
                    return null;
                }

                // Read serialzed mesh from file via binary formatter.
                BinaryFormatter formatter = new BinaryFormatter();
                SerializableMeshInfo serializedMesh = (SerializableMeshInfo)formatter.Deserialize(filesStream);
                filesStream.Close();

                // Deserialize mesh,.
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