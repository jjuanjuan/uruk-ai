using Godot;

public partial class UIMain : CanvasLayer
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
		GetNode<Button>("GenerateMapUnitButton").Pressed += GenerateMapUnit;

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
	void GenerateMapUnit()
	{
		var spawner = GetSpawner();

		if (spawner == null)
		{
			GD.PrintErr("Spawner not found");
			return;
		}

		var orc = GameManager.I.GenerateOrc();

		var party = new CharacterParty();
		party.PlaceOrc(orc, 0, 0);
		party.SetLeader(orc);

		Vector2 worldPos = GameManager.I.MapManager.MapCamera.GlobalPosition;
		Vector2I gridPos = GameManager.I.MapManager.WorldToGrid(worldPos);

		var unit = spawner.Spawn(gridPos, party);
	}
	UnitSpawner GetSpawner()
	{
		return GetTree().GetFirstNodeInGroup("unit_spawner") as UnitSpawner;
	}
}
