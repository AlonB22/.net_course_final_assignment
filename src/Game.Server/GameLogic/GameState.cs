using Game.Contracts.Enums;

namespace Game.Server.GameLogic;

public sealed class GameState
{
    public required List<GamePieceState> Pieces { get; init; }

    public PlayerSide CurrentTurn { get; set; }

    public SessionStatus Status { get; set; }

    public GameOutcome Outcome { get; set; }
}
