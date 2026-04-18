using System.Net.Http.Json;
using Game.Contracts.Enums;
using Game.Contracts.Models;

namespace Game.Client.Services;

public sealed class GameServerApiClient(HttpClient httpClient) : IGameServerApiClient
{
    public async Task<GameSessionDetailsDto> GetSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/sessions/{sessionId}", cancellationToken);
        return await ReadOrThrowAsync<GameSessionDetailsDto>(response, cancellationToken);
    }

    public async Task<SessionRegistrationResponseDto> StartSessionAsync(
        SessionRegistrationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/sessions/start", request, cancellationToken);
        return await ReadOrThrowAsync<SessionRegistrationResponseDto>(response, cancellationToken);
    }

    public async Task<TurnResolutionDto> SubmitMoveAsync(
        MoveSubmissionDto request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync($"api/sessions/{request.SessionId}/human-move", request, cancellationToken);
        return await ReadOrThrowAsync<TurnResolutionDto>(response, cancellationToken);
    }

    private static async Task<T> ReadOrThrowAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
        where T : class
    {
        if (response.IsSuccessStatusCode)
        {
            var payload = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
            if (payload is not null)
            {
                return payload;
            }

            throw new HttpRequestException("The server returned an empty payload.");
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var message = string.IsNullOrWhiteSpace(body)
            ? $"Server request failed with status {(int)response.StatusCode} ({response.StatusCode})."
            : body.Trim();

        throw new HttpRequestException(message, null, response.StatusCode);
    }
}
