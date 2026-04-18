using Game.Contracts.Enums;
using Game.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Game.Server.Data;

public sealed class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public DbSet<Country> Countries => Set<Country>();

    public DbSet<Player> Players => Set<Player>();

    public DbSet<GameSession> GameSessions => Set<GameSession>();

    public DbSet<GameParticipant> GameParticipants => Set<GameParticipant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Country>(entity =>
        {
            entity.ToTable("Countries");
            entity.HasKey(country => country.CountryId);
            entity.Property(country => country.Name).HasMaxLength(100).IsRequired();
            entity.Property(country => country.IsoCode).HasMaxLength(2).IsRequired();
            entity.HasIndex(country => country.Name).IsUnique();
            entity.HasIndex(country => country.IsoCode).IsUnique();
            entity.HasData(CountrySeedData.Countries);
        });

        modelBuilder.Entity<Player>(entity =>
        {
            entity.ToTable("Players");
            entity.HasKey(player => player.PlayerId);
            entity.Property(player => player.PlayerId).ValueGeneratedNever();
            entity.Property(player => player.FirstName).HasMaxLength(60).IsRequired();
            entity.Property(player => player.PhoneNumber).HasMaxLength(10).IsRequired();
            entity.Property(player => player.ExternalId).IsRequired();
            entity.HasIndex(player => player.ExternalId).IsUnique();
            entity.HasIndex(player => player.CountryId);

            entity.HasOne(player => player.Country)
                .WithMany(country => country.Players)
                .HasForeignKey(player => player.CountryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Players_ExternalId_Range", "[ExternalId] BETWEEN 1 AND 1000");
                table.HasCheckConstraint("CK_Players_FirstName_Length", "LEN([FirstName]) >= 2");
                table.HasCheckConstraint(
                    "CK_Players_PhoneNumber_Format",
                    "LEN([PhoneNumber]) = 10 AND [PhoneNumber] NOT LIKE '%[^0-9]%'");
            });
        });

        modelBuilder.Entity<GameSession>(entity =>
        {
            entity.ToTable("GameSessions");
            entity.HasKey(session => session.GameSessionId);
            entity.Property(session => session.GameSessionId).ValueGeneratedNever();
            entity.Property(session => session.Status).HasConversion<int>();
            entity.Property(session => session.Outcome).HasConversion<int>();
            entity.Property(session => session.StartedAtUtc).IsRequired();
            entity.Property(session => session.MoveTimeLimitSeconds).IsRequired();
            entity.Property(session => session.ParticipantCount).IsRequired();
            entity.Property(session => session.StateJson).HasColumnType("nvarchar(max)").IsRequired();

            entity.HasMany(session => session.Participants)
                .WithOne(participant => participant.GameSession)
                .HasForeignKey(participant => participant.GameSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_GameSessions_MoveTimeLimitSeconds",
                    "[MoveTimeLimitSeconds] IN (2, 5, 10, 15)");
                table.HasCheckConstraint(
                    "CK_GameSessions_ParticipantCount",
                    "[ParticipantCount] BETWEEN 1 AND 10");
            });
        });

        modelBuilder.Entity<GameParticipant>(entity =>
        {
            entity.ToTable("GameParticipants");
            entity.HasKey(participant => new { participant.GameSessionId, participant.PlayerId });
            entity.Property(participant => participant.TurnOrder).IsRequired();
            entity.Property(participant => participant.JoinedAtUtc).IsRequired();
            entity.HasIndex(participant => new { participant.GameSessionId, participant.TurnOrder }).IsUnique();
            entity.HasIndex(participant => participant.PlayerId);

            entity.HasOne(participant => participant.Player)
                .WithMany(player => player.GameParticipations)
                .HasForeignKey(participant => participant.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
