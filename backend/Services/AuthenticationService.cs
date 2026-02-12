using DartMaster.Api.Data;
using DartMaster.Api.Models;
using BCrypt.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace DartMaster.Api.Services;

public interface IAuthenticationService
{
    Task<ApiResponse<AuthResponse>> RegisterAsync(string username, string email, string password, string fullName);
    Task<ApiResponse<AuthResponse>> LoginAsync(string username, string password);
}

public class AuthenticationService : IAuthenticationService
{
    private readonly ApplicationDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(ApplicationDbContext db, ITokenService tokenService, ILogger<AuthenticationService> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(string username, string email, string password, string fullName)
    {
        try
        {
            var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == username || u.Email == email);
            if (existingUser is not null)
                return ApiResponse<AuthResponse>.FailureResult("Username or email already exists");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Username = username,
                Email = email,
                PasswordHash = passwordHash,
                FullName = fullName,
                Role = "Player",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            _logger.LogInformation("User {Username} registered successfully", username);

            var token = _tokenService.GenerateToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken(user.Id);

            return ApiResponse<AuthResponse>.SuccessResult(new AuthResponse
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Token = token,
                RefreshToken = refreshToken
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user {Username}", username);
            return ApiResponse<AuthResponse>.FailureResult($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(string username, string password)
    {
        try
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                _logger.LogWarning("Failed login attempt for {Username}", username);
                return ApiResponse<AuthResponse>.FailureResult("Invalid username or password");
            }

            if (!user.IsActive)
                return ApiResponse<AuthResponse>.FailureResult("User account is inactive");

            user.LastLogin = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var token = _tokenService.GenerateToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken(user.Id);

            _logger.LogInformation("User {Username} logged in successfully", username);

            return ApiResponse<AuthResponse>.SuccessResult(new AuthResponse
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Token = token,
                RefreshToken = refreshToken
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging in user {Username}", username);
            return ApiResponse<AuthResponse>.FailureResult($"Error: {ex.Message}");
        }
    }
}

public interface ITokenService
{
    string GenerateToken(User user);
    string GenerateRefreshToken(string userId);
    bool ValidateToken(string token);
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<TokenService> _logger;

    public TokenService(IConfiguration configuration, ApplicationDbContext db, ILogger<TokenService> logger)
    {
        _configuration = configuration;
        _db = db;
        _logger = logger;
    }

    public string GenerateToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secret = jwtSettings.GetValue<string>("Secret") ?? throw new InvalidOperationException("JWT Secret not configured");
        var issuer = jwtSettings.GetValue<string>("Issuer");
        var audience = jwtSettings.GetValue<string>("Audience");
        var expirationMinutes = jwtSettings.GetValue<int>("ExpirationMinutes", 60);

        var key = Encoding.UTF8.GetBytes(secret);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("Role", user.Role)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken(string userId)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var refreshExpirationDays = jwtSettings.GetValue<int>("RefreshExpirationDays", 7);

        var refreshToken = Guid.NewGuid().ToString();
        var hash = BCrypt.Net.BCrypt.HashPassword(refreshToken);
        var expiresAt = DateTime.UtcNow.AddDays(refreshExpirationDays);

        var token = new RefreshToken
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            TokenHash = hash,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };

        _db.RefreshTokens.Add(token);
        _db.SaveChanges();

        return refreshToken;
    }

    public bool ValidateToken(string token)
    {
        try
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secret = jwtSettings.GetValue<string>("Secret");

            var handler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret!)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.GetValue<string>("Issuer"),
                ValidateAudience = true,
                ValidAudience = jwtSettings.GetValue<string>("Audience"),
                ValidateLifetime = true
            };

            handler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation failed");
            return false;
        }
    }
}

public record AuthResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

    private ApiResponse() { }

    private ApiResponse(bool success, string? message, T? data)
    {
        Success = success;
        Message = message;
        Data = data;
    }

    public static ApiResponse<T> SuccessResult(T data, string? message = null)
    {
        return new ApiResponse<T>(true, message, data);
    }

    public static ApiResponse<T> FailureResult(string message)
    {
        return new ApiResponse<T>(false, message, default);
    }
}


