using Godot;
using System.Collections.Generic;

public partial class GameDatabase : Node
{
    // ---------------------------------------
    // DATA
    // ---------------------------------------
    [Export] public TerrainData[] TerrainList;
    [Export] public TerrainFeatureData[] FeatureList;
    [Export] public BuildingData[] BuildingList;

    // ---------------------------------------
    // RUNTIME CACHE
    // ---------------------------------------
    private Dictionary<TerrainType, TerrainData> TerrainDB;
    private Dictionary<TerrainFeatureType, TerrainFeatureData> FeatureDB;
    private Dictionary<BuildingType, BuildingData> BuildingDB;

    // ---------------------------------------
    // INIT
    // ---------------------------------------
    public override void _Ready()
    {
        BuildDatabases();
    }

    void BuildDatabases()
    {
        TerrainDB = new();
        FeatureDB = new();
        BuildingDB = new();

        // ---- TERRAIN ----
        if (TerrainList == null || TerrainList.Length == 0)
        {
            GD.PrintErr("TerrainList is empty or NULL");
        }
        else
        {
            foreach (var t in TerrainList)
            {
                if (t == null) continue;

                TerrainDB[t.Type] = t;
                t.BuildCache();
            }
        }

        // ---- FEATURES ----
        if (FeatureList != null)
        {
            foreach (var f in FeatureList)
            {
                if (f == null) continue;

                FeatureDB[f.Type] = f;
                f.BuildCache();
            }
        }

        // ---- BUILDINGS ----
        if (BuildingList != null)
        {
            foreach (var b in BuildingList)
            {
                if (b == null) continue;

                BuildingDB[b.Type] = b;
            }
        }
    }

    // ---------------------------------------
    // GETTERS
    // ---------------------------------------
    public TerrainData GetTerrain(TerrainType type)
    {
        if (TerrainDB != null && TerrainDB.TryGetValue(type, out var t))
            return t;

        GD.PrintErr($"Terrain not found: {type}");
        return null;
    }

    public TerrainFeatureData GetTerrainFeature(TerrainFeatureType type)
    {
        if (FeatureDB != null && FeatureDB.TryGetValue(type, out var f))
            return f;

        return null;
    }

    public BuildingData GetBuilding(BuildingType type)
    {
        if (BuildingDB != null && BuildingDB.TryGetValue(type, out var b))
            return b;

        return null;
    }
}