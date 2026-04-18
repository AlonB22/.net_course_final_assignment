using Game.Contracts.Enums;

namespace Game.Server.GameLogic;

public sealed record MoveDefinition(
    Guid PieceId,
    PlayerSide Side,
    int FromRow,
    int FromColumn,
    int ToRow,
    int ToColumn,
    bool WasCapture,
    bool UsedBackwardMove,
    Guid? CapturedPieceId);
