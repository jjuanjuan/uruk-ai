using Godot;
using System;

public partial class UIUnitManagement : Control
{
    [Export] public UIOrcPool OrcPoolUI;
    [Export] public UIPartyPool PartyPoolUI;
    [Export] UIPartyInfo UIPartyInfo;

    [Export] Button CloseButton;
    [Export] Button GenerateOrcButton;
    [Export] Button CreatePartyButton;
    [Export] Button EditPartyButton;

    public override void _Ready()
    {
        GameManager.I.PartiesChanged += RefreshPools;
        CloseButton.Pressed += Close;
        GenerateOrcButton.Pressed += GenerateOrc;
        CreatePartyButton.Pressed += CreatePartyPlayer;
        EditPartyButton.Pressed += EditParty;

        RefreshPools();
    }

    public override void _ExitTree()
    {
        if (GameManager.I != null)
            GameManager.I.PartiesChanged -= RefreshPools;
    }

    void Close()
    {
        // TODO: animation
        QueueFree();
    }

    void RefreshPools()
    {
        OrcPoolUI.SetOrcs(GameManager.I.GetAvailableOrcs());
        PartyPoolUI.Refresh();
    }

    void EditParty()
    {
        var party = SelectionManager.I.SelectedParty;

        if (party == null)
        {
            GD.Print("No party selected");
            return;
        }

        UIPartyInfo.Visible = true;
        UIPartyInfo.Setup(party);
    }

    void GenerateOrc()
    {
        GameManager.I.GenerateOrc();
    }
    void CreatePartyPlayer()
    {
        var selected = SelectionManager.I.SelectedOrc;

        if (selected == null)
            return;

        var party = GameManager.I.CreateParty(GameManager.I.PlayerTeam, selected.GetCustomName() + "'s Party");

        party.SetLeader(selected);

        // poner leader en posición default
        party.PlaceOrc(selected, 2, 2);

        PartyPoolUI.Refresh();

        SelectionManager.I.SelectedParty = party;
    }
}
