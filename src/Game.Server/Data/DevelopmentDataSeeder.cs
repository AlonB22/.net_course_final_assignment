using Game.Contracts.Enums;
using Game.Server.GameLogic;
using Game.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Game.Server.Data;

public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(GameDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var alreadySeeded = await dbContext.Players
            .AsNoTracking()
            .AnyAsync(player => player.ExternalId >= 800, cancellationToken);

        if (alreadySeeded)
        {
            return;
        }

        var baseTime = DateTime.UtcNow.Date.AddDays(-14);

        var avi = CreatePlayer(801, "Avi", "0501111111", 1);
        var aviVariant = CreatePlayer(802, "avI", "0502222222", 2);
        var dana = CreatePlayer(803, "Dana", "0503333333", 3);
        var noam = CreatePlayer(804, "Noam", "0504444444", 4);
        var maya = CreatePlayer(805, "Maya", "0505555555", 1);
        var lior = CreatePlayer(806, "Lior", "0506666666", 5);
        var omer = CreatePlayer(807, "Omer", "0507777777", 6);
        var nora = CreatePlayer(808, "Nora", "0508888888", 7);

        var sessions = new[]
        {
            CreateSession(baseTime.AddDays(1), 10, GameOutcome.ServerVictory, SessionStatus.Completed, avi, noam),
            CreateSession(baseTime.AddDays(4), 5, GameOutcome.HumanVictory, SessionStatus.Completed, aviVariant, dana),
            CreateSession(baseTime.AddDays(9), 15, GameOutcome.ServerVictory, SessionStatus.Completed, avi, maya, lior)
        };

        dbContext.Players.AddRange(avi, aviVariant, dana, noam, maya, lior, omer, nora);
        dbContext.GameSessions.AddRange(sessions);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Player CreatePlayer(int externalId, string firstName, string phoneNumber, int countryId)
    {
        return new Player
        {
            PlayerId = Guid.NewGuid(),
            ExternalId = externalId,
            FirstName = firstName,
            PhoneNumber = phoneNumber,
            CountryId = countryId
        };
    }

    private static GameSession CreateSession(
        DateTime startedAtUtc,
        int moveTimeLimitSeconds,
        GameOutcome outcome,
        SessionStatus status,
        params Player[] players)
    {
        var session = new GameSession
        {
            GameSessionId = Guid.NewGuid(),
            StartedAtUtc = startedAtUtc,
            CompletedAtUtc = startedAtUtc.AddMinutes(18),
            Status = status,
            Outcome = outcome,
            MoveTimeLimitSeconds = moveTimeLimitSeconds,
            ParticipantCount = players.Length,
            StateJson = CreateCompletedStateJson(outcome)
        };

        session.Participants = players
            .Select((player, index) => new GameParticipant
            {
                GameSessionId = session.GameSessionId,
                GameSession = session,
                PlayerId = player.PlayerId,
                Player = player,
                TurnOrder = index + 1,
                JoinedAtUtc = startedAtUtc
            })
            .ToList();

        return session;
    }

    private static string CreateCompletedStateJson(GameOutcome outcome)
    {
        var state = GameStateFactory.CreateInitial();
        state.Status = SessionStatus.Completed;
        state.Outcome = outcome;
        return GameStateFactory.Serialize(state);
    }
}
