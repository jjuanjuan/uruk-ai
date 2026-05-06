using Godot;
using System;
using System.Collections.Generic;

public partial class MapManager : Node
{
    [Export] public TileMapLayer TerrainLayer;
    [Export] public TileMapLayer FeatureLayer;
    [Export] public TileMapLayer BuildingLayer;
    [Export] public CameraController MapCamera;

    public UICombatScene CombatScene;
    public List<MapUnit> Units = new();
    Dictionary<MovementType, AStar2D> _astarCache = new();

    Rect2I _usedRect;

    // Grid
    Dictionary<Vector2I, int> _pointIds = new();
    Dictionary<int, Vector2I> _idToPos = new();

    const int TILE_SIZE = 64;
    const int HALF_TILE = TILE_SIZE / 2;

    int _width;
    int _height;
    bool paused = false;

    MapUnit combatUnitA;
    MapUnit combatUnitB;

    static readonly Vector2I[] Directions8 = new[]
    {
        new Vector2I(0, -1),
        new Vector2I(0,  1),
        new Vector2I(-1, 0),
        new Vector2I(1,  0),

        new Vector2I(-1, -1),
        new Vector2I(1, -1),
        new Vector2I(-1, 1),
        new Vector2I(1,  1),
    };

    public Vector2 GridToWorld(Vector2I grid)
    {
        return new Vector2(
            grid.X * TILE_SIZE + HALF_TILE,
            grid.Y * TILE_SIZE + HALF_TILE
        );
    }

    public Vector2I WorldToGrid(Vector2 world)
    {
        return new Vector2I(
            Mathf.FloorToInt(world.X / TILE_SIZE),
            Mathf.FloorToInt(world.Y / TILE_SIZE)
        );
    }

    // ---------------------------------------
    // INIT
    // ---------------------------------------
    public override void _Ready()
    {
        GameManager.I.MapManager = this;

        BuildGrid();
        BuildAllAStars();

        AddToGroup("map_manager");

        MapCamera.Init();
    }

    public override void _ExitTree()
    {
        if (GameManager.I.MapManager == this)
            GameManager.I.MapManager = null;
    }

    // ---------------------------------------
    // CHECK FOR ENCOUNTERS
    // ---------------------------------------
    public override void _Process(double delta)
    {
        CheckEncounters();
    }

    void CheckEncounters()
    {
        if (paused)
            return;

        for (int i = 0; i < Units.Count; i++)
        {
            var a = Units[i];
            if (a == null || a.Party == null) continue;

            for (int j = i + 1; j < Units.Count; j++)
            {
                var b = Units[j];
                if (b == null || b.Party == null) continue;

                // mismo team → ignorar
                if (a.Team == b.Team)
                    continue;

                // condición de encuentro
                if (AreUnitsColliding(a, b))
                {
                    bool aSeesB = a.CanSee(b);
                    bool bSeesA = b.CanSee(a);

                    if (aSeesB && !bSeesA)
                    {
                        GD.Print("A ambushes B");
                        StartCombat(a, b, EncounterType.A_Ambushes_B);
                    }
                    else if (!aSeesB && bSeesA)
                    {
                        GD.Print("B ambushes A");
                        StartCombat(b, a, EncounterType.B_Ambushes_A);
                    }
                    else
                    {
                        GD.Print("Normal combat");
                        StartCombat(a, b, EncounterType.Equal);
                    }

                    return;
                }
            }
        }
    }
    bool AreUnitsColliding(MapUnit a, MapUnit b)
    {
        return a.GlobalPosition.DistanceTo(b.GlobalPosition) < GameManager.I.CombatConfig.CombatTriggerDistance;
    }

    void StartCombat(MapUnit aUnit, MapUnit bUnit, EncounterType type)
    {
        paused = true;

        GD.Print($"Combat: {aUnit.Name} vs {bUnit.Name}");

        aUnit.Stop();
        bUnit.Stop();

        combatUnitA = aUnit;
        combatUnitB = bUnit;

        CombatScene = GameManager.I.CombatScene.Instantiate<UICombatScene>();
        var uiRoot = GetTree().GetFirstNodeInGroup("ui_root");
        uiRoot.AddChild(CombatScene);

        CharacterParty front;
        CharacterParty back;

        switch (type)
        {
            case EncounterType.A_Ambushes_B:
                front = combatUnitA.Party;
                back = combatUnitB.Party;
                break;

            case EncounterType.B_Ambushes_A:
                front = combatUnitB.Party;
                back = combatUnitA.Party;
                break;

            default: // Equal
                front = combatUnitA.Party;
                back = combatUnitB.Party;
                break;
        }

        CombatScene.Setup(front, back);

        CombatScene.CombatManager.Connect(
            CombatManager.SignalName.CombatFinished,
            new Callable(this, nameof(OnCombatFinished))
        );
    }

    void OnCombatFinished(CharacterParty winnerParty, CharacterParty loserParty)
    {
        GD.Print("On Combat finished");

        CombatScene.QueueFree();

        var winner = GetUnitFromParty(winnerParty);
        var loser = GetUnitFromParty(loserParty);

        // destroy loser if all units were killed
        if (!loserParty.HasLivingOrcs())
        {
            Units.Remove(loser);
            loser.QueueFree();
        }
        else
        {
            PushLoser(loser, winner.Position);
        }
    }

    // ---------------------------------------
    // GRID BUILD
    // ---------------------------------------
    void BuildGrid()
    {
        _pointIds.Clear();
        _idToPos.Clear();

        int id = 0;

        foreach (var pos in TerrainLayer.GetUsedCells())
        {
            _pointIds[pos] = id;
            _idToPos[id] = pos;
            id++;
        }
        _usedRect = TerrainLayer.GetUsedRect();
    }

    // ---------------------------------------
    // BUILD ALL A*
    // ---------------------------------------
    void BuildAllAStars()
    {
        _astarCache.Clear();

        foreach (MovementType type in Enum.GetValues(typeof(MovementType)))
        {
            var astar = new AStar2D();

            // Nodos
            foreach (var kv in _pointIds)
                astar.AddPoint(kv.Value, kv.Key);

            // Conexiones
            foreach (var kv in _pointIds)
            {
                var pos = kv.Key;
                int id = kv.Value;

                foreach (var dir in Directions8)
                {
                    var neighborPos = pos + dir;

                    if (!_pointIds.ContainsKey(neighborPos))
                        continue;

                    int neighborId = _pointIds[neighborPos];

                    var cell = GetCell(pos);
                    var neighbor = GetCell(neighborPos);

                    if (!cell.TerrainData.Walkable || !neighbor.TerrainData.Walkable)
                        continue;

                    // evitar corner cutting
                    if (dir.X != 0 && dir.Y != 0)
                    {
                        var side1 = GetCell(pos + new Vector2I(dir.X, 0));
                        var side2 = GetCell(pos + new Vector2I(0, dir.Y));

                        if (!side1.TerrainData.Walkable || !side2.TerrainData.Walkable)
                            continue;
                    }

                    if (!astar.ArePointsConnected(id, neighborId))
                        astar.ConnectPoints(id, neighborId);
                }
            }
            // Pesos
            foreach (var kv in _pointIds)
            {
                var pos = kv.Key;
                int id = kv.Value;

                var cell = GetCell(pos);
                float weight = GetCellCost(cell, type);

                astar.SetPointWeightScale(id, weight);
            }

            _astarCache[type] = astar;
        }
    }

    // ---------------------------------------
    // PATH
    // ---------------------------------------
    public List<Vector2I> GetPath(Vector2I from, Vector2I to, MovementType movementType)
    {
        var path = new List<Vector2I>();

        if (!_pointIds.ContainsKey(from) || !_pointIds.ContainsKey(to))
            return path;

        if (!_astarCache.TryGetValue(movementType, out var astar))
            return path;

        int fromId = _pointIds[from];
        int toId = _pointIds[to];

        var idPath = astar.GetPointPath(fromId, toId);

        foreach (var p in idPath)
            path.Add((Vector2I)p);

        return path;
    }

    // no me copó este Smooth, no lo uso //
    public List<Vector2I> SmoothPath(List<Vector2I> path)
    {
        if (path.Count <= 2)
            return path;

        var result = new List<Vector2I>();
        int current = 0;

        result.Add(path[current]);

        while (current < path.Count - 1)
        {
            int next = path.Count - 1;

            for (int i = path.Count - 1; i > current; i--)
            {
                if (HasLineOfSight(path[current], path[i]))
                {
                    next = i;
                    break;
                }
            }

            result.Add(path[next]);
            current = next;
        }

        return result;
    }

    bool HasLineOfSight(Vector2I a, Vector2I b)
    {
        Vector2 start = GridToWorld(a);
        Vector2 end = GridToWorld(b);

        Vector2 dir = (end - start).Normalized();
        float dist = start.DistanceTo(end);

        float step = 16f; // resolución

        for (float t = 0; t < dist; t += step)
        {
            Vector2 p = start + dir * t;
            var cell = GetCell(WorldToGrid(p));

            if (cell.TerrainData == null || !cell.TerrainData.Walkable)
                return false;
        }

        return true;
    }
    // ---------------------------------------
    // COST
    // ---------------------------------------
    float GetCellCost(MapCell cell, MovementType movementType)
    {
        if (cell.TerrainData == null || !cell.TerrainData.Walkable)
            return float.MaxValue;

        float cost = cell.TerrainData.GetCost(movementType);

        if (cell.FeatureData != null)
        {
            foreach (var feature in cell.FeatureData.MovementModifier)
            {
                cost += feature.Cost;
            }
        }

        if (cell.BuildingData != null && cell.BuildingData.BlocksMovement)
            return float.MaxValue;

        return Mathf.Max(0.01f, cost);
    }

    // ---------------------------------------
    // MAP CELL
    // ---------------------------------------
    public MapCell GetCell(Vector2I pos)
    {
        var terrain = GetTerrainType(pos);

        // esto pasa si tocás fuera del mapa
        if (terrain == TerrainType.None)
        {
            GD.Print($"Invalid terrain at {pos}");
            // encontrar el tile más cercano
            pos = ClampToMap(pos);
            GD.Print($"New {pos}");
            terrain = GetTerrainType(pos);
        }

        var feature = GetFeatureType(pos);
        var building = GetBuildingType(pos);

        return new MapCell
        {
            Position = pos,
            Terrain = terrain,
            Feature = feature,
            Building = building,

            TerrainData = GameManager.I.Database.GetTerrain(terrain),
            FeatureData = GameManager.I.Database.GetTerrainFeature(feature),
            BuildingData = GameManager.I.Database.GetBuilding(building)
        };
    }

    public Vector2I ClampToMap(Vector2I pos)
    {
        int x = Mathf.Clamp(pos.X,
            _usedRect.Position.X,
            _usedRect.End.X - 1);

        int y = Mathf.Clamp(pos.Y,
            _usedRect.Position.Y,
            _usedRect.End.Y - 1);

        return new Vector2I(x, y);
    }

    // ---------------------------------------
    // TILEMAP READERS
    // ---------------------------------------

    TerrainType GetTerrainType(Vector2I pos)
    {
        var data = TerrainLayer.GetCellTileData(pos);
        if (data == null) return TerrainType.None;

        var value = data.GetCustomData("terrain_type");
        return value.VariantType == Variant.Type.Nil
            ? TerrainType.None
            : (TerrainType)(int)value;
    }
    TerrainFeatureType GetFeatureType(Vector2I pos)
    {
        var data = FeatureLayer?.GetCellTileData(pos);
        if (data == null) return TerrainFeatureType.None;

        var value = data.GetCustomData("feature_type");
        return value.VariantType == Variant.Type.Nil
            ? TerrainFeatureType.None
            : (TerrainFeatureType)(int)value;
    }
    BuildingType GetBuildingType(Vector2I pos)
    {
        if (BuildingLayer == null)
        {
            GD.PrintErr("BuildingLayer NULL");
            return BuildingType.None;
        }

        int sourceId = BuildingLayer.GetCellSourceId(pos);

        if (sourceId == -1)
            return BuildingType.None;

        Vector2I atlas = BuildingLayer.GetCellAtlasCoords(pos);

        var source = BuildingLayer.TileSet.GetSource(sourceId) as TileSetAtlasSource;

        if (source == null)
        {
            GD.PrintErr($"Source NULL at {pos}");
            return BuildingType.None;
        }

        if (!source.HasTile(atlas))
        {
            GD.PrintErr($"INVALID TILE at {pos} atlas={atlas}");
            return BuildingType.None;
        }

        var data = BuildingLayer.GetCellTileData(pos);

        if (data == null)
            return BuildingType.None;

        return (BuildingType)(int)data.GetCustomData("building_type");
    }

    // ---------------------------------------
    // DYNAMIC UPDATE
    // ---------------------------------------
    public void UpdateCell(Vector2I pos)
    {
        if (!_pointIds.ContainsKey(pos))
            return;

        var cell = GetCell(pos);

        if (cell.TerrainData == null)
            return;

        int id = _pointIds[pos];

        foreach (var kv in _astarCache)
        {
            var type = kv.Key;
            var astar = kv.Value;

            float weight = GetCellCost(cell, type);
            astar.SetPointWeightScale(id, weight);
        }
    }

    // ---------------------------------------
    // FULL REBUILD (fallback)
    // ---------------------------------------
    public void RebuildAll()
    {
        BuildGrid();
        BuildAllAStars();
    }

    // ---------------------------------------
    // BOUNDS
    // ---------------------------------------
    public Rect2 GetWorldBounds()
    {
        Vector2 topLeft = TerrainLayer.MapToLocal(_usedRect.Position);
        Vector2 bottomRight = TerrainLayer.MapToLocal(_usedRect.Position + _usedRect.Size);

        return new Rect2(topLeft, bottomRight - topLeft);
    }

    // ---------------------------------------
    // UNITS
    // ---------------------------------------
    public void RegisterUnit(MapUnit unit)
    {
        if (!Units.Contains(unit))
            Units.Add(unit);
    }

    public void UnregisterUnit(MapUnit unit)
    {
        Units.Remove(unit);
    }

    public List<MapUnit> GetUnitsByTeam(TeamId teamId)
    {
        return Units.FindAll(u =>
            u.Party != null &&
            u.Party.Team != null &&
            u.Party.Team.Id == teamId
        );
    }

    public List<MapUnit> GetEnemyUnits(TeamId teamId)
    {
        return Units.FindAll(u =>
            u.Party != null &&
            u.Party.Team != null &&
            u.Party.Team.Id != teamId
        );
    }

    MapUnit GetUnitFromParty(CharacterParty party)
    {
        return Units.Find(u => u.Party == party);
    }

    void PushLoser(MapUnit loser, Vector2 lastAttackerPosition)
    {
        Vector2 pushDir = (loser.GlobalPosition - lastAttackerPosition).Normalized();
        Vector2 target = loser.GlobalPosition + pushDir * GameManager.I.CombatConfig.LoserPushDistance;

        loser.PushTo(target);

        loser.Connect(
            MapUnit.SignalName.PushFinished,
            new Callable(this, nameof(OnPushFinished)),
            (uint)ConnectFlags.OneShot
        );
    }

    void OnPushFinished()
    {
        GD.Print("Push finished → resume map");

        paused = false;
    }
}