using Godot;

public partial class UIOrcPool : Control
{
    [Export] public PackedScene PoolItemScene;
    [Export] VBoxContainer list;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop; // sin esto puede no detectar drop
    }

    public void SetOrcs(Godot.Collections.Array<Orc> orcs)
    {
        // limpiar
        foreach (Node child in list.GetChildren())
            child.QueueFree();

        // crear items
        foreach (var orc in orcs)
        {
            var item = PoolItemScene.Instantiate<PoolItem>();
            item.Setup(orc);

            list.AddChild(item);
        }
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

        if (source == "party")
        {
            var fromParty = dict["from_party"].As<CharacterParty>();
            int fromRow = (int)dict["from_row"];
            int fromCol = (int)dict["from_col"];

            fromParty.RemoveOrc(fromRow, fromCol);
        }
    }
}