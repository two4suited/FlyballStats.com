using flyballstats.ApiService.Models;

namespace flyballstats.ApiService.Services;

public interface IAuthenticationService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<User?> GetUserByIdAsync(string userId);
    Task<User?> GetUserByUsernameAsync(string username);
    string GenerateToken(User user);
    Task<bool> ValidateTokenAsync(string token);
    Task<User?> GetUserFromTokenAsync(string token);
    Task LogAuthorizationAttemptAsync(string userId, string action, string resource, bool success, string? reason = null, string? userAgent = null, string? ipAddress = null);
}