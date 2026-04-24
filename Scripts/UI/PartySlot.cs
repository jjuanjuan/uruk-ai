using Godot;

public partial class PartySlot : PanelContainer
{
    [Export] public int Row;
    [Export] public int Column;

    [Export]
    TextureRect icon;
    [Export]
    RichTextLabel label;

    public Orc Orc;
    public CharacterParty Party;

    public void SetOrc(Orc orc)
    {
        Orc = orc;

        if (orc == null)
        {
            icon.Texture = null;
            label.Text = "";
            return;
        }

        icon.Texture = orc.GetCharacterClass().GetFrontTexture();
        label.Text = orc.GetFirstName();
    }

    // DRAG START
    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (Orc == null)
            return new Variant();

        var data = new Godot.Collections.Dictionary
        {
            { "orc", Orc },
            { "source", "party" },
            { "from_row", Row },
            { "from_col", Column }
        };

        // preview
        var preview = new TextureRect
        {
            Texture = icon.Texture,
            CustomMinimumSize = new Vector2(48, 48)
        };

        SetDragPreview(preview);

        return data;
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.Dictionary)
            return false;

        var dict = (Godot.Collections.Dictionary)data;

        return dict.ContainsKey("orc");
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        var dict = (Godot.Collections.Dictionary)data;

        var orc = dict["orc"].As<Orc>();
        string source = dict["source"].AsString();

        var ui = GetParent().GetParent<UIParty>();

        if (source == "party")
        {
            int fromRow = (int)dict["from_row"];
            int fromCol = (int)dict["from_col"];

            Party.SwapOrc(fromRow, fromCol, Row, Column);
        }
        else if (source == "pool")
        {
            // colocar si está libre
            if (Party.GetOrc(Row, Column) == null)
            {
                Party.PlaceOrc(orc, Row, Column);
            }
            else
            {
                // opcional: swap con pool
                // por ahora no hace nada
            }
        }

        ui.Refresh();
    }
}