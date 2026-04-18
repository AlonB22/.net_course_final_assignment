namespace Game.Client.Replay.Models;

public sealed record ReplayStrokeRecord(
    int TurnIndex,
    AnnotationStrokeSnapshot Stroke);
