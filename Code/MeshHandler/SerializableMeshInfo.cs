using System;
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
        /// Constructor - converts the provided mesh to seralizable format.
        /// </summary>
        /// <param name="mesh">Mesh to serialize</param>
        public SerializableMeshInfo(Mesh mesh)
        {
            // Vertices; serialise each Vector3 mesh vertex into three sequential floats.
            vertices = new float[mesh.vertexCount * 3];
            int index = 0;
            for (int i = 0; i < mesh.vertexCount; ++i)
            {
                vertices[index++] = mesh.vertices[i].x;
                vertices[index++] = mesh.vertices[i].y;
                vertices[index++] = mesh.vertices[i].z;
            }

            // Triangles; simple arrray of sequential ints storing the indices of the verticies (in order) forming each triangle.
            triangles = new int[mesh.triangles.Length];
            for (int i = 0; i < mesh.triangles.Length; ++i)
            {
                triangles[i] = mesh.triangles[i];
            }

            // UVs; serialise each Vector2 UV coordinate into two sequential floats.
            uv = new float[mesh.uv.Length * 2];
            index = 0;
            for (int i = 0; i < mesh.uv.Length; ++i)
            {
                uv[index++] = mesh.uv[i].x;
                uv[index++] = mesh.uv[i].y;
            }

            // Normals; Vector3s serialised as per vertices. 
            normals = new float[mesh.normals.Length * 3];
            index = 0;
            for (int i = 0; i < mesh.normals.Length; ++i)
            {
                normals[index++] = mesh.normals[i].x;
                normals[index++] = mesh.normals[i].y;
                normals[index++] = mesh.normals[i].z;
            }

            // Colors; serialised as four sequential floats.
            colors = new float[mesh.colors.Length * 4];
            index = 0;
            for (int i = 0; i < mesh.colors.Length; ++i)
            {
                colors[index++] = mesh.colors[i].r;
                colors[index++] = mesh.colors[i].g;
                colors[index++] = mesh.colors[i].b;
                colors[index++] = mesh.colors[i].a;
            }
        }

        
        /// <summary>
        /// Deserialization: returns a new mesh created from the current serializable data.
        /// </summary>
        /// <returns>New mesh, deserialized from data</returns>
        public Mesh GetMesh()
        {
            // New mesh to deserialize into.
            Mesh mesh = new Mesh();

            // Deserialize vertices; three sequential floats into each Vector3.
            List<Vector3> verticesList = new List<Vector3>();
            int index = 0;
            for (int i = 0; i < vertices.Length; i += 3)
            {
                verticesList.Add(new Vector3(vertices[index++], vertices[index++], vertices[index++]));
            }
            mesh.SetVertices(verticesList);

            // Deserialize triangles; this is already a sequential list of ints, no further work needed.
            mesh.triangles = triangles;

            // Deserialize UVs; two sequential floats into each Vector2.
            List<Vector2>uvList = new List<Vector2>();
            index = 0;
            for (int i = 0; i < uv.Length; i += 2)
            {
                uvList.Add(new Vector2(uv[index++], uv[index++]));
            }
            mesh.SetUVs(0, uvList);

            // Deserialize normals; three sequential floats into each Vector3.
            List<Vector3> normalsList = new List<Vector3>();
            index = 0;
            for (int i = 0; i < normals.Length; i += 3)
            {
                normalsList.Add(new Vector3(normals[index++], normals[index++], normals[index++]));
            }
            mesh.SetNormals(normalsList);

            // Deserialize colors; four sequential floats into each color.
            List<Color> colorsList = new List<Color>();
            index = 0;
            for (int i = 0; i < colors.Length; i += 4)
            {
                colorsList.Add(new Color(colors[index++], colors[index++], colors[index++], colors[index++]));
            }
            mesh.SetColors(colorsList);

            return mesh;
        }
    }
}