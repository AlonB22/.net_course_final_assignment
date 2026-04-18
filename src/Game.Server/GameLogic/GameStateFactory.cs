using System.Text.Json;
using Game.Contracts.Enums;

namespace Game.Server.GameLogic;

public static class GameStateFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static GameState CreateInitial()
    {
        var pieces = Enumerable.Range(0, 3)
            .SelectMany(row => Enumerable.Range(0, BoardCoordinateHelper.ColumnCount)
                .Select(column => new GamePieceState
                {
                    PieceId = Guid.NewGuid(),
                    Side = PlayerSide.Server,
                    Row = row,
                    Column = column
                }))
            .Concat(
                Enumerable.Range(5, 3)
                    .SelectMany(row => Enumerable.Range(0, BoardCoordinateHelper.ColumnCount)
                        .Select(column => new GamePieceState
                        {
                            PieceId = Guid.NewGuid(),
                            Side = PlayerSide.Human,
                            Row = row,
                            Column = column
                        })))
            .ToList();

        return new GameState
        {
            Pieces = pieces,
            CurrentTurn = PlayerSide.Human,
            Status = SessionStatus.InProgress,
            Outcome = GameOutcome.None
        };
    }

    public static string Serialize(GameState state)
        => JsonSerializer.Serialize(state, JsonOptions);

    public static GameState Deserialize(string stateJson)
        => JsonSerializer.Deserialize<GameState>(stateJson, JsonOptions)
            ?? throw new InvalidOperationException("Session state could not be deserialized.");
}
