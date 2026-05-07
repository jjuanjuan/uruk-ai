using Godot;
using System;
using System.Collections.Generic;

public partial class UICombatScene : Control
{
    [Export] public UIPartyCombat Team1UI;
    [Export] public UIPartyCombat Team2UI;

    [Export] Button StartCombatButton;

    [Export] RichTextLabel Log;
    [Export] RichTextLabel CurrentUnitLabel;

    [Export] CombatAdvantageBar AdvantageBar;

    [Export] Control ResultPanel;
    [Export] RichTextLabel ResultText;
    [Export] float ResultDuration = 1.5f;

    public CombatManager CombatManager;
    
    List<string> logs = new();

    [Signal] public delegate void CombatFinishedEventHandler();
    [Signal] public delegate void ResultFinishedEventHandler();

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

        StartCombatButton.Pressed += CombatManager.StartCombat;
    }

    public void Setup(CharacterParty partyA, CharacterParty partyB)
    {
        Log.Text = "";

        ResultPanel.Visible = false;

        CombatManager.SetupCombat(partyA, partyB);

        Team1UI.Setup(partyA, true);
        Team2UI.Setup(partyB, false);

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
    public void AnimateAdvantageBar(float value)
    {
        AdvantageBar.UpdateBarAnimated(value);
    }

    public async void ShowResult(CharacterParty winner, CharacterParty loser, bool isDraw)
    {
        ResultPanel.Visible = true;

        if (isDraw)
        {
            ResultText.Text = "DRAW";
        }
        else if (winner == CombatManager.PartyFront)
        {
            ResultText.Text = "TEAM 1 WINS";
        }
        else
        {
            ResultText.Text = "TEAM 2 WINS";
        }

        await ToSignal(GetTree().CreateTimer(ResultDuration), SceneTreeTimer.SignalName.Timeout);

        EmitSignal(SignalName.ResultFinished);
    }
}