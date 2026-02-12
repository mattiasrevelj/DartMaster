using DartMaster.Api.Data;
using DartMaster.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DartMaster.Api.Services;

public interface IDartScoreService
{
    Task<ApiResponse<DartThrowDto>> RecordDartThrowAsync(string matchId, RecordDartThrowRequest request, string userId);
    Task<ApiResponse<List<DartThrowDto>>> GetMatchDartsAsync(string matchId);
    Task<ApiResponse<MatchScoreDto>> GetMatchScoreAsync(string matchId);
    Task<ApiResponse<bool>> UndoLastDartAsync(string matchId, string userId);
}

public class DartScoreService : IDartScoreService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<DartScoreService> _logger;

    public DartScoreService(ApplicationDbContext db, ILogger<DartScoreService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ApiResponse<DartThrowDto>> RecordDartThrowAsync(string matchId, RecordDartThrowRequest request, string userId)
    {
        try
        {
            // Validate match exists and is in progress
            var match = await _db.Matches
                .Include(m => m.Participants)
                .FirstOrDefaultAsync(m => m.Id == matchId);

            if (match is null)
                return ApiResponse<DartThrowDto>.FailureResult("Match not found");

            if (match.Status != "Live")
                return ApiResponse<DartThrowDto>.FailureResult("Match is not in progress");

            // Validate user is a participant
            var participant = match.Participants?.FirstOrDefault(p => p.UserId == userId);
            if (participant is null)
                return ApiResponse<DartThrowDto>.FailureResult("User is not a participant in this match");

            // Validate points (0-180 standard)
            if (request.Points < 0 || request.Points > 180)
                return ApiResponse<DartThrowDto>.FailureResult("Invalid points (0-180)");

            // Get tournament to understand format (301, 501, 701, etc)
            var tournament = await _db.Tournaments.FindAsync(match.TournamentId);
            var startScore = tournament?.MatchFormat == "301" ? 301 : 501;

            // Get current score
            var previousThrows = await _db.DartThrows
                .Where(d => d.MatchId == matchId && d.UserId == userId)
                .OrderByDescending(d => d.ThrownAt)
                .FirstOrDefaultAsync();

            var currentScore = previousThrows?.RemainingScore ?? startScore;
            var newRemainingScore = currentScore - request.Points;

            // Validate score doesn't go below 0 or bust (unless double out)
            if (newRemainingScore < 0)
                return ApiResponse<DartThrowDto>.FailureResult("Score would go below zero - bust");

            if (newRemainingScore == 0 && !request.IsDouble)
                return ApiResponse<DartThrowDto>.FailureResult("Must finish with a double");

            // Get round number
            var roundNumber = await _db.DartThrows
                .Where(d => d.MatchId == matchId && d.UserId == userId)
                .GroupBy(d => d.RoundNumber)
                .CountAsync() + 1;

            // Get throw number in current round
            var throwInRound = await _db.DartThrows
                .Where(d => d.MatchId == matchId && d.UserId == userId && d.RoundNumber == roundNumber)
                .CountAsync() + 1;

            var dartThrow = new DartThrow
            {
                Id = Guid.NewGuid().ToString(),
                MatchId = matchId,
                UserId = userId,
                ThrowNumber = throwInRound,
                RoundNumber = roundNumber,
                Points = request.Points,
                RemainingScore = newRemainingScore,
                IsDouble = request.IsDouble,
                ThrownAt = DateTime.UtcNow
            };

            _db.DartThrows.Add(dartThrow);

            // If scored 0, match is complete
            if (newRemainingScore == 0)
            {
                match.Status = "Waiting for confirmation";
                participant.FinishingScore = 0;
                participant.Position = 1;
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation("Dart recorded - Match {MatchId}, User {UserId}, Points {Points}, Remaining {Remaining}",
                matchId, userId, request.Points, newRemainingScore);

            var dto = new DartThrowDto
            {
                Id = dartThrow.Id,
                MatchId = dartThrow.MatchId,
                UserId = dartThrow.UserId,
                Points = dartThrow.Points,
                RemainingScore = dartThrow.RemainingScore,
                IsDouble = dartThrow.IsDouble,
                RoundNumber = dartThrow.RoundNumber,
                ThrowNumber = dartThrow.ThrowNumber,
                ThrownAt = dartThrow.ThrownAt
            };

            return ApiResponse<DartThrowDto>.SuccessResult(dto, newRemainingScore == 0 ? "Match finished!" : "Dart recorded");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording dart throw for match {MatchId}", matchId);
            return ApiResponse<DartThrowDto>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<DartThrowDto>>> GetMatchDartsAsync(string matchId)
    {
        try
        {
            var darts = await _db.DartThrows
                .Where(d => d.MatchId == matchId)
                .OrderBy(d => d.UserId)
                .ThenBy(d => d.RoundNumber)
                .ThenBy(d => d.ThrowNumber)
                .Select(d => new DartThrowDto
                {
                    Id = d.Id,
                    MatchId = d.MatchId,
                    UserId = d.UserId,
                    Points = d.Points,
                    RemainingScore = d.RemainingScore,
                    IsDouble = d.IsDouble,
                    RoundNumber = d.RoundNumber,
                    ThrowNumber = d.ThrowNumber,
                    ThrownAt = d.ThrownAt
                })
                .ToListAsync();

            return ApiResponse<List<DartThrowDto>>.SuccessResult(darts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dart throws for match {MatchId}", matchId);
            return ApiResponse<List<DartThrowDto>>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<MatchScoreDto>> GetMatchScoreAsync(string matchId)
    {
        try
        {
            var match = await _db.Matches
                .Include(m => m.Participants)
                .FirstOrDefaultAsync(m => m.Id == matchId);

            if (match is null)
                return ApiResponse<MatchScoreDto>.FailureResult("Match not found");

            var scores = new List<PlayerScoreDto>();

            foreach (var participant in match.Participants ?? new List<MatchParticipant>())
            {
                var latestThrow = await _db.DartThrows
                    .Where(d => d.MatchId == matchId && d.UserId == participant.UserId)
                    .OrderByDescending(d => d.ThrownAt)
                    .FirstOrDefaultAsync();

                var tournament = await _db.Tournaments.FindAsync(match.TournamentId);
                var startScore = tournament?.MatchFormat == "301" ? 301 : 501;
                var currentScore = latestThrow?.RemainingScore ?? startScore;

                var roundCount = await _db.DartThrows
                    .Where(d => d.MatchId == matchId && d.UserId == participant.UserId)
                    .Select(d => d.RoundNumber)
                    .Distinct()
                    .CountAsync();

                scores.Add(new PlayerScoreDto
                {
                    UserId = participant.UserId,
                    CurrentScore = currentScore,
                    RoundsPlayed = roundCount,
                    DartsThrown = latestThrow != null ? await _db.DartThrows.CountAsync(d => d.MatchId == matchId && d.UserId == participant.UserId) : 0,
                    Status = currentScore == 0 ? "Finished" : "In Progress"
                });
            }

            var dto = new MatchScoreDto
            {
                MatchId = matchId,
                Status = match.Status,
                PlayerScores = scores,
                UpdatedAt = DateTime.UtcNow
            };

            return ApiResponse<MatchScoreDto>.SuccessResult(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching match score for {MatchId}", matchId);
            return ApiResponse<MatchScoreDto>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> UndoLastDartAsync(string matchId, string userId)
    {
        try
        {
            var lastDart = await _db.DartThrows
                .Where(d => d.MatchId == matchId && d.UserId == userId)
                .OrderByDescending(d => d.ThrownAt)
                .FirstOrDefaultAsync();

            if (lastDart is null)
                return ApiResponse<bool>.FailureResult("No darts to undo");

            var match = await _db.Matches.FirstOrDefaultAsync(m => m.Id == matchId);
            if (match?.Status != "Live" && match?.Status != "Waiting for confirmation")
                return ApiResponse<bool>.FailureResult("Cannot undo darts in this match state");

            _db.DartThrows.Remove(lastDart);

            // Reset match status if it was completed
            if (match?.Status == "Waiting for confirmation")
            {
                var participant = await _db.MatchParticipants.FirstOrDefaultAsync(p => p.MatchId == matchId && p.UserId == userId);
                if (participant != null)
                {
                    participant.FinishingScore = null;
                    participant.Position = null;
                }
                match.Status = "Live";
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation("Undo dart - Match {MatchId}, User {UserId}", matchId, userId);

            return ApiResponse<bool>.SuccessResult(true, "Dart undone successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error undoing dart for match {MatchId}", matchId);
            return ApiResponse<bool>.FailureResult($"Error: {ex.Message}");
        }
    }
}

// DTOs
public record DartThrowDto
{
    public string Id { get; set; } = string.Empty;
    public string MatchId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public int Points { get; set; }
    public int RemainingScore { get; set; }
    public bool IsDouble { get; set; }
    public int RoundNumber { get; set; }
    public int ThrowNumber { get; set; }
    public DateTime ThrownAt { get; set; }
}

public record MatchScoreDto
{
    public string MatchId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<PlayerScoreDto> PlayerScores { get; set; } = new();
    public DateTime UpdatedAt { get; set; }
}

public record PlayerScoreDto
{
    public string UserId { get; set; } = string.Empty;
    public int CurrentScore { get; set; }
    public int RoundsPlayed { get; set; }
    public int DartsThrown { get; set; }
    public string Status { get; set; } = "In Progress";
}

public class RecordDartThrowRequest
{
    public int Points { get; set; }
    public bool IsDouble { get; set; }
}
