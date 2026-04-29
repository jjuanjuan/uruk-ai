using Godot;
using System.Collections.Generic;

public partial class MapInputController : Node2D
{
	private MapUnit _selectedUnit;

	[Export] public float SelectionRadius = 32f;

	public override void _Ready()
	{
		GD.Print("MapInputController READY");
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mb || !mb.Pressed)
			return;

		Vector2 mouseWorld = GetGlobalMousePosition();

		if (mb.ButtonIndex == MouseButton.Left)
		{
			SelectUnit(mouseWorld);
		}
		else if (mb.ButtonIndex == MouseButton.Right)
		{
			MoveSelectedUnit(mouseWorld);
		}
	}

	// ---------------------------------------
	// SELECT (RTS STYLE)
	// ---------------------------------------
	void SelectUnit(Vector2 mouseWorld)
	{
		var unit = GetUnitAtWorld(mouseWorld);

		if (_selectedUnit != null)
			_selectedUnit.SetSelected(false);

		if (unit != null)
		{
			_selectedUnit = unit;
			_selectedUnit.SetSelected(true);

			GD.Print($"Selected {unit.Name}");
		}
		else
		{
			_selectedUnit = null;
			GD.Print("Selection cleared");
		}
	}

	// ---------------------------------------
	// MOVE
	// ---------------------------------------
	void MoveSelectedUnit(Vector2 mouseWorld)
	{
		if (_selectedUnit == null)
			return;

		Vector2I grid = WorldToGrid(mouseWorld);

		GD.Print($"Move to {grid}");

		_selectedUnit.MoveTo(grid);
	}

	// ---------------------------------------
	// DETECCIÓN REAL (NO GRID)
	// ---------------------------------------
	MapUnit GetUnitAtWorld(Vector2 mouseWorld)
	{
		var units = GetTree().GetNodesInGroup("map_unit");

		foreach (var node in units)
		{
			if (node is not MapUnit unit)
				continue;

			float dist = unit.GlobalPosition.DistanceTo(mouseWorld);

			if (dist <= SelectionRadius)
				return unit;
		}

		return null;
	}

	// ---------------------------------------
	Vector2I WorldToGrid(Vector2 world)
	{
		return new Vector2I(
			Mathf.FloorToInt(world.X / 64),
			Mathf.FloorToInt(world.Y / 64)
		);
	}
}
