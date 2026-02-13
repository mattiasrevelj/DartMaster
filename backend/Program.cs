using DartMaster.Api.Data;
using DartMaster.Api.Models;
using DartMaster.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(10, 5, 0)), // MariaDB version
        mysqlOptions => mysqlOptions.EnableRetryOnFailure(
            maxRetryCount: 30,
            delayMilliseconds: 1000
        )
    )
);

// JWT Configuration
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secret = jwtSettings.GetValue<string>("Secret") ?? throw new InvalidOperationException("JWT Secret not found");
var key = Encoding.ASCII.GetBytes(secret);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.GetValue<string>("Issuer"),
        ValidateAudience = true,
        ValidAudience = jwtSettings.GetValue<string>("Audience"),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Add services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ITournamentService, TournamentService>();
builder.Services.AddScoped<IMatchService, MatchService>();
builder.Services.AddScoped<IDartScoreService, DartScoreService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // Retry database migration with exponential backoff
    int maxAttempts = 10;
    int attempt = 0;
    while (attempt < maxAttempts)
    {
        try
        {
            await db.Database.MigrateAsync();
            Console.WriteLine("✅ Database migration completed successfully");
            break;
        }
        catch (Exception ex)
        {
            attempt++;
            if (attempt >= maxAttempts)
            {
                Console.WriteLine($"❌ Failed to migrate database after {maxAttempts} attempts: {ex.Message}");
                throw;
            }
            
            int delayMs = (int)Math.Pow(2, attempt) * 500; // Exponential backoff: 1s, 2s, 4s, 8s...
            Console.WriteLine($"⚠️  Database migration attempt {attempt}/{maxAttempts} failed, retrying in {delayMs}ms...");
            await Task.Delay(delayMs);
        }
    }
}



app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Health check
app.MapGet("/health", () => new { status = "ok", timestamp = DateTime.UtcNow })
    .WithName("Health")
    .WithOpenApi();

// API Version
app.MapGet("/api/version", () => new { version = "1.0.0", environment = app.Environment.EnvironmentName })
    .WithName("Version")
    .WithOpenApi();

// Auth endpoints
var authEndpoints = app.MapGroup("/api/auth")
    .WithOpenApi();

authEndpoints.MapPost("/register", Register)
    .WithName("Register")
    .WithDescription("Register a new user")
    .AllowAnonymous();

authEndpoints.MapPost("/login", Login)
    .WithName("Login")
    .WithDescription("Login with username and password")
    .AllowAnonymous();

// User endpoints
var userEndpoints = app.MapGroup("/api/users")
    .RequireAuthorization()
    .WithOpenApi();

userEndpoints.MapGet("/{id}", GetUser)
    .WithName("GetUser")
    .WithDescription("Get user profile");

userEndpoints.MapPut("/{id}", UpdateUser)
    .WithName("UpdateUser")
    .WithDescription("Update user profile");

// Tournament endpoints
var tournamentEndpoints = app.MapGroup("/api/tournaments")
    .WithOpenApi();

tournamentEndpoints.MapGet("", GetAllTournaments)
    .WithName("GetAllTournaments")
    .WithDescription("Get all tournaments")
    .AllowAnonymous();

tournamentEndpoints.MapGet("/{id}", GetTournamentById)
    .WithName("GetTournamentById")
    .WithDescription("Get tournament by ID")
    .AllowAnonymous();

tournamentEndpoints.MapPost("", CreateTournament)
    .WithName("CreateTournament")
    .WithDescription("Create new tournament")
    .RequireAuthorization();

tournamentEndpoints.MapPut("/{id}", UpdateTournament)
    .WithName("UpdateTournament")
    .WithDescription("Update tournament")
    .RequireAuthorization();

tournamentEndpoints.MapDelete("/{id}", DeleteTournament)
    .WithName("DeleteTournament")
    .WithDescription("Delete tournament")
    .RequireAuthorization();

// Match endpoints
var matchEndpoints = app.MapGroup("/api/matches")
    .WithOpenApi();

matchEndpoints.MapGet("/tournament/{tournamentId}", GetTournamentMatches)
    .WithName("GetTournamentMatches")
    .WithDescription("Get all matches for a tournament")
    .AllowAnonymous();

matchEndpoints.MapGet("/{id}", GetMatchById)
    .WithName("GetMatchById")
    .WithDescription("Get match by ID")
    .AllowAnonymous();

matchEndpoints.MapPost("", CreateMatch)
    .WithName("CreateMatch")
    .WithDescription("Create new match")
    .RequireAuthorization();

matchEndpoints.MapPut("/{id}/status", UpdateMatchStatus)
    .WithName("UpdateMatchStatus")
    .WithDescription("Update match status")
    .RequireAuthorization();

matchEndpoints.MapPost("/{id}/participants", AddMatchParticipant)
    .WithName("AddMatchParticipant")
    .WithDescription("Add participant to match")
    .RequireAuthorization();

matchEndpoints.MapDelete("/{id}", DeleteMatch)
    .WithName("DeleteMatch")
    .WithDescription("Delete match")
    .RequireAuthorization();

// Dart Score endpoints
var dartEndpoints = app.MapGroup("/api/matches/{matchId}/darts")
    .WithOpenApi();

dartEndpoints.MapPost("", RecordDartThrow)
    .WithName("RecordDartThrow")
    .WithDescription("Record a dart throw with score")
    .RequireAuthorization();

dartEndpoints.MapGet("", GetMatchDarts)
    .WithName("GetMatchDarts")
    .WithDescription("Get all dart throws for a match")
    .AllowAnonymous();

dartEndpoints.MapGet("/score", GetMatchScore)
    .WithName("GetMatchScore")
    .WithDescription("Get current match score")
    .AllowAnonymous();

dartEndpoints.MapPost("/undo", UndoLastDart)
    .WithName("UndoLastDart")
    .WithDescription("Undo last dart throw")
    .RequireAuthorization();

app.Run();

// Endpoint handlers
async Task<IResult> Register(IAuthenticationService authService, RegisterRequest request)
{
    try
    {
        var result = await authService.RegisterAsync(request.Username, request.Email, request.Password, request.FullName);
        if (!result.Success)
            return Results.BadRequest(result);
        return Results.Created($"/api/users/{result.Data?.UserId}", result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
}

async Task<IResult> Login(IAuthenticationService authService, LoginRequest request)
{
    try
    {
        var result = await authService.LoginAsync(request.Username, request.Password);
        if (!result.Success)
            return Results.Unauthorized();
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
}

async Task<IResult> GetUser(string id, ApplicationDbContext db)
{
    var user = await db.Users.FindAsync(id);
    if (user is null)
        return Results.NotFound();
    return Results.Ok(user);
}

async Task<IResult> UpdateUser(string id, UpdateUserRequest request, ApplicationDbContext db)
{
    var user = await db.Users.FindAsync(id);
    if (user is null)
        return Results.NotFound();

    user.FullName = request.FullName ?? user.FullName;
    user.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();
    return Results.Ok(user);
}

// Tournament endpoint handlers
async Task<IResult> GetAllTournaments(ITournamentService tournamentService)
{
    var result = await tournamentService.GetAllTournamentsAsync();
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
}

async Task<IResult> GetTournamentById(string id, ITournamentService tournamentService)
{
    var result = await tournamentService.GetTournamentByIdAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
}

async Task<IResult> CreateTournament(CreateTournamentRequest request, ITournamentService tournamentService, System.Security.Claims.ClaimsPrincipal user)
{
    var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();
    
    var result = await tournamentService.CreateTournamentAsync(request, userId);
    return result.Success ? Results.Created($"/api/tournaments/{result.Data?.Id}", result) : Results.BadRequest(result);
}

async Task<IResult> UpdateTournament(string id, UpdateTournamentRequest request, ITournamentService tournamentService, System.Security.Claims.ClaimsPrincipal user)
{
    var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();
    
    var result = await tournamentService.UpdateTournamentAsync(id, request, userId);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
}

async Task<IResult> DeleteTournament(string id, ITournamentService tournamentService, System.Security.Claims.ClaimsPrincipal user)
{
    var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();
    
    var result = await tournamentService.DeleteTournamentAsync(id, userId);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
}

// Match endpoint handlers
async Task<IResult> GetTournamentMatches(string tournamentId, IMatchService matchService)
{
    var result = await matchService.GetTournamentMatchesAsync(tournamentId);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
}

async Task<IResult> GetMatchById(string id, IMatchService matchService)
{
    var result = await matchService.GetMatchByIdAsync(id);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
}

async Task<IResult> CreateMatch(CreateMatchRequest request, IMatchService matchService, System.Security.Claims.ClaimsPrincipal user)
{
    var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();
    
    var result = await matchService.CreateMatchAsync(request, userId);
    return result.Success ? Results.Created($"/api/matches/{result.Data?.Id}", result) : Results.BadRequest(result);
}

async Task<IResult> UpdateMatchStatus(string id, UpdateMatchStatusRequest request, IMatchService matchService, System.Security.Claims.ClaimsPrincipal user)
{
    var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();
    
    var result = await matchService.UpdateMatchStatusAsync(id, request, userId);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
}

async Task<IResult> AddMatchParticipant(string id, IMatchService matchService, System.Security.Claims.ClaimsPrincipal user)
{
    var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();
    
    var result = await matchService.AddParticipantAsync(id, userId);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
}

async Task<IResult> DeleteMatch(string id, IMatchService matchService, System.Security.Claims.ClaimsPrincipal user)
{
    var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();
    
    var result = await matchService.DeleteMatchAsync(id, userId);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
}

// Dart Score endpoint handlers
async Task<IResult> RecordDartThrow(string matchId, RecordDartThrowRequest request, IDartScoreService dartService, System.Security.Claims.ClaimsPrincipal user)
{
    var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();
    
    var result = await dartService.RecordDartThrowAsync(matchId, request, userId);
    return result.Success ? Results.Created($"/api/matches/{matchId}/darts/{result.Data?.Id}", result) : Results.BadRequest(result);
}

async Task<IResult> GetMatchDarts(string matchId, IDartScoreService dartService)
{
    var result = await dartService.GetMatchDartsAsync(matchId);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
}

async Task<IResult> GetMatchScore(string matchId, IDartScoreService dartService)
{
    var result = await dartService.GetMatchScoreAsync(matchId);
    return result.Success ? Results.Ok(result) : Results.NotFound(result);
}

async Task<IResult> UndoLastDart(string matchId, IDartScoreService dartService, System.Security.Claims.ClaimsPrincipal user)
{
    var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();
    
    var result = await dartService.UndoLastDartAsync(matchId, userId);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
}

// DTOs
public record RegisterRequest(string Username, string Email, string Password, string FullName);
public record LoginRequest(string Username, string Password);
public record UpdateUserRequest(string? FullName);
