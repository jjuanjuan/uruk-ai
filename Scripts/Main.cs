using Godot;
using System;

public partial class Main : Node
{
    [Export] public PackedScene MapScene;
    [Export] public PackedScene UIMainScene;
    [Export] public Node MapRoot;
    [Export] public Node UIRoot;

    Node _mapInstance;

    public override void _Ready()
    {
        CreateMainUI();
        CreateMap();
    }

    void CreateMap()
    {
        if (_mapInstance != null)
            return;

        if (MapScene == null)
        {
            GD.PrintErr("MapScene not assigned");
            return;
        }

        _mapInstance = MapScene.Instantiate();
        MapRoot.AddChild(_mapInstance);
    }
    void CreateMainUI()
    {
        if (UIMainScene == null)
        {
            GD.PrintErr("UIMainScene not assigned");
            return;
        }

        var ui = UIMainScene.Instantiate();

        if (UIRoot != null)
            UIRoot.AddChild(ui);
        else
            AddChild(ui);

    }
}
