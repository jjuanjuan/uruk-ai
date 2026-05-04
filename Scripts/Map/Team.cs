using Godot;

public partial class Team : Resource
{
    [Export] public TeamId Id;
    [Export] public string Name;
    [Export] public Color Color;
}

public enum TeamId
{
    Player,
    Enemy,
    Neutral,
    Ally
}