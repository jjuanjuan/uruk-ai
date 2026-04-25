using Godot;
using System;

public partial class UICombatScene : Control
{
    [Export] public CombatManager CombatManager;

    [Export] public UIParty Team1UI;
    [Export] public UIParty Team2UI;

    [Export] Button StartButton;
    [Export] RichTextLabel Log;
    [Export] RichTextLabel CurrentUnitLabel;

    public override void _Ready()
    {
        StartButton.Pressed += OnStartPressed;

        CombatManager.Connect(
            CombatManager.SignalName.CombatStateChanged,
            new Callable(this, nameof(OnCombatUpdated))
        );

        CombatManager.Connect(
            CombatManager.SignalName.UnitChanged,
            new Callable(this, nameof(OnUnitChanged))
        );

        CombatManager.UI = this;

        CombatManager.Team1 = GameManager.I.Team1;
        CombatManager.Team2 = GameManager.I.Team2;

        Team1UI.SetParty(GameManager.I.Team1);
        Team2UI.SetParty(GameManager.I.Team2);

        Team1UI.Setup(GameManager.I.Team1);
        Team2UI.Setup(GameManager.I.Team2);

        Team1UI.Refresh();
        Team2UI.Refresh();
    }

    private void OnStartPressed()
    {
        CombatManager.StartCombat();
    }

    private void OnCombatUpdated()
    {
        Team1UI.Refresh();
        Team2UI.Refresh();
    }

    private void OnUnitChanged(Orc unit)
    {
        if (unit == null)
        {
            CurrentUnitLabel.Text = "—";
            return;
        }

        CurrentUnitLabel.Text =
            $"{unit.GetFirstName()} ({unit.GetCharacterClass().GetClassName()})";
    }

    public void AddLog(string text)
    {
        Log.AddText(text + "\n");
    }
}