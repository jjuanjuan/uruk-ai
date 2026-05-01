using Godot;
using System;
using System.Collections.Generic;

[GlobalClass]
public partial class TerrainData : Resource
{
    [Export] public TerrainType Type;
    [Export] public bool Walkable = true;
    [Export] public Godot.Collections.Array<MovementCostEntry> MovementCosts;
    int DefaultCost = 1;

    private Dictionary<MovementType, int> _movementCostMap;

    public void BuildCache()
    {
        _movementCostMap = new Dictionary<MovementType, int>();

        foreach (MovementType mt in Enum.GetValues(typeof(MovementType)))
            _movementCostMap[mt] = DefaultCost;

        if (MovementCosts == null)
            return;

        foreach (var entry in MovementCosts)
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

public enum TerrainType
{
    None = 0,
    Grass,
    Water,
}
