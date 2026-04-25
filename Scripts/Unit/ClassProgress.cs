using Godot;

[GlobalClass]
public partial class ClassProgress : Resource
{
    [Export] public CharacterClass CharacterClass;
    [Export] public int Level = 0;
    [Export] public int XP = 0;
}