using Godot;
using System;

[GlobalClass]
public partial class AttackPerPosition : Resource
{
    [Export] public AttackAction AttackAction;
    [Export] public int Amount = 1;
}
