namespace Game.Contracts.Models;

public sealed record MoveSubmissionDto(
    Guid SessionId,
    Guid HumanPlayerId,
    int FromRow,
    int FromColumn,
    int ToRow,
    int ToColumn,
    int RemainingSeconds);
