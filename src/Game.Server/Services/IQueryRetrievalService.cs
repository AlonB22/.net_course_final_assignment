using Game.Server.Models;

namespace Game.Server.Services;

public interface IQueryRetrievalService
{
    Task<IReadOnlyList<Country>> GetCountriesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Player>> GetPlayersAsync(
        bool includeGameParticipations = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GameSession>> GetGameSessionsAsync(
        bool includeParticipants = false,
        CancellationToken cancellationToken = default);

    Task<Player?> GetPlayerAsync(
        Guid playerId,
        bool includeGameParticipations = false,
        CancellationToken cancellationToken = default);

    Task<GameSession?> GetSessionAsync(
        Guid sessionId,
        bool includeParticipants = false,
        CancellationToken cancellationToken = default);
}
