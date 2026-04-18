namespace Game.Client.Replay.Data;

public sealed class ReplayStrokeEntity
{
    public int ReplayStrokeEntityId { get; set; }

    public int ReplayGameId { get; set; }

    public int TurnIndex { get; set; }

    public int StrokeIndex { get; set; }

    public string StrokeJson { get; set; } = string.Empty;

    public DateTime RecordedAtUtc { get; set; }

    public ReplayGameEntity ReplayGame { get; set; } = null!;
}
