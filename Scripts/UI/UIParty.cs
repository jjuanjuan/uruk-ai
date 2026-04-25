using Godot;

public partial class UIParty : Control
{
    public CharacterParty Party;

    [Export] public PackedScene SlotScene;
    [Export] public Control BoardRoot;
    [Export] public Node2D UnitsRoot;

    public void Setup(CharacterParty party)
    {
        Party = party;

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
        if (Party == null || UnitsRoot == null)
            return;

        foreach (var orc in Party.GetAllOrcs())
        {
            if (orc == null)
                continue;

            if (!orc.IsInsideTree())
            {
                UnitsRoot.AddChild(orc);
            }

            if (!IsInstanceValid(orc))
                continue;

            if (orc.GetParent() != UnitsRoot)
            {
                if (orc.GetParent() != null)
                {
                    orc.GetParent().RemoveChild(orc);
                }

                UnitsRoot.AddChild(orc);
            }

            orc.Visible = true;
            orc.ZIndex = 100;

            orc.Position = GetVisualPosition(
                orc.PartyPosition.Row,
                orc.PartyPosition.Column
            );

            GD.Print($"{orc.GetFirstName()} parent: {orc.GetParent()} insideTree: {orc.IsInsideTree()}");
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

            slot.Size = new Vector2(cellW, cellH);
        }
    }
}