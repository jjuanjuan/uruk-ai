using Godot;
using System;

[GlobalClass]
public partial class OrcTemplate : Resource
{
    [Export] int BaseHP = 20;
    [Export] int BaseStr = 10;
    [Export] int BaseDex = 10;
    [Export] int BaseInt = 10;
    [Export] int BaseWis = 10;
    [Export] int BaseSpd = 10;
    [Export] public CharacterClass[] BaseClasses;

    public StatBlock GetBaseStats()
    {
        return new StatBlock
        {
            HP = BaseHP,
            Str = BaseStr,
            Dex = BaseDex,
            Int = BaseInt,
            Wis = BaseWis,
            Spd = BaseSpd
        };
    }
}