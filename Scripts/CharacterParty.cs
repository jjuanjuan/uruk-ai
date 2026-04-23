using Godot;
using System.Collections.Generic;

public partial class CharacterParty : Node
{
    public const int ROWS = 3;
    public const int COLUMNS = 5;

    // grid
    private Orc[,] grid = new Orc[ROWS, COLUMNS];

    [Export] public int MaxUnits = 6;

    public int CurrentUnits
    {
        get
        {
            int count = 0;
            foreach (var orc in grid)
                if (orc != null) count++;
            return count;
        }
    }

    public bool PlaceOrc(Orc orc, int row, int column)
    {
        if (!IsValidPosition(row, column))
            return false;

        if (grid[row, column] != null)
            return false;

        if (CurrentUnits >= MaxUnits)
            return false;

        grid[row, column] = orc;
        return true;
    }

    public bool MoveOrc(int fromRow, int fromCol, int toRow, int toCol)
    {
        if (!IsValidPosition(toRow, toCol))
            return false;

        if (grid[fromRow, fromCol] == null)
            return false;

        if (grid[toRow, toCol] != null)
            return false;

        grid[toRow, toCol] = grid[fromRow, fromCol];
        grid[fromRow, fromCol] = null;

        return true;
    }

    public void RemoveOrc(int row, int column)
    {
        if (!IsValidPosition(row, column))
            return;

        grid[row, column] = null;
    }

    public Orc GetOrc(int row, int column)
    {
        if (!IsValidPosition(row, column))
            return null;

        return grid[row, column];
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
    public int Row;    // 0–2
    public int Column; // 0–4

    public PartyPosition(int row, int column)
    {
        Row = row;
        Column = column;
    }
}