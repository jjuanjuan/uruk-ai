using Godot;
using System;
using System.Collections.Generic;

public partial class GameManager : Node
{
    [Export] public NamePool OrcNames;
    [Export] public OrcTemplate OrcTemplate;
    [Export] public CombatConfig CombatConfig;

    public CharacterParty Team1;
    public CharacterParty Team2;
    public RandomNumberGenerator rng = new RandomNumberGenerator();
    public Godot.Collections.Array<OrcInstance> AllOrcs = new();

    [Signal] public delegate void PartiesChangedEventHandler();
    [Signal] public delegate void SelectedOrcChangedEventHandler(OrcInstance orc);

    public OrcInstance SelectedOrc { get; private set; }

    // SINGLETON
    public static GameManager I { get; private set; }
    public override void _EnterTree()
    {
        I = this;
    }

    public override void _Ready()
    {
        rng.Randomize();

        Team1 = new CharacterParty();
        Team1.Name = "Team 1";
        AddChild(Team1);

        Team2 = new CharacterParty();
        Team2.Name = "Team 2";
        AddChild(Team2);

        Team1.PartyChanged += OnPartyChanged;
        Team2.PartyChanged += OnPartyChanged;
    }

    void OnPartyChanged()
    {
        EmitSignal(SignalName.PartiesChanged);
    }

    public bool IsInParty(OrcInstance orc, CharacterParty party)
    {
        return party.IsMember(orc);
    }

    public Godot.Collections.Array<OrcInstance> GetAvailableOrcs()
    {
        var available = new Godot.Collections.Array<OrcInstance>();

        foreach (var orc in AllOrcs)
        {
            if (!Team1.IsMember(orc) &&
                !Team2.IsMember(orc))
            {
                available.Add(orc);
            }
        }

        return available;
    }

    public void SelectOrc(OrcInstance orc)
    {
        SelectedOrc = orc;
        EmitSignal(SignalName.SelectedOrcChanged, orc);
    }
    public void ClearSelection()
    {
        SelectedOrc = null;
        EmitSignal(SignalName.SelectedOrcChanged, (OrcInstance)null);
    }

    public OrcInstance GenerateOrc()
    {
        var instance = new OrcInstance
        {
            Template = OrcTemplate,
            CustomName = GetRandomName(),
            CharacterClass = GetRandomClass(),
        };

        AllOrcs.Add(instance);
        EmitSignal(SignalName.PartiesChanged);
        GD.Print($"Generated: {instance.CustomName} the {instance.CharacterClass.GetClassName()}");
        return instance;
    }

    string GetRandomName()
    {
        if (OrcNames == null || OrcNames.Names.Count == 0)
            return "Orc";

        var used = new HashSet<string>();

        foreach (var orc in AllOrcs)
        {
            if (!string.IsNullOrEmpty(orc.CustomName))
                used.Add(orc.CustomName);
        }

        var available = new List<string>();

        foreach (var name in OrcNames.Names)
        {
            if (!used.Contains(name))
                available.Add(name);
        }

        if (available.Count == 0)
            return $"Orc {AllOrcs.Count}";

        int index = rng.RandiRange(0, available.Count - 1);
        return available[index];
    }
    CharacterClass GetRandomClass()
    {
        return OrcTemplate.BaseClasses[NextInt(0, OrcTemplate.BaseClasses.Length - 1)];
    }

    // UTILITY
    public int NextInt(int min, int max)
    {
        return rng.RandiRange(min, max);
    }
    public float NextFloat(float min, float max)
    {
        return rng.RandfRange(min, max);
    }
}