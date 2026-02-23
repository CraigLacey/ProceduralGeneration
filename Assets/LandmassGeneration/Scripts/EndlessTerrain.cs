using System.Collections.Generic;
using UnityEngine;
using static MapGenerator;
using static MeshGenerator;

public class EndlessTerrain : MonoBehaviour
{
    public const float MAX_VIEW_DISTANCE = 450f;
    [SerializeField] private Transform _viewer;
    [SerializeField] private Material _mapMaterial;

    public static Vector2 viewerPosition;
    private int _chunkSize;
    private int _chunksVisibleInViewDistance;

    private Dictionary<Vector2, TerrainChunk> _terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> _terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    private static MapGenerator _mapGenerator;

    private void Start()
    {
        _mapGenerator = FindFirstObjectByType<MapGenerator>();
        _chunkSize = MapGenerator.MAP_CHUNK_SIZE - 1;
        _chunksVisibleInViewDistance = Mathf.RoundToInt(MAX_VIEW_DISTANCE / _chunkSize);
    }

    private void Update()
    {
        viewerPosition = new Vector2(_viewer.position.x, _viewer.position.z);
        UpdateVisibleChunks();
    }

    private void UpdateVisibleChunks()
    {
        for (int i = 0; i < _terrainChunksVisibleLastUpdate.Count; i++)
        {
            _terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        _terrainChunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / _chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / _chunkSize);
        for (int yOffset = -_chunksVisibleInViewDistance; yOffset <= _chunksVisibleInViewDistance; yOffset++)
        {
            for (int xOffset = -_chunksVisibleInViewDistance; xOffset <= _chunksVisibleInViewDistance; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                if (_terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                {
                    _terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                    if (_terrainChunkDictionary[viewedChunkCoord].IsVisible())
                    {
                        _terrainChunksVisibleLastUpdate.Add(_terrainChunkDictionary[viewedChunkCoord]);
                    }
                }
                else
                {
                    _terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, _chunkSize, transform, _mapMaterial));
                }
            }
        }
    }

    public class TerrainChunk
    {
        private GameObject _meshObject;
        private Vector2 _position;
        private Bounds _bounds;

        private MapData _mapData;
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;

        public TerrainChunk(Vector2 coord, int size, Transform parent, Material material)
        {
            _position = coord * size;
            _bounds = new Bounds(_position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(_position.x, 0, _position.y);


            _meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _meshRenderer = _meshObject.GetComponent<MeshRenderer>();
            _meshFilter = _meshObject.GetComponent<MeshFilter>();
            _meshRenderer.sharedMaterial = material;

            _meshObject.transform.position = positionV3;
            _meshObject.transform.SetParent(parent);

            SetVisible(false);

            _mapGenerator.RequestMapData(OnMapDataReceived);
        }

        private void OnMeshDataReceived(MeshData meshData)
        {
            _meshFilter.mesh = meshData.CreateMesh();
        }

        private void OnMapDataReceived(MapData mapData)
        {
            _mapGenerator.RequestMeshData(mapData, OnMeshDataReceived);
            //Debug.Log("Map data received for chunk at " + _position);
            //MeshRenderer meshRenderer = _meshObject.GetComponent<MeshRenderer>();
            //meshRenderer.material.mainTexture = TextureGenerator.TextureFromColourMap(mapData.ColourMap, MapGenerator.MAP_CHUNK_SIZE, MapGenerator.MAP_CHUNK_SIZE);
        }

        public void UpdateTerrainChunk()
        {
            float viewerDistanceFromNearestEdge = Mathf.Sqrt(_bounds.SqrDistance(viewerPosition));
            bool visible = viewerDistanceFromNearestEdge <= MAX_VIEW_DISTANCE;
            SetVisible(visible);
        }

        public void SetVisible(bool visible)
        {
            _meshObject.SetActive(visible);
        }

        public bool IsVisible()
        {
            return _meshObject.activeSelf;
        }
    }
}
