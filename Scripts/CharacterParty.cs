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

    public bool SwapOrc(int r1, int c1, int r2, int c2)
    {
        if (!IsValidPosition(r1, c1) || !IsValidPosition(r2, c2))
            return false;

        var temp = grid[r2, c2];
        grid[r2, c2] = grid[r1, c1];
        grid[r1, c1] = temp;

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