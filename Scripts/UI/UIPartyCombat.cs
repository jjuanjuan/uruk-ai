using Godot;

public partial class UIPartyCombat : Control
{
    public CharacterParty Party;

    [Export] public Control BoardRoot;
    [Export] Control TopLayer;
    [Export] PackedScene DamagePopupScene;

    public bool IsFront;

    public void Setup(CharacterParty party, bool front)
    {
        Party = party;
        IsFront = front;

        Refresh();

        AssignSlots();
        SetParty(party);
    }

    void AssignSlots()
    {
        foreach (PartySlotCombat slot in BoardRoot.GetChildren())
        {
            slot.UIParty = this;
            slot.IsFront = IsFront;
        }
    }

    public void SetParty(CharacterParty party)
    {
        if (Party != null)
            Party.PartyChanged -= Refresh;

        Party = party;

        foreach (PartySlotCombat slot in BoardRoot.GetChildren())
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

        foreach (PartySlotCombat slot in BoardRoot.GetChildren())
        {
            var orc = Party.GetOrc(slot.Row, slot.Column);
            slot.SetOrc(orc);
        }
    }

    public PartySlotCombat GetSlot(OrcInstance unit)
    {
        foreach (PartySlotCombat slot in BoardRoot.GetChildren())
        {
            if (slot.Orc == unit) return slot;
        }
        return null;
    }

    public void ShowDamageText(OrcInstance unit, float value)
    {
        GD.Print($"{unit.GetCustomName()} took {value} damage");

        var slot = GetSlot(unit);
        if (slot == null) return;

        var popup = DamagePopupScene.Instantiate<DamageText>();

        TopLayer.AddChild(popup);

        popup.GlobalPosition = slot.GlobalPosition;
        popup.Setup(value);
    }

    public void SetNamesVisible(bool visible)
    {
        foreach (PartySlotCombat slot in BoardRoot.GetChildren())
        {
            slot.SetNameVisible(visible);
        }
    }
}