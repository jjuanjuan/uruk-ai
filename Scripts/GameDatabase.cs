using Godot;
using System;
using System.Collections.Generic;

public partial class GameDatabase : Node
{
    public Dictionary<TerrainType, TerrainData> TerrainDB;
    public Dictionary<TerrainFeatureType, TerrainFeatureData> FeatureDB;
    public Dictionary<BuildingType, BuildingData> BuildingDB;

    public TerrainData GetTerrain(TerrainType type)
        => TerrainDB[type];
    public TerrainFeatureData GetTerrainFeature(TerrainFeatureType type)
        => FeatureDB[type];
    public BuildingData GetBuilding(BuildingType type)
        => BuildingDB[type];

    public override void _Ready()
    {
        foreach (var terrain in TerrainDB.Values)
            terrain.BuildCache();
        foreach (var feature in FeatureDB.Values)
            feature.BuildCache();
    }

}