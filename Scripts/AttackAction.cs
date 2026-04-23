using Godot;
using System;

[GlobalClass]
public partial class AttackAction : Resource
{
    [Export]
    Double BaseDamageMultiplier = 1.0;
    [Export]
    CombatConfig.AttackType AttackType = CombatConfig.AttackType.Slash;
    [Export]
    AttackActionTarget Target = AttackActionTarget.CloseSingle;

    public enum AttackActionTarget
    {
        CloseSingle,
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
