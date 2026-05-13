using Godot;
using System;

[GlobalClass]
public partial class OrcTemplate : Resource
{
    [Export] public int BaseHP = 20;
    [Export] public int BaseStr = 10;
    [Export] public int BaseDex = 10;
    [Export] public int BaseInt = 10;
    [Export] public int BaseWis = 10;
    [Export] public int BaseSpd = 10;
    [Export] public CharacterClass[] BaseClasses;
}