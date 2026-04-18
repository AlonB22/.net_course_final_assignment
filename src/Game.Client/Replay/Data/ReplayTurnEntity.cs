namespace Game.Client.Replay.Data;

public sealed class ReplayTurnEntity
{
    public int ReplayTurnEntityId { get; set; }

    public int ReplayGameId { get; set; }

    public int TurnIndex { get; set; }

    public string ResolutionJson { get; set; } = string.Empty;

    public DateTime RecordedAtUtc { get; set; }

    public ReplayGameEntity ReplayGame { get; set; } = null!;
}
