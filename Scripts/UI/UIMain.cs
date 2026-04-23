using Godot;

public partial class UIMain : Control
{
    [Export] public PackedScene ClassTreeScene;
    [Export] public Orc Orc;

    public override void _Ready()
    {
        GetNode<Button>("OpenClassTreeButton").Pressed += OpenTree;
    }

    void OpenTree()
    {
        if (GetNodeOrNull<UIClassTree>("UIClassTree") != null)
            return;

        var tree = ClassTreeScene.Instantiate<UIClassTree>();
        tree.Name = "UIClassTree";
        tree.Orc = Orc;

        AddChild(tree);
    }
}