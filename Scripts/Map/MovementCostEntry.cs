using Godot;
using System;

[GlobalClass]
public partial class MovementCostEntry : Resource
{
    [Export] public MovementType Type;
    [Export] public int Cost;
}