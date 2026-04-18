namespace Game.Server.Models;

public sealed class GameParticipant
{
    public Guid GameSessionId { get; set; }

    public GameSession? GameSession { get; set; }

    public Guid PlayerId { get; set; }

    public Player? Player { get; set; }

    public int TurnOrder { get; set; }

    public DateTime JoinedAtUtc { get; set; }
}
