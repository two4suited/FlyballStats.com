using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using flyballstats.ApiService.Data;
using flyballstats.ApiService.Models;

namespace flyballstats.ApiService.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly FlyballStatsDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;

    public AuthenticationService(
        FlyballStatsDbContext context, 
        IConfiguration configuration,
        ILogger<AuthenticationService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _jwtSecret = _configuration["Jwt:Secret"] ?? "your-256-bit-secret-key-that-is-long-enough-for-jwt-signing-minimum-32-characters";
        _jwtIssuer = _configuration["Jwt:Issuer"] ?? "FlyballStats";
        _jwtAudience = _configuration["Jwt:Audience"] ?? "FlyballStats";
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            var userEntity = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

            if (userEntity == null)
            {
                await LogAuthorizationAttemptAsync("unknown", "login", "system", false, "Invalid username");
                return new LoginResponse(false, null, null, "Invalid username or password");
            }

            if (!VerifyPassword(request.Password, userEntity.PasswordHash))
            {
                await LogAuthorizationAttemptAsync(userEntity.Id, "login", "system", false, "Invalid password");
                return new LoginResponse(false, null, null, "Invalid username or password");
            }

            // Update last login
            userEntity.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var user = new User(userEntity.Id, userEntity.Username, userEntity.Email, userEntity.Role, userEntity.IsActive);
            var token = GenerateToken(user);

            await LogAuthorizationAttemptAsync(userEntity.Id, "login", "system", true);
            
            return new LoginResponse(true, token, user, "Login successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for username: {Username}", request.Username);
            return new LoginResponse(false, null, null, "An error occurred during login");
        }
    }

    public async Task<User?> GetUserByIdAsync(string userId)
    {
        var userEntity = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

        return userEntity == null ? null : 
            new User(userEntity.Id, userEntity.Username, userEntity.Email, userEntity.Role, userEntity.IsActive);
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        var userEntity = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

        return userEntity == null ? null : 
            new User(userEntity.Id, userEntity.Username, userEntity.Email, userEntity.Role, userEntity.IsActive);
    }

    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("role", user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecret);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtIssuer,
                ValidateAudience = true,
                ValidAudience = _jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public async Task<User?> GetUserFromTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecret);

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtIssuer,
                ValidateAudience = true,
                ValidAudience = _jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (userId == null) return null;

            return await GetUserByIdAsync(userId);
        }
        catch
        {
            return null;
        }
    }

    public async Task LogAuthorizationAttemptAsync(string userId, string action, string resource, bool success, string? reason = null, string? userAgent = null, string? ipAddress = null)
    {
        try
        {
            var logEntry = new AuthorizationLogEntity
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Action = action,
                Resource = resource,
                Success = success,
                Reason = reason,
                Timestamp = DateTime.UtcNow,
                UserAgent = userAgent,
                IpAddress = ipAddress
            };

            _context.AuthorizationLogs.Add(logEntry);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Authorization attempt logged: User {UserId}, Action {Action}, Resource {Resource}, Success {Success}, Reason {Reason}",
                userId, action, resource, success, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log authorization attempt for user {UserId}", userId);
        }
    }

    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }
}