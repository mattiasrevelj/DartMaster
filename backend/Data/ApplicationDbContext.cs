using DartMaster.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DartMaster.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Tournament> Tournaments { get; set; } = null!;
    public DbSet<TournamentGroup> TournamentGroups { get; set; } = null!;
    public DbSet<TournamentParticipant> TournamentParticipants { get; set; } = null!;
    public DbSet<Match> Matches { get; set; } = null!;
    public DbSet<MatchParticipant> MatchParticipants { get; set; } = null!;
    public DbSet<DartThrow> DartThrows { get; set; } = null!;
    public DbSet<MatchConfirmation> MatchConfirmations { get; set; } = null!;
    public DbSet<PlayerStatistics> PlayerStatistics { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User Configuration
        modelBuilder.Entity<User>()
            .HasKey(u => u.Id);
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Tournament Configuration
        modelBuilder.Entity<Tournament>()
            .HasKey(t => t.Id);
        modelBuilder.Entity<Tournament>()
            .HasOne(t => t.Admin)
            .WithMany(u => u.AdminTournaments)
            .HasForeignKey(t => t.AdminId)
            .OnDelete(DeleteBehavior.Cascade);

        // TournamentGroup Configuration
        modelBuilder.Entity<TournamentGroup>()
            .HasKey(g => g.Id);
        modelBuilder.Entity<TournamentGroup>()
            .HasOne(g => g.Tournament)
            .WithMany(t => t.Groups)
            .HasForeignKey(g => g.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        // TournamentParticipant Configuration
        modelBuilder.Entity<TournamentParticipant>()
            .HasKey(tp => tp.Id);
        modelBuilder.Entity<TournamentParticipant>()
            .HasOne(tp => tp.Tournament)
            .WithMany(t => t.Participants)
            .HasForeignKey(tp => tp.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TournamentParticipant>()
            .HasOne(tp => tp.User)
            .WithMany(u => u.Participants)
            .HasForeignKey(tp => tp.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TournamentParticipant>()
            .HasOne(tp => tp.Group)
            .WithMany(g => g.Participants)
            .HasForeignKey(tp => tp.GroupId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<TournamentParticipant>()
            .HasIndex(tp => new { tp.TournamentId, tp.UserId })
            .IsUnique();

        // Match Configuration
        modelBuilder.Entity<Match>()
            .HasKey(m => m.Id);
        modelBuilder.Entity<Match>()
            .HasOne(m => m.Tournament)
            .WithMany(t => t.Matches)
            .HasForeignKey(m => m.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Match>()
            .HasOne(m => m.Group)
            .WithMany(g => g.Matches)
            .HasForeignKey(m => m.GroupId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Match>()
            .HasOne(m => m.ReportingPlayer)
            .WithMany()
            .HasForeignKey(m => m.ReportingPlayerId)
            .OnDelete(DeleteBehavior.SetNull);

        // MatchParticipant Configuration
        modelBuilder.Entity<MatchParticipant>()
            .HasKey(mp => mp.Id);
        modelBuilder.Entity<MatchParticipant>()
            .HasOne(mp => mp.Match)
            .WithMany(m => m.Participants)
            .HasForeignKey(mp => mp.MatchId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MatchParticipant>()
            .HasOne(mp => mp.User)
            .WithMany(u => u.MatchParticipants)
            .HasForeignKey(mp => mp.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MatchParticipant>()
            .HasIndex(mp => new { mp.MatchId, mp.UserId })
            .IsUnique();

        // DartThrow Configuration
        modelBuilder.Entity<DartThrow>()
            .HasKey(dt => dt.Id);
        modelBuilder.Entity<DartThrow>()
            .HasOne(dt => dt.Match)
            .WithMany(m => m.DartThrows)
            .HasForeignKey(dt => dt.MatchId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<DartThrow>()
            .HasOne(dt => dt.User)
            .WithMany(u => u.DartThrows)
            .HasForeignKey(dt => dt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<DartThrow>()
            .HasIndex(dt => new { dt.MatchId, dt.RoundNumber });

        // MatchConfirmation Configuration
        modelBuilder.Entity<MatchConfirmation>()
            .HasKey(mc => mc.Id);
        modelBuilder.Entity<MatchConfirmation>()
            .HasOne(mc => mc.Match)
            .WithMany(m => m.Confirmations)
            .HasForeignKey(mc => mc.MatchId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MatchConfirmation>()
            .HasOne(mc => mc.User)
            .WithMany(u => u.Confirmations)
            .HasForeignKey(mc => mc.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MatchConfirmation>()
            .HasIndex(mc => new { mc.MatchId, mc.UserId })
            .IsUnique();

        // PlayerStatistics Configuration
        modelBuilder.Entity<PlayerStatistics>()
            .HasKey(ps => ps.Id);
        modelBuilder.Entity<PlayerStatistics>()
            .HasOne(ps => ps.Tournament)
            .WithMany(t => t.Statistics)
            .HasForeignKey(ps => ps.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PlayerStatistics>()
            .HasOne(ps => ps.User)
            .WithMany(u => u.Statistics)
            .HasForeignKey(ps => ps.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PlayerStatistics>()
            .HasIndex(ps => new { ps.TournamentId, ps.UserId })
            .IsUnique();
        modelBuilder.Entity<PlayerStatistics>()
            .HasIndex(ps => ps.Ranking);

        // RefreshToken Configuration
        modelBuilder.Entity<RefreshToken>()
            .HasKey(rt => rt.Id);
        modelBuilder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.ExpiresAt);
    }
}
