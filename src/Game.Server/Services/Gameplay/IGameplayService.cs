using Game.Contracts.Models;

namespace Game.Server.Services.Gameplay;

public interface IGameplayService
{
    Task<GameSessionDetailsDto> StartSessionAsync(
        SessionRegistrationRequestDto request,
        CancellationToken cancellationToken = default);

    Task<GameSessionDetailsDto> GetSessionDetailsAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<TurnResolutionDto> SubmitHumanMoveAsync(
        MoveSubmissionDto request,
        CancellationToken cancellationToken = default);
}
