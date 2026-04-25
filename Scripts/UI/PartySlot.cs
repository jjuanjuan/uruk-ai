using Godot;

public partial class PartySlot : PanelContainer
{
    [Export] public int Row;
    [Export] public int Column;

    [Export]
    TextureRect CharImg;
    [Export]
    RichTextLabel CharName;

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

        UIParty?.LayoutSlots();

        GD.Print($"Slot {Row},{Column} Party is NULL? {Party == null}");
    }

    public void UpdateVisual()
    {
        if (Orc == null)
        {
            CharImg.Visible = false;
            CharName.Visible = false;
        }
        else
        {
            CharImg.Visible = true;
            CharName.Text = Orc.GetFirstName();
            CharName.Visible = true;
        }
    }

    void BuildStyles()
    {
        normalStyle = new StyleBoxFlat
        {
            BgColor = new Color(0f, 0f, 0f, .1f),
        };

        // válido (verde leve)
        highlightValid = new StyleBoxFlat
        {
            BgColor = normalStyle.BgColor.Lerp(new Color(0.2f, 0.6f, 0.2f), 0.5f),
        };

        // inválido (rojo leve)
        highlightInvalid = new StyleBoxFlat
        {
            BgColor = normalStyle.BgColor.Lerp(new Color(0.6f, 0.2f, 0.2f), 0.5f),
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
            CharImg.Texture = null;
            CharName.Text = "";
            UpdateVisual();
            return;
        }

        CharImg.Texture = orc.GetCharacterClass().GetFrontTexture();
        CharName.Text = orc.GetFirstName();
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
            { "from_col", Column },
            { "from_party", Party }
        };

        DragState.IsDragging = true;
        DragState.Data = data;

        // preview
        var preview = new TextureRect
        {
            Texture = CharImg.Texture,
            Size = new Vector2(48, 48),
            CustomMinimumSize = new Vector2(48, 48)
        };

        SetDragPreview(preview);

        return data;
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (Party == null)
        {
            ApplyInvalid();
            return false;
        }

        if (data.VariantType != Variant.Type.Dictionary)
        {
            ApplyInvalid();
            return false;
        }

        var dict = (Godot.Collections.Dictionary)data;

        if (!dict.ContainsKey("orc"))
        {
            ApplyInvalid();
            return false;
        }

        var orc = dict["orc"].As<Orc>();
        if (orc == null)
        {
            ApplyInvalid();
            return false;
        }

        bool valid = true;

        int start = Mathf.Max(0, Column - 1);
        int end = Mathf.Min(CharacterParty.COLUMNS - 1, Column + 1);

        for (int c = start; c <= end; c++)
        {
            var other = Party.GetOrc(Row, c);

            if (other != null && other != orc)
            {
                valid = false;
                break;
            }
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
            var fromParty = dict["from_party"].As<CharacterParty>();

            if (fromParty == Party)
            {
                Party.SwapOrc(fromRow, fromCol, Row, Column);
            }
            else
            {
                fromParty.RemoveOrc(fromRow, fromCol);
                Party.PlaceOrc(orc, Row, Column);
            }
        }
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
        if (!DragState.IsDragging || DragState.Data == null)
        {
            ApplyNormal();
            return;
        }
        EvaluateHighlight();
    }

    void EvaluateHighlight()
    {
        if (Party == null)
            return;

        if (DragState.Data == null)
            return;

        if (DragState.Data is not Godot.Collections.Dictionary dict)
            return;

        if (!dict.ContainsKey("orc"))
            return;

        var orc = dict["orc"].As<Orc>();
        if (orc == null)
            return;

        bool valid = true;

        int start = Mathf.Max(0, Column - 1);
        int end = Mathf.Min(CharacterParty.COLUMNS - 1, Column + 1);

        for (int c = start; c <= end; c++)
        {
            var other = Party.GetOrc(Row, c);

            if (other != null && other != orc)
            {
                valid = false;
                break;
            }
        }

        if (valid) ApplyValid();
        else ApplyInvalid();
    }

    public void UpdateLayout()
    {
        Vector2 pos = UIParty.GetVisualPosition(Row, Column);
        Position = pos;
    }
}