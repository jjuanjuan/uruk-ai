using Godot;

public partial class UnitSpawner : Node
{
    [Export] public PackedScene MapUnitScene;
    [Export] public Node2D UnitsContainer;

    public override void _Ready()
    {
        AddToGroup("unit_spawner");
    }

    public MapUnit Spawn(Vector2I gridPos, CharacterParty party)
    {
        GD.Print("MapUnitScene: ", MapUnitScene);
        GD.Print("UnitsContainer: ", UnitsContainer);

        if (MapUnitScene == null)
        {
            GD.PrintErr("MapUnitScene is NULL");
            return null;
        }

        if (UnitsContainer == null)
        {
            GD.PrintErr("UnitsContainer is NULL");
            return null;
        }

        var node = MapUnitScene.Instantiate();

        if (node is not MapUnit unit)
        {
            GD.PrintErr($"Wrong scene: {node.GetType()}");
            return null;
        }

        UnitsContainer.AddChild(unit);

        unit.Position = GridToWorld(gridPos);
        unit.Party = party;
        unit.Init();

        return unit;
    }

    Vector2 GridToWorld(Vector2I grid)
    {
        return new Vector2(grid.X * 64 + 32, grid.Y * 64 + 32);
    }
}