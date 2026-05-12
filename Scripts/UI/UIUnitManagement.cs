using Godot;
using System;

public partial class UIUnitManagement : Control
{
    [Export] public UIOrcPool OrcPoolUI;
    [Export] public UIPartyPool PartyPoolUI;

    [Export] Button CloseButton;
    [Export] Button GenerateOrcButton;
    [Export] Button CreatePartyButton;

    public override void _Ready()
    {
        GameManager.I.PartiesChanged += RefreshPools;
        CloseButton.Pressed += Close;
        GenerateOrcButton.Pressed += GenerateOrc;
        CreatePartyButton.Pressed += CreatePartyPlayer;

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

    void GenerateOrc()
    {
        GameManager.I.GenerateOrc();
    }
    void CreatePartyPlayer()
    {
        var selected = GameManager.I.SelectedOrc;

        if (selected == null)
            return;

        var party = GameManager.I.CreateParty(GameManager.I.PlayerTeam, selected.GetCustomName() + "'s Party");

        party.SetLeader(selected);

        // poner leader en posición default
        party.PlaceOrc(selected, 2, 2);

        PartyPoolUI.Refresh();
    }
}
