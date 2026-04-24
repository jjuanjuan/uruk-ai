using Godot;

public partial class UIOrcPool : Control
{
    [Export] public PackedScene PoolItemScene;
    [Export] VBoxContainer list;

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
}