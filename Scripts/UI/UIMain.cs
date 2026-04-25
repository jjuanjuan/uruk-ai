using Godot;

public partial class UIMain : Control
{
    [Export] public PackedScene CombatScene;
    [Export] public PackedScene ClassTreeScene;
    [Export] public Orc OrcForTree;
    [Export] public UIOrcPool PoolUI;

    CharacterParty PlayerParty => GameManager.I.Team1;
    CharacterParty EnemyParty => GameManager.I.Team2;

    public override void _Ready()
    {
        GetNode<Button>("OpenClassTreeButton").Pressed += OpenClassTree;
        GetNode<Button>("OpenCombatButton").Pressed += OpenCombat;

        foreach (var node in GetTree().GetNodesInGroup("orcs"))
        {
            if (node is Orc orc)
                GameManager.I.AllOrcs.Add(orc);
        }

        PlayerParty.PartyChanged += RefreshAll;
        EnemyParty.PartyChanged += RefreshAll;
        GameManager.I.PartiesChanged += RefreshPool;

        RefreshPool();
        RefreshAll();
    }

    void RefreshAll()
    {
        PoolUI.SetOrcs(GameManager.I.GetAvailableOrcs());
    }
    void RefreshPool()
    {
        PoolUI.SetOrcs(GameManager.I.GetAvailableOrcs());
    }

    // Buttons
    void OpenClassTree()
    {
        if (GetNodeOrNull<UIClassTree>("UIClassTree") != null)
            return;

        var tree = ClassTreeScene.Instantiate<UIClassTree>();
        tree.Name = "UIClassTree";
        tree.Orc = OrcForTree;

        AddChild(tree);
    }
    void OpenCombat()
    {
        var combat = CombatScene.Instantiate<UICombatScene>();
        combat.Name = "UICombatScene";

        AddChild(combat);
    }
}