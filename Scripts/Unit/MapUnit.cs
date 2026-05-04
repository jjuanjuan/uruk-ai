using Godot;
using System.Collections.Generic;

public partial class MapUnit : Area2D
{
    [Export] public float BaseSpeed = 120f; // px/seg
    [Export] public TextureRect LeaderTexture;
    
    public CharacterParty Party;
    public Team Team => Party?.Team;
    public Vector2I GridPosition { get; private set; }

    MapManager _map;
    List<Vector2> _pathWorld = new();
    int _pathIndex = 0;
    bool _moving = false;
    bool _selected = false;
    float _currentSpeed;

    public MovementType MovementType => Party.GetLeader().CharacterClass.MovementType;

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

        GridPosition = _map.WorldToGrid(Position);
        Position = _map.GridToWorld(GridPosition);

        _currentSpeed = BaseSpeed;

        LeaderTexture.Texture = Party.GetLeader().CharacterClass.GetFrontTexture();
    }
    public void Setup(CharacterParty party)
    {
        Party = party;
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
        var cell = _map.GetCell(_map.WorldToGrid(Position));

        // no caminar sobre bloqueado
        if (cell.TerrainData == null || !cell.TerrainData.Walkable)
        {
            _moving = false;
            return;
        }

        float targetSpeed = GetSpeed(cell);
        _currentSpeed = targetSpeed;
        float step = _currentSpeed * (float)delta;

        // evitar overshoot
        if (step > dist)
            step = dist;

        Position += dir * step;

        // actualizar grid lógico
        GridPosition = _map.WorldToGrid(Position);
    }

    // ---------------------------------------
    // PATH API
    // ---------------------------------------
    public void MoveTo(Vector2I targetGrid)
    {
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

        targetGrid = _map.ClampToMap(targetGrid);

        var gridPath = _map.GetPath(GridPosition, targetGrid, MovementType);
        //gridPath = _map.SmoothPath(gridPath); no me copó este smooth

        GD.Print($"Path size: {gridPath.Count}");

        _pathWorld.Clear();

        if (gridPath.Count == 0)
        {
            _moving = false;
            return;
        }

        foreach (var p in gridPath)
            _pathWorld.Add(_map.GridToWorld(p));

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
        {
            cost += cell.FeatureData.GetCost(MovementType);
        }

        cost = Mathf.Max(0.5f, cost);

        return BaseSpeed / cost;
    }
    // ---------------------------------------
    void OnPathFinished()
    {
        //GD.Print($"Arrived at {GridPosition}");
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