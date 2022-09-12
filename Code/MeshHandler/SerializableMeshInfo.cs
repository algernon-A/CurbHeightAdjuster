// <copyright file="SerializableMeshInfo.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace CurbHeightAdjuster
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;

    /// <summary>
    /// Class to serialize Unity meshes into binary format.
    /// </summary>
    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:Accessible fields should begin with upper-case letter", Justification = "Internal data class")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Internal data class")]
    internal class SerializableMeshInfo
    {
        /// <summary>
        /// Mesh vertices, suitable for binary serialization.
        /// </summary>
        [SerializeField]
        public float[] m_vertices;

        /// <summary>
        /// Mesh triangles, suitable for binary serialization.
        /// </summary>
        [SerializeField]
        public int[] m_triangles;

        /// <summary>
        /// Mesh UVs, suitable for binary serialization.
        /// </summary>
        [SerializeField]
        public float[] m_uvs;

        /// <summary>
        /// Mesh normals, suitable for binary serialization.
        /// </summary>
        [SerializeField]
        public float[] m_normals;

        /// <summary>
        /// Mesh colors, suitable for binary serialization.
        /// </summary>
        [SerializeField]
        public float[] m_colors;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableMeshInfo"/> class.
        /// Loads a serializable mesh from the specified file.
        /// </summary>
        /// <param name="filename">Filename to load.</param>
        public SerializableMeshInfo(string filename) => Deserialize(filename);

        /// <summary>
        /// Deserialization: returns a new mesh created from the current serializable data.
        /// </summary>
        /// <returns>New mesh, deserialized from data.</returns>
        public Mesh GetMesh()
        {
            // New mesh to deserialize into.
            Mesh mesh = new Mesh();

            // Deserialize vertices; three sequential floats into each Vector3.
            if (m_vertices != null)
            {
                List<Vector3> verticesList = new List<Vector3>();
                int index = 0;
                for (int i = 0; i < m_vertices.Length; i += 3)
                {
                    verticesList.Add(new Vector3(m_vertices[index++], m_vertices[index++], m_vertices[index++]));
                }

                mesh.SetVertices(verticesList);
            }

            // Deserialize triangles; this is already a sequential list of ints, no further work needed.
            mesh.triangles = m_triangles;

            // Deserialize UVs; two sequential floats into each Vector2.
            if (m_uvs != null)
            {
                List<Vector2> uvList = new List<Vector2>();
                int index = 0;
                for (int i = 0; i < m_uvs.Length; i += 2)
                {
                    uvList.Add(new Vector2(m_uvs[index++], m_uvs[index++]));
                }

                mesh.SetUVs(0, uvList);
            }

            // Deserialize normals; three sequential floats into each Vector3.
            if (m_normals != null)
            {
                List<Vector3> normalsList = new List<Vector3>();
                int index = 0;
                for (int i = 0; i < m_normals.Length; i += 3)
                {
                    normalsList.Add(new Vector3(m_normals[index++], m_normals[index++], m_normals[index++]));
                }

                mesh.SetNormals(normalsList);
            }

            // Deserialize colors; four sequential floats into each color.
            List<Color> colorsList = new List<Color>();
            if (colorsList != null)
            {
                int index = 0;
                for (int i = 0; i < m_colors.Length; i += 4)
                {
                    colorsList.Add(new Color(m_colors[index++], m_colors[index++], m_colors[index++], m_colors[index++]));
                }

                mesh.SetColors(colorsList);
            }

            return mesh;
        }

        /// <summary>
        /// Deserializes a mesh from file.
        /// </summary>
        /// <param name="fileName">File to deserialize.</param>
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
                m_vertices = new float[reader.ReadInt32()];
                m_triangles = new int[reader.ReadInt32()];
                m_uvs = new float[reader.ReadInt32()];
                m_normals = new float[reader.ReadInt32()];
                m_colors = new float[reader.ReadInt32()];

                // Read arrays.
                for (int i = 0; i < m_vertices.Length; ++i)
                {
                    m_vertices[i] = reader.ReadSingle();
                }

                for (int i = 0; i < m_triangles.Length; ++i)
                {
                    m_triangles[i] = reader.ReadInt32();
                }

                for (int i = 0; i < m_uvs.Length; ++i)
                {
                    m_uvs[i] = reader.ReadSingle();
                }

                for (int i = 0; i < m_normals.Length; ++i)
                {
                    m_normals[i] = reader.ReadSingle();
                }

                for (int i = 0; i < m_colors.Length; ++i)
                {
                    m_colors[i] = reader.ReadSingle();
                }
            }

            fileStream.Close();
        }
    }
}