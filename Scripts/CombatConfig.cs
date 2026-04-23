using Godot;
using System;

[GlobalClass]
public partial class CombatConfig : Resource
{
    [Export] public Godot.Collections.Array<DamageModifier> Modifiers;

    public float GetMultiplier(AttackType atk, ArmorType armor)
    {
        foreach (var mod in Modifiers)
        {
            if (mod.AttackType == atk && mod.ArmorType == armor)
                return mod.Multiplier;
        }

        return 1f;
    }

    public enum AttackType
    {
        Smash,
        Slash,
        Pierce,
        Fire,
        Ice,
        Electric,
    }

    public enum ArmorType
    {
        Light,
        Medium,
        Heavy,
        Fire,
        Ice,
    }
}
