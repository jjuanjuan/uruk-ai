using Godot;

public partial class UIMain : Control
{
    [Export] public PackedScene ClassTreeScene;
    [Export] public Orc OrcForTree;
    [Export] public UIOrcPool PoolUI;

    public override void _Ready()
    {
        GetNode<Button>("OpenClassTreeButton").Pressed += OpenTree;

        var orcs = new Godot.Collections.Array<Orc>();

        foreach (var node in GetTree().GetNodesInGroup("orcs"))
        {
            if (node is Orc orc)
                orcs.Add(orc);
        }

        PoolUI.CallDeferred(nameof(UIOrcPool.SetOrcs), orcs);

        GD.Print("Cantidad de nodos en grupo orcs: ", GetTree().GetNodesInGroup("orcs").Count);
    }

    void OpenTree()
    {
        if (GetNodeOrNull<UIClassTree>("UIClassTree") != null)
            return;

        var tree = ClassTreeScene.Instantiate<UIClassTree>();
        tree.Name = "UIClassTree";
        tree.Orc = OrcForTree;

        AddChild(tree);
    }

    // TODO: generar un montón random
    void CreateOrcs()
    {
        var orcs = new Godot.Collections.Array<Orc>();

        for (int i = 0; i < 10; i++)
        {
            var orc = new Orc();
            orcs.Add(orc);
        }

        PoolUI.SetOrcs(orcs);
    }
}