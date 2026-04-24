using Godot;

public partial class UIMain : Control
{
    [Export] public CharacterParty Party;
    [Export] public UIParty UIParty;
    [Export] public PackedScene ClassTreeScene;
    [Export] public Orc OrcForTree;
    [Export] public UIOrcPool PoolUI;
    public Godot.Collections.Array<Orc> AllOrcs = new();

    public override void _Ready()
    {
        GetNode<Button>("OpenClassTreeButton").Pressed += OpenClassTree;

        foreach (var node in GetTree().GetNodesInGroup("orcs"))
        {
            if (node is Orc orc)
                AllOrcs.Add(orc);
        }

        Party.PartyChanged += RefreshAll;

        // primer sync
        CallDeferred(nameof(RefreshAll));
    }

    void RefreshAll()
    {
        GD.Print("REFRESH");

        // actualizar party
        UIParty.Refresh();

        // reconstruir pool
        var available = new Godot.Collections.Array<Orc>();

        foreach (var orc in AllOrcs)
        {
            if (!IsInParty(orc))
                available.Add(orc);
        }

        PoolUI.SetOrcs(available);
    }

    bool IsInParty(Orc orc)
    {
        for (int r = 0; r < CharacterParty.ROWS; r++)
        {
            for (int c = 0; c < CharacterParty.COLUMNS; c++)
            {
                if (Party.GetOrc(r, c) == orc)
                    return true;
            }
        }

        return false;
    }

    void OpenClassTree()
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