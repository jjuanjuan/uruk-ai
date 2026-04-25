using Godot;
using System;

public partial class GameManager : Node
{
    public CharacterParty Team1;
    public CharacterParty Team2;
    public RandomNumberGenerator rng = new RandomNumberGenerator();
    public Godot.Collections.Array<Orc> AllOrcs = new();

    [Signal] public delegate void PartiesChangedEventHandler();

    // SINGLETON
    public static GameManager I { get; private set; }
    public override void _EnterTree()
    {
        I = this;
    }

    public override void _Ready()
    {
        Team1 = new CharacterParty();
        AddChild(Team1);

        Team2 = new CharacterParty();
        AddChild(Team2);

        Team1.PartyChanged += OnPartyChanged;
        Team2.PartyChanged += OnPartyChanged;
    }

    void OnPartyChanged()
    {
        EmitSignal(SignalName.PartiesChanged);
    }

    public bool IsInParty(Orc orc, CharacterParty party)
    {
        return party.IsMember(orc);
    }

    public Godot.Collections.Array<Orc> GetAvailableOrcs()
    {
        var available = new Godot.Collections.Array<Orc>();

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
    
    public int NextInt(int min, int max)
    {
        return rng.RandiRange(min, max);
    }
}