using Game.Contracts.Enums;

namespace Game.Contracts.Models;

public sealed record SessionSummaryDto(
    Guid SessionId,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    SessionStatus Status,
    GameOutcome Outcome,
    int MoveTimeLimitSeconds);
