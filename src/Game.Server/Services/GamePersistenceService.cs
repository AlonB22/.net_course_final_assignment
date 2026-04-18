using Game.Contracts.Enums;
using Game.Server.Data;
using Game.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Game.Server.Services;

public sealed class GamePersistenceService(GameDbContext dbContext) : IGamePersistenceService
{
    public async Task<GameSession> CreateSessionAsync(GameSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        await dbContext.GameSessions.AddAsync(session, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return session;
    }

    public async Task<Player> UpdatePlayerAsync(
        Guid playerId,
        string firstName,
        string phoneNumber,
        int countryId,
        CancellationToken cancellationToken = default)
    {
        var player = await dbContext.Players
            .FirstOrDefaultAsync(existingPlayer => existingPlayer.PlayerId == playerId, cancellationToken)
            ?? throw new KeyNotFoundException($"Player {playerId} was not found.");

        player.FirstName = firstName.Trim();
        player.PhoneNumber = phoneNumber;
        player.CountryId = countryId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return player;
    }

    public async Task DeletePlayerAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var sessionIds = await dbContext.GameParticipants
            .Where(participant => participant.PlayerId == playerId)
            .Select(participant => participant.GameSessionId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (sessionIds.Count > 0)
        {
            var sessionsToDelete = await dbContext.GameSessions
                .Where(session => sessionIds.Contains(session.GameSessionId))
                .ToListAsync(cancellationToken);

            dbContext.GameSessions.RemoveRange(sessionsToDelete);
        }

        var player = await dbContext.Players
            .FirstOrDefaultAsync(existingPlayer => existingPlayer.PlayerId == playerId, cancellationToken)
            ?? throw new KeyNotFoundException($"Player {playerId} was not found.");

        dbContext.Players.Remove(player);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await dbContext.GameSessions
            .FirstOrDefaultAsync(existingSession => existingSession.GameSessionId == sessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Game session {sessionId} was not found.");

        dbContext.GameSessions.Remove(session);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Player?> GetPlayerByIdAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        return dbContext.Players
            .Include(player => player.Country)
            .Include(player => player.GameParticipations)
            .ThenInclude(participation => participation.GameSession)
            .AsNoTracking()
            .FirstOrDefaultAsync(player => player.PlayerId == playerId, cancellationToken);
    }

    public Task<Player?> GetPlayerByExternalIdAsync(int externalId, CancellationToken cancellationToken = default)
    {
        return dbContext.Players
            .Include(player => player.Country)
            .Include(player => player.GameParticipations)
            .ThenInclude(participation => participation.GameSession)
            .AsNoTracking()
            .FirstOrDefaultAsync(player => player.ExternalId == externalId, cancellationToken);
    }

    public Task<GameSession?> GetSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return dbContext.GameSessions
            .Include(session => session.Participants)
            .ThenInclude(participant => participant.Player)
            .ThenInclude(player => player!.Country)
            .AsNoTracking()
            .FirstOrDefaultAsync(session => session.GameSessionId == sessionId, cancellationToken);
    }
}
