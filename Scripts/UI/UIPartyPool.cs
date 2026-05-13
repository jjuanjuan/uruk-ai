using Godot;

public partial class UIPartyPool : Control
{
    [Export] PackedScene UIPartyScene;
    [Export] GridContainer List;

    public override void _Ready()
    {
        GameManager.I.PartiesChanged += Refresh;

        Refresh();
    }

    public void Refresh()
    {
        foreach (Node child in List.GetChildren())
        {
            child.QueueFree();
        }

        var parties = GameManager.I.GetPartiesByTeam(
            TeamId.Player
        );

        foreach (var party in parties)
        {
            var ui = UIPartyScene.Instantiate<UIPartySmall>();

            List.AddChild(ui);

            ui.Setup(party);
        }
    }
}