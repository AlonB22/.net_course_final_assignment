using Game.Contracts.Enums;

namespace Game.Contracts.Models;

public sealed record TurnResolutionDto(
    MoveResultCode ResultCode,
    string Message,
    BoardSnapshotDto Board,
    SessionSummaryDto Session,
    MoveDescriptorDto? HumanMove,
    MoveDescriptorDto? ServerMove);
