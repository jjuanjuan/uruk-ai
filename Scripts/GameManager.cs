using Godot;
using System;
using System.Collections.Generic;

public partial class GameManager : Node
{
    [Export] public NamePool OrcNames;
    [Export] public OrcTemplate OrcTemplate;
    [Export] public CombatConfig CombatConfig;
    [Export] public GameDatabase Database;
    [Export] public PackedScene CombatScene;

    public MapManager MapManager;
    public CombatManager CombatManager;

    public List<CharacterParty> Parties = new();
    public CharacterPartyPool PlayerPartyPool;
    public CharacterPartyPool EnemyPartyPool;

    public RandomNumberGenerator rng = new RandomNumberGenerator();
    public Godot.Collections.Array<OrcInstance> AllOrcs = new();

    // TEAMS
    public Team PlayerTeam;
    public Team EnemyTeam;

    [Signal] public delegate void PartiesChangedEventHandler();
    [Signal] public delegate void SelectedOrcChangedEventHandler(OrcInstance orc);

    // SINGLETON
    public static GameManager I { get; private set; }

    public override void _EnterTree()
    {
        I = this;
    }

    public override void _Ready()
    {
        rng.Randomize();

        CreateTeams();
        CreatePools();
    }

    // =====================================================
    // TEAMS
    // =====================================================
    void CreateTeams()
    {
        PlayerTeam = new Team
        {
            Id = TeamId.Player,
            Name = "Player",
            Color = new Color(0.2f, 0.6f, 1f)
        };

        EnemyTeam = new Team
        {
            Id = TeamId.Enemy,
            Name = "Enemy",
            Color = new Color(1f, 0.3f, 0.3f)
        };
    }
    void CreatePools()
    {
        PlayerPartyPool = new CharacterPartyPool();
        AddChild(PlayerPartyPool);
        PlayerPartyPool.Setup(PlayerTeam);

        EnemyPartyPool = new CharacterPartyPool();
        AddChild(EnemyPartyPool);
        EnemyPartyPool.Setup(EnemyTeam);
    }
    // =====================================================
    // PARTIES
    // =====================================================
    public CharacterParty CreateParty(Team team, string name = "Party")
    {
        var party = new CharacterParty
        {
            Name = name,
        };

        party.SetTeam(team);

        AddChild(party);
        Parties.Add(party);

        party.PartyChanged += OnPartyChanged;

        EmitSignal(SignalName.PartiesChanged);

        return party;
    }

    void OnPartyChanged()
    {
        EmitSignal(SignalName.PartiesChanged);
    }

    public List<CharacterParty> GetPartiesByTeam(TeamId teamId)
    {
        return Parties.FindAll(p => p.Team != null && p.Team.Id == teamId);
    }

    public CharacterParty GetFirstParty(TeamId teamId)
    {
        return Parties.Find(p => p.Team != null && p.Team.Id == teamId);
    }

    public CharacterParty GenerateRandomParty(
        Team team,
        int unitCount,
        string partyName = "Party")
    {
        var party = CreateParty(team, partyName);

        int attempts = 0;
        int maxAttempts = 200;

        while (party.CurrentUnits < unitCount &&
               attempts < maxAttempts)
        {
            attempts++;

            int row = NextInt(0, CharacterParty.ROWS - 1);
            int col = NextInt(0, CharacterParty.COLUMNS - 1);

            if (!party.CanPlaceOrc(row, col))
                continue;

            var orc = GenerateOrc();

            party.PlaceOrc(orc, row, col);
        }

        // leader random
        var all = party.GetAllOrcs();

        if (all.Count > 0)
        {
            var leader = all[
                NextInt(0, all.Count - 1)
            ];

            party.SetLeader(leader);
        }

        return party;
    }
    // =====================================================
    // ORCS
    // =====================================================
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

    public Godot.Collections.Array<OrcInstance> GetAvailableOrcs()
    {
        var available = new Godot.Collections.Array<OrcInstance>();

        foreach (var orc in AllOrcs)
        {
            if (!IsOrcInAnyParty(orc))
                available.Add(orc);
        }

        return available;
    }

    public bool IsOrcInAnyParty(OrcInstance orc)
    {
        foreach (var party in Parties)
        {
            if (party.IsMember(orc))
                return true;
        }
        return false;
    }

    // =====================================================
    // UTILS
    // =====================================================
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
        return OrcTemplate.BaseClasses[
            NextInt(0, OrcTemplate.BaseClasses.Length - 1)
        ];
    }

    public int NextInt(int min, int max)
    {
        return rng.RandiRange(min, max);
    }

    public float NextFloat(float min, float max)
    {
        return rng.RandfRange(min, max);
    }
}