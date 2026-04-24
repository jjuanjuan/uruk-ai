using Godot;

public partial class PoolItem : PanelContainer
{
    [Export]
    TextureRect icon;
    [Export]
    RichTextLabel label;

    public Orc Orc;

    public void Setup(Orc orc)
    {
        Orc = orc;

        icon.Texture = orc.GetCharacterClass().GetFrontTexture();
        label.Text = orc.GetFirstName();
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (Orc == null)
            return new Variant();

        var data = new Godot.Collections.Dictionary
        {
            { "orc", Orc },
            { "source", "pool" }
        };

        var preview = new TextureRect
        {
            Texture = icon.Texture,
            CustomMinimumSize = new Vector2(48, 48)
        };

        SetDragPreview(preview);

        return data;
    }
}