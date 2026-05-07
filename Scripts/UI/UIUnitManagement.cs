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
        // TODO: Create party with Orc selected as Leader and add it to the party
        GameManager.I.PlayerPartyPool.CreateParty();
    }
}
