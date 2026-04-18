using Game.Client.Configuration;
using Game.Client.Replay.Data;
using Game.Client.Replay.Services;
using Game.Client.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Game.Client;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        using var host = CreateHostBuilder().Build();
        using var scope = host.Services.CreateScope();
        var mainForm = scope.ServiceProvider.GetRequiredService<Form1>();

        Application.Run(mainForm);
    }

    private static IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.Sources.Clear();
                configuration
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                    .AddEnvironmentVariables(prefix: "GAMECLIENT_");
            })
            .ConfigureServices((context, services) =>
            {
                services.Configure<ServerApiOptions>(context.Configuration.GetSection(ServerApiOptions.SectionName));
                services.Configure<ClientDatabaseOptions>(context.Configuration.GetSection(ClientDatabaseOptions.SectionName));

                var apiOptions = context.Configuration.GetSection(ServerApiOptions.SectionName).Get<ServerApiOptions>() ?? new ServerApiOptions();
                var databaseOptions = context.Configuration.GetSection(ClientDatabaseOptions.SectionName).Get<ClientDatabaseOptions>() ?? new ClientDatabaseOptions();

                services.AddHttpClient<IGameServerApiClient, GameServerApiClient>(client =>
                {
                    client.BaseAddress = new Uri(apiOptions.BaseUrl);
                });

                services.AddDbContextFactory<ReplayDbContext>(builder =>
                {
                    builder.UseSqlServer(databaseOptions.ConnectionString);
                });
                services.AddSingleton<IReplayJournalService, EfReplayJournalService>();
                services.AddTransient<Form1>();
            });
    }
}
