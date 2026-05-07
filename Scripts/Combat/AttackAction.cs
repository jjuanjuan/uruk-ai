using Godot;
using System;

[GlobalClass]
public partial class AttackAction : Resource
{
    [Export]
    public string AttackName = "ATTACK NAME HERE";
    [Export]
    public double BaseDamageMultiplier = 1.0;
    [Export]
    CombatConfig.AttackType AttackType = CombatConfig.AttackType.Slash;
    [Export]
    public AttackActionTarget Target = AttackActionTarget.CloseSingle;

    public enum AttackActionTarget
    {
        CloseSingle,
        FarSingle,
        AnySingle,
        RandomSingle,
        CloseColumn,
        AnyColumn,
        CloseRow,
        FarRow,
        AnyRow,
        AllEnemies,
        AllAllies,
        All,
    }
}
