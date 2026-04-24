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
    public UIParty UIParty;

    // changing style when dragging
    StyleBoxFlat normalStyle;
    StyleBoxFlat highlightValid;
    StyleBoxFlat highlightInvalid;
    int BorderWidthAll;

    public override void _Ready()
    {
        BuildStyles();
        ApplyNormal();
        UpdateVisual();
    }

    public void UpdateVisual()
    {
        if (Orc == null)
        {
            label.Visible = false;
        }
        else
        {
            label.Visible = true;
            label.Text = Orc.GetFirstName();
        }
    }

    void BuildStyles()
    {
        normalStyle = new StyleBoxFlat
        {
            BgColor = new Color(0f, 0f, 0f, .1f),
            BorderColor = new Color(0.4f, 0.4f, 0.4f)
        };

        // válido (verde leve)
        highlightValid = new StyleBoxFlat
        {
            BgColor = normalStyle.BgColor.Lerp(new Color(0.2f, 0.6f, 0.2f), 0.5f),
            BorderWidthBottom = normalStyle.BorderWidthBottom + 1,
            BorderWidthLeft = normalStyle.BorderWidthLeft + 1,
            BorderWidthRight = normalStyle.BorderWidthRight + 1,
            BorderWidthTop = normalStyle.BorderWidthTop + 1,
            BorderColor = new Color(0.3f, 0.9f, 0.3f)
        };

        // inválido (rojo leve)
        highlightInvalid = new StyleBoxFlat
        {
            BgColor = normalStyle.BgColor.Lerp(new Color(0.6f, 0.2f, 0.2f), 0.5f),
            BorderWidthBottom = normalStyle.BorderWidthBottom + 1,
            BorderWidthLeft = normalStyle.BorderWidthLeft + 1,
            BorderWidthRight = normalStyle.BorderWidthRight + 1,
            BorderWidthTop = normalStyle.BorderWidthTop + 1,
            BorderColor = new Color(0.9f, 0.3f, 0.3f)
        };
    }

    void ApplyNormal() => AddThemeStyleboxOverride("panel", normalStyle);
    void ApplyValid() => AddThemeStyleboxOverride("panel", highlightValid);
    void ApplyInvalid() => AddThemeStyleboxOverride("panel", highlightInvalid);


    public void SetOrc(Orc orc)
    {
        Orc = orc;

        if (orc == null)
        {
            icon.Texture = null;
            label.Text = "";
            UpdateVisual();
            return;
        }

        icon.Texture = orc.GetCharacterClass().GetFrontTexture();
        label.Text = orc.GetFirstName();
        UpdateVisual();
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

        DragState.IsDragging = true;
        DragState.Data = data;

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
        {
            ApplyInvalid();
            return false;
        }

        var dict = (Godot.Collections.Dictionary)data;
        var orc = dict["orc"].As<Orc>();
        string source = dict["source"].AsString();

        bool valid = false;

        if (source == "pool")
        {
            // válido si la celda está vacía
            valid = Party.GetOrc(Row, Column) == null;
        }
        else if (source == "party")
        {
            // válido si es swap o mover a vacío
            int fromRow = (int)dict["from_row"];
            int fromCol = (int)dict["from_col"];

            valid = !(fromRow == Row && fromCol == Column);
        }

        if (valid) ApplyValid();
        else ApplyInvalid();

        return valid;
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        var dict = (Godot.Collections.Dictionary)data;

        var orc = dict["orc"].As<Orc>();
        string source = dict["source"].AsString();

        if (source == "pool")
        {
            Party.PlaceOrc(orc, Row, Column);
        }
        else if (source == "party")
        {
            int fromRow = (int)dict["from_row"];
            int fromCol = (int)dict["from_col"];

            Party.SwapOrc(fromRow, fromCol, Row, Column);
        }

        ApplyNormal();
        UpdateVisual();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationDragEnd)
        {
            DragState.IsDragging = false;
            ApplyNormal();
        }
    }

    public override void _Process(double delta)
    {
        if (!DragState.IsDragging)
        {
            ApplyNormal();
            return;
        }
        EvaluateHighlight();
    }

    void EvaluateHighlight()
    {
        if (DragState.Data == null)
            return;

        var dict = DragState.Data;

        var orc = dict["orc"].As<Orc>();
        string source = dict["source"].AsString();

        bool valid = false;

        if (source == "pool")
        {
            valid = Party.GetOrc(Row, Column) == null;
        }
        else if (source == "party")
        {
            int fromRow = (int)dict["from_row"];
            int fromCol = (int)dict["from_col"];

            valid = !(fromRow == Row && fromCol == Column);
        }

        if (valid) ApplyValid();
        else ApplyInvalid();
    }
}