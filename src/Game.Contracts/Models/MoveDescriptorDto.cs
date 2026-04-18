using Game.Contracts.Enums;

namespace Game.Contracts.Models;

public sealed record MoveDescriptorDto(
    Guid PieceId,
    PlayerSide Side,
    int FromRow,
    int FromColumn,
    int ToRow,
    int ToColumn,
    bool WasCapture,
    bool UsedBackwardMove);
