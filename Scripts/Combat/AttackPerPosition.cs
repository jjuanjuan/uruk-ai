using Godot;
using System;

[GlobalClass]
public partial class AttackPerPosition : Resource
{
    [Export] public AttackAction AttackAction;
    [Export] public int Amount = 1;
    // row 0 back
    // row 1 y 2 middle
    // row 3 y 4 front

    // o sea que en el array: 0 es back, 2 es front
}
