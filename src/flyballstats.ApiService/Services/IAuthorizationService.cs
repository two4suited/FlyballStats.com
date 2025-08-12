using flyballstats.ApiService.Models;

namespace flyballstats.ApiService.Services;

public interface IAuthorizationService
{
    Task<bool> CanPerformActionAsync(User user, string action, string resource);
    Task<bool> HasRoleAsync(User user, UserRole requiredRole);
    Task<bool> HasAnyRoleAsync(User user, params UserRole[] roles);
    Task LogUnauthorizedAttemptAsync(User user, string action, string resource, string? reason = null);
}

public class AuthorizationService : IAuthorizationService
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AuthorizationService> _logger;

    public AuthorizationService(
        IAuthenticationService authenticationService,
        ILogger<AuthorizationService> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    public async Task<bool> CanPerformActionAsync(User user, string action, string resource)
    {
        if (!user.IsActive)
        {
            await LogUnauthorizedAttemptAsync(user, action, resource, "User account is inactive");
            return false;
        }

        var canPerform = action.ToLowerInvariant() switch
        {
            // Viewer permissions
            "view" => user.Role >= UserRole.Viewer,
            "read" => user.Role >= UserRole.Viewer,
            
            // Race Director permissions
            "assign_race" => user.Role >= UserRole.RaceDirector,
            "clear_ring" => user.Role >= UserRole.RaceDirector,
            "manage_assignments" => user.Role >= UserRole.RaceDirector,
            
            // Director permissions
            "create_tournament" => user.Role >= UserRole.Director,
            "delete_tournament" => user.Role >= UserRole.Director,
            "upload_csv" => user.Role >= UserRole.Director,
            "configure_rings" => user.Role >= UserRole.Director,
            "manage_tournaments" => user.Role >= UserRole.Director,
            "manage_users" => user.Role >= UserRole.Director,
            
            _ => false // Default deny for unknown actions
        };

        if (!canPerform)
        {
            await LogUnauthorizedAttemptAsync(user, action, resource, "Insufficient role permissions");
        }

        return canPerform;
    }

    public async Task<bool> HasRoleAsync(User user, UserRole requiredRole)
    {
        var hasRole = user.IsActive && user.Role >= requiredRole;
        
        if (!hasRole)
        {
            await LogUnauthorizedAttemptAsync(user, "role_check", requiredRole.ToString(), $"Required role: {requiredRole}, User role: {user.Role}");
        }

        return hasRole;
    }

    public async Task<bool> HasAnyRoleAsync(User user, params UserRole[] roles)
    {
        if (!user.IsActive)
        {
            await LogUnauthorizedAttemptAsync(user, "role_check", string.Join(",", roles), "User account is inactive");
            return false;
        }

        var hasAnyRole = roles.Any(role => user.Role >= role);
        
        if (!hasAnyRole)
        {
            await LogUnauthorizedAttemptAsync(user, "role_check", string.Join(",", roles), $"User role: {user.Role}, Required any of: {string.Join(",", roles)}");
        }

        return hasAnyRole;
    }

    public async Task LogUnauthorizedAttemptAsync(User user, string action, string resource, string? reason = null)
    {
        await _authenticationService.LogAuthorizationAttemptAsync(
            user.Id, 
            action, 
            resource, 
            false, 
            reason
        );

        _logger.LogWarning("Unauthorized access attempt: User {UserId} ({Username}) tried to perform {Action} on {Resource}. Reason: {Reason}",
            user.Id, user.Username, action, resource, reason);
    }
}