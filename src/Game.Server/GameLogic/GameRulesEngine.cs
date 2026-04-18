using Game.Contracts.Enums;

namespace Game.Server.GameLogic;

public sealed class GameRulesEngine
{
    public IReadOnlyList<MoveDefinition> GetLegalMoves(GameState state, PlayerSide side)
    {
        var activePieces = state.Pieces
            .Where(piece => !piece.IsCaptured && piece.Side == side)
            .OrderBy(piece => piece.Row)
            .ThenBy(piece => piece.Column)
            .ToArray();

        return activePieces
            .SelectMany(piece => GetMovesForPiece(state, piece))
            .OrderByDescending(move => move.WasCapture)
            .ThenBy(move => move.FromRow)
            .ThenBy(move => move.FromColumn)
            .ThenBy(move => move.ToRow)
            .ThenBy(move => move.ToColumn)
            .ToArray();
    }

    public MoveDefinition? TryFindHumanMove(GameState state, int fromRow, int fromColumn, int toRow, int toColumn)
    {
        var piece = state.Pieces.FirstOrDefault(candidate =>
            !candidate.IsCaptured
            && candidate.Side == PlayerSide.Human
            && candidate.Row == fromRow
            && candidate.Column == fromColumn);

        if (piece is null)
        {
            return null;
        }

        return GetMovesForPiece(state, piece)
            .FirstOrDefault(move => move.ToRow == toRow && move.ToColumn == toColumn);
    }

    public void ApplyMove(GameState state, MoveDefinition move)
    {
        var piece = state.Pieces.First(candidate => candidate.PieceId == move.PieceId);
        piece.Row = move.ToRow;
        piece.Column = move.ToColumn;
        piece.HasUsedBackwardMove |= move.UsedBackwardMove;

        if (move.CapturedPieceId.HasValue)
        {
            var capturedPiece = state.Pieces.First(candidate => candidate.PieceId == move.CapturedPieceId.Value);
            capturedPiece.IsCaptured = true;
        }

        state.CurrentTurn = move.Side == PlayerSide.Human ? PlayerSide.Server : PlayerSide.Human;
    }

    public bool HasReachedBackRank(GameState state, PlayerSide side)
    {
        var targetRow = side == PlayerSide.Human ? 0 : BoardCoordinateHelper.RowCount - 1;
        return state.Pieces.Any(piece => !piece.IsCaptured && piece.Side == side && piece.Row == targetRow);
    }

    private IEnumerable<MoveDefinition> GetMovesForPiece(GameState state, GamePieceState piece)
    {
        var forwardDirection = piece.Side == PlayerSide.Human ? -1 : 1;

        foreach (var move in GetStepMoves(state, piece, forwardDirection, usedBackwardMove: false))
        {
            yield return move;
        }

        if (!piece.HasUsedBackwardMove)
        {
            foreach (var move in GetStepMoves(state, piece, -forwardDirection, usedBackwardMove: true))
            {
                yield return move;
            }
        }

        foreach (var move in GetCaptureMoves(state, piece, forwardDirection))
        {
            yield return move;
        }
    }

    private IEnumerable<MoveDefinition> GetStepMoves(GameState state, GamePieceState piece, int rowDirection, bool usedBackwardMove)
    {
        foreach (var actualColumnDelta in new[] { -1, 1 })
        {
            if (TryTranslateActual(piece.Row, piece.Column, rowDirection, actualColumnDelta, out var targetRow, out var targetColumn)
                && IsEmpty(state, targetRow, targetColumn))
            {
                yield return new MoveDefinition(
                    piece.PieceId,
                    piece.Side,
                    piece.Row,
                    piece.Column,
                    targetRow,
                    targetColumn,
                    WasCapture: false,
                    UsedBackwardMove: usedBackwardMove,
                    CapturedPieceId: null);
            }
        }
    }

    private IEnumerable<MoveDefinition> GetCaptureMoves(GameState state, GamePieceState piece, int forwardDirection)
    {
        foreach (var actualColumnDelta in new[] { -2, 2 })
        {
            if (!TryTranslateActual(piece.Row, piece.Column, forwardDirection * 2, actualColumnDelta, out var targetRow, out var targetColumn))
            {
                continue;
            }

            if (!IsEmpty(state, targetRow, targetColumn))
            {
                continue;
            }

            if (!TryTranslateActual(piece.Row, piece.Column, forwardDirection, actualColumnDelta / 2, out var jumpedRow, out var jumpedColumn))
            {
                continue;
            }

            var jumpedPiece = state.Pieces.FirstOrDefault(candidate =>
                !candidate.IsCaptured
                && candidate.Row == jumpedRow
                && candidate.Column == jumpedColumn
                && candidate.Side != piece.Side);

            if (jumpedPiece is null)
            {
                continue;
            }

            yield return new MoveDefinition(
                piece.PieceId,
                piece.Side,
                piece.Row,
                piece.Column,
                targetRow,
                targetColumn,
                WasCapture: true,
                UsedBackwardMove: false,
                CapturedPieceId: jumpedPiece.PieceId);
        }
    }

    private static bool IsEmpty(GameState state, int row, int column)
        => state.Pieces.All(piece => piece.IsCaptured || piece.Row != row || piece.Column != column);

    private static bool TryTranslateActual(int row, int column, int rowDelta, int actualColumnDelta, out int targetRow, out int targetColumn)
    {
        targetRow = row + rowDelta;
        targetColumn = -1;

        if (targetRow is < 0 or >= BoardCoordinateHelper.RowCount)
        {
            return false;
        }

        var actualColumn = BoardCoordinateHelper.ToActualColumn(row, column) + actualColumnDelta;
        return BoardCoordinateHelper.TryToCompressedColumn(targetRow, actualColumn, out targetColumn);
    }
}
