using Godot;
using System.Collections.Generic;

public partial class VisionCone : Area2D
{
    public float Angle = 90f;
    public float Distance = 120f;

    [Export] public MapUnit ParentUnit;
    [Export] public CollisionPolygon2D Polygon;
    [Export] public Color ColorFill;
    [Export] float MaxAlpha = 0.25f;
    [Export] float FadeSpeed = 10f;

    float _alpha = 0f;
    HashSet<MapUnit> _visibleUnits = new();

    public override void _Ready()
    {
        AreaEntered += OnAreaEntered;
        AreaExited += OnAreaExited;
        BuildConeShape();
    }

    public override void _Process(double delta)
    {
        float target = HasTargets() ? MaxAlpha : 0f;

        _alpha = Mathf.Lerp(_alpha, target, FadeSpeed * (float)delta);

        // Solo redibujar si realmente hay cambio visible
        if (Mathf.Abs(_alpha - target) > 0.01f)
            QueueRedraw();
    }

    void OnAreaEntered(Area2D area)
    {
        if (area is not MapUnit other) return;
        if (other == ParentUnit) return;
        if (other.Team == ParentUnit.Team) return;

        _visibleUnits.Add(other);
        QueueRedraw();
    }

    void OnAreaExited(Area2D area)
    {
        if (area is not MapUnit other) return;

        _visibleUnits.Remove(other);
        QueueRedraw();
    }

    public bool CanSee(MapUnit other)
    {
        return _visibleUnits.Contains(other);
    }

    public bool HasTargets()
    {
        return _visibleUnits.Count > 0;
    }

    void BuildConeShape()
    {
        int steps = 12;
        float half = Mathf.DegToRad(Angle * 0.5f);

        var points = new Vector2[steps + 2];

        points[0] = Vector2.Zero;

        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            float a = Mathf.Lerp(-half, half, t);

            Vector2 dir = Vector2.Right.Rotated(a);
            points[i + 1] = dir * Distance;
        }

        Polygon.Polygon = points;
    }

    public override void _Draw()
    {
        if (_alpha <= 0.01f)
            return;

        var points = Polygon.Polygon;

        if (points.Length < 3)
            return;

        var _colorFill = new Color(ColorFill.R, ColorFill.G, ColorFill.B, _alpha);

        DrawColoredPolygon(points, _colorFill);
    }
}