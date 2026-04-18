using Game.Contracts.Enums;

namespace Game.Server.Models;

public sealed class GameSession
{
    public Guid GameSessionId { get; set; }

    public DateTime StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public SessionStatus Status { get; set; }

    public GameOutcome Outcome { get; set; }

    public int MoveTimeLimitSeconds { get; set; }

    public int ParticipantCount { get; set; }

    public string StateJson { get; set; } = string.Empty;

    public ICollection<GameParticipant> Participants { get; set; } = [];
}
