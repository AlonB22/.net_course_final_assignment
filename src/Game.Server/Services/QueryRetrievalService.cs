using Game.Server.Data;
using Game.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Game.Server.Services;

public sealed class QueryRetrievalService(GameDbContext dbContext) : IQueryRetrievalService
{
    public async Task<IReadOnlyList<Country>> GetCountriesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Countries
            .AsNoTracking()
            .OrderBy(country => country.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Player>> GetPlayersAsync(
        bool includeGameParticipations = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Player> query = dbContext.Players.AsNoTracking().Include(player => player.Country);

        if (includeGameParticipations)
        {
            query = query
                .Include(player => player.GameParticipations)
                .ThenInclude(participation => participation.GameSession);
        }

        return await query
            .OrderBy(player => player.FirstName)
            .ThenBy(player => player.ExternalId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GameSession>> GetGameSessionsAsync(
        bool includeParticipants = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<GameSession> query = dbContext.GameSessions.AsNoTracking();

        if (includeParticipants)
        {
            query = query
                .Include(session => session.Participants)
                .ThenInclude(participant => participant.Player)
                .ThenInclude(player => player!.Country);
        }

        return await query
            .OrderByDescending(session => session.StartedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<Player?> GetPlayerAsync(
        Guid playerId,
        bool includeGameParticipations = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Player> query = dbContext.Players.AsNoTracking().Include(player => player.Country);

        if (includeGameParticipations)
        {
            query = query
                .Include(player => player.GameParticipations)
                .ThenInclude(participation => participation.GameSession);
        }

        return query.FirstOrDefaultAsync(player => player.PlayerId == playerId, cancellationToken);
    }

    public Task<GameSession?> GetSessionAsync(
        Guid sessionId,
        bool includeParticipants = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<GameSession> query = dbContext.GameSessions.AsNoTracking();

        if (includeParticipants)
        {
            query = query
                .Include(session => session.Participants)
                .ThenInclude(participant => participant.Player)
                .ThenInclude(player => player!.Country);
        }

        return query.FirstOrDefaultAsync(session => session.GameSessionId == sessionId, cancellationToken);
    }
}
