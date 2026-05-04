using Godot;

public partial class VisionCone : Node2D
{
    [Export] public Color Color = new Color(1, 0, 0, 0.2f);
    public float Distance = 120f;
    public float Angle = 90f;

    public MapUnit ParentUnit;

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (ParentUnit == null)
            return;

        int steps = 24;
        float half = Mathf.DegToRad(Angle * 0.5f);

        Vector2 forward = Vector2.Right;

        Vector2 prev = forward.Rotated(-half) * Distance;

        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            float angle = Mathf.Lerp(-half, half, t);

            Vector2 next = forward.Rotated(angle) * Distance;

            DrawLine(prev, next, Color, 2);
            DrawLine(Vector2.Zero, next, Color, 1);

            prev = next;
        }
    }
}