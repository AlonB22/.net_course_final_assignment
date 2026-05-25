using Game.Client.Replay.Data;
using Game.Server.Data;
using Microsoft.EntityFrameworkCore;

const string ServerConnectionString =
    "Server=(localdb)\\MSSQLLocalDB;Database=DotNetFinalAssignment_GameServer;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

const string ReplayConnectionString =
    "Server=(localdb)\\MSSQLLocalDB;Database=DotNetFinalAssignment_ClientReplay;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

var serverOptions = new DbContextOptionsBuilder<GameDbContext>()
    .UseSqlServer(ServerConnectionString)
    .Options;

await using (var serverContext = new GameDbContext(serverOptions))
{
    await serverContext.Database.EnsureCreatedAsync();
    await DevelopmentDataSeeder.SeedAsync(serverContext);
}

var replayOptions = new DbContextOptionsBuilder<ReplayDbContext>()
    .UseSqlServer(ReplayConnectionString)
    .Options;

await using (var replayContext = new ReplayDbContext(replayOptions))
{
    await replayContext.Database.EnsureCreatedAsync();
}

Console.WriteLine("Server and replay LocalDB databases are ready for DACPAC export.");
