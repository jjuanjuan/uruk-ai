using Godot;
using System;

[GlobalClass]
public partial class OrcInstance : Resource
{
    [Export] public OrcTemplate Template;
    [Export] public string CustomName;
    [Export] public CharacterClass CharacterClass;
    [Export] public Godot.Collections.Array<ClassProgress> ClassProgresses = new();

    public PartyPosition PartyPosition;
    public CharacterParty CurrentParty;

    // =====================================================
    // STATS CACHE
    // =====================================================

    StatBlock cachedStats;
    bool dirtyStats = true;

    // =====================================================
    // STATS
    // =====================================================

    public StatBlock TotalStats => GetTotalStats();

    public int MaxHP => TotalStats.HP;
    public int Str => TotalStats.Str;
    public int Dex => TotalStats.Dex;
    public int Int => TotalStats.Int;
    public int Wis => TotalStats.Wis;
    public int Spd => TotalStats.Spd;

    public int CurrentHP => Mathf.Max(MaxHP - Damage, 0);
    public float CurrentHPPercentile => CurrentHP / (float)MaxHP;
    public bool IsAlive => CurrentHP > 0;

    public int Damage = 0;

    // =====================================================
    // BASIC
    public string GetCustomName()
    {
        return string.IsNullOrEmpty(CustomName)
            ? "No name"
            : CustomName;
    }

    // HP
    public void TakeDamage(int amount)
    {
        Damage += amount;
    }

    public void Heal(int amount)
    {
        Damage = Mathf.Max(0, Damage - amount);
    }

    // CLASS
    public void ChangeClass(CharacterClass characterClass)
    {
        if (!CanChangeToClass(characterClass))
        {
            GD.Print("No cumple requisitos");
            return;
        }

        CharacterClass = characterClass;

        MarkStatsDirty();
    }

    public bool CanChangeToClass(CharacterClass newClass)
    {
        foreach (var req in newClass.GetRequirements())
        {
            var progress = GetProgress(req.RequiredClass);

            if (progress.Level < req.RequiredLevel)
                return false;
        }

        return true;
    }

    // PROGRESSION
    public ClassProgress GetProgress(CharacterClass characterClass)
    {
        foreach (var cp in ClassProgresses)
        {
            if (cp.CharacterClass == characterClass)
                return cp;
        }

        // crear progreso si no existe
        var newProgress = new ClassProgress
        {
            CharacterClass = characterClass,
            Level = 0,
            XP = 0
        };

        ClassProgresses.Add(newProgress);

        MarkStatsDirty();

        return newProgress;
    }

    public void GainXP(int amount)
    {
        if (CharacterClass == null)
            return;

        var progress = GetProgress(CharacterClass);

        progress.XP += amount;

        int xpToLevel = CharacterClass.GetXPToNextLevel();

        while (progress.XP >= xpToLevel)
        {
            progress.XP -= xpToLevel;
            progress.Level++;

            MarkStatsDirty();

            GD.Print(
                $"{GetCustomName()} subió a nivel " +
                $"{progress.Level} en " +
                $"{CharacterClass.GetClassName()}"
            );
        }
    }

    // STATS
    public StatBlock GetTotalStats()
    {
        if (!dirtyStats)
            return cachedStats;

        cachedStats = new StatBlock();

        // template
        if (Template != null)
        {
            cachedStats.Add(
                Template.GetBaseStats()
            );
        }

        // clase actual
        if (CharacterClass != null)
        {
            cachedStats.Add(
                CharacterClass.GetBaseStats()
            );
        }

        // progreso acumulado de todas las clases
        foreach (var progress in ClassProgresses)
        {
            if (progress == null)
                continue;

            if (progress.CharacterClass == null)
                continue;

            cachedStats.Add(
                progress.CharacterClass
                    .GetGrowthStatsAtLevel(progress.Level)
            );
        }

        dirtyStats = false;

        return cachedStats;
    }

    public void MarkStatsDirty()
    {
        dirtyStats = true;
    }
}