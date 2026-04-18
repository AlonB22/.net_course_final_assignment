using Game.Contracts.Models;

namespace Game.Client.Replay.Models;

public sealed record ReplayPlaybackData(
    ReplayGameListItem Game,
    GameSessionDetailsDto InitialDetails,
    IReadOnlyList<ReplayTurnRecord> Turns,
    IReadOnlyList<ReplayStrokeRecord> Strokes);
