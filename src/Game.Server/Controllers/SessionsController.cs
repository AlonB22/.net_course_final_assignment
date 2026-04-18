using Game.Contracts.Models;
using Game.Server.Services.Gameplay;
using Microsoft.AspNetCore.Mvc;

namespace Game.Server.Controllers;

[ApiController]
[Route("api/sessions")]
public sealed class SessionsController(IGameplayService gameplayService) : ControllerBase
{
    [HttpPost("start")]
    public async Task<ActionResult<GameSessionDetailsDto>> StartSessionAsync(
        [FromBody] SessionRegistrationRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await gameplayService.StartSessionAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{sessionId:guid}")]
    public async Task<ActionResult<GameSessionDetailsDto>> GetSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await gameplayService.GetSessionDetailsAsync(sessionId, cancellationToken);
            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{sessionId:guid}/human-move")]
    public async Task<ActionResult<TurnResolutionDto>> SubmitHumanMoveAsync(
        Guid sessionId,
        [FromBody] MoveSubmissionDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var normalizedRequest = request with { SessionId = sessionId };
            var response = await gameplayService.SubmitHumanMoveAsync(normalizedRequest, cancellationToken);
            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
