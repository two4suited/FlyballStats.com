using System.Security.Claims;
using flyballstats.ApiService.Models;
using flyballstats.ApiService.Services;
using Microsoft.AspNetCore.Authorization;

namespace flyballstats.ApiService.Authorization;

public class RoleRequirement : IAuthorizationRequirement
{
    public UserRole RequiredRole { get; }
    public string Action { get; }
    public string Resource { get; }

    public RoleRequirement(UserRole requiredRole, string action, string resource)
    {
        RequiredRole = requiredRole;
        Action = action;
        Resource = resource;
    }
}

public class RoleAuthorizationHandler : AuthorizationHandler<RoleRequirement>
{
    private readonly IAuthenticationService _authenticationService;
    private readonly Services.IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RoleAuthorizationHandler(
        IAuthenticationService authenticationService,
        Services.IAuthorizationService authorizationService,
        IHttpContextAccessor httpContextAccessor)
    {
        _authenticationService = authenticationService;
        _authorizationService = authorizationService;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoleRequirement requirement)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                     context.User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            await LogUnauthorizedAttempt("unknown", requirement.Action, requirement.Resource, "No user ID in token");
            context.Fail();
            return;
        }

        var user = await _authenticationService.GetUserByIdAsync(userId);
        if (user == null)
        {
            await LogUnauthorizedAttempt(userId, requirement.Action, requirement.Resource, "User not found");
            context.Fail();
            return;
        }

        if (!user.IsActive)
        {
            await LogUnauthorizedAttempt(userId, requirement.Action, requirement.Resource, "User account is inactive");
            context.Fail();
            return;
        }

        var hasPermission = await _authorizationService.HasRoleAsync(user, requirement.RequiredRole);
        if (hasPermission)
        {
            // Log successful authorization
            await _authenticationService.LogAuthorizationAttemptAsync(
                userId, 
                requirement.Action, 
                requirement.Resource, 
                true,
                null,
                GetUserAgent(),
                GetIpAddress()
            );
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }

    private async Task LogUnauthorizedAttempt(string userId, string action, string resource, string reason)
    {
        await _authenticationService.LogAuthorizationAttemptAsync(
            userId, 
            action, 
            resource, 
            false, 
            reason,
            GetUserAgent(),
            GetIpAddress()
        );
    }

    private string? GetUserAgent()
    {
        return _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.FirstOrDefault();
    }

    private string? GetIpAddress()
    {
        return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    }
}

// Custom authorization attributes for different roles
public class RequireDirectorAttribute : AuthorizeAttribute
{
    public RequireDirectorAttribute(string action = "access", string resource = "api")
    {
        Policy = $"Director_{action}_{resource}";
    }
}

public class RequireRaceDirectorAttribute : AuthorizeAttribute
{
    public RequireRaceDirectorAttribute(string action = "access", string resource = "api")
    {
        Policy = $"RaceDirector_{action}_{resource}";
    }
}

public class RequireViewerAttribute : AuthorizeAttribute
{
    public RequireViewerAttribute(string action = "access", string resource = "api")
    {
        Policy = $"Viewer_{action}_{resource}";
    }
}