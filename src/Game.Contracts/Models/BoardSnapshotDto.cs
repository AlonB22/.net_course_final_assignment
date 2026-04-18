using Game.Contracts.Enums;

namespace Game.Contracts.Models;

public sealed record BoardSnapshotDto(
    IReadOnlyList<BoardPieceDto> Pieces,
    PlayerSide CurrentTurn,
    SessionStatus Status,
    GameOutcome Outcome);
