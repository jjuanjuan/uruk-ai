using Godot;
using System;

[Tool]
public partial class CombatConfigInspector : EditorInspectorPlugin
{
    public override bool _CanHandle(GodotObject obj)
    {
        // Evita problemas de "is" en C#
        return obj.GetType().Name == "CombatConfig";
    }

    public override void _ParseBegin(GodotObject obj)
    {
        var config = (CombatConfig)obj;

        int attackCount = Enum.GetValues(typeof(CombatConfig.AttackType)).Length;
        int armorCount  = Enum.GetValues(typeof(CombatConfig.ArmorType)).Length;

        int expectedSize = attackCount * armorCount;

        // Asegurar tamaño + defaults
        if (config.Multipliers == null || config.Multipliers.Length != expectedSize)
        {
            config.Multipliers = new float[expectedSize];
            for (int i = 0; i < expectedSize; i++)
                config.Multipliers[i] = 1f;
        }

        // Normalizar ceros a 1
        for (int i = 0; i < config.Multipliers.Length; i++)
        {
            if (Mathf.IsZeroApprox(config.Multipliers[i]))
                config.Multipliers[i] = 1f;
        }

        var root = new VBoxContainer();

        // Título general
        var title = new Label
        {
            Text = "Damage Multipliers",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 16);
        root.AddChild(title);

        var grid = new GridContainer
        {
            Columns = armorCount + 2 // Attack col + row labels + armors
        };

        // =========================
        // FILA 0 → "Armor Type"
        // =========================

        grid.AddChild(new Label()); // esquina vacía
        grid.AddChild(new Label()); // espacio sobre labels de ataque

        var armorTitle = new Label
        {
            Text = "Armor Type",
            HorizontalAlignment = HorizontalAlignment.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        armorTitle.AddThemeFontSizeOverride("font_size", 14);
        armorTitle.Modulate = new Color(0.8f, 0.8f, 1f);

        grid.AddChild(armorTitle);

        // completar fila
        for (int i = 1; i < armorCount; i++)
            grid.AddChild(new Label());

        // =========================
        // FILA 1 → headers de armor
        // =========================

        grid.AddChild(new Label()); // esquina
        grid.AddChild(new Label()); // espacio ataque

        foreach (CombatConfig.ArmorType armor in Enum.GetValues(typeof(CombatConfig.ArmorType)))
        {
            var lbl = new Label
            {
                Text = armor.ToString(),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            lbl.AddThemeFontSizeOverride("font_size", 13);
            grid.AddChild(lbl);
        }

        // =========================
        // FILAS de datos
        // =========================

        bool firstRow = true;

        foreach (CombatConfig.AttackType atk in Enum.GetValues(typeof(CombatConfig.AttackType)))
        {
            // Columna izquierda: "Attack Type"
            if (firstRow)
            {
                var attackTitle = new Label
                {
                    Text = "Attack Type",
                    VerticalAlignment = VerticalAlignment.Center,
                    SizeFlagsVertical = Control.SizeFlags.ExpandFill
                };
                attackTitle.RotationDegrees = -90;
                attackTitle.AddThemeFontSizeOverride("font_size", 14);
                attackTitle.Modulate = new Color(1f, 0.8f, 0.8f);

                grid.AddChild(attackTitle);
                firstRow = false;
            }
            else
            {
                grid.AddChild(new Label());
            }

            // Label del tipo de ataque
            var atkLabel = new Label
            {
                Text = atk.ToString(),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            grid.AddChild(atkLabel);

            // Celdas
            foreach (CombatConfig.ArmorType armor in Enum.GetValues(typeof(CombatConfig.ArmorType)))
            {
                int index = (int)atk * armorCount + (int)armor;

                var spin = new SpinBox
                {
                    MinValue = -1.0f,
                    MaxValue = 2.0f,
                    Step = 0.05f,
                    Value = config.Multipliers[index],
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
                };

                var panel = new PanelContainer();
                panel.AddChild(spin);

                UpdateVisual(panel, spin.Value, atk, armor);

                spin.ValueChanged += (double value) =>
                {
                    config.Multipliers[index] = (float)value;
                    UpdateVisual(panel, value, atk, armor);
                    config.EmitChanged();
                    ResourceSaver.Save(config);
                };

                grid.AddChild(panel);
            }
        }

        root.AddChild(grid);
        AddCustomControl(root);
    }

    private void UpdateVisual(PanelContainer panel, double value,
        CombatConfig.AttackType atk,
        CombatConfig.ArmorType armor)
    {
        var style = new StyleBoxFlat();

        // Color tipo heatmap simple
        float t = Mathf.Clamp((float)(value - 1.0), -1f, 1f);

        if (t > 0)
            style.BgColor = new Color(0.3f, 0.3f + t * 0.5f, 0.3f);
        else if (t < 0)
            style.BgColor = new Color(0.3f - Math.Abs(t) * 0.5f, 0.3f, 0.3f);
        else
            style.BgColor = new Color(0.3f, 0.3f, 0.3f);

        panel.AddThemeStyleboxOverride("panel", style);

        string relation =
            value > 1.01 ? "Fuerte" :
            value < 0.99 ? "Débil" : "Neutral";

        panel.TooltipText =
            $"{atk} vs {armor}\n" +
            $"Multiplicador: {value:0.00}\n" +
            $"Relación: {relation}";
    }

    public override bool _ParseProperty(
        GodotObject obj,
        Variant.Type type,
        string name,
        PropertyHint hint,
        string hintText,
        PropertyUsageFlags usage,
        bool wide)
    {
        if (name == "Multipliers")
            return true;

        return false;
    }
}