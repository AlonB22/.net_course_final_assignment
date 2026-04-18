using Game.Server.Models;

namespace Game.Server.Services;

public interface IGamePersistenceService
{
    Task<GameSession> CreateSessionAsync(GameSession session, CancellationToken cancellationToken = default);

    Task<Player> UpdatePlayerAsync(
        Guid playerId,
        string firstName,
        string phoneNumber,
        int countryId,
        CancellationToken cancellationToken = default);

    Task DeletePlayerAsync(Guid playerId, CancellationToken cancellationToken = default);

    Task DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<Player?> GetPlayerByIdAsync(Guid playerId, CancellationToken cancellationToken = default);

    Task<Player?> GetPlayerByExternalIdAsync(int externalId, CancellationToken cancellationToken = default);

    Task<GameSession?> GetSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
