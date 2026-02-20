using System;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] private MapDisplay _mapDisplay;
    [SerializeField] private int _mapWidth;
    [SerializeField] private int _mapHeight;
    [SerializeField] private float _noiseScale;

    public bool AutoUpdate => _autoUpdate;
    [SerializeField] private bool _autoUpdate;

    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(_mapWidth, _mapHeight, _noiseScale);
        _mapDisplay.DrawNoiseMap(noiseMap);
    }
}
