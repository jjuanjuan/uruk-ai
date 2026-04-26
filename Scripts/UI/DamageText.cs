using Godot;

public partial class DamageText : Control
{
    [Export] public RichTextLabel Label;
    [Export] Vector2 initialPos = new Vector2(0, -10);
    [Export] Vector2 finalPos = new Vector2(0, -20);
    [Export] float animDuration = 1f;
    [Export] float initialPosVarX = 4;
    [Export] float initialPosVarY = 2;

    public void Setup(float value)
    {
        Label.Text = value.ToString("0");

        Vector2 basePos = Position;

        // random offset
        Vector2 startOffset = new Vector2(
            GameManager.I.NextFloat(-initialPosVarX, initialPosVarX),
            GameManager.I.NextFloat(-initialPosVarY, initialPosVarY)
        );

        Position = basePos + initialPos + startOffset;

        Vector2 target = Position + finalPos;

        var tween = CreateTween();

        tween.TweenProperty(this, "position", target, animDuration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);

        tween.TweenProperty(this, "modulate:a", 0, animDuration);

        tween.Finished += QueueFree;
    }
}