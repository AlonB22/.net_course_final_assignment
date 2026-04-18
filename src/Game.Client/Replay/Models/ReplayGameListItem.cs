using Game.Contracts.Enums;

namespace Game.Client.Replay.Models;

public sealed record ReplayGameListItem(
    int ReplayGameId,
    Guid SessionId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    SessionStatus Status,
    GameOutcome Outcome,
    int TurnCount,
    string PlayerSummary)
{
    public override string ToString()
        => $"Session {SessionId:D} | {Status} | {Outcome} | turns {TurnCount}";
}
