namespace Game.Client.Replay.Models;

public sealed record AnnotationStrokeSnapshot(
    int TurnIndex,
    int StrokeIndex,
    int StrokeColorArgb,
    float StrokeWidth,
    IReadOnlyList<AnnotationPointSnapshot> Points);
