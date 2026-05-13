using Godot;
using System;

[GlobalClass]
public partial class AttackAction : Resource
{
    [Export] public string AttackName = "ATTACK NAME HERE";
    [Export] public CombatConfig.AttackType AttackType = CombatConfig.AttackType.Slash;
    [Export] public AttackActionTarget Target = AttackActionTarget.CloseSingle;

    [Export] public int BaseDamage = 10;
    [Export] public float StrFactor = 0f;
    [Export] public float DexFactor = 0f;
    [Export] public float IntFactor = 0f;
    [Export] public float WisFactor = 0f;

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
