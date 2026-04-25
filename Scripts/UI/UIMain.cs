using Godot;

public partial class UIMain : Control
{
    [Export] public PackedScene CombatScene;
    [Export] public PackedScene ClassTreeScene;
    [Export] public UIOrcPool PoolUI;

    CharacterParty PlayerParty => GameManager.I.Team1;
    CharacterParty EnemyParty => GameManager.I.Team2;

    public override void _Ready()
    {
        GetNode<Button>("OpenClassTreeButton").Pressed += OpenClassTree;
        GetNode<Button>("OpenCombatButton").Pressed += OpenCombat;
        GetNode<Button>("GenerateOrcButton").Pressed += GenerateOrc;

        GameManager.I.PartiesChanged += RefreshPool;

        RefreshPool();
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

        AddChild(tree);
    }
    void OpenCombat()
    {
        var combat = CombatScene.Instantiate<UICombatScene>();
        combat.Name = "UICombatScene";

        AddChild(combat);
    }
    void GenerateOrc()
    {
        GameManager.I.GenerateOrc();
    }
}