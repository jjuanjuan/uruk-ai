using Godot;
using System.Collections.Generic;

public partial class UIClassTree : Control
{
    [Export] public PackedScene ClassNodeScene;

    Godot.Collections.Array<CharacterClass> AllClasses = new();

    Control nodesLayer;
    Dictionary<CharacterClass, int> classDepth = new();

    // CONSTANTES PARA UI
    const int X_SPACING = 140;
    const int Y_SPACING = 100;

    public override void _Ready()
    {
        if (GameManager.I.SelectedOrc == null)
        {
            CloseTree();
            return;
        }
        nodesLayer = GetNode<Control>("NodesParent");
        GetNode<Button>("CloseButton").Pressed += CloseTree;
        LoadAllClasses();
        Build();
        GameManager.I.SelectedOrcChanged += OnOrcSelected;
    }
    
    public override void _ExitTree()
    {
        if (GameManager.I != null)
            GameManager.I.SelectedOrcChanged -= OnOrcSelected;
    }
    void CloseTree()
    {
        QueueFree(); // destruye la UI
    }

    void Build()
    {
        BuildColumns();

        foreach (var kv in columns)
        {
            int column = kv.Key;
            var list = kv.Value;

            // ordenar dentro de la columna (opcional)
            list.Sort((a, b) => a.GetClassName().CompareTo(b.GetClassName()));

            for (int i = 0; i < list.Count; i++)
            {
                var cc = list[i];

                var node = ClassNodeScene.Instantiate<UIClassNode>();
                node.Setup(cc, GameManager.I.SelectedOrc);

                node.Position = new Vector2(
                    column * X_SPACING,
                    i * Y_SPACING
                );

                nodesLayer.AddChild(node);
            }
        }
    }

    public override void _Draw()
    {
        foreach (var cc in AllClasses)
        {
            var reqs = cc.GetRequirements();
            if (reqs == null) continue;

            foreach (var r in reqs)
            {
                var fromNode = FindNode(r.RequiredClass);
                var toNode = FindNode(cc);

                if (fromNode == null || toNode == null)
                    continue;

                DrawLine(
                    fromNode.Position + new Vector2(32, 32),
                    toNode.Position + new Vector2(32, 32),
                    Colors.Gray,
                    2
                );
            }
        }
    }

    UIClassNode FindNode(CharacterClass cc)
    {
        foreach (var child in nodesLayer.GetChildren())
        {
            if (child is UIClassNode node && node.Class == cc)
                return node;
        }
        return null;
    }

    void LoadAllClasses()
    {
        var dir = DirAccess.Open("res://Data/CharacterClasses");

        if (dir == null)
        {
            GD.PrintErr("No se pudo abrir carpeta de clases");
            return;
        }

        dir.ListDirBegin();

        while (true)
        {
            string file = dir.GetNext();

            if (file == "")
                break;

            if (dir.CurrentIsDir())
                continue;

            if (!file.EndsWith(".tres"))
                continue;

            string path = "res://Data/CharacterClasses/" + file;

            var resource = GD.Load<CharacterClass>(path);

            if (resource != null)
                AllClasses.Add(resource);
        }

        dir.ListDirEnd();
    }

    int GetDepth(CharacterClass cc)
    {
        if (classDepth.ContainsKey(cc))
            return classDepth[cc];

        var reqs = cc.GetRequirements();

        if (reqs == null || reqs.Count == 0)
        {
            classDepth[cc] = 0;
            return 0;
        }

        int max = 0;

        foreach (var r in reqs)
        {
            int d = GetDepth(r.RequiredClass);
            if (d > max)
                max = d;
        }

        classDepth[cc] = max + 1;
        return classDepth[cc];
    }

    Dictionary<int, List<CharacterClass>> columns = new();

    void BuildColumns()
    {
        foreach (var cc in AllClasses)
        {
            int depth = GetDepth(cc);

            if (!columns.ContainsKey(depth))
                columns[depth] = new List<CharacterClass>();

            columns[depth].Add(cc);
        }
    }

    void OnOrcSelected(OrcInstance orc)
    {
        if (orc == null)
            return;

        Build(); // refresh?
    }
}