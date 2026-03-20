using System;
using System.Collections.Generic;
using UnityEngine;
using static MapGenerator;
using static MeshGenerator;

public class EndlessTerrain : MonoBehaviour
{
    const float SCALE = 1f;

    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    public static float MaxViewDist;
    [SerializeField] private LODInfo[] _detailLevels;
    [SerializeField] private Transform _viewer;
    [SerializeField] private Material _mapMaterial;

    public static Vector2 viewerPosition;
    private Vector2 _viewerPositionOld;

    private int _chunkSize;
    private int _chunksVisibleInViewDistance;

    private Dictionary<Vector2, TerrainChunk> _terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> _terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    private static MapGenerator _mapGenerator;

    private void Start()
    {
        _mapGenerator = FindFirstObjectByType<MapGenerator>();

        MaxViewDist = _detailLevels[_detailLevels.Length - 1].VisibleDistanceThreshold;
        _chunkSize = MapGenerator.MAP_CHUNK_SIZE - 1;
        _chunksVisibleInViewDistance = Mathf.RoundToInt(MaxViewDist / _chunkSize);

        UpdateVisibleChunks();
    }

    private void Update()
    {
        viewerPosition = new Vector2(_viewer.position.x, _viewer.position.z) / SCALE;

        if((_viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
        {
            _viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
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
                    _terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, _chunkSize, _detailLevels, transform, _mapMaterial));
                }
            }
        }
    }

    public class TerrainChunk
    {
        private GameObject _meshObject;
        private Vector2 _position;
        private Bounds _bounds;

        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;

        private LODInfo[] _detailLevels;
        private LODMesh[] _lodMeshes;

        private MapData _mapData;
        private bool _mapDataReceived;
        private int _previousLODIndex = -1;

        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material)
        {
            _detailLevels = detailLevels;

            _position = coord * size;
            _bounds = new Bounds(_position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(_position.x, 0, _position.y);

            _meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _meshRenderer = _meshObject.GetComponent<MeshRenderer>();
            _meshFilter = _meshObject.GetComponent<MeshFilter>();
            _meshRenderer.sharedMaterial = material;

            _meshObject.transform.position = positionV3 * SCALE;
            _meshObject.transform.localScale = Vector3.one * SCALE;
            _meshObject.transform.SetParent(parent);

            SetVisible(false);

            _lodMeshes = new LODMesh[_detailLevels.Length];
            for (int i = 0; i < _detailLevels.Length; i++)
            {
                _lodMeshes[i] = new LODMesh(_detailLevels[i].Lod, UpdateTerrainChunk);
            }

            _mapGenerator.RequestMapData(_position, OnMapDataReceived);
        }

        private void OnMapDataReceived(MapData mapData)
        {
            _mapData = mapData;
            _mapDataReceived = true;

            Texture2D texture = TextureGenerator.TextureFromColourMap(mapData.ColourMap, MapGenerator.MAP_CHUNK_SIZE, MapGenerator.MAP_CHUNK_SIZE);
            _meshRenderer.material.mainTexture = texture;

            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk()
        {
            if (!_mapDataReceived)
            {
                return;
            }

            float viewerDistanceFromNearestEdge = Mathf.Sqrt(_bounds.SqrDistance(viewerPosition));
            bool visible = viewerDistanceFromNearestEdge <= MaxViewDist;

            if (visible) 
            {
                int lodIndex = 0;
                for (int i = 0; i < _detailLevels.Length - 1; i++)
                {
                    if (viewerDistanceFromNearestEdge > _detailLevels[i].VisibleDistanceThreshold)
                    {
                        lodIndex = i + 1;
                    }
                    else
                    {
                        break;
                    }
                }

                if(lodIndex != _previousLODIndex)
                {
                    LODMesh lodMesh = _lodMeshes[lodIndex];
                    if (lodMesh.HasMesh)
                    {
                        _previousLODIndex = lodIndex;
                        _meshFilter.mesh = lodMesh.Mesh;
                    }
                    else if (!lodMesh.HasRequestedMesh)
                    {
                        lodMesh.RequestMesh(_mapData);
                    }
                }

                _terrainChunksVisibleLastUpdate.Add(this);
            }

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

    public class LODMesh
    {
        public Mesh Mesh;
        public bool HasRequestedMesh;
        public bool HasMesh;
        private int _lod;
        private Action _updateCallback;

        public LODMesh(int lod, Action updateCallback)
        {
            _lod = lod;
            _updateCallback = updateCallback;
        }

        public void RequestMesh(MapData mapData)
        {
            HasRequestedMesh = true;
            _mapGenerator.RequestMeshData(mapData, _lod, OnMeshDataReceived);
        }

        private void OnMeshDataReceived(MeshData data)
        {
            Mesh = data.CreateMesh();
            HasMesh = true;

            _updateCallback();
        }
    }

    [Serializable]
    public struct LODInfo
    {
        public int Lod;
        public float VisibleDistanceThreshold;
        public LODInfo(int lod, float visibleDistanceThreshold)
        {
            Lod = lod;
            VisibleDistanceThreshold = visibleDistanceThreshold;
        }
    }
}
