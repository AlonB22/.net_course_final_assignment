namespace Game.Client.Configuration;

public sealed class ClientDatabaseOptions
{
    public const string SectionName = "ReplayDatabase";

    public string ConnectionString { get; set; }
        = "Server=(localdb)\\MSSQLLocalDB;Database=DotNetFinalAssignment_ClientReplay;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
}
