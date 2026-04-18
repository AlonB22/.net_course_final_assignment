using Game.Contracts.Enums;
using Game.Contracts.Models;
using Game.Server.Data;
using Game.Server.GameLogic;
using Game.Server.Models;
using Game.Server.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Game.Server.Services;

public sealed class RegistrationService(
    GameDbContext dbContext,
    IGamePersistenceService gamePersistenceService,
    IOptions<GameplayOptions> gameplayOptions) : IRegistrationService
{
    public async Task<SessionRegistrationResponseDto> RegisterSessionAsync(
        SessionRegistrationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request, gameplayOptions.Value);

        var participantCountries = request.Participants
            .Select(participant => participant.CountryId)
            .Distinct()
            .ToArray();

        var countries = await dbContext.Countries
            .AsNoTracking()
            .Where(country => participantCountries.Contains(country.CountryId))
            .ToDictionaryAsync(country => country.CountryId, cancellationToken);

        if (countries.Count != participantCountries.Length)
        {
            throw new InvalidOperationException("One or more selected countries do not exist.");
        }

        var externalIds = request.Participants
            .Select(participant => participant.ExternalId)
            .ToArray();

        var existingExternalId = await dbContext.Players
            .AsNoTracking()
            .Where(player => externalIds.Contains(player.ExternalId))
            .Select(player => player.ExternalId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingExternalId is not 0)
        {
            throw new InvalidOperationException($"Player ID {existingExternalId} already exists.");
        }

        var now = DateTime.UtcNow;
        var initialState = GameStateFactory.CreateInitial();
        var session = new GameSession
        {
            GameSessionId = Guid.NewGuid(),
            StartedAtUtc = now,
            Status = SessionStatus.InProgress,
            Outcome = GameOutcome.None,
            MoveTimeLimitSeconds = request.MoveTimeLimitSeconds,
            ParticipantCount = request.Participants.Count,
            StateJson = GameStateFactory.Serialize(initialState)
        };

        session.Participants = request.Participants
            .Select((participant, index) =>
            {
                var player = new Player
                {
                    PlayerId = Guid.NewGuid(),
                    ExternalId = participant.ExternalId,
                    FirstName = participant.FirstName.Trim(),
                    PhoneNumber = participant.PhoneNumber,
                    CountryId = participant.CountryId
                };

                return new GameParticipant
                {
                    GameSessionId = session.GameSessionId,
                    GameSession = session,
                    Player = player,
                    TurnOrder = index + 1,
                    JoinedAtUtc = now
                };
            })
            .ToList();

        await gamePersistenceService.CreateSessionAsync(session, cancellationToken);

        return new SessionRegistrationResponseDto(
            SessionId: session.GameSessionId,
            Players: session.Participants
                .OrderBy(participant => participant.TurnOrder)
                .Select(participant =>
                {
                    var player = participant.Player!;
                    var country = countries[player.CountryId];
                    return new RegisteredPlayerDto(
                        player.PlayerId,
                        player.FirstName,
                        player.ExternalId,
                        player.PhoneNumber,
                        country.Name);
                })
                .ToArray(),
            InitialBoard: new BoardSnapshotDto(
                initialState.Pieces
                    .OrderBy(piece => piece.Side)
                    .ThenBy(piece => piece.Row)
                    .ThenBy(piece => piece.Column)
                    .Select(piece => new BoardPieceDto(
                        piece.PieceId,
                        piece.Side,
                        piece.Row,
                        piece.Column,
                        piece.HasUsedBackwardMove,
                        piece.IsCaptured))
                    .ToArray(),
                initialState.CurrentTurn,
                initialState.Status,
                initialState.Outcome),
            MoveTimeLimitSeconds: request.MoveTimeLimitSeconds);
    }

    private static void ValidateRequest(SessionRegistrationRequestDto request, GameplayOptions gameplayOptions)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Participants.Count is < 1 or > 10)
        {
            throw new ArgumentException("Participant count must be between 1 and 10.");
        }

        if (!gameplayOptions.AllowedMoveTimeLimitsSeconds.Contains(request.MoveTimeLimitSeconds))
        {
            throw new ArgumentException("Move time limit must be 2, 5, 10, or 15 seconds.");
        }

        var duplicateExternalId = request.Participants
            .GroupBy(participant => participant.ExternalId)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateExternalId is not null)
        {
            throw new ArgumentException($"Player ID {duplicateExternalId.Key} appears more than once in the registration form.");
        }

        foreach (var participant in request.Participants)
        {
            if (participant.ExternalId is < 1 or > 1000)
            {
                throw new ArgumentException("Player ID must be between 1 and 1000.");
            }

            if (string.IsNullOrWhiteSpace(participant.FirstName) || participant.FirstName.Trim().Length < 2)
            {
                throw new ArgumentException("First name must contain at least 2 letters.");
            }

            if (string.IsNullOrWhiteSpace(participant.PhoneNumber)
                || participant.PhoneNumber.Length != 10
                || participant.PhoneNumber.Any(character => !char.IsDigit(character)))
            {
                throw new ArgumentException("Phone number must contain exactly 10 digits.");
            }
        }
    }
}
