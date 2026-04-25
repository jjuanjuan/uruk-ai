using Godot;

public partial class PoolItem : PanelContainer
{
    [Export]
    TextureRect icon;
    [Export]
    RichTextLabel label;

    public OrcInstance Orc;

    public void Setup(OrcInstance orc)
    {
        Orc = orc;

        icon.Texture = orc.CharacterClass.GetFrontTexture();
        label.Text = orc.GetCustomName();
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
}