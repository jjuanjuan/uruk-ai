using Godot;

public partial class HealthBar : Control
{
    float Value; // entre 0f y 1f

    [Export] public Color EmptyColor = Colors.Black;
    [Export] public Color HealthyColor = Colors.Red;

    public override void _Draw()
    {
        var size = Size;

        float midY = size.Y * Value;

        DrawRect(new Rect2(0, 0, size.X, midY), EmptyColor);
        DrawRect(new Rect2(0, midY, size.X, size.Y - midY), HealthyColor);
    }

    public void SetValue(float v)
    {
        Value = Mathf.Clamp(v, 0, 1);
        QueueRedraw();
    }
}