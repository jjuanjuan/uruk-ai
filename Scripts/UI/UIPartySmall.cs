using Godot;

public partial class UIPartySmall : Control
{
    public CharacterParty Party;

    [Export] public Control BoardRoot;

    StyleBoxFlat normalStyle;
    StyleBoxFlat selectedStyle;
    void ApplyNormal() => AddThemeStyleboxOverride("panel", normalStyle);
    void ApplySelected() => AddThemeStyleboxOverride("panel", selectedStyle);

    public override void _Ready()
    {
        BuildStyles();
        ApplyNormal();

        SelectionManager.I.SelectedPartyChanged += OnSelectedPartyChanged;
    }
    public override void _ExitTree()
    {
        if (SelectionManager.I != null)
        {
            SelectionManager.I.SelectedPartyChanged -= OnSelectedPartyChanged;
        }
    }

    void OnSelectedPartyChanged(CharacterParty party)
    {
        if (party == Party)
            ApplySelected();
        else
            ApplyNormal();
    }

    public void Setup(CharacterParty party)
    {
        AssignSlots();
        SetParty(party);
    }

    void AssignSlots()
    {
        foreach (PartySlotSmall slot in BoardRoot.GetChildren())
        {
            slot.UIParty = this;
        }
    }

    public void SetParty(CharacterParty party)
    {
        if (Party != null)
            Party.PartyChanged -= Refresh;

        Party = party;

        foreach (PartySlotSmall slot in BoardRoot.GetChildren())
        {
            slot.Party = Party;
        }

        if (Party != null)
            Party.PartyChanged += Refresh;

        Refresh();
    }

    public void Refresh()
    {
        if (Party == null)
            return;

        foreach (PartySlotSmall slot in BoardRoot.GetChildren())
        {
            var orc = Party.GetOrc(slot.Row, slot.Column);

            slot.SetOrc(orc);
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb &&
            mb.Pressed &&
            mb.ButtonIndex == MouseButton.Left)
        {
            SelectionManager.I.SelectParty(Party);
        }
    }

    void BuildStyles()
    {
        normalStyle = new StyleBoxFlat
        {
            BgColor = new Color(0, 0, 0, 0.1f)
        };

        selectedStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.2f, 0.6f, 1f, 0.35f), // azul suave
            BorderColor = new Color(0.2f, 0.6f, 1f),
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2
        };
    }
}