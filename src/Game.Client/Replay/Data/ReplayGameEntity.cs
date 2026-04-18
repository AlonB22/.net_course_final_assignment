using Game.Contracts.Enums;

namespace Game.Client.Replay.Data;

public sealed class ReplayGameEntity
{
    public int ReplayGameId { get; set; }

    public Guid SessionId { get; set; }

    public string InitialDetailsJson { get; set; } = string.Empty;

    public string LatestSummaryJson { get; set; } = string.Empty;

    public string LatestBoardJson { get; set; } = string.Empty;

    public int LatestTurnIndex { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public SessionStatus Status { get; set; }

    public GameOutcome Outcome { get; set; }

    public ICollection<ReplayTurnEntity> Turns { get; set; } = new List<ReplayTurnEntity>();

    public ICollection<ReplayStrokeEntity> Strokes { get; set; } = new List<ReplayStrokeEntity>();
}
