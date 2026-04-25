using Godot;
using System;

[GlobalClass]
public partial class OrcTemplate : Resource
{
    [Export] public int BaseHP = 20;
    [Export] public int BaseAttack = 10;
    [Export] public CharacterClass[] BaseClasses;
}