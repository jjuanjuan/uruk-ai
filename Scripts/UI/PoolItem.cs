using Godot;

public partial class PoolItem : PanelContainer
{
    [Export]
    TextureRect icon;
    [Export]
    RichTextLabel label;

    public OrcInstance Orc;

    StyleBoxFlat normalStyle;
    StyleBoxFlat selectedStyle;
    void ApplyNormal() => AddThemeStyleboxOverride("panel", normalStyle);
    void ApplySelected() => AddThemeStyleboxOverride("panel", selectedStyle);

    public override void _Ready()
    {
        BuildStyles();
        ApplyNormal();

        GameManager.I.SelectedOrcChanged += OnSelectedChanged;
    }

    public void Setup(OrcInstance orc)
    {
        Orc = orc;

        icon.Texture = orc.CharacterClass.GetFrontTexture();
        label.Text = orc.GetCustomName();
    }

    public override void _ExitTree()
    {
        if (GameManager.I != null)
            GameManager.I.SelectedOrcChanged -= OnSelectedChanged;
    }

    // DRAG START
    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (Orc == null)
            return new Variant();

        var data = new Godot.Collections.Dictionary
        {
            { "orc", Orc },
            { "source", "pool" }
        };

        DragState.IsDragging = true;
        DragState.Data = data;

        var preview = new TextureRect
        {
            Texture = icon.Texture,
            CustomMinimumSize = new Vector2(48, 48)
        };

        SetDragPreview(preview);

        return data;
    }

    // Select orc
    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb &&
            mb.Pressed &&
            mb.ButtonIndex == MouseButton.Left)
        {
            GameManager.I.SelectOrc(Orc);
        }
    }

    void BuildStyles()
    {
        normalStyle = new StyleBoxFlat
        {
            BgColor = new Color(0, 0, 0, 0.1f)
        };

        selectedStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.2f, 0.6f, 1f, 0.35f), // azul suave
            BorderColor = new Color(0.2f, 0.6f, 1f),
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2
        };
    }
    void OnSelectedChanged(OrcInstance selected)
    {
        if (selected == Orc)
            ApplySelected();
        else
            ApplyNormal();
    }
}