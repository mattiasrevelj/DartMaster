namespace DartMaster.Api.Models;

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "Player"; // Admin, Player, Spectator
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Tournament>? AdminTournaments { get; set; }
    public ICollection<TournamentParticipant>? Participants { get; set; }
    public ICollection<MatchParticipant>? MatchParticipants { get; set; }
    public ICollection<DartThrow>? DartThrows { get; set; }
    public ICollection<MatchConfirmation>? Confirmations { get; set; }
    public ICollection<PlayerStatistics>? Statistics { get; set; }
    public ICollection<RefreshToken>? RefreshTokens { get; set; }
}

public class Tournament
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Planning"; // Planning, Active, Completed
    public string Format { get; set; } = "Group"; // Group, Series, Knockout
    public string MatchFormat { get; set; } = "301"; // 301, 501
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? RegistrationDeadline { get; set; }
    public int MaxPlayers { get; set; } = 100;
    public int NumberOfGroups { get; set; } = 1;
    public string AdminId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? Admin { get; set; }
    public ICollection<TournamentGroup>? Groups { get; set; }
    public ICollection<TournamentParticipant>? Participants { get; set; }
    public ICollection<Match>? Matches { get; set; }
    public ICollection<PlayerStatistics>? Statistics { get; set; }
}

public class TournamentGroup
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TournamentId { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public int GroupNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Tournament? Tournament { get; set; }
    public ICollection<TournamentParticipant>? Participants { get; set; }
    public ICollection<Match>? Matches { get; set; }
}

public class TournamentParticipant
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TournamentId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? GroupId { get; set; }
    public string Status { get; set; } = "Registered"; // Registered, Active, Withdrawn, WO
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Tournament? Tournament { get; set; }
    public User? User { get; set; }
    public TournamentGroup? Group { get; set; }
}

public class Match
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TournamentId { get; set; } = string.Empty;
    public string? GroupId { get; set; }
    public string MatchFormat { get; set; } = "301";
    public string Status { get; set; } = "Scheduled"; // Scheduled, Live, Waiting for confirmation, Completed
    public DateTime? ScheduledStart { get; set; }
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualEnd { get; set; }
    public string? ReportingPlayerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Tournament? Tournament { get; set; }
    public TournamentGroup? Group { get; set; }
    public User? ReportingPlayer { get; set; }
    public ICollection<MatchParticipant>? Participants { get; set; }
    public ICollection<DartThrow>? DartThrows { get; set; }
    public ICollection<MatchConfirmation>? Confirmations { get; set; }
}

public class MatchParticipant
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string MatchId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public int? FinishingScore { get; set; } // 0 for winner
    public int? Position { get; set; } // 1st, 2nd, 3rd, etc
    public bool IsConfirmed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Match? Match { get; set; }
    public User? User { get; set; }
}

public class DartThrow
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string MatchId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public int ThrowNumber { get; set; } // 1-3 per round
    public int RoundNumber { get; set; }
    public int Points { get; set; }
    public int RemainingScore { get; set; }
    public bool IsDouble { get; set; }
    public DateTime ThrownAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Match? Match { get; set; }
    public User? User { get; set; }
}

public class MatchConfirmation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string MatchId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public bool Confirmed { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Match? Match { get; set; }
    public User? User { get; set; }
}

public class PlayerStatistics
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TournamentId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public int MatchesPlayed { get; set; }
    public int MatchesWon { get; set; }
    public int MatchesLost { get; set; }
    public decimal WinLossRatio { get; set; }
    public decimal AverageScore { get; set; }
    public int? Ranking { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Tournament? Tournament { get; set; }
    public User? User { get; set; }
}

public class RefreshToken
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }

    // Navigation
    public User? User { get; set; }
}
