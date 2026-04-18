using System.Text.Json;
using Game.Client.Replay.Data;
using Game.Client.Replay.Models;
using Game.Contracts.Enums;
using Game.Contracts.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Game.Client.Replay.Services;

public sealed class EfReplayJournalService(
    IDbContextFactory<ReplayDbContext> dbContextFactory,
    ILogger<EfReplayJournalService> logger) : IReplayJournalService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<int> EnsureSessionAsync(GameSessionDetailsDto details, CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.EnsureCreatedAsync(cancellationToken);

        var sessionId = details.Session.SessionId;
        var entity = await context.ReplayGames
            .Include(game => game.Turns)
            .FirstOrDefaultAsync(game => game.SessionId == sessionId, cancellationToken);

        var initialDetailsJson = JsonSerializer.Serialize(details, JsonOptions);
        var latestSummaryJson = JsonSerializer.Serialize(details.Session, JsonOptions);
        var latestBoardJson = JsonSerializer.Serialize(details.Board, JsonOptions);
        var now = DateTime.UtcNow;

        if (entity is null)
        {
            entity = new ReplayGameEntity
            {
                SessionId = sessionId,
                InitialDetailsJson = initialDetailsJson,
                LatestSummaryJson = latestSummaryJson,
                LatestBoardJson = latestBoardJson,
                LatestTurnIndex = 0,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                Status = details.Session.Status,
                Outcome = details.Session.Outcome
            };

            context.ReplayGames.Add(entity);
        }
        else
        {
            entity.LatestSummaryJson = latestSummaryJson;
            entity.LatestBoardJson = latestBoardJson;
            entity.UpdatedAtUtc = now;
            entity.Status = details.Session.Status;
            entity.Outcome = details.Session.Outcome;
        }

        await context.SaveChangesAsync(cancellationToken);
        return entity.LatestTurnIndex;
    }

    public async Task RecordTurnAsync(
        GameSessionDetailsDto sessionDetails,
        int turnIndex,
        TurnResolutionDto resolution,
        IReadOnlyList<AnnotationStrokeSnapshot> strokes,
        CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.EnsureCreatedAsync(cancellationToken);

        var sessionId = sessionDetails.Session.SessionId;
        var entity = await context.ReplayGames
            .Include(game => game.Turns)
            .Include(game => game.Strokes)
            .FirstOrDefaultAsync(game => game.SessionId == sessionId, cancellationToken);

        if (entity is null)
        {
            entity = new ReplayGameEntity
            {
                SessionId = sessionId,
                InitialDetailsJson = JsonSerializer.Serialize(sessionDetails, JsonOptions),
                LatestSummaryJson = JsonSerializer.Serialize(sessionDetails.Session, JsonOptions),
                LatestBoardJson = JsonSerializer.Serialize(sessionDetails.Board, JsonOptions),
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                Status = sessionDetails.Session.Status,
                Outcome = sessionDetails.Session.Outcome
            };

            context.ReplayGames.Add(entity);
            await context.SaveChangesAsync(cancellationToken);
        }

        var turnEntity = entity.Turns.FirstOrDefault(turn => turn.TurnIndex == turnIndex);
        if (turnEntity is null)
        {
            turnEntity = new ReplayTurnEntity
            {
                ReplayGameId = entity.ReplayGameId,
                TurnIndex = turnIndex,
                ResolutionJson = JsonSerializer.Serialize(resolution, JsonOptions),
                RecordedAtUtc = DateTime.UtcNow
            };
            context.ReplayTurns.Add(turnEntity);
        }
        else
        {
            turnEntity.ResolutionJson = JsonSerializer.Serialize(resolution, JsonOptions);
            turnEntity.RecordedAtUtc = DateTime.UtcNow;
        }

        var existingStrokes = entity.Strokes.Where(stroke => stroke.TurnIndex == turnIndex).ToList();
        context.ReplayStrokes.RemoveRange(existingStrokes);

        for (var strokeIndex = 0; strokeIndex < strokes.Count; strokeIndex++)
        {
            var stroke = strokes[strokeIndex];
            context.ReplayStrokes.Add(new ReplayStrokeEntity
            {
                ReplayGameId = entity.ReplayGameId,
                TurnIndex = turnIndex,
                StrokeIndex = strokeIndex,
                StrokeJson = JsonSerializer.Serialize(stroke, JsonOptions),
                RecordedAtUtc = DateTime.UtcNow
            });
        }

        entity.LatestSummaryJson = JsonSerializer.Serialize(resolution.Session, JsonOptions);
        entity.LatestBoardJson = JsonSerializer.Serialize(resolution.Board, JsonOptions);
        entity.LatestTurnIndex = Math.Max(entity.LatestTurnIndex, turnIndex + 1);
        entity.Status = resolution.Session.Status;
        entity.Outcome = resolution.Session.Outcome;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReplayGameListItem>> GetGameSummariesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.EnsureCreatedAsync(cancellationToken);

        var games = await context.ReplayGames
            .AsNoTracking()
            .OrderByDescending(game => game.UpdatedAtUtc)
            .Select(game => new
            {
                game.ReplayGameId,
                game.SessionId,
                game.CreatedAtUtc,
                game.UpdatedAtUtc,
                game.Status,
                game.Outcome,
                game.LatestTurnIndex,
                game.InitialDetailsJson
            })
            .ToListAsync(cancellationToken);

        return games
            .Select(game =>
            {
                var details = JsonSerializer.Deserialize<GameSessionDetailsDto>(game.InitialDetailsJson, JsonOptions);
                var players = details?.Players.Select(player => $"{player.FirstName}#{player.ExternalId}").ToArray() ?? [];
                return new ReplayGameListItem(
                    game.ReplayGameId,
                    game.SessionId,
                    game.CreatedAtUtc,
                    game.UpdatedAtUtc,
                    game.Status,
                    game.Outcome,
                    game.LatestTurnIndex,
                    string.Join(", ", players));
            })
            .ToList();
    }

    public async Task<ReplayPlaybackData?> GetPlaybackAsync(int replayGameId, CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.EnsureCreatedAsync(cancellationToken);

        var entity = await context.ReplayGames
            .AsNoTracking()
            .FirstOrDefaultAsync(game => game.ReplayGameId == replayGameId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var turns = await context.ReplayTurns
            .AsNoTracking()
            .Where(turn => turn.ReplayGameId == replayGameId)
            .OrderBy(turn => turn.TurnIndex)
            .ToListAsync(cancellationToken);

        var strokes = await context.ReplayStrokes
            .AsNoTracking()
            .Where(stroke => stroke.ReplayGameId == replayGameId)
            .OrderBy(stroke => stroke.TurnIndex)
            .ThenBy(stroke => stroke.StrokeIndex)
            .ToListAsync(cancellationToken);

        var initialDetails = JsonSerializer.Deserialize<GameSessionDetailsDto>(entity.InitialDetailsJson, JsonOptions);
        if (initialDetails is null)
        {
            logger.LogWarning("Replay game {ReplayGameId} is missing initial details JSON.", replayGameId);
            return null;
        }

        var playbackTurns = turns
            .Select(turn => new ReplayTurnRecord(
                turn.TurnIndex,
                JsonSerializer.Deserialize<TurnResolutionDto>(turn.ResolutionJson, JsonOptions) ?? throw new InvalidOperationException("Replay turn JSON could not be parsed.")))
            .ToList();

        var playbackStrokes = strokes
            .Select(stroke => new ReplayStrokeRecord(
                stroke.TurnIndex,
                JsonSerializer.Deserialize<AnnotationStrokeSnapshot>(stroke.StrokeJson, JsonOptions) ?? throw new InvalidOperationException("Replay stroke JSON could not be parsed.")))
            .ToList();

        var summary = new ReplayGameListItem(
            entity.ReplayGameId,
            entity.SessionId,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc,
            entity.Status,
            entity.Outcome,
            entity.LatestTurnIndex,
            string.Join(", ", initialDetails.Players.Select(player => $"{player.FirstName}#{player.ExternalId}")));

        return new ReplayPlaybackData(summary, initialDetails, playbackTurns, playbackStrokes);
    }
}
