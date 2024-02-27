using System;
using System.Collections.Generic;
using System.Numerics;
using ABEngine.ABERuntime.Core.Assets;

namespace ABEngine.ABERuntime.Rendering
{
	public static class NormalSolver
	{
        public static Vector3[] CalculateNormals(VertexStandard[] verts, ushort[] indices)
        {
            Vector3[] vertices = new Vector3[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                vertices[i] = verts[i].Position;
            }
            var normals = new Vector3[vertices.Length];
            var triangleNormals = new List<Vector3>[vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                triangleNormals[i] = new List<Vector3>();
            }

            for (int i = 0; i < indices.Length; i += 3)
            {
                ushort index1 = indices[i];
                ushort index2 = indices[i + 1];
                ushort index3 = indices[i + 2];

                Vector3 v1 = vertices[index1];
                Vector3 v2 = vertices[index2];
                Vector3 v3 = vertices[index3];

                // Compute the normal of the triangle
                Vector3 side1 = v3 - v1;
                Vector3 side2 = v2 - v1;
                Vector3 normal = Vector3.Cross(side1, side2);
                normal = Vector3.Normalize(normal);

                // Store the triangle's normal for each of the triangle's vertices
                triangleNormals[index1].Add(normal);
                triangleNormals[index2].Add(normal);
                triangleNormals[index3].Add(normal);
            }

            // Compute the normal for each vertex by averaging the normals of the triangles that share this vertex
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 averageNormal = Vector3.Zero;
                foreach (var triNormal in triangleNormals[i])
                {
                    averageNormal += triNormal;
                }
                if (triangleNormals[i].Count > 0)
                {
                    averageNormal /= triangleNormals[i].Count;
                }
                normals[i] = Vector3.Normalize(averageNormal);
            }

            return normals;
        }
    }
}

