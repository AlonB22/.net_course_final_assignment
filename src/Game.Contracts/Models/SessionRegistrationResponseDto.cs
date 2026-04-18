namespace Game.Contracts.Models;

public sealed record SessionRegistrationResponseDto(
    Guid SessionId,
    IReadOnlyList<RegisteredPlayerDto> Players,
    BoardSnapshotDto InitialBoard,
    int MoveTimeLimitSeconds);
