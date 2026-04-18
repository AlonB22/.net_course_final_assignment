using Game.Contracts.Models;

namespace Game.Server.Services;

public interface IRegistrationService
{
    Task<SessionRegistrationResponseDto> RegisterSessionAsync(
        SessionRegistrationRequestDto request,
        CancellationToken cancellationToken = default);
}
