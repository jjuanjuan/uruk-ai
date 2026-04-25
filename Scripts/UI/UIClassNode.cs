using Godot;
using System.Text;

public partial class UIClassNode : Button
{
    [Export] TextureRect icon;
    [Export] RichTextLabel nameLabel;
    [Export] RichTextLabel reqLabel;

    public CharacterClass Class;
    OrcInstance Orc;

    public override void _Ready()
    {
        Pressed += OnPressed;
    }

    void OnPressed()
    {
        Orc.ChangeClass(Class);
    }

    public void Setup(CharacterClass cc, OrcInstance orc)
    {
        Class = cc;
        Orc = orc;

        icon.Texture = cc.GetFrontTexture();
        nameLabel.Text = cc.GetClassName();

        BuildRequirements();

        UpdateState();
    }

    void BuildRequirements()
    {
        var reqs = Class.GetRequirements();

        if (reqs == null || reqs.Count == 0)
        {
            reqLabel.Visible = false;
            return;
        }

        reqLabel.Visible = true;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Requires:");

        foreach (var r in reqs)
        {
            sb.AppendLine($"{r.RequiredClass.GetClassName()} level {r.RequiredLevel}");
        }

        reqLabel.Text = sb.ToString();
    }

    public void UpdateState()
    {
        bool unlocked = Orc.CanChangeToClass(Class);
        var progress = Orc.GetProgress(Class);

        if (progress.Level > 0)
        {
            Modulate = new Color(0.7f, 0.9f, 1f); // ya usada
        }
        else if (unlocked)
        {
            Modulate = new Color(0.7f, 1f, 0.7f); // disponible
        }
        else
        {
            Modulate = new Color(0.4f, 0.4f, 0.4f); // bloqueada
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            Orc.ChangeClass(Class);
        }
    }
}