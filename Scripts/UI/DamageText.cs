using Godot;

public partial class DamageText : Control
{
    [Export] public RichTextLabel Label;

    public void Setup(float value)
    {
        Label.Text = value.ToString("0");

        // animación simple TODO: no hardcodear
        var tween = CreateTween();

        Position += new Vector2(0, -10);

        tween.TweenProperty(this, "position", Position + new Vector2(0, -40), 0.6f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);

        tween.TweenProperty(this, "modulate:a", 0, 0.6f);

        tween.Finished += QueueFree;
    }
}