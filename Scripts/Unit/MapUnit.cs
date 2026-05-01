using Godot;
using System.Collections.Generic;

public partial class MapUnit : Area2D
{
    [Export] public CharacterParty Party;
    [Export] public float BaseSpeed = 120f; // px/seg
    [Export] public TextureRect LeaderTexture;

    public Vector2I GridPosition { get; private set; }

    private MapManager _map;
    private List<Vector2> _pathWorld = new();
    private int _pathIndex = 0;
    private bool _moving = false;
    private bool _selected = false;
    private float _currentSpeed;

    public MovementType MovementType =>
        Party?.GetLeader()?.CharacterClass?.MovementType ?? MovementType.Ground;

    public override void _Ready()
    {
        AddToGroup("map_unit");

        _map = GetTree().GetFirstNodeInGroup("map_manager") as MapManager;

        if (_map == null)
            GD.PrintErr("MapManager not found!");
    }

    public void Init()
    {
        _map = GetTree().GetFirstNodeInGroup("map_manager") as MapManager;

        GridPosition = WorldToGrid(Position);
        Position = GridToWorld(GridPosition);

        _currentSpeed = BaseSpeed;

        LeaderTexture.Texture = Party.GetLeader().CharacterClass.GetFrontTexture();
    }

    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (@event is not InputEventMouseButton mb || !mb.Pressed)
            return;

        if (mb.ButtonIndex == MouseButton.Left)
        {
            Select();
        }
        else if (mb.ButtonIndex == MouseButton.Right)
        {
            IssueMoveCommand();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_moving || _pathIndex >= _pathWorld.Count)
            return;

        var target = _pathWorld[_pathIndex];

        Vector2 toTarget = target - Position;
        float dist = toTarget.Length();

        // snap limpio al llegar
        if (dist < 2f)
        {
            Position = target;
            _pathIndex++;

            if (_pathIndex >= _pathWorld.Count)
            {
                _moving = false;
                OnPathFinished();
            }

            return;
        }

        Vector2 dir = toTarget / dist;

        // terreno actual
        var cell = _map.GetCell(WorldToGrid(Position));

        // seguridad: no caminar sobre bloqueado
        if (cell.TerrainData == null || !cell.TerrainData.Walkable)
        {
            _moving = false;
            return;
        }

        float targetSpeed = GetSpeed(cell);

        // suavizado (inercia ligera tipo Ogre Battle)
        _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, 0.15f);

        float step = _currentSpeed * (float)delta;

        // evitar overshoot
        if (step > dist)
            step = dist;

        Position += dir * step;

        // actualizar grid lógico
        GridPosition = WorldToGrid(Position);
    }

    // ---------------------------------------
    // PATH API
    // ---------------------------------------
    public void MoveTo(Vector2I targetGrid)
    {
        GD.Print("Map: ", _map);
        GD.Print($"FROM {GridPosition} TO {targetGrid}");
        GD.Print("MovementType: ", MovementType);

        if (_map == null)
        {
            _map = GetTree().GetFirstNodeInGroup("map_manager") as MapManager;

            if (_map == null)
            {
                GD.PrintErr("MapManager still null in MoveTo");
                return;
            }
        }

        GD.Print($"MoveTo called");
        GD.Print($"From: {GridPosition}, To: {targetGrid}");

        for (int x = targetGrid.X - 1; x <= targetGrid.X + 1; x++)
        {
            for (int y = targetGrid.Y - 1; y <= targetGrid.Y + 1; y++)
            {
                var cell = _map.GetCell(new Vector2I(x, y));
                GD.Print($"Cell {x},{y}: {cell}");
            }
        }

        var gridPath = _map.GetPath(GridPosition, targetGrid, MovementType);

        GD.Print($"Path size: {gridPath.Count}");

        if (gridPath.Count > 0)
        {
            foreach (var p in gridPath)
                GD.Print($"  step: {p}");
        }

        _pathWorld.Clear();

        if (gridPath.Count == 0)
        {
            _moving = false;
            return;
        }

        foreach (var p in gridPath)
            _pathWorld.Add(GridToWorld(p));

        _pathIndex = 0;
        _moving = true;
    }

    public void Stop()
    {
        _moving = false;
        _pathWorld.Clear();
    }

    // ---------------------------------------
    // SPEED
    // ---------------------------------------
    float GetSpeed(MapCell cell)
    {
        float cost = cell.TerrainData.GetCost(MovementType);

        if (cell.FeatureData != null)
            foreach (var feature in cell.FeatureData.MovementModifier)
            {
                cost += feature.Cost;
            }

        cost = Mathf.Max(0.1f, cost);

        return BaseSpeed / cost;
    }

    // ---------------------------------------
    // GRID <-> WORLD
    // ---------------------------------------
    const int TILE_SIZE = 64;
    const int HALF_TILE = TILE_SIZE / 2;

    Vector2 GridToWorld(Vector2I grid)
    {
        return new Vector2(
            grid.X * TILE_SIZE + HALF_TILE,
            grid.Y * TILE_SIZE + HALF_TILE
        );
    }

    Vector2I WorldToGrid(Vector2 world)
    {
        return new Vector2I(
            Mathf.FloorToInt(world.X / TILE_SIZE),
            Mathf.FloorToInt(world.Y / TILE_SIZE)
        );
    }

    // ---------------------------------------
    void OnPathFinished()
    {
        GD.Print($"Arrived at {GridPosition}");
    }

    // SELECTION
    void Select()
    {
        SelectionManager.I.Select(this);
    }
    void IssueMoveCommand()
    {
        if (SelectionManager.I.Selected != this)
            return;

        Vector2 mouseWorld = GetGlobalMousePosition();

        Vector2I grid = new Vector2I(
            Mathf.FloorToInt(mouseWorld.X / 64),
            Mathf.FloorToInt(mouseWorld.Y / 64)
        );

        MoveTo(grid);
    }

    // For feedback
    public void SetSelected(bool value)
    {
        _selected = value;
        Modulate = value ? Colors.Yellow : Colors.White;
    }
}