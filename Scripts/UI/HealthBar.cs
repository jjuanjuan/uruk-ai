using Godot;

public partial class HealthBar : Control
{
    float Value; // entre 0f y 1f

    [Export] public Color DamageColor = Colors.Red;
    [Export] public Color HealthyColor = Colors.Green;

    public override void _Draw()
    {
        var size = Size;

        float midY = size.Y * Value;

        DrawRect(new Rect2(0, 0, size.X, midY), HealthyColor);
        DrawRect(new Rect2(0, midY, size.X, size.Y - midY), DamageColor);
    }

    public void SetValue(float v)
    {
        Value = Mathf.Clamp(v, 0, 1);
        QueueRedraw();
    }
}