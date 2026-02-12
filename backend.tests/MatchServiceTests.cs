using DartMaster.Api.Data;
using DartMaster.Api.Models;
using DartMaster.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MatchModel = DartMaster.Api.Models.Match;

namespace DartMaster.Tests;

public class MatchServiceTests
{
    private ApplicationDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateMatchAsync_WithValidData_CreatesMatch()
    {
        // Arrange
        var db = GetDbContext();
        var logger = new Mock<ILogger<MatchService>>().Object;
        var service = new MatchService(db, logger);
        
        var admin = new User { Id = "admin1", Username = "admin", Email = "admin@test.com", PasswordHash = "hash", FullName = "Admin" };
        db.Users.Add(admin);
        
        var tournament = new Tournament
        {
            Id = "tour1",
            Name = "Tournament 1",
            AdminId = admin.Id,
            Status = "Active",
            StartDate = DateTime.UtcNow,
            MaxPlayers = 16
        };
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        var request = new CreateMatchRequest
        {
            TournamentId = tournament.Id,
            MatchFormat = "501"
        };

        // Act
        var result = await service.CreateMatchAsync(request, admin.Id);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(tournament.Id, result.Data.TournamentId);
        Assert.Equal("Scheduled", result.Data.Status);
    }

    [Fact]
    public async Task CreateMatchAsync_WithNonAdminUser_ReturnsFailed()
    {
        // Arrange
        var db = GetDbContext();
        var logger = new Mock<ILogger<MatchService>>().Object;
        var service = new MatchService(db, logger);
        
        var admin = new User { Id = "admin1", Username = "admin", Email = "admin@test.com", PasswordHash = "hash", FullName = "Admin" };
        var user = new User { Id = "user1", Username = "user", Email = "user@test.com", PasswordHash = "hash", FullName = "User" };
        db.Users.AddRange(admin, user);
        
        var tournament = new Tournament
        {
            Id = "tour1",
            Name = "Tournament 1",
            AdminId = admin.Id,
            Status = "Active",
            StartDate = DateTime.UtcNow,
            MaxPlayers = 16
        };
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        var request = new CreateMatchRequest { TournamentId = tournament.Id };

        // Act
        var result = await service.CreateMatchAsync(request, user.Id);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("admin", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddParticipantAsync_WithValidUser_AddsParticipant()
    {
        // Arrange
        var db = GetDbContext();
        var logger = new Mock<ILogger<MatchService>>().Object;
        var service = new MatchService(db, logger);
        
        var user = new User { Id = "user1", Username = "user", Email = "user@test.com", PasswordHash = "hash", FullName = "User" };
        var admin = new User { Id = "admin1", Username = "admin", Email = "admin@test.com", PasswordHash = "hash", FullName = "Admin" };
        db.Users.AddRange(user, admin);
        
        var tournament = new Tournament { Id = "tour1", Name = "Tournament 1", AdminId = admin.Id, Status = "Active", StartDate = DateTime.UtcNow, MaxPlayers = 16 };
        db.Tournaments.Add(tournament);
        
        var match = new MatchModel { Id = "match1", TournamentId = tournament.Id, Status = "Scheduled" };
        db.Matches.Add(match);
        await db.SaveChangesAsync();

        // Act
        var result = await service.AddParticipantAsync(match.Id, user.Id);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.Data.ParticipantsCount);
    }

    [Fact]
    public async Task AddParticipantAsync_WithDuplicateUser_ReturnsFailed()
    {
        // Arrange
        var db = GetDbContext();
        var logger = new Mock<ILogger<MatchService>>().Object;
        var service = new MatchService(db, logger);
        
        var user = new User { Id = "user1", Username = "user", Email = "user@test.com", PasswordHash = "hash", FullName = "User" };
        var admin = new User { Id = "admin1", Username = "admin", Email = "admin@test.com", PasswordHash = "hash", FullName = "Admin" };
        db.Users.AddRange(user, admin);
        
        var tournament = new Tournament { Id = "tour1", Name = "Tournament 1", AdminId = admin.Id, Status = "Active", StartDate = DateTime.UtcNow, MaxPlayers = 16 };
        db.Tournaments.Add(tournament);
        
        var match = new MatchModel { Id = "match1", TournamentId = tournament.Id, Status = "Scheduled" };
        db.Matches.Add(match);
        
        var participant = new MatchParticipant { Id = "part1", MatchId = match.Id, UserId = user.Id };
        db.MatchParticipants.Add(participant);
        await db.SaveChangesAsync();

        // Act
        var result = await service.AddParticipantAsync(match.Id, user.Id);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetTournamentMatchesAsync_ReturnsMatches()
    {
        // Arrange
        var db = GetDbContext();
        var logger = new Mock<ILogger<MatchService>>().Object;
        var service = new MatchService(db, logger);
        
        var admin = new User { Id = "admin1", Username = "admin", Email = "admin@test.com", PasswordHash = "hash", FullName = "Admin" };
        db.Users.Add(admin);
        
        var tournament = new Tournament { Id = "tour1", Name = "Tournament 1", AdminId = admin.Id, Status = "Active", StartDate = DateTime.UtcNow, MaxPlayers = 16 };
        db.Tournaments.Add(tournament);
        
        var matches = new List<MatchModel>
        {
            new MatchModel { Id = "match1", TournamentId = tournament.Id, Status = "Scheduled" },
            new MatchModel { Id = "match2", TournamentId = tournament.Id, Status = "Scheduled" }
        };
        db.Matches.AddRange(matches);
        await db.SaveChangesAsync();

        // Act
        var result = await service.GetTournamentMatchesAsync(tournament.Id);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Data.Count);
    }
}
