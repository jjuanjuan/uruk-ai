using Godot;
using System;
using System.Collections.Generic;

public partial class UICombatScene : Control
{
    // mostrar números de daño

    [Export] public CombatManager CombatManager;

    [Export] public UIParty Team1UI;
    [Export] public UIParty Team2UI;

    [Export] Button StartButton;
    [Export] RichTextLabel Log;
    [Export] RichTextLabel CurrentUnitLabel;

    [Export] CombatAdvantageBar AdvantageBar;

    List<string> logs = new();

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

        CombatManager.Connect(
            "CombatLogEvent",
            new Callable(this, nameof(AddLog))
        );
    }

    public void Setup(CharacterParty partyFront, CharacterParty partyBack)
    {
        // configurar CombatManager
        CombatManager.PartyFront = partyFront;
        CombatManager.PartyBack = partyBack;

        // configurar UI
        Team1UI.SetParty(partyFront);
        Team2UI.SetParty(partyBack);

        Team1UI.Setup(partyFront, true);
        Team2UI.Setup(partyBack, false);

        Team1UI.Refresh();
        Team2UI.Refresh();
    }

    private void OnStartPressed()
    {
        CombatManager.StartCombat();
        Log.Text = "";
    }

    private void OnCombatUpdated()
    {
        Team1UI.Refresh();
        Team2UI.Refresh();
    }

    private void OnUnitChanged(OrcInstance unit)
    {
        if (unit == null)
        {
            CurrentUnitLabel.Text = "—";
            return;
        }

        CurrentUnitLabel.Text =
            $"Turno de {unit.GetCustomName()} ({unit.CharacterClass.GetClassName()})";
    }

    public void AddLog(string text)
    {
        logs.Insert(0, text); // nuevo arriba

        Log.Text = string.Join("\n", logs);
    }

    public void SetAdvantageBar(float value)
    {
        AdvantageBar.SetValue(value);
    }
}