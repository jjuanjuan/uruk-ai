using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

[GlobalClass]
public partial class OrcInstance : Resource
{
    [Export] public OrcTemplate Template;
    [Export] public string CustomName;
    [Export] public CharacterClass CharacterClass;
    [Export] public Godot.Collections.Array<ClassProgress> ClassProgresses = new();

    public int Damage = 0;
    public int CurrentHP => Mathf.Max(Template.BaseHP + CharacterClass.GetBaseHP() - Damage, 0);
    public bool IsAlive => CurrentHP > 0;

    public PartyPosition PartyPosition;

    public string GetCustomName()
    {
        return string.IsNullOrEmpty(CustomName)
            ? "No name"
            : CustomName;
    }

    public void TakeDamage(int amount)
    {
        Damage += amount;
    }
    public void Heal(int amount)
    {
        Damage = Mathf.Max(0, Damage - amount);
    }

    public void ChangeClass(CharacterClass characterClass)
    {
        if (!CanChangeToClass(characterClass))
        {
            GD.Print("No cumple requisitos");
            return;
        }

        CharacterClass = characterClass;
    }

    public ClassProgress GetProgress(CharacterClass characterClass)
    {
        foreach (var cp in ClassProgresses)
        {
            if (cp.CharacterClass == characterClass)
                return cp;
        }

        // si no existe, crear
        var newProgress = new ClassProgress
        {
            CharacterClass = characterClass,
            Level = 0,
            XP = 0
        };

        ClassProgresses.Add(newProgress);
        return newProgress;
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

    public void GainXP(int amount)
    {
        var progress = GetProgress(CharacterClass);

        progress.XP += amount;

        int xpToLevel = CharacterClass.GetXPToNextLevel();

        while (progress.XP >= xpToLevel)
        {
            progress.XP -= xpToLevel;
            progress.Level++;

            GD.Print($"Subió a nivel {progress.Level} en {CharacterClass.GetClassName()}");
        }
    }
}