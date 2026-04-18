namespace Game.Client.Configuration;

public sealed class ServerApiOptions
{
    public const string SectionName = "ServerApi";

    public string BaseUrl { get; set; } = "https://localhost:5001/";
}
