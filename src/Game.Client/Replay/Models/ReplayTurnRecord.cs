using Game.Contracts.Models;

namespace Game.Client.Replay.Models;

public sealed record ReplayTurnRecord(
    int TurnIndex,
    TurnResolutionDto Resolution);
