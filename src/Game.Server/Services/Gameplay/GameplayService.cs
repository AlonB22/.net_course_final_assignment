using Game.Contracts.Enums;
using Game.Contracts.Models;
using Game.Server.Data;
using Game.Server.GameLogic;
using Game.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Game.Server.Services.Gameplay;

public sealed class GameplayService(
    GameDbContext dbContext,
    IRegistrationService registrationService,
    GameRulesEngine rulesEngine) : IGameplayService
{
    public async Task<GameSessionDetailsDto> StartSessionAsync(
        SessionRegistrationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var registration = await registrationService.RegisterSessionAsync(request, cancellationToken);
        return await GetSessionDetailsAsync(registration.SessionId, cancellationToken);
    }

    public async Task<GameSessionDetailsDto> GetSessionDetailsAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await LoadSessionAsync(sessionId, cancellationToken);
        var state = GameStateFactory.Deserialize(session.StateJson);
        return ToSessionDetails(session, state);
    }

    public async Task<TurnResolutionDto> SubmitHumanMoveAsync(
        MoveSubmissionDto request,
        CancellationToken cancellationToken = default)
    {
        var session = await LoadSessionAsync(request.SessionId, cancellationToken);
        var state = GameStateFactory.Deserialize(session.StateJson);

        if (session.Status == SessionStatus.Completed || state.Status == SessionStatus.Completed)
        {
            return CreateResolution(
                MoveResultCode.SessionCompleted,
                "This session is already complete.",
                session,
                state,
                null,
                null);
        }

        if (!session.Participants.Any(participant => participant.PlayerId == request.HumanPlayerId))
        {
            return CreateResolution(
                MoveResultCode.NotYourTurn,
                "The submitted player is not registered for this session.",
                session,
                state,
                null,
                null);
        }

        if (request.RemainingSeconds <= 0)
        {
            session.Status = SessionStatus.Completed;
            session.CompletedAtUtc = DateTime.UtcNow;
            session.Outcome = GameOutcome.HumanTimeoutLoss;
            state.Status = SessionStatus.Completed;
            state.Outcome = GameOutcome.HumanTimeoutLoss;
            await SaveStateAsync(session, state, cancellationToken);

            return CreateResolution(
                MoveResultCode.TimedOut,
                "The human player ran out of time.",
                session,
                state,
                null,
                null);
        }

        if (state.CurrentTurn != PlayerSide.Human)
        {
            return CreateResolution(
                MoveResultCode.NotYourTurn,
                "It is not the human turn.",
                session,
                state,
                null,
                null);
        }

        var humanMove = rulesEngine.TryFindHumanMove(
            state,
            request.FromRow,
            request.FromColumn,
            request.ToRow,
            request.ToColumn);

        if (humanMove is null)
        {
            return CreateResolution(
                MoveResultCode.InvalidMove,
                "That move is not legal under the game rules.",
                session,
                state,
                null,
                null);
        }

        rulesEngine.ApplyMove(state, humanMove);

        if (rulesEngine.HasReachedBackRank(state, PlayerSide.Human))
        {
            CompleteSession(session, state, GameOutcome.HumanVictory);
            await SaveStateAsync(session, state, cancellationToken);

            return CreateResolution(
                MoveResultCode.Success,
                "Human victory by reaching the back rank.",
                session,
                state,
                humanMove,
                null);
        }

        var serverMoves = rulesEngine.GetLegalMoves(state, PlayerSide.Server);
        if (serverMoves.Count == 0)
        {
            CompleteSession(session, state, GameOutcome.HumanVictory);
            await SaveStateAsync(session, state, cancellationToken);

            return CreateResolution(
                MoveResultCode.Success,
                "Human victory by blocking the server.",
                session,
                state,
                humanMove,
                null);
        }

        var serverMove = serverMoves[0];
        rulesEngine.ApplyMove(state, serverMove);

        if (rulesEngine.HasReachedBackRank(state, PlayerSide.Server))
        {
            CompleteSession(session, state, GameOutcome.ServerVictory);
            await SaveStateAsync(session, state, cancellationToken);

            return CreateResolution(
                MoveResultCode.Success,
                "Server victory by reaching the back rank.",
                session,
                state,
                humanMove,
                serverMove);
        }

        var humanMoves = rulesEngine.GetLegalMoves(state, PlayerSide.Human);
        if (humanMoves.Count == 0)
        {
            CompleteSession(session, state, GameOutcome.ServerVictory);
            await SaveStateAsync(session, state, cancellationToken);

            return CreateResolution(
                MoveResultCode.Success,
                "Server victory by blocking the human side.",
                session,
                state,
                humanMove,
                serverMove);
        }

        session.Status = SessionStatus.InProgress;
        session.Outcome = GameOutcome.None;
        state.Status = SessionStatus.InProgress;
        state.Outcome = GameOutcome.None;
        await SaveStateAsync(session, state, cancellationToken);

        return CreateResolution(
            MoveResultCode.Success,
            "Turn completed successfully.",
            session,
            state,
            humanMove,
            serverMove);
    }

    private async Task<GameSession> LoadSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        return await dbContext.GameSessions
            .Include(session => session.Participants)
            .ThenInclude(participant => participant.Player)
            .ThenInclude(player => player!.Country)
            .FirstOrDefaultAsync(session => session.GameSessionId == sessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Game session {sessionId} was not found.");
    }

    private async Task SaveStateAsync(GameSession session, GameState state, CancellationToken cancellationToken)
    {
        session.StateJson = GameStateFactory.Serialize(state);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void CompleteSession(GameSession session, GameState state, GameOutcome outcome)
    {
        session.Status = SessionStatus.Completed;
        session.CompletedAtUtc = DateTime.UtcNow;
        session.Outcome = outcome;
        state.Status = SessionStatus.Completed;
        state.Outcome = outcome;
    }

    private static TurnResolutionDto CreateResolution(
        MoveResultCode resultCode,
        string message,
        GameSession session,
        GameState state,
        MoveDefinition? humanMove,
        MoveDefinition? serverMove)
    {
        return new TurnResolutionDto(
            resultCode,
            message,
            ToBoardSnapshot(state),
            new SessionSummaryDto(
                session.GameSessionId,
                session.StartedAtUtc,
                session.CompletedAtUtc,
                session.Status,
                session.Outcome,
                session.MoveTimeLimitSeconds),
            humanMove is null ? null : ToMoveDescriptor(humanMove),
            serverMove is null ? null : ToMoveDescriptor(serverMove));
    }

    private static GameSessionDetailsDto ToSessionDetails(GameSession session, GameState state)
    {
        var orderedParticipants = session.Participants
            .OrderBy(participant => participant.TurnOrder)
            .ToArray();

        return new GameSessionDetailsDto(
            new SessionSummaryDto(
                session.GameSessionId,
                session.StartedAtUtc,
                session.CompletedAtUtc,
                session.Status,
                session.Outcome,
                session.MoveTimeLimitSeconds),
            orderedParticipants
                .Select(participant => new RegisteredPlayerDto(
                    participant.PlayerId,
                    participant.Player!.FirstName,
                    participant.Player.ExternalId,
                    participant.Player.PhoneNumber,
                    participant.Player.Country!.Name))
                .ToArray(),
            ToBoardSnapshot(state),
            orderedParticipants.First().PlayerId);
    }

    private static BoardSnapshotDto ToBoardSnapshot(GameState state)
    {
        return new BoardSnapshotDto(
            state.Pieces
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
            state.CurrentTurn,
            state.Status,
            state.Outcome);
    }

    private static MoveDescriptorDto ToMoveDescriptor(MoveDefinition move)
    {
        return new MoveDescriptorDto(
            move.PieceId,
            move.Side,
            move.FromRow,
            move.FromColumn,
            move.ToRow,
            move.ToColumn,
            move.WasCapture,
            move.UsedBackwardMove);
    }
}
