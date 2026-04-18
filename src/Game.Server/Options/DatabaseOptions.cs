namespace Game.Server.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string ConnectionStringName { get; set; } = "GameServer";
    public bool EnableSensitiveDataLogging { get; set; }
}
