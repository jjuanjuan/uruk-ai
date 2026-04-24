using Godot;

public partial class UIParty : Control
{
    [Export] public PackedScene SlotScene;
    [Export] public CharacterParty Party;
    [Export] public Control BoardRoot;

    public override void _Ready()
    {
        BuildGrid();
        CallDeferred(nameof(LayoutSlots));
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
                slot.UIParty = this;

                BoardRoot.AddChild(slot);
            }
        }
    }

    public void Refresh()
    {
        foreach (PartySlot slot in BoardRoot.GetChildren())
        {
            var orc = Party.GetOrc(slot.Row, slot.Column);
            slot.SetOrc(orc);
        }
    }

    public void LayoutSlots()
    {
        float width = BoardRoot.Size.X;
        float height = BoardRoot.Size.Y;

        float cellW = width / CharacterParty.COLUMNS;
        float cellH = height / CharacterParty.ROWS;

        foreach (PartySlot slot in BoardRoot.GetChildren())
        {
            slot.Position = new Vector2(
                slot.Column * cellW,
                slot.Row * cellH
            );

            slot.CustomMinimumSize = new Vector2(cellW, cellH);
            slot.Size = new Vector2(cellW, cellH);
        }
    }

    public Vector2 GetVisualPosition(int row, int col)
    {
        float width = BoardRoot.Size.X;
        float height = BoardRoot.Size.Y;

        float[] xMap = [0.00f, 0.25f, 0.5f, 0.75f, 1.00f];

        float x = xMap[col] * width;
        float y = row * (height / CharacterParty.ROWS);

        return new Vector2(x, y);
    }
}