using Godot;
using System;

public partial class UIUnitManagement : Control
{
    [Export] public UIOrcPool OrcPoolUI;
    [Export] public UIPartyPool PartyPoolUI;

    public override void _Ready()
    {
        GameManager.I.PartiesChanged += RefreshPools;

        RefreshPools();
    }

    void RefreshPools()
    {
        OrcPoolUI.SetOrcs(GameManager.I.GetAvailableOrcs());
        PartyPoolUI.Refresh();
    }

}
