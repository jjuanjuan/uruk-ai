using Godot;
using System;
using System.Collections.Generic;

[GlobalClass]
public partial class TerrainFeatureData : Resource
{
    [Export] public TerrainFeatureType Type;
    [Export] public bool Walkable = true;
    [Export] public bool BlocksVision = false;
    [Export] public Godot.Collections.Array<MovementCostEntry> MovementModifiers;
    int DefaultCost = 0;

    private Dictionary<MovementType, int> _movementCostMap;

    public void BuildCache()
    {
        _movementCostMap = new Dictionary<MovementType, int>();

        if (MovementModifiers == null)
            return;

        foreach (var entry in MovementModifiers)
            _movementCostMap[entry.Type] = entry.Cost;
    }

    public int GetCost(MovementType type)
    {
        if (_movementCostMap == null)
            BuildCache();

        return _movementCostMap.TryGetValue(type, out var cost)
            ? cost
            : DefaultCost;
    }

}

public enum TerrainFeatureType
{
    None = 0,
    Forest,
    Mountain,
    Road,
}
