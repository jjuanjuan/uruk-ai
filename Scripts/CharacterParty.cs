using Godot;
using System.Collections.Generic;

public partial class CharacterParty : Node
{
    public const int ROWS = 3;
    public const int COLUMNS = 5;
    private Dictionary<Orc, PartyPosition> origin = new();

    [Export] public int MaxUnits = 6;

    [Signal]
    public delegate void PartyChangedEventHandler();

    public int CurrentUnits => origin.Count;

    public List<int> GetOccupiedColumns(int col)
    {
        var cols = new List<int> { col };

        if (col - 1 >= 0)
            cols.Add(col - 1);

        if (col + 1 < COLUMNS)
            cols.Add(col + 1);

        return cols;
    }

    public Orc GetOrc(int row, int col)
    {
        foreach (var kv in origin)
        {
            if (kv.Value.Row == row && kv.Value.Column == col)
                return kv.Key;
        }

        return null;
    }

    public bool CanPlaceOrc(int row, int col, Orc movingOrc = null)
    {
        if (!IsValidPosition(row, col))
            return false;

        foreach (var kv in origin)
        {
            var other = kv.Key;

            if (other == movingOrc)
                continue;

            var pos = kv.Value;

            if (pos.Row != row)
                continue;

            int otherCol = pos.Column;

            // regla de adyacencia (NO ocupación)
            if (Mathf.Abs(otherCol - col) <= 1)
                return false;
        }

        return true;
    }
    public bool PlaceOrc(Orc orc, int row, int col)
    {
        if (orc == null)
            return false;

        if (CurrentUnits >= MaxUnits)
            return false;

        if (!CanPlaceOrc(row, col, orc))
            return false;

        origin[orc] = new PartyPosition(row, col);

        EmitSignal(SignalName.PartyChanged);
        return true;
    }

    public bool MoveOrc(int fromRow, int fromCol, int toRow, int toCol)
    {
        var orc = GetOrc(fromRow, fromCol);
        if (orc == null)
            return false;

        if (!CanPlaceOrc(toRow, toCol, orc))
            return false;

        origin[orc] = new PartyPosition(toRow, toCol);

        EmitSignal(SignalName.PartyChanged);
        return true;
    }

    public bool SwapOrc(int r1, int c1, int r2, int c2)
    {
        var orcA = GetOrc(r1, c1);
        var orcB = GetOrc(r2, c2);

        if (orcA == null)
            return false;

        var posA = origin[orcA];

        if (orcB == null)
        {
            return MoveOrc(r1, c1, r2, c2);
        }

        var posB = origin[orcB];

        // validar ambos movimientos
        if (!CanPlaceOrc(posB.Row, posB.Column, orcA))
            return false;

        if (!CanPlaceOrc(posA.Row, posA.Column, orcB))
            return false;

        origin[orcA] = posB;
        origin[orcB] = posA;

        EmitSignal(SignalName.PartyChanged);
        return true;
    }

    public void RemoveOrc(int row, int col)
    {
        var orc = GetOrc(row, col);
        if (orc == null)
            return;

        origin.Remove(orc);

        EmitSignal(SignalName.PartyChanged);
    }

    bool IsValidPosition(int row, int column)
    {
        return row >= 0 && row < ROWS &&
               column >= 0 && column < COLUMNS;
    }

    public enum RowType
    {
        Front = 0,
        Middle = 1,
        Back = 2
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