using Godot;

public partial class PartySlotEdit : Control
{
    [Export] public int Row;
    [Export] public int Column;
    [Export] TextureRect CharImg;
    [Export] RichTextLabel CharName;
    [Export] ColorRect HPBarParent;
    [Export] HealthBar HPBar;
    [Export] Panel Background;

    public CharacterParty Party;
    public UIPartyInfo UIParty;
    public OrcInstance Orc;

    // changing style when dragging
    StyleBoxFlat normalStyle;
    StyleBoxFlat highlightValid;
    StyleBoxFlat highlightInvalid;
    StyleBoxFlat selectedStyle;
    int BorderWidthAll;
    bool nameVisible = true;

    public override void _Ready()
    {
        BuildStyles();
        ApplyNormal();
        UpdateVisual();
        Background.Visible = false;

        SelectionManager.I.SelectedOrcChanged += OnSelectedChanged;
    }

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

        var preview = new TextureRect
        {
            Texture = CharImg.Texture,
            CustomMinimumSize = new Vector2(48, 48),
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional
        };

        SetDragPreview(preview);

        return data;
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        Background.Visible = true;

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

        var orc = dict["orc"].As<OrcInstance>();
        if (orc == null)
        {
            ApplyInvalid();
            return false;
        }

        // límite de unidades
        string source = dict["source"].AsString();

        if (source == "pool" && Party.CurrentUnits >= Party.MaxUnits)
        {
            ApplyInvalid();
            return false;
        }

        bool valid = Party.CanPlaceOrc(Row, Column, orc);
        if (valid) ApplyValid();
        else ApplyInvalid();

        return valid;
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        var dict = (Godot.Collections.Dictionary)data;

        var orc = dict["orc"].As<OrcInstance>();
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

    public void SetOrc(OrcInstance orc)
    {
        Orc = orc;

        if (orc == null)
        {
            UpdateVisual();
            ApplyNormal();
            return;
        }

        UpdateVisual();

        if (SelectionManager.I.SelectedOrc == orc)
            AddThemeStyleboxOverride("panel", selectedStyle);
        else
            ApplyNormal();
    }

    public void UpdateVisual()
    {
        if (Orc == null)
        {
            CharImg.Visible = false;
            UpdateNameVisibility();
            HPBarParent.Visible = false;
        }
        else
        {
            CharImg.Texture = Orc.CharacterClass.GetFrontTexture();
            //CharImg.Texture = orc.CharacterClass.GetBackTexture();
            CharImg.Visible = true;
            CharName.Text = Orc.GetCustomName();
            // esto capaz en el caso de mirar partys de enemies?
            // float charNameX = IsFront ? 1f : -1f;
            // CharName.Scale = new Vector2(charNameX, 1f);
            UpdateNameVisibility();
            HPBarParent.Visible = true;
            HPBar.SetValue(Orc.CurrentHPPercentile);
        }
    }
    public void SetNameVisible(bool visible)
    {
        nameVisible = visible;
        UpdateNameVisibility();
    }
    void UpdateNameVisibility()
    {
        if (CharName == null)
            return;

        if (Orc != null)
            CharName.Visible = Orc.IsAlive && nameVisible;
        else
            CharName.Visible = nameVisible;
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

        // seleccionado
        selectedStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.2f, 0.6f, 1f, 0.25f),
            BorderColor = new Color(0.2f, 0.6f, 1f),
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2
        };
    }

    void ApplyNormal() => Background.AddThemeStyleboxOverride("panel", normalStyle);
    void ApplyValid() => Background.AddThemeStyleboxOverride("panel", highlightValid);
    void ApplyInvalid() => Background.AddThemeStyleboxOverride("panel", highlightInvalid);

    // Select orc
    public override void _GuiInput(InputEvent @event)
    {
        if (Orc == null)
            return;

        if (@event is InputEventMouseButton mb &&
            mb.Pressed &&
            mb.ButtonIndex == MouseButton.Left)
        {
            SelectionManager.I.SelectOrc(Orc);
        }
    }
    void OnSelectedChanged(OrcInstance selected)
    {
        if (Orc != null && selected == Orc)
            AddThemeStyleboxOverride("panel", selectedStyle);
        else
            ApplyNormal();
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
        // no sobreescribir el highlight de selected
        if (SelectionManager.I.SelectedOrc == Orc)
            return;

        if (!DragState.IsDragging || DragState.Data == null)
        {
            Background.Visible = false;
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

        var orc = dict["orc"].As<OrcInstance>();
        if (orc == null)
            return;

        string source = dict["source"].AsString();

        if (source == "pool" && Party.CurrentUnits >= Party.MaxUnits)
        {
            ApplyInvalid();
            return;
        }

        bool valid = Party.CanPlaceOrc(Row, Column, orc);
        if (valid) ApplyValid();
        else ApplyInvalid();
    }
}