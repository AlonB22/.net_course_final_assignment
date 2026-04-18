using Game.Contracts.Enums;

namespace Game.Server.GameLogic;

public sealed class GamePieceState
{
    public Guid PieceId { get; init; }

    public PlayerSide Side { get; init; }

    public int Row { get; set; }

    public int Column { get; set; }

    public bool HasUsedBackwardMove { get; set; }

    public bool IsCaptured { get; set; }
}
