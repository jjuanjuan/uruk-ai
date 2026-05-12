using Godot;

public partial class UIPartyManagement : Control
{
    public CharacterParty Party;

    [Export] public Control BoardRoot;

    public void Setup(CharacterParty party)
    {
        AssignSlots();
        SetParty(party);
    }

    void AssignSlots()
    {
        foreach (PartySlotManagement slot in BoardRoot.GetChildren())
        {
            slot.UIParty = this;
        }
    }

    public void SetParty(CharacterParty party)
    {
        if (Party != null)
            Party.PartyChanged -= Refresh;

        Party = party;

        foreach (PartySlotManagement slot in BoardRoot.GetChildren())
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

        foreach (PartySlotManagement slot in BoardRoot.GetChildren())
        {
            var orc = Party.GetOrc(slot.Row, slot.Column);

            slot.SetOrc(orc);
        }
    }
}