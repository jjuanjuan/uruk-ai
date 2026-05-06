using Godot;

public partial class SelectionManager : Node
{
    public static SelectionManager I;

    public MapUnit Selected;

    public override void _EnterTree()
    {
        I = this;
    }

    public void Select(MapUnit unit)
    {
        if (Selected != null)
            Selected.SetSelected(false);

        Selected = unit;

        if (Selected != null)
            Selected.SetSelected(true);
    }

    public void ClearSelection()
    {
        if (Selected != null && IsInstanceValid(Selected))
            Selected.SetSelected(false);

        Selected = null;
    }
}