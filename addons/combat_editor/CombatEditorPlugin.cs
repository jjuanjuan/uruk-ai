using Godot;

[Tool]
public partial class CombatEditorPlugin : EditorPlugin
{
    private CombatConfigInspector inspector;

    public override void _EnterTree()
    {
        inspector = new CombatConfigInspector();
        AddInspectorPlugin(inspector);
    }

    public override void _ExitTree()
    {
        RemoveInspectorPlugin(inspector);
    }
}