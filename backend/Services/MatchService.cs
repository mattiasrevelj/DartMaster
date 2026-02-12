using DartMaster.Api.Data;
using DartMaster.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DartMaster.Api.Services;

public interface IMatchService
{
    Task<ApiResponse<List<MatchDto>>> GetTournamentMatchesAsync(string tournamentId);
    Task<ApiResponse<MatchDto>> GetMatchByIdAsync(string id);
    Task<ApiResponse<MatchDto>> CreateMatchAsync(CreateMatchRequest request, string userId);
    Task<ApiResponse<MatchDto>> UpdateMatchStatusAsync(string id, UpdateMatchStatusRequest request, string userId);
    Task<ApiResponse<MatchDto>> AddParticipantAsync(string matchId, string userId);
    Task<ApiResponse<bool>> DeleteMatchAsync(string id, string userId);
}

public class MatchService : IMatchService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<MatchService> _logger;

    public MatchService(ApplicationDbContext db, ILogger<MatchService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ApiResponse<List<MatchDto>>> GetTournamentMatchesAsync(string tournamentId)
    {
        try
        {
            var tournament = await _db.Tournaments.FindAsync(tournamentId);
            if (tournament is null)
                return ApiResponse<List<MatchDto>>.FailureResult("Tournament not found");

            var matches = await _db.Matches
                .Where(m => m.TournamentId == tournamentId)
                .Include(m => m.Participants)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new MatchDto
                {
                    Id = m.Id,
                    TournamentId = m.TournamentId,
                    Status = m.Status,
                    MatchFormat = m.MatchFormat,
                    ParticipantsCount = m.Participants.Count,
                    DartThrowsCount = _db.DartThrows.Count(d => d.MatchId == m.Id),
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt
                })
                .ToListAsync();

            return ApiResponse<List<MatchDto>>.SuccessResult(matches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tournament matches for {TournamentId}", tournamentId);
            return ApiResponse<List<MatchDto>>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<MatchDto>> GetMatchByIdAsync(string id)
    {
        try
        {
            var match = await _db.Matches
                .Include(m => m.Participants)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match is null)
                return ApiResponse<MatchDto>.FailureResult("Match not found");

            var dartThrowsCount = await _db.DartThrows.CountAsync(d => d.MatchId == match.Id);

            var dto = new MatchDto
            {
                Id = match.Id,
                TournamentId = match.TournamentId,
                Status = match.Status,
                MatchFormat = match.MatchFormat,
                ParticipantsCount = match.Participants.Count,
                DartThrowsCount = dartThrowsCount,
                CreatedAt = match.CreatedAt,
                UpdatedAt = match.UpdatedAt
            };

            return ApiResponse<MatchDto>.SuccessResult(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching match {MatchId}", id);
            return ApiResponse<MatchDto>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<MatchDto>> CreateMatchAsync(CreateMatchRequest request, string userId)
    {
        try
        {
            // Validate tournament exists and user is admin
            var tournament = await _db.Tournaments.FirstOrDefaultAsync(t => t.Id == request.TournamentId);
            if (tournament is null)
                return ApiResponse<MatchDto>.FailureResult("Tournament not found");

            if (tournament.AdminId != userId)
                return ApiResponse<MatchDto>.FailureResult("Only tournament admin can create matches");

            if (tournament.Status == "Completed")
                return ApiResponse<MatchDto>.FailureResult("Cannot create matches for completed tournament");

            var match = new Match
            {
                Id = Guid.NewGuid().ToString(),
                TournamentId = request.TournamentId,
                Status = "Scheduled",
                MatchFormat = request.MatchFormat ?? "301",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Matches.Add(match);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Match {MatchId} created in tournament {TournamentId}", match.Id, request.TournamentId);

            var dto = new MatchDto
            {
                Id = match.Id,
                TournamentId = match.TournamentId,
                Status = match.Status,
                MatchFormat = match.MatchFormat,
                ParticipantsCount = 0,
                DartThrowsCount = 0,
                CreatedAt = match.CreatedAt,
                UpdatedAt = match.UpdatedAt
            };

            return ApiResponse<MatchDto>.SuccessResult(dto, "Match created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating match");
            return ApiResponse<MatchDto>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<MatchDto>> UpdateMatchStatusAsync(string id, UpdateMatchStatusRequest request, string userId)
    {
        try
        {
            var match = await _db.Matches
                .Include(m => m.Participants)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match is null)
                return ApiResponse<MatchDto>.FailureResult("Match not found");

            var tournament = await _db.Tournaments.FindAsync(match.TournamentId);
            if (tournament?.AdminId != userId)
                return ApiResponse<MatchDto>.FailureResult("Only tournament admin can update match status");

            // Validate status transition
            var validStatuses = new[] { "Pending", "InProgress", "Paused", "Completed" };
            if (!validStatuses.Contains(request.Status))
                return ApiResponse<MatchDto>.FailureResult("Invalid match status");

            if (request.Status == "Live")
                match.ActualStart = DateTime.UtcNow;
            else if (request.Status == "Completed")
                match.ActualEnd = DateTime.UtcNow;

            match.Status = request.Status;
            match.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Match {MatchId} status updated to {Status}", id, request.Status);

            var dartThrowsCount = await _db.DartThrows.CountAsync(d => d.MatchId == match.Id);

            var dto = new MatchDto
            {
                Id = match.Id,
                TournamentId = match.TournamentId,
                Status = match.Status,
                MatchFormat = match.MatchFormat,
                ParticipantsCount = match.Participants.Count,
                DartThrowsCount = dartThrowsCount,
                CreatedAt = match.CreatedAt,
                UpdatedAt = match.UpdatedAt
            };

            return ApiResponse<MatchDto>.SuccessResult(dto, "Match status updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating match {MatchId}", id);
            return ApiResponse<MatchDto>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<MatchDto>> AddParticipantAsync(string matchId, string userId)
    {
        try
        {
            var match = await _db.Matches
                .Include(m => m.Participants)
                .FirstOrDefaultAsync(m => m.Id == matchId);

            if (match is null)
                return ApiResponse<MatchDto>.FailureResult("Match not found");

            if (match.Status != "Scheduled")
                return ApiResponse<MatchDto>.FailureResult("Can only add participants to scheduled matches");

            // Check if user is not already a participant
            var existsAsParticipant = match.Participants.Any(p => p.UserId == userId);
            if (existsAsParticipant)
                return ApiResponse<MatchDto>.FailureResult("User is already a participant");

            // Check max participants (usually 2 for standard darts)
            if (match.Participants.Count >= 2)
                return ApiResponse<MatchDto>.FailureResult("Match is full");

            var participant = new MatchParticipant
            {
                Id = Guid.NewGuid().ToString(),
                MatchId = matchId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _db.MatchParticipants.Add(participant);
            await _db.SaveChangesAsync();

            _logger.LogInformation("User {UserId} added to match {MatchId}", userId, matchId);

            // Update match participants count
            match.Participants.Add(participant);

            var dartThrowsCount = await _db.DartThrows.CountAsync(d => d.MatchId == match.Id);

            var dto = new MatchDto
            {
                Id = match.Id,
                TournamentId = match.TournamentId,
                Status = match.Status,
                MatchFormat = match.MatchFormat,
                ParticipantsCount = match.Participants.Count,
                DartThrowsCount = dartThrowsCount,
                CreatedAt = match.CreatedAt,
                UpdatedAt = match.UpdatedAt
            };

            return ApiResponse<MatchDto>.SuccessResult(dto, "Participant added successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding participant to match {MatchId}", matchId);
            return ApiResponse<MatchDto>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> DeleteMatchAsync(string id, string userId)
    {
        try
        {
            var match = await _db.Matches.FirstOrDefaultAsync(m => m.Id == id);
            if (match is null)
                return ApiResponse<bool>.FailureResult("Match not found");

            var tournament = await _db.Tournaments.FindAsync(match.TournamentId);
            if (tournament?.AdminId != userId)
                return ApiResponse<bool>.FailureResult("Only tournament admin can delete matches");

            if (match.Status != "Scheduled")
                return ApiResponse<bool>.FailureResult("Can only delete scheduled matches");

            _db.Matches.Remove(match);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Match {MatchId} deleted by {UserId}", id, userId);

            return ApiResponse<bool>.SuccessResult(true, "Match deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting match {MatchId}", id);
            return ApiResponse<bool>.FailureResult($"Error: {ex.Message}");
        }
    }
}

// DTOs
public record MatchDto
{
    public string Id { get; set; } = string.Empty;
    public string TournamentId { get; set; } = string.Empty;
    public string Status { get; set; } = "Scheduled";
    public string MatchFormat { get; set; } = "301";
    public int ParticipantsCount { get; set; }
    public int DartThrowsCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateMatchRequest
{
    public string TournamentId { get; set; } = string.Empty;
    public string? MatchFormat { get; set; }
}

public class UpdateMatchStatusRequest
{
    public string Status { get; set; } = "Scheduled";
}
