using Godot;
using System;

[GlobalClass]
public partial class BuildingData : Resource
{
    [Export] public BuildingType Type;

    // Interacción
    [Export] public bool Enterable = false;
    [Export] public bool Capturable = false;

    // Gameplay
    [Export] public float DefenseMultiplier = 1f;
    [Export] public float HealPerTick = .1f;
    [Export] public bool BlocksMovement = false;
}

public enum BuildingType
{
    None = 0,
    Town,
    Shop,
    Fort,
}