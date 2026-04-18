namespace Game.Client.Controls;

public sealed class BoardCellClickedEventArgs(int row, int column) : EventArgs
{
    public int Row { get; } = row;

    public int Column { get; } = column;
}
