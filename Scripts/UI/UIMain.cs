using Godot;

public partial class UIMain : CanvasLayer
{
	[Export] public PackedScene ClassTreeScene;
	[Export] public PackedScene UnitManagementScene;

	public override void _Ready()
	{
        AddToGroup("ui_root");

		GetNode<Button>("OpenClassTreeButton").Pressed += OpenClassTree;
		GetNode<Button>("OpenUnitManagementButton").Pressed += OpenUnitManagement;
		GetNode<Button>("GenerateMapUnitPlayerButton").Pressed += GenerateMapUnitPlayer;
		GetNode<Button>("GenerateMapUnitEnemyButton").Pressed += GenerateMapUnitEnemy;
	}


	// Buttons
	void OpenUnitManagement()
	{
		if (GetNodeOrNull<UIUnitManagement>("UnitManagement") != null)
			return;

		var scene = UnitManagementScene.Instantiate<UIUnitManagement>();
		scene.Name = "UIUnitManagement";

		AddChild(scene);
	}
	void OpenClassTree()
	{
		if (GetNodeOrNull<UIClassTree>("UIClassTree") != null)
			return;

		var tree = ClassTreeScene.Instantiate<UIClassTree>();
		tree.Name = "UIClassTree";

		AddChild(tree);
	}
	
	void CreatePartyEnemy()
	{
		GameManager.I.EnemyPartyPool.CreateParty();
	}
	// TODO: hacer esto de manera no idiota
	void GenerateMapUnitPlayer()
	{
		var spawner = GetSpawner();

		if (spawner == null)
		{
			GD.PrintErr("Spawner not found");
			return;
		}

		var gm = GameManager.I;

		// elegir team (ejemplo: player)
		var pool = gm.PlayerPartyPool;

		var party = pool.CreateRandomParty();

		// spawn
		Vector2 worldPos = gm.MapManager.MapCamera.GlobalPosition;
		Vector2I gridPos = gm.MapManager.WorldToGrid(worldPos);

		var unit = spawner.Spawn(gridPos, party);

		gm.MapManager.RegisterUnit(unit);
	}
	// TODO: hacer esto de manera no idiota
	void GenerateMapUnitEnemy()
	{
		var spawner = GetSpawner();

		if (spawner == null)
		{
			GD.PrintErr("Spawner not found");
			return;
		}

		var gm = GameManager.I;

		// elegir team (ahora enemy (esto es lo idiota))
		var pool = gm.EnemyPartyPool;

		var party = pool.CreateRandomParty();

		// spawn
		Vector2 worldPos = gm.MapManager.MapCamera.GlobalPosition;
		Vector2I gridPos = gm.MapManager.WorldToGrid(worldPos);

		var unit = spawner.Spawn(gridPos, party);

		gm.MapManager.RegisterUnit(unit);
	}
	UnitSpawner GetSpawner()
	{
		return GetTree().GetFirstNodeInGroup("unit_spawner") as UnitSpawner;
	}
}
