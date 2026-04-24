using Godot;

public partial class UIParty : Control
{
    [Export] public PackedScene SlotScene;
    [Export] public CharacterParty Party;

    GridContainer grid;

    public override void _Ready()
    {
        grid = GetNode<GridContainer>("Grid");

        BuildGrid();
        Refresh();
    }

    void BuildGrid()
    {
        for (int r = 0; r < CharacterParty.ROWS; r++)
        {
            for (int c = 0; c < CharacterParty.COLUMNS; c++)
            {
                var slot = SlotScene.Instantiate<PartySlot>();

                slot.Row = r;
                slot.Column = c;
                slot.Party = Party;

                grid.AddChild(slot);
            }
        }
    }

    public void Refresh()
    {
        int i = 0;

        foreach (PartySlot slot in grid.GetChildren())
        {
            int r = slot.Row;
            int c = slot.Column;

            var orc = Party.GetOrc(r, c);
            slot.SetOrc(orc);

            i++;
        }
    }
}