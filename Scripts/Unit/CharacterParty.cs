using Godot;
using System.Collections.Generic;

public partial class CharacterParty : Node
{
    public const int ROWS = 3;
    public const int COLUMNS = 5;

    private Dictionary<OrcInstance, PartyPosition> origin = new();

    [Export] public int MaxUnits = 6;

    [Signal]
    public delegate void PartyChangedEventHandler();

    public int CurrentUnits => origin.Count;

    public OrcInstance GetOrc(int row, int col)
    {
        foreach (var kv in origin)
        {
            if (kv.Value.Row == row && kv.Value.Column == col)
                return kv.Key;
        }
        return null;
    }

    public bool CanPlaceOrc(int row, int col, OrcInstance movingOrc = null)
    {
        if (!IsValidPosition(row, col))
            return false;

        foreach (var kv in origin)
        {
            var other = kv.Key;
            if (other == movingOrc) continue;

            var pos = kv.Value;
            if (pos.Row != row) continue;

            if (Mathf.Abs(pos.Column - col) <= 1)
                return false;
        }

        return true;
    }

    public bool PlaceOrc(OrcInstance orc, int row, int col)
    {
        if (orc == null) return false;

        if (!CanPlaceOrc(row, col, orc))
            return false;

        origin[orc] = new PartyPosition(row, col);
        orc.PartyPosition = new PartyPosition(row, col);
        GD.Print($"Placed {orc.GetCustomName()} in {row},{col}");
        EmitAllSignals();
        return true;
    }

    public bool MoveOrc(int fromRow, int fromCol, int toRow, int toCol)
    {
        var orc = GetOrc(fromRow, fromCol);
        if (orc == null) return false;

        if (!CanPlaceOrc(toRow, toCol, orc))
            return false;

        origin[orc] = new PartyPosition(toRow, toCol);
        orc.PartyPosition = new PartyPosition(toRow, toCol);

        EmitAllSignals();
        return true;
    }

    public bool SwapOrc(int r1, int c1, int r2, int c2)
    {
        var a = GetOrc(r1, c1);
        var b = GetOrc(r2, c2);

        if (a == null) return false;

        if (b == null)
            return MoveOrc(r1, c1, r2, c2);

        var posA = origin[a];
        var posB = origin[b];

        if (!CanPlaceOrc(posB.Row, posB.Column, a)) return false;
        if (!CanPlaceOrc(posA.Row, posA.Column, b)) return false;

        origin[a] = posB;
        origin[b] = posA;

        a.PartyPosition = posB;
        b.PartyPosition = posA;

        EmitAllSignals();
        return true;
    }

    public void RemoveOrc(int row, int col)
    {
        var orc = GetOrc(row, col);
        if (orc == null) return;

        origin.Remove(orc);

        EmitAllSignals();
    }

    void EmitAllSignals()
    {
        EmitSignal(SignalName.PartyChanged);
        GameManager.I.EmitSignal(GameManager.SignalName.PartiesChanged);
    }

    bool IsValidPosition(int row, int col)
    {
        return row >= 0 && row < ROWS &&
               col >= 0 && col < COLUMNS;
    }

    public List<OrcInstance> GetAllOrcs()
    {
        return new List<OrcInstance>(origin.Keys);
    }
    public List<OrcInstance> GetAllLivingOrcs()
    {
        var list = new List<OrcInstance>();
        foreach (var kv in origin)
            if (kv.Key.IsAlive) list.Add(kv.Key);

        return list;
    }

    public bool IsDefeated()
    {
        return GetAllLivingOrcs().Count <= 0;
    }

    public bool IsMember(OrcInstance orc)
    {
        return origin.ContainsKey(orc);
    }
}

public struct PartyPosition
{
    public int Row;
    public int Column;

    public PartyPosition(int row, int column)
    {
        Row = row;
        Column = column;
    }
}