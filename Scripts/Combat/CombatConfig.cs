using Godot;

[Tool]
[GlobalClass]
public partial class CombatConfig : Resource
{
    public enum AttackType
    {
        Smash,
        Slash,
        Pierce,
        Fire,
        Ice,
        Electric,
    }

    public enum ArmorType
    {
        Light,
        Medium,
        Heavy,
        Fire,
        Ice,
    }

    [Export]
    public float[] Multipliers = new float[30]; // 6 x 5

    // Combat
    [Export]
    public float CombatStartDelay = 4.0f;
    [Export]
    public float AttackInterval = 2.0f;
    [Export]
    public float AttackDelayWhenUI = 1.0f;
    [Export]
    public float ScoreMultiplierPerKill = .1f;    
    
    // Map
    [Export]
    public float CombatNoticeDistance = 120f;    
    [Export]
    public float CombatTriggerDistance = 40f;    
    [Export]
    public float CombatNoticeAngle = 15f;    
    [Export]
    public float LoserPushDistance = 200f;
    [Export]
    public float LoserPushTime = 1f;
}