namespace Game.Server.Options;

public sealed class GameplayOptions
{
    public const string SectionName = "Gameplay";

    public int DefaultMoveTimeLimitSeconds { get; set; } = 10;
    public int[] AllowedMoveTimeLimitsSeconds { get; set; } = [2, 5, 10, 15];
    public int BoardRows { get; set; } = 8;
    public int BoardColumns { get; set; } = 4;
}
