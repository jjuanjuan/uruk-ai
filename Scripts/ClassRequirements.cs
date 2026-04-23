using Godot;

[GlobalClass]
public partial class ClassRequirements : Resource
{
    [Export] public CharacterClass RequiredClass;
    [Export] public int RequiredLevel;
}