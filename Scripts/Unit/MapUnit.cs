using Godot;

public partial class MapUnit : Node2D
{
    [Export] public CharacterParty Party;
    public Vector2I GridPosition;
    public OrcInstance Leader => Party?.GetLeader();
    public MovementType MovementType
    {
        get
        {
            var leader = Party?.GetLeader();
            return leader?.CharacterClass?.GetMovementType() ?? MovementType.Ground;
        }
    }

    public override void _Ready()
    {
        if (Party != null)
            Party.PartyChanged += OnPartyChanged;
    }

    private void OnPartyChanged()
    {
        // Delete the MapUnit if its defeated
        if (Party.IsDefeated())
            QueueFree();
    }

    public void SetGridPosition(Vector2I pos)
    {
        GridPosition = pos;
        Position = GridToWorld(pos);
    }

    private Vector2 GridToWorld(Vector2I grid)
    {
        return new Vector2(grid.X * 64, grid.Y * 64);
    }

    public int GetMovementCost(MapCell cell)
    {
        return cell.TerrainData.GetCost(MovementType);
    }
}