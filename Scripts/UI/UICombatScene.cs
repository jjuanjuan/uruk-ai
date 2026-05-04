using Godot;
using System;
using System.Collections.Generic;

public partial class UICombatScene : Control
{
    [Export] public UIParty Team1UI;
    [Export] public UIParty Team2UI;

    [Export] Button StartButton;
    [Export] RichTextLabel Log;
    [Export] RichTextLabel CurrentUnitLabel;

    [Export] CombatAdvantageBar AdvantageBar;

    public CombatManager CombatManager;

    List<string> logs = new();

    [Signal] public delegate void CombatFinishedEventHandler();

    public override void _Ready()
    {
        CombatManager = GetNode<CombatManager>("CombatManager");

        CombatManager.Connect(
            CombatManager.SignalName.CombatStateChanged,
            new Callable(this, nameof(OnCombatUpdated))
        );

        CombatManager.Connect(
            CombatManager.SignalName.UnitChanged,
            new Callable(this, nameof(OnUnitChanged))
        );

        CombatManager.Connect(
            "CombatLog",
            new Callable(this, nameof(AddLog))
        );

        CombatManager.Connect(
            CombatManager.SignalName.CombatStateChanged,
            new Callable(this, nameof(OnCombatStateChanged))
        );

        Team1UI.Refresh();
        Team2UI.Refresh();
    }

    public void Setup(CharacterParty partyA, CharacterParty partyB)
    {
        Log.Text = "";

        CombatManager.StartCombat(partyA, partyB);

        Team1UI.SetParty(partyA);
        Team2UI.SetParty(partyB);

        Team1UI.Refresh();
        Team2UI.Refresh();
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

    void OnCombatStateChanged()
    {
        //TODO: cambiar a animación para cerrar
        /*
        if (CombatManager.CurrentState == CombatManager.CombatState.Ended)
        {
            EmitSignal(SignalName.CombatFinished);

            // cerrar UI : TODO: cambiar a animación para cerrar
            QueueFree();
        }
        */
    }
    void OnCombatFinished()
    {
        // cerrar UI : TODO: cambiar a animación para cerrar
        QueueFree();
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