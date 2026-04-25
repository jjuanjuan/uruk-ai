using Godot;

[GlobalClass]
public partial class NamePool : Resource
{
    [Export]
    public Godot.Collections.Array<string> Names = new();
}