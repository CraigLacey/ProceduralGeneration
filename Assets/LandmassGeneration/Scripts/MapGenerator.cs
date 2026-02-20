using System;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] private MapDisplay _mapDisplay;
    [SerializeField] private int _mapWidth;
    [SerializeField] private int _mapHeight;
    [SerializeField] private float _noiseScale;
    [SerializeField] private int _octaves;
    [Range(0,1)]
    [SerializeField] private float _persistence;
    [SerializeField] private float _lacunarity;
    [SerializeField] private int _seed;
    [SerializeField] private Vector2 _offset;

    public enum DrawMode { NoiseMap, ColourMap };
    [SerializeField] private DrawMode _drawMode;

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
        if (_mapWidth < 1)
        {
            _mapWidth = 1;
        }
        if (_mapHeight < 1)
        {
            _mapHeight = 1;
        }
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
        float[,] noiseMap = Noise.GenerateNoiseMap(_mapWidth, _mapHeight, _noiseScale, _octaves, _persistence, _lacunarity, _seed, _offset);

        Color[] colourMap = new Color[_mapWidth * _mapHeight];
        for (int y = 0; y < _mapHeight; y++)
        {
            for (int x = 0; x < _mapWidth; x++)
            {
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < _regions.Length; i++)
                {
                    if (currentHeight <= _regions[i].Height)
                    {
                        colourMap[y * _mapWidth + x] = _regions[i].Colour;
                        break;
                    }
                }
            }
        }

        if (_drawMode == DrawMode.ColourMap)
        {
            _mapDisplay.DrawTexture(TextureGenerator.TextureFromColourMap(colourMap, _mapWidth, _mapHeight));
        }
        else if (_drawMode == DrawMode.NoiseMap)
        {
            _mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
        }
    }

}
