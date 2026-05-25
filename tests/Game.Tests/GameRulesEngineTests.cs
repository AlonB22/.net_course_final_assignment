using Game.Contracts.Enums;
using Game.Server.GameLogic;

namespace Game.Tests;

public sealed class GameRulesEngineTests
{
    [Fact]
    public void TryFindHumanMove_AllowsSingleForwardCapture()
    {
        var humanPieceId = Guid.NewGuid();
        var serverPieceId = Guid.NewGuid();
        var state = new GameState
        {
            CurrentTurn = PlayerSide.Human,
            Status = SessionStatus.InProgress,
            Outcome = GameOutcome.None,
            Pieces =
            [
                new GamePieceState { PieceId = humanPieceId, Side = PlayerSide.Human, Row = 5, Column = 1 },
                new GamePieceState { PieceId = serverPieceId, Side = PlayerSide.Server, Row = 4, Column = 1 }
            ]
        };

        var engine = new GameRulesEngine();

        var move = engine.TryFindHumanMove(state, fromRow: 5, fromColumn: 1, toRow: 3, toColumn: 2);

        Assert.NotNull(move);
        Assert.True(move.WasCapture);
        Assert.Equal(serverPieceId, move.CapturedPieceId);

        engine.ApplyMove(state, move);

        Assert.True(state.Pieces.Single(piece => piece.PieceId == serverPieceId).IsCaptured);
        Assert.Equal(3, state.Pieces.Single(piece => piece.PieceId == humanPieceId).Row);
        Assert.Equal(2, state.Pieces.Single(piece => piece.PieceId == humanPieceId).Column);
    }

    [Fact]
    public void GetLegalMoves_RemovesBackwardOptionAfterPieceUsesIt()
    {
        var humanPieceId = Guid.NewGuid();
        var state = new GameState
        {
            CurrentTurn = PlayerSide.Human,
            Status = SessionStatus.InProgress,
            Outcome = GameOutcome.None,
            Pieces =
            [
                new GamePieceState { PieceId = humanPieceId, Side = PlayerSide.Human, Row = 5, Column = 1 }
            ]
        };

        var engine = new GameRulesEngine();
        var backwardMove = engine.GetLegalMoves(state, PlayerSide.Human)
            .Single(move => move.UsedBackwardMove && move.ToRow == 6 && move.ToColumn == 0);

        engine.ApplyMove(state, backwardMove);

        var remainingMoves = engine.GetLegalMoves(state, PlayerSide.Human);

        Assert.True(state.Pieces.Single().HasUsedBackwardMove);
        Assert.DoesNotContain(remainingMoves, move => move.UsedBackwardMove);
    }
}
