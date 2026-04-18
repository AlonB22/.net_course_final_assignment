using Game.Contracts.Enums;

namespace Game.Contracts.Models;

public sealed record BoardPieceDto(
    Guid PieceId,
    PlayerSide Side,
    int Row,
    int Column,
    bool HasUsedBackwardMove,
    bool IsCaptured = false);
