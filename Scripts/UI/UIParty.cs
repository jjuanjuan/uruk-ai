using Godot;

public partial class UIParty : Control
{
    public CharacterParty Party;

    [Export] public PackedScene SlotScene;
    [Export] public Control BoardRoot;
    public bool IsFront;

    public void Setup(CharacterParty party, bool front)
    {
        Party = party;
        IsFront = front;

        BuildGrid();
        LayoutSlots();
        Refresh();
    }

    public void SetParty(CharacterParty party)
    {
        Party = party;

        foreach (PartySlot slot in BoardRoot.GetChildren())
        {
            slot.Party = Party;
        }

        Party.PartyChanged += Refresh;

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
                slot.UIParty = this;
                slot.Party = Party;

                BoardRoot.AddChild(slot);
            }
        }
    }

    public void Refresh()
    {
        if (Party == null || BoardRoot == null)
            return;

        foreach (PartySlot slot in BoardRoot.GetChildren())
        {
            var orc = Party.GetOrc(slot.Row, slot.Column);
            slot.SetOrc(orc);
        }
    }

    public Vector2 GetVisualPosition(int row, int col)
    {
        float width = BoardRoot.Size.X;
        float height = BoardRoot.Size.Y;

        float cellW = width / CharacterParty.COLUMNS;
        float cellH = height / CharacterParty.ROWS;

        int visualRow = IsFront
            ? (CharacterParty.ROWS - 1 - row)
            : row;

        float x = col * cellW;
        float y = visualRow * cellH;

        return new Vector2(x, y);
    }
    
    public void LayoutSlots()
    {
        float width = BoardRoot.Size.X;
        float height = BoardRoot.Size.Y;

        float cellW = width / CharacterParty.COLUMNS;
        float cellH = height / CharacterParty.ROWS;

        foreach (PartySlot slot in BoardRoot.GetChildren())
        {
            slot.IsFront = IsFront;

            int visualRow = IsFront
                ? (CharacterParty.ROWS - 1 - slot.Row)
                : slot.Row;

            slot.Position = new Vector2(
                slot.Column * cellW,
                visualRow * cellH
            );

            slot.Size = new Vector2(cellW, cellH);
        }
    }
}