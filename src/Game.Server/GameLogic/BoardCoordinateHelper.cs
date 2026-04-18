namespace Game.Server.GameLogic;

public static class BoardCoordinateHelper
{
    public const int RowCount = 8;
    public const int ColumnCount = 4;

    public static int ToActualColumn(int row, int compressedColumn)
        => (compressedColumn * 2) + ((row + 1) % 2);

    public static bool TryToCompressedColumn(int row, int actualColumn, out int compressedColumn)
    {
        compressedColumn = -1;

        if (row is < 0 or >= RowCount || actualColumn is < 0 or > 7)
        {
            return false;
        }

        var isPlayable = ((row + actualColumn) % 2) == 1;
        if (!isPlayable)
        {
            return false;
        }

        compressedColumn = row % 2 == 0
            ? (actualColumn - 1) / 2
            : actualColumn / 2;

        return compressedColumn is >= 0 and < ColumnCount;
    }

    public static bool IsInBounds(int row, int column)
        => row is >= 0 and < RowCount && column is >= 0 and < ColumnCount;
}
