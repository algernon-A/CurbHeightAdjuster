using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;


namespace CurbHeightAdjuster
{
    /// <summary>
    /// Class to serialize Unity meshes into binary format.
    /// </summary>
    [Serializable]
    class SerializableMeshInfo
    {
        /// <summary>
        /// Mesh vertices, suitable for binary serialization.
        /// </summary>
        [SerializeField]
        public float[] vertices;

        /// <summary>
        /// Mesh triangles, suitable for binary serialization.
        /// </summary>
        [SerializeField]
        public int[] triangles;

        /// <summary>
        /// Mesh UVs, suitable for binary serialization.
        /// </summary>
        [SerializeField]
        public float[] uv;

        /// <summary>
        /// Mesh normals, suitable for binary serialization.
        /// </summary>
        [SerializeField]
        public float[] normals;

        /// <summary>
        /// Mesh colors, suitable for binary serialization.
        /// </summary>
        [SerializeField]
        public float[] colors;


        /// <summary>
        /// Constructor - loads a serializable mesh from the specified file.
        /// </summary>
        /// <param name="filename">Filename to load</param>
        public SerializableMeshInfo(string filename) => Deserialize(filename);


        /// <summary>
        /// Deserialization: returns a new mesh created from the current serializable data.
        /// </summary>
        /// <returns>New mesh, deserialized from data</returns>
        public Mesh GetMesh()
        {
            // New mesh to deserialize into.
            Mesh mesh = new Mesh();

            // Deserialize vertices; three sequential floats into each Vector3.
            if (vertices != null)
            {
                List<Vector3> verticesList = new List<Vector3>();
                int index = 0;
                for (int i = 0; i < vertices.Length; i += 3)
                {
                    verticesList.Add(new Vector3(vertices[index++], vertices[index++], vertices[index++]));
                }
                mesh.SetVertices(verticesList);
            }

            // Deserialize triangles; this is already a sequential list of ints, no further work needed.
            mesh.triangles = triangles;

            // Deserialize UVs; two sequential floats into each Vector2.
            if (uv != null)
            {
                List<Vector2> uvList = new List<Vector2>();
                int index = 0;
                for (int i = 0; i < uv.Length; i += 2)
                {
                    uvList.Add(new Vector2(uv[index++], uv[index++]));
                }
                mesh.SetUVs(0, uvList);
            }

            // Deserialize normals; three sequential floats into each Vector3.
            if (normals != null)
            {
                List<Vector3> normalsList = new List<Vector3>();
                int index = 0;
                for (int i = 0; i < normals.Length; i += 3)
                {
                    normalsList.Add(new Vector3(normals[index++], normals[index++], normals[index++]));
                }
                mesh.SetNormals(normalsList);
            }

            // Deserialize colors; four sequential floats into each color.
            List<Color> colorsList = new List<Color>();
            if (colorsList != null)
            {
                int index = 0;
                for (int i = 0; i < colors.Length; i += 4)
                {
                    colorsList.Add(new Color(colors[index++], colors[index++], colors[index++], colors[index++]));
                }
                mesh.SetColors(colorsList);
            }

            return mesh;
        }


        /// <summary>
        /// Deserializes a mesh from file.
        /// </summary>
        /// <param name="fileName"></param>
        public void Deserialize(string fileName)
        {
            // Don't do anything if filename is null or file doesn't exist.
            if (string.IsNullOrEmpty(fileName) || !File.Exists(fileName))
            {
                return;
            }

            // Read from file.
            FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            using (BinaryReader reader = new BinaryReader(fileStream))
            {
                // Header - read component sizes and initialize arrays.
                vertices = new float[reader.ReadInt32()];
                triangles = new int[reader.ReadInt32()];
                uv = new float[reader.ReadInt32()];
                normals = new float[reader.ReadInt32()];
                colors = new float[reader.ReadInt32()];

                // Read arrays.
                for (int i = 0; i < vertices.Length; ++i)
                {
                    vertices[i] = reader.ReadSingle();
                }
                for (int i = 0; i < triangles.Length; ++i)
                {
                    triangles[i] = reader.ReadInt32();
                }
                for (int i = 0; i < uv.Length; ++i)
                {
                    uv[i] = reader.ReadSingle();
                }
                for (int i = 0; i < normals.Length; ++i)
                {
                    normals[i] = reader.ReadSingle();
                }
                for (int i = 0; i < colors.Length; ++i)
                {
                    colors[i] = reader.ReadSingle();
                }
            }
            fileStream.Close();
        }
    }
}