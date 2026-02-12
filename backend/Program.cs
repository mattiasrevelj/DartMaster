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
        new MySqlServerVersion(new Version(10, 5, 0)) // MariaDB version
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
    await db.Database.MigrateAsync();
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

// DTOs
public record RegisterRequest(string Username, string Email, string Password, string FullName);
public record LoginRequest(string Username, string Password);
public record UpdateUserRequest(string? FullName);
