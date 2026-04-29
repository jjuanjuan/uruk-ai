using Godot;
using System;

public struct MapCell
{
    public Vector2I Position;

    public TerrainType Terrain;
    public TerrainFeatureType Feature;
    public BuildingType Building;

    public TerrainData TerrainData;
    public TerrainFeatureData FeatureData;
    public BuildingData BuildingData;
}