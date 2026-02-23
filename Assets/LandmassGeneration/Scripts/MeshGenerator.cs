using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurve, int levelOfDetail)
    {
        AnimationCurve heightCurveCopy = new AnimationCurve(heightCurve.keys);
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        float halfWidth = (width - 1) / 2f;
        float halfHeight = (height - 1) / 2f;

        // LOD Increment step value
        int lodIncrement = levelOfDetail == 0 ? 1 : levelOfDetail * 2;
        int verticesPerLine = (width - 1) / lodIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);
        int vertexIndex = 0;

        for (int y = 0; y < height; y += lodIncrement)
        {
            for (int x = 0; x < width; x += lodIncrement)
            {
                meshData.Vertices[vertexIndex] = new Vector3(x - halfWidth, heightCurveCopy.Evaluate(heightMap[x,y]) * heightMultiplier, y - halfHeight);
                meshData.Uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)height);
                if (x < width - 1 && y < height - 1)
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerLine, vertexIndex + verticesPerLine + 1);
                    meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + 1);
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
