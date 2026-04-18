using Game.Client.Replay.Models;
using Game.Contracts.Models;

namespace Game.Client.Replay.Services;

public interface IReplayJournalService
{
    Task<int> EnsureSessionAsync(GameSessionDetailsDto details, CancellationToken cancellationToken = default);

    Task RecordTurnAsync(
        GameSessionDetailsDto sessionDetails,
        int turnIndex,
        TurnResolutionDto resolution,
        IReadOnlyList<AnnotationStrokeSnapshot> strokes,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReplayGameListItem>> GetGameSummariesAsync(CancellationToken cancellationToken = default);

    Task<ReplayPlaybackData?> GetPlaybackAsync(int replayGameId, CancellationToken cancellationToken = default);
}
