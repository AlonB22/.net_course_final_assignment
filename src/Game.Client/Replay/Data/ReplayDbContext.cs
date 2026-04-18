using Game.Client.Replay.Models;
using Game.Contracts.Models;
using Microsoft.EntityFrameworkCore;

namespace Game.Client.Replay.Data;

public sealed class ReplayDbContext(DbContextOptions<ReplayDbContext> options) : DbContext(options)
{
    public DbSet<ReplayGameEntity> ReplayGames => Set<ReplayGameEntity>();

    public DbSet<ReplayTurnEntity> ReplayTurns => Set<ReplayTurnEntity>();

    public DbSet<ReplayStrokeEntity> ReplayStrokes => Set<ReplayStrokeEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ReplayGameEntity>(entity =>
        {
            entity.HasKey(game => game.ReplayGameId);
            entity.HasIndex(game => game.SessionId).IsUnique();
            entity.Property(game => game.InitialDetailsJson).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(game => game.LatestSummaryJson).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(game => game.LatestBoardJson).IsRequired().HasColumnType("nvarchar(max)");
            entity.HasMany(game => game.Turns)
                .WithOne(turn => turn.ReplayGame)
                .HasForeignKey(turn => turn.ReplayGameId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(game => game.Strokes)
                .WithOne(stroke => stroke.ReplayGame)
                .HasForeignKey(stroke => stroke.ReplayGameId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReplayTurnEntity>(entity =>
        {
            entity.HasKey(turn => turn.ReplayTurnEntityId);
            entity.HasIndex(turn => new { turn.ReplayGameId, turn.TurnIndex }).IsUnique();
            entity.Property(turn => turn.ResolutionJson).IsRequired().HasColumnType("nvarchar(max)");
        });

        modelBuilder.Entity<ReplayStrokeEntity>(entity =>
        {
            entity.HasKey(stroke => stroke.ReplayStrokeEntityId);
            entity.HasIndex(stroke => new { stroke.ReplayGameId, stroke.TurnIndex, stroke.StrokeIndex }).IsUnique();
            entity.Property(stroke => stroke.StrokeJson).IsRequired().HasColumnType("nvarchar(max)");
        });
    }
}
