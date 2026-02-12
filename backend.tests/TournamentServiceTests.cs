using DartMaster.Api.Data;
using DartMaster.Api.Models;
using DartMaster.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DartMaster.Tests;

public class TournamentServiceTests
{
    private ApplicationDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }



    [Fact]
    public async Task CreateTournamentAsync_WithValidData_CreatesTournament()
    {
        // Arrange
        var db = GetDbContext();
        var logger = new Mock<ILogger<TournamentService>>().Object;
        var service = new TournamentService(db, logger);
        
        var adminUser = new User 
        { 
            Id = "admin1", 
            Username = "admin", 
            Email = "admin@test.com",
            PasswordHash = "hash",
            FullName = "Admin"
        };
        db.Users.Add(adminUser);
        await db.SaveChangesAsync();

        var request = new CreateTournamentRequest
        {
            Name = "Test Tournament",
            Description = "Test Description",
            StartDate = DateTime.UtcNow.AddDays(1),
            MaxPlayers = 16
        };

        // Act
        var result = await service.CreateTournamentAsync(request, adminUser.Id);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Test Tournament", result.Data.Name);
        Assert.Equal(16, result.Data.MaxPlayers);
        Assert.Equal("Draft", result.Data.Status);
    }

    [Fact]
    public async Task CreateTournamentAsync_WithPastStartDate_ReturnsFailed()
    {
        // Arrange
        var db = GetDbContext();
        var logger = new Mock<ILogger<TournamentService>>().Object;
        var service = new TournamentService(db, logger);
        
        var adminUser = new User { Id = "admin1", Username = "admin", Email = "admin@test.com", PasswordHash = "hash", FullName = "Admin" };
        db.Users.Add(adminUser);
        await db.SaveChangesAsync();

        var request = new CreateTournamentRequest
        {
            Name = "Test Tournament",
            StartDate = DateTime.UtcNow.AddDays(-1),
            MaxPlayers = 16
        };

        // Act
        var result = await service.CreateTournamentAsync(request, adminUser.Id);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("future", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAllTournamentsAsync_ReturnsList()
    {
        // Arrange
        var db = GetDbContext();
        var logger = new Mock<ILogger<TournamentService>>().Object;
        var service = new TournamentService(db, logger);
        
        var admin = new User { Id = "admin1", Username = "admin", Email = "admin@test.com", PasswordHash = "hash", FullName = "Admin" };
        db.Users.Add(admin);
        
        var tournament = new Tournament
        {
            Id = "tour1",
            Name = "Tournament 1",
            AdminId = admin.Id,
            StartDate = DateTime.UtcNow.AddDays(1),
            MaxPlayers = 16
        };
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        // Act
        var result = await service.GetAllTournamentsAsync();

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.Data);
        Assert.Single(result.Data);
        Assert.Equal("Tournament 1", result.Data.First().Name);
    }

    [Fact]
    public async Task UpdateTournamentAsync_WithAdminUser_UpdatesSuccessfully()
    {
        // Arrange
        var db = GetDbContext();
        var logger = new Mock<ILogger<TournamentService>>().Object;
        var service = new TournamentService(db, logger);
        
        var admin = new User { Id = "admin1", Username = "admin", Email = "admin@test.com", PasswordHash = "hash", FullName = "Admin" };
        db.Users.Add(admin);
        
        var tournament = new Tournament
        {
            Id = "tour1",
            Name = "Tournament 1",
            AdminId = admin.Id,
            Status = "Draft",
            StartDate = DateTime.UtcNow.AddDays(1),
            MaxPlayers = 16
        };
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        var updateRequest = new UpdateTournamentRequest
        {
            Name = "Updated Tournament",
            MaxPlayers = 32
        };

        // Act
        var result = await service.UpdateTournamentAsync(tournament.Id, updateRequest, admin.Id);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Updated Tournament", result.Data.Name);
        Assert.Equal(32, result.Data.MaxPlayers);
    }

    [Fact]
    public async Task DeleteTournamentAsync_WithNonAdminUser_ReturnsFailed()
    {
        // Arrange
        var db = GetDbContext();
        var logger = new Mock<ILogger<TournamentService>>().Object;
        var service = new TournamentService(db, logger);
        
        var admin = new User { Id = "admin1", Username = "admin", Email = "admin@test.com", PasswordHash = "hash", FullName = "Admin" };
        var otherUser = new User { Id = "user1", Username = "user", Email = "user@test.com", PasswordHash = "hash", FullName = "User" };
        db.Users.AddRange(admin, otherUser);
        
        var tournament = new Tournament
        {
            Id = "tour1",
            Name = "Tournament 1",
            AdminId = admin.Id,
            Status = "Draft",
            StartDate = DateTime.UtcNow.AddDays(1),
            MaxPlayers = 16
        };
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        // Act
        var result = await service.DeleteTournamentAsync(tournament.Id, otherUser.Id);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("admin", result.Message, StringComparison.OrdinalIgnoreCase);
    }
}
