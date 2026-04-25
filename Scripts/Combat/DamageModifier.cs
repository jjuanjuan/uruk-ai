using Godot;
using System;

[GlobalClass]
public partial class DamageModifier : Resource
{
    [Export] public CombatConfig.AttackType AttackType;
    [Export] public CombatConfig.ArmorType ArmorType;
    [Export] public float Multiplier = 1f;
}
