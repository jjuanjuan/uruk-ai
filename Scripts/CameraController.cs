using Godot;

public partial class CameraController : Camera2D
{
    [Export] public float MoveSpeed = 800f;
    [Export] public float ZoomSpeed = 0.1f;
    [Export] public float MinZoom = 0.5f;
    [Export] public float MaxZoom = 2.5f;
    [Export] public MapManager Map;

    Rect2 bounds;
    bool IsInit;

    public void Init()
    {
        bounds = Map.GetWorldBounds();
        IsInit = true;
    }

    public override void _Process(double delta)
    {
        if (!IsInit) return;
        HandleMovement((float)delta);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed)
        {
            if (mb.ButtonIndex == MouseButton.WheelUp)
                ZoomCamera(ZoomSpeed);

            if (mb.ButtonIndex == MouseButton.WheelDown)
                ZoomCamera(-ZoomSpeed);
        }
    }

    void HandleMovement(float delta)
    {
        Vector2 dir = Vector2.Zero;

        if (Input.IsActionPressed("map_up"))
            dir.Y -= 1;
        if (Input.IsActionPressed("map_down"))
            dir.Y += 1;
        if (Input.IsActionPressed("map_left"))
            dir.X -= 1;
        if (Input.IsActionPressed("map_right"))
            dir.X += 1;

        if (dir != Vector2.Zero)
        {
            dir = dir.Normalized();
            Position += dir * MoveSpeed * delta;
            ClampToBounds();
        }
    }

    // zoom hacia el centro nomás
    /*
    void ZoomCamera(float amount)
    {
        float newZoom = Mathf.Clamp(Zoom.X + amount, MinZoom, MaxZoom);
        Zoom = new Vector2(newZoom, newZoom);
    }
    */

    // zoom hacia el cursor
    void ZoomCamera(float amount)
    {
        Vector2 mouseBefore = GetGlobalMousePosition();

        float newZoom = Mathf.Clamp(Zoom.X + amount, MinZoom, MaxZoom);
        Zoom = new Vector2(newZoom, newZoom);

        Vector2 mouseAfter = GetGlobalMousePosition();
        Position += (mouseBefore - mouseAfter);
    }

    void ClampToBounds()
    {
        Vector2 screenSize = GetViewportRect().Size;
        Vector2 half = screenSize * 0.5f * Zoom;

        float minX = bounds.Position.X + half.X;
        float maxX = bounds.End.X - half.X;

        float minY = bounds.Position.Y + half.Y;
        float maxY = bounds.End.Y - half.Y;

        Position = new Vector2(
            Mathf.Clamp(Position.X, minX, maxX),
            Mathf.Clamp(Position.Y, minY, maxY)
        );
    }
}