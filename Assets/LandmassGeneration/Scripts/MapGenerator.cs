using System;
using UnityEngine;
using System.Threading;
using System.Collections.Generic;
using UnityEngine.AI;
using static MeshGenerator;

public class MapGenerator : MonoBehaviour
{
    [Header("Map Settings")]
    [SerializeField] private MapDisplay _mapDisplay;
    [Range(0, 6)]
    [SerializeField] private int _editorLevelOfDetail;
    public const int MAP_CHUNK_SIZE = 241;

    [Space]
    [Header("Noise Settings")]
    [SerializeField] private float _noiseScale;
    [SerializeField] private int _octaves;
    [Range(0, 1)]
    [SerializeField] private float _persistence;
    [SerializeField] private float _lacunarity;
    [SerializeField] private int _seed;
    [SerializeField] private Vector2 _offset;

    [Space]
    [Header("HeightMap")]
    [SerializeField] private float _meshHeightMultiplier;
    [SerializeField] private AnimationCurve _meshHeightCurve;

    [Space]
    [Header("Draw Mode Options")]
    [SerializeField] private DrawMode _drawMode;
    public enum DrawMode
    {
        NoiseMap,
        ColourMap,
        Mesh,
    };

    [Serializable]
    public struct TerrainType
    {
        public string Name;
        public float Height;
        public Color Colour;
    }
    [SerializeField] private TerrainType[] _regions;

    public bool AutoUpdate => _autoUpdate;
    [SerializeField] private bool _autoUpdate;


    private Queue<MapThreadInfo<MapData>> _mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    private Queue<MapThreadInfo<MeshData>> _meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    private void OnValidate()
    {
        if (_octaves < 0)
        {
            _octaves = 0;
        }
        if (_lacunarity < 1)
        {
            _lacunarity = 1;
        }
    }

    private void Update()
    {
        if (_mapDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < _mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = _mapDataThreadInfoQueue.Dequeue();
                threadInfo.Callback(threadInfo.Parameter);
            }
        }

        if (_meshDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < _meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = _meshDataThreadInfoQueue.Dequeue();
                threadInfo.Callback(threadInfo.Parameter);
            }
        }
    }


    public void RequestMapData(Vector2 center, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(center, callback);
        };
        new Thread(threadStart).Start();
    }

    private void MapDataThread(Vector2 center, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(center);
        lock (_mapDataThreadInfoQueue)
        {
            _mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, lod, callback);
        };
        new Thread(threadStart).Start();
    }

    private void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.HeightMap, _meshHeightMultiplier, _meshHeightCurve, lod);
        lock (_meshDataThreadInfoQueue)
        {
            _meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    private MapData GenerateMapData(Vector2 center)
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(MAP_CHUNK_SIZE, MAP_CHUNK_SIZE, _noiseScale, _octaves, _persistence, _lacunarity, _seed, center + _offset);

        Color[] colourMap = new Color[MAP_CHUNK_SIZE * MAP_CHUNK_SIZE];
        for (int y = 0; y < MAP_CHUNK_SIZE; y++)
        {
            for (int x = 0; x < MAP_CHUNK_SIZE; x++)
            {
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < _regions.Length; i++)
                {
                    if (currentHeight <= _regions[i].Height)
                    {
                        colourMap[y * MAP_CHUNK_SIZE + x] = _regions[i].Colour;
                        break;
                    }
                }
            }
        }

        return new MapData(noiseMap, colourMap);
    }

    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData(Vector2.zero);
        if (_drawMode == DrawMode.ColourMap)
        {
            _mapDisplay.DrawTexture(TextureGenerator.TextureFromColourMap(mapData.ColourMap, MAP_CHUNK_SIZE, MAP_CHUNK_SIZE));
        }
        else if (_drawMode == DrawMode.NoiseMap)
        {
            _mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.HeightMap));
        }
        else if (_drawMode == DrawMode.Mesh)
        {
            _mapDisplay.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.HeightMap, _meshHeightMultiplier, _meshHeightCurve, _editorLevelOfDetail), TextureGenerator.TextureFromColourMap(mapData.ColourMap, MAP_CHUNK_SIZE, MAP_CHUNK_SIZE));
        }
    }

    struct MapThreadInfo<T>
    {
        public readonly Action<T> Callback;
        public readonly T Parameter;
        public MapThreadInfo(Action<T> callback, T parameter)
        {
            Callback = callback;
            Parameter = parameter;
        }
    }

    public struct MapData
    {
        public readonly float[,] HeightMap;
        public readonly Color[] ColourMap;
        public MapData(float[,] heightMap, Color[] colourMap)
        {
            HeightMap = heightMap;
            ColourMap = colourMap;
        }
    }
}
