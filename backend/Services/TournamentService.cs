using DartMaster.Api.Data;
using DartMaster.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DartMaster.Api.Services;

public interface ITournamentService
{
    Task<ApiResponse<List<TournamentDto>>> GetAllTournamentsAsync();
    Task<ApiResponse<TournamentDto>> GetTournamentByIdAsync(string id);
    Task<ApiResponse<TournamentDto>> CreateTournamentAsync(CreateTournamentRequest request, string adminId);
    Task<ApiResponse<TournamentDto>> UpdateTournamentAsync(string id, UpdateTournamentRequest request, string userId);
    Task<ApiResponse<bool>> DeleteTournamentAsync(string id, string userId);
}

public class TournamentService : ITournamentService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<TournamentService> _logger;

    public TournamentService(ApplicationDbContext db, ILogger<TournamentService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ApiResponse<List<TournamentDto>>> GetAllTournamentsAsync()
    {
        try
        {
            var tournaments = await _db.Tournaments
                .Include(t => t.Admin)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TournamentDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description,
                    Status = t.Status,
                    Format = t.Format,
                    MatchFormat = t.MatchFormat,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    MaxPlayers = t.MaxPlayers,
                    NumberOfGroups = t.NumberOfGroups,
                    AdminId = t.AdminId,
                    AdminName = t.Admin.Username,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return ApiResponse<List<TournamentDto>>.SuccessResult(tournaments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tournaments");
            return ApiResponse<List<TournamentDto>>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<TournamentDto>> GetTournamentByIdAsync(string id)
    {
        try
        {
            var tournament = await _db.Tournaments
                .Include(t => t.Admin)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tournament is null)
                return ApiResponse<TournamentDto>.FailureResult("Tournament not found");

            var dto = new TournamentDto
            {
                Id = tournament.Id,
                Name = tournament.Name,
                Description = tournament.Description,
                Status = tournament.Status,
                Format = tournament.Format,
                MatchFormat = tournament.MatchFormat,
                StartDate = tournament.StartDate,
                EndDate = tournament.EndDate,
                MaxPlayers = tournament.MaxPlayers,
                NumberOfGroups = tournament.NumberOfGroups,
                AdminId = tournament.AdminId,
                AdminName = tournament.Admin.Username,
                CreatedAt = tournament.CreatedAt
            };

            return ApiResponse<TournamentDto>.SuccessResult(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tournament {TournamentId}", id);
            return ApiResponse<TournamentDto>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<TournamentDto>> CreateTournamentAsync(CreateTournamentRequest request, string adminId)
    {
        try
        {
            // Validate
            if (string.IsNullOrWhiteSpace(request.Name))
                return ApiResponse<TournamentDto>.FailureResult("Tournament name is required");

            if (request.StartDate < DateTime.UtcNow)
                return ApiResponse<TournamentDto>.FailureResult("Start date must be in the future");

            if (request.MaxPlayers < 2)
                return ApiResponse<TournamentDto>.FailureResult("Tournament must have at least 2 players");

            var tournament = new Tournament
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                Status = "Draft",
                Format = request.Format ?? "SingleElimination",
                MatchFormat = request.MatchFormat ?? "Standard",
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                RegistrationDeadline = request.RegistrationDeadline,
                MaxPlayers = request.MaxPlayers,
                NumberOfGroups = request.NumberOfGroups ?? 1,
                AdminId = adminId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Tournaments.Add(tournament);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Tournament {TournamentName} created by {AdminId}", tournament.Name, adminId);

            var admin = await _db.Users.FindAsync(adminId);
            var dto = new TournamentDto
            {
                Id = tournament.Id,
                Name = tournament.Name,
                Description = tournament.Description,
                Status = tournament.Status,
                Format = tournament.Format,
                MatchFormat = tournament.MatchFormat,
                StartDate = tournament.StartDate,
                EndDate = tournament.EndDate,
                MaxPlayers = tournament.MaxPlayers,
                NumberOfGroups = tournament.NumberOfGroups,
                AdminId = tournament.AdminId,
                AdminName = admin?.Username ?? "Unknown",
                CreatedAt = tournament.CreatedAt
            };

            return ApiResponse<TournamentDto>.SuccessResult(dto, "Tournament created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tournament");
            return ApiResponse<TournamentDto>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<TournamentDto>> UpdateTournamentAsync(string id, UpdateTournamentRequest request, string userId)
    {
        try
        {
            var tournament = await _db.Tournaments
                .Include(t => t.Admin)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tournament is null)
                return ApiResponse<TournamentDto>.FailureResult("Tournament not found");

            // Only admin can update
            if (tournament.AdminId != userId)
                return ApiResponse<TournamentDto>.FailureResult("Only tournament admin can update");

            // Can't update if tournament has started
            if (tournament.Status != "Draft")
                return ApiResponse<TournamentDto>.FailureResult("Can only update tournaments in Draft status");

            if (!string.IsNullOrWhiteSpace(request.Name))
                tournament.Name = request.Name.Trim();

            if (!string.IsNullOrWhiteSpace(request.Description))
                tournament.Description = request.Description.Trim();

            if (request.StartDate.HasValue)
                tournament.StartDate = request.StartDate.Value;

            if (request.EndDate.HasValue)
                tournament.EndDate = request.EndDate.Value;

            if (request.MaxPlayers.HasValue)
                tournament.MaxPlayers = request.MaxPlayers.Value;

            tournament.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            _logger.LogInformation("Tournament {TournamentId} updated by {UserId}", tournament.Id, userId);

            var dto = new TournamentDto
            {
                Id = tournament.Id,
                Name = tournament.Name,
                Description = tournament.Description,
                Status = tournament.Status,
                Format = tournament.Format,
                MatchFormat = tournament.MatchFormat,
                StartDate = tournament.StartDate,
                EndDate = tournament.EndDate,
                MaxPlayers = tournament.MaxPlayers,
                NumberOfGroups = tournament.NumberOfGroups,
                AdminId = tournament.AdminId,
                AdminName = tournament.Admin.Username,
                CreatedAt = tournament.CreatedAt
            };

            return ApiResponse<TournamentDto>.SuccessResult(dto, "Tournament updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tournament {TournamentId}", id);
            return ApiResponse<TournamentDto>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> DeleteTournamentAsync(string id, string userId)
    {
        try
        {
            var tournament = await _db.Tournaments.FirstOrDefaultAsync(t => t.Id == id);

            if (tournament is null)
                return ApiResponse<bool>.FailureResult("Tournament not found");

            if (tournament.AdminId != userId)
                return ApiResponse<bool>.FailureResult("Only tournament admin can delete");

            if (tournament.Status != "Draft")
                return ApiResponse<bool>.FailureResult("Can only delete tournaments in Draft status");

            _db.Tournaments.Remove(tournament);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Tournament {TournamentId} deleted by {UserId}", id, userId);

            return ApiResponse<bool>.SuccessResult(true, "Tournament deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tournament {TournamentId}", id);
            return ApiResponse<bool>.FailureResult($"Error: {ex.Message}");
        }
    }
}

// DTOs
public record TournamentDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Draft";
    public string Format { get; set; } = string.Empty;
    public string MatchFormat { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int MaxPlayers { get; set; }
    public int NumberOfGroups { get; set; }
    public string AdminId { get; set; } = string.Empty;
    public string AdminName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateTournamentRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Format { get; set; }
    public string? MatchFormat { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? RegistrationDeadline { get; set; }
    public int MaxPlayers { get; set; }
    public int? NumberOfGroups { get; set; }
}

public class UpdateTournamentRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? MaxPlayers { get; set; }
}
