using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        float halfWidth = (width - 1) / 2f;
        float halfHeight = (height - 1) / 2f;

        MeshData meshData = new MeshData(width, height);
        int vertexIndex = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                meshData.Vertices[vertexIndex] = new Vector3(x - halfWidth, heightMap[x,y], y - halfHeight);
                meshData.Uvs[vertexIndex] = new Vector2(1f - (x / (float)width), 1f - (y / (float)height));
                if (x < width - 1 && y < height - 1)
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + width, vertexIndex + width + 1);
                    meshData.AddTriangle(vertexIndex, vertexIndex + width + 1, vertexIndex + 1);
                }
                vertexIndex++;
            }
        }

        return meshData;
    }

    public struct MeshData
    {
        public Vector3[] Vertices;
        public int[] Triangles;
        public Vector2[] Uvs;

        private int _triangleIndex;

        public MeshData(int meshWidth, int meshHeight)
        {
            Vertices = new Vector3[meshWidth * meshHeight];
            Triangles = new int[(meshWidth-1) * (meshHeight-1) * 6];
            Uvs = new Vector2[meshWidth * meshHeight];
            _triangleIndex = 0;
        }

        public void AddTriangle(int a, int b, int c)
        {
            Triangles[_triangleIndex] = a;
            Triangles[_triangleIndex + 1] = b;
            Triangles[_triangleIndex + 2] = c;
            _triangleIndex += 3;
        }

        public Mesh CreateMesh()
        {
            Mesh mesh = new Mesh();
            mesh.vertices = Vertices;
            mesh.triangles = Triangles;
            mesh.uv = Uvs;
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
