using Game.Server.Data;
using Game.Server.GameLogic;
using Game.Server.Options;
using Game.Server.Services;
using Game.Server.Services.Gameplay;
using Microsoft.EntityFrameworkCore;

namespace Game.Server.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGameServerCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<GameplayOptions>(configuration.GetSection(GameplayOptions.SectionName));

        var connectionString = configuration.GetConnectionString("GameServer")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=DotNetFinalAssignment_GameServer;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        services.AddDbContext<GameDbContext>(options =>
        {
            options.UseSqlServer(connectionString);

            var enableSensitiveDataLogging =
                configuration.GetSection(DatabaseOptions.SectionName).GetValue<bool>(nameof(DatabaseOptions.EnableSensitiveDataLogging));

            if (enableSensitiveDataLogging)
            {
                options.EnableSensitiveDataLogging();
            }
        });

        services.AddScoped<ICountryCatalogService, EfCountryCatalogService>();
        services.AddScoped<IGamePersistenceService, GamePersistenceService>();
        services.AddScoped<IQueryRetrievalService, QueryRetrievalService>();
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<IGameplayService, GameplayService>();
        services.AddSingleton<GameRulesEngine>();

        return services;
    }
}
