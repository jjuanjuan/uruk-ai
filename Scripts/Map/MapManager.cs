using Godot;
using System;

public partial class MapManager : Node
{
    [Export] public TileMapLayer TerrainTileMap;
    [Export] public TileMapLayer FeatureTileMap;
    [Export] public TileMapLayer BuildingTileMap;

    [Export] public GameDatabase Database;

}

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