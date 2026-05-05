using Godot;

public partial class CombatAdvantageBar : Control
{
    float Value; // entre 0f y 1f

    [Export] public Color TeamAColor = Colors.Blue;
    [Export] public Color TeamBColor = Colors.Red;
    [Export] public float TweenDuration = 1.5f;

    public override void _Draw()
    {
        var size = Size;

        float midY = size.Y * Value;

        DrawRect(new Rect2(0, 0, size.X, midY), TeamAColor);
        DrawRect(new Rect2(0, midY, size.X, size.Y - midY), TeamBColor);
    }

    public void SetValue(float v)
    {
        Value = Mathf.Clamp(v, 0, 1);
        QueueRedraw();
    }

    public void UpdateBarAnimated(float to)
    {
        float fromNormalized = Value;
        float toNormalized = to;

        var tween = CreateTween();

        tween.TweenMethod(
            Callable.From<float>(SetValue),
            fromNormalized,
            toNormalized,
            TweenDuration
        )
        .SetTrans(Tween.TransitionType.Quad)
        .SetEase(Tween.EaseType.Out);
    }
}