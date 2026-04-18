using Game.Contracts.Models;

namespace Game.Client.Services;

public interface IGameServerApiClient
{
    Task<GameSessionDetailsDto> GetSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<SessionRegistrationResponseDto> StartSessionAsync(
        SessionRegistrationRequestDto request,
        CancellationToken cancellationToken = default);

    Task<TurnResolutionDto> SubmitMoveAsync(
        MoveSubmissionDto request,
        CancellationToken cancellationToken = default);
}
