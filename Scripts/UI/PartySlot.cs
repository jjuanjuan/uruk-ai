using Godot;

public partial class PartySlot : PanelContainer
{
    [Export] public int Row;
    [Export] public int Column;

    [Export] TextureRect CharImg;
    [Export] RichTextLabel CharName;
    [Export] HealthBar HPBar;

    [Export] Control ContentParent; // uso este para sacudir y otros efectos

    [Export] float ShakeDuration = 0.6f;
    [Export] Vector2 ShakeIntensity = new Vector2(2f, 10f);
    [Export] float SquashDuration = 0.3f;
    [Export] Vector2 SquashIntensity = new Vector2(1f, 2f);
    [Export] float DeathAnimationDuration = 1.5f;

    public OrcInstance Orc;
    public CharacterParty Party;
    public UIParty UIParty;
    public bool IsFront;

    // changing style when dragging
    StyleBoxFlat normalStyle;
    StyleBoxFlat highlightValid;
    StyleBoxFlat highlightInvalid;
    StyleBoxFlat selectedStyle;
    int BorderWidthAll;

    public override void _Ready()
    {
        BuildStyles();
        ApplyNormal();
        UpdateVisual();

        UIParty?.LayoutSlots();

        GameManager.I.SelectedOrcChanged += OnSelectedChanged;
    }

    public override void _ExitTree()
    {
        if (GameManager.I != null)
            GameManager.I.SelectedOrcChanged -= OnSelectedChanged;
    }

    public void UpdateVisual()
    {
        if (Orc == null)
        {
            CharImg.Visible = false;
            CharName.Visible = false;
            HPBar.Visible = false;
        }
        else
        {
            CharImg.Visible = true;
            CharName.Text = Orc.GetCustomName();
            CharName.Visible = true;
            HPBar.Visible = true;
            HPBar.SetValue(Orc.CurrentHPPercentile);
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

    void ApplyNormal() => AddThemeStyleboxOverride("panel", normalStyle);
    void ApplyValid() => AddThemeStyleboxOverride("panel", highlightValid);
    void ApplyInvalid() => AddThemeStyleboxOverride("panel", highlightInvalid);


    public void SetOrc(OrcInstance orc)
    {
        Orc = orc;

        if (orc == null)
        {
            CharImg.Visible = false;
            CharName.Visible = false;
            UpdateVisual();
            ApplyNormal();
            return;
        }

        CharImg.Texture = orc.CharacterClass.GetFrontTexture();
        CharImg.FlipV = !IsFront; // TODO: reemplazar con imagenes back
        //CharImg.Texture = orc.CharacterClass.GetBackTexture();
        CharName.Text = orc.GetCustomName();

        CharImg.Visible = true;
        CharName.Visible = true;

        UpdateVisual();

        if (GameManager.I.SelectedOrc == orc)
            AddThemeStyleboxOverride("panel", selectedStyle);
        else
            ApplyNormal();
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

        bool valid = true;

        // adyacencia
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
        if (GameManager.I.SelectedOrc == Orc)
            return;

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

        var orc = dict["orc"].As<OrcInstance>();
        if (orc == null)
            return;

        string source = dict["source"].AsString();

        if (source == "pool" && Party.CurrentUnits >= Party.MaxUnits)
        {
            ApplyInvalid();
            return;
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
    }

    public void UpdateLayout()
    {
        Vector2 pos = UIParty.GetVisualPosition(Row, Column);
        Position = pos;
    }

    // ANIMATIONS ///////////////////////////////////////////////////////////////
    public void UpdateHPBarAnimated(OrcInstance orc, float from, float to)
    {
        float max = orc.MaxHP;

        float fromNormalized = from / max;
        float toNormalized = to / max;

        // 1. barra HP
        var hpTween = CreateTween();

        hpTween.TweenMethod(
            Callable.From<float>(value =>
            {
                if (HPBar != null)
                    HPBar.SetValue(value);
            }),
            fromNormalized,
            toNormalized,
            0.35f
        )
        .SetTrans(Tween.TransitionType.Quad)
        .SetEase(Tween.EaseType.Out);

        // 2. flash separado (NO mezclar con el otro tween)
        var flashTween = CreateTween();

        if (HPBar != null)
        {
            HPBar.Modulate = Colors.Red;

            flashTween.TweenProperty(
                HPBar,
                "modulate",
                Colors.White,
                0.25f
            )
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        }
    }
    public void PlayHitShake(int damage)
    {
        float intensity = Mathf.Clamp(damage * 0.1f, ShakeIntensity.X, ShakeIntensity.Y);
        var tween = CreateTween();

        Vector2 original = ContentParent.Position;

        tween.TweenMethod(
            Callable.From<float>(t =>
            {
                float x = GameManager.I.NextFloat(-intensity, intensity);
                float y = GameManager.I.NextFloat(-intensity, intensity);

                ContentParent.Position = original + new Vector2(x, y);
            }),
            0f,
            ShakeDuration,
            ShakeDuration / 6f
        );

        tween.TweenCallback(Callable.From(() =>
        {
            ContentParent.Position = original;
        }));
    }
    public void PlayHitSquash(int damage)
    {
        float intensity = Mathf.Clamp(damage / 50f, SquashIntensity.X, SquashIntensity.Y);

        ContentParent.Scale = Vector2.One;

        var tween = CreateTween();

        tween.TweenProperty(ContentParent, "scale", new Vector2(1.15f * intensity, 0.85f), SquashDuration * 0.3f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        tween.TweenProperty(ContentParent, "scale", new Vector2(.95f * intensity, 1.05f), SquashDuration * 0.3f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        tween.TweenProperty(ContentParent, "scale", Vector2.One, SquashDuration * 0.4f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
    }
    public void PlayDeathFade()
    {
        var tween = CreateTween();

        // fade general del contenido
        tween.TweenProperty(CharImg, "modulate:a", 0f, DeathAnimationDuration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);

        tween.Finished += () =>
        {
            ContentParent.Modulate = new Color(1, 1, 1, 0);
        };
    }
    ////////////////////////////////////////////////////////////////////////

    // Select orc
    public override void _GuiInput(InputEvent @event)
    {
        if (Orc == null)
            return;

        if (@event is InputEventMouseButton mb &&
            mb.Pressed &&
            mb.ButtonIndex == MouseButton.Left)
        {
            GameManager.I.SelectOrc(Orc);
        }
    }
    void OnSelectedChanged(OrcInstance selected)
    {
        if (Orc != null && selected == Orc)
            AddThemeStyleboxOverride("panel", selectedStyle);
        else
            ApplyNormal();
    }
}