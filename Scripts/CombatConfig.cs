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

    [Export]
    public float CombatStartDelay = 4.0f;
    [Export]
    public float AttackInterval = 2.0f;
}