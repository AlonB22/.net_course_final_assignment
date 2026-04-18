namespace Game.Contracts.Models;

public sealed record GameSessionDetailsDto(
    SessionSummaryDto Session,
    IReadOnlyList<RegisteredPlayerDto> Players,
    BoardSnapshotDto Board,
    Guid PrimaryHumanPlayerId);
