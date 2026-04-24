using Godot;

public static class NodeExtensions
{
    public static T GetParentOfType<T>(this Node node) where T : Node
    {
        Node current = node.GetParent();

        while (current != null)
        {
            if (current is T typed)
                return typed;

            current = current.GetParent();
        }

        return null;
    }
}