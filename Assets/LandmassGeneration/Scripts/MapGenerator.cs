using System;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("Map Settings")]
    [SerializeField] private MapDisplay _mapDisplay;
    [Range(0,6)]
    [SerializeField] private int _levelOfDetail;
    private const int MAP_CHUNK_SIZE = 241;

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

    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(MAP_CHUNK_SIZE, MAP_CHUNK_SIZE, _noiseScale, _octaves, _persistence, _lacunarity, _seed, _offset);

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

        if (_drawMode == DrawMode.ColourMap)
        {
            _mapDisplay.DrawTexture(TextureGenerator.TextureFromColourMap(colourMap, MAP_CHUNK_SIZE, MAP_CHUNK_SIZE));
        }
        else if (_drawMode == DrawMode.NoiseMap)
        {
            _mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
        }
        else if (_drawMode == DrawMode.Mesh)
        {
            _mapDisplay.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap, _meshHeightMultiplier, _meshHeightCurve, _levelOfDetail), TextureGenerator.TextureFromColourMap(colourMap, MAP_CHUNK_SIZE, MAP_CHUNK_SIZE));
        }
    }

}
