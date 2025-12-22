using flyballstats.ApiService.Models;
using flyballstats.ApiService.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace flyballstats.Tests.Unit;

public class AuthorizationServiceTests
{
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly Mock<ILogger<AuthorizationService>> _mockLogger;
    private readonly AuthorizationService _authorizationService;

    public AuthorizationServiceTests()
    {
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockLogger = new Mock<ILogger<AuthorizationService>>();
        _authorizationService = new AuthorizationService(_mockAuthService.Object, _mockLogger.Object);
    }

    [Theory]
    [InlineData(UserRole.Director, "view", true)]
    [InlineData(UserRole.Director, "assign_race", true)]
    [InlineData(UserRole.Director, "manage_tournaments", true)]
    [InlineData(UserRole.RaceDirector, "view", true)]
    [InlineData(UserRole.RaceDirector, "assign_race", true)]
    [InlineData(UserRole.RaceDirector, "manage_tournaments", false)]
    [InlineData(UserRole.Viewer, "view", true)]
    [InlineData(UserRole.Viewer, "assign_race", false)]
    [InlineData(UserRole.Viewer, "manage_tournaments", false)]
    public async Task CanPerformActionAsync_WithVariousRolesAndActions_ReturnsExpectedResult(
        UserRole role, string action, bool expectedResult)
    {
        // Arrange
        var user = new User("test-id", "testuser", "test@example.com", role, true);

        // Act
        var result = await _authorizationService.CanPerformActionAsync(user, action, "test_resource");

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task CanPerformActionAsync_WithInactiveUser_ReturnsFalse()
    {
        // Arrange
        var user = new User("test-id", "testuser", "test@example.com", UserRole.Director, false);

        // Act
        var result = await _authorizationService.CanPerformActionAsync(user, "view", "test_resource");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanPerformActionAsync_WithUnknownAction_ReturnsFalse()
    {
        // Arrange
        var user = new User("test-id", "testuser", "test@example.com", UserRole.Director, true);

        // Act
        var result = await _authorizationService.CanPerformActionAsync(user, "unknown_action", "test_resource");

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(UserRole.Director, UserRole.Viewer, true)]
    [InlineData(UserRole.Director, UserRole.RaceDirector, true)]
    [InlineData(UserRole.Director, UserRole.Director, true)]
    [InlineData(UserRole.RaceDirector, UserRole.Viewer, true)]
    [InlineData(UserRole.RaceDirector, UserRole.RaceDirector, true)]
    [InlineData(UserRole.RaceDirector, UserRole.Director, false)]
    [InlineData(UserRole.Viewer, UserRole.Viewer, true)]
    [InlineData(UserRole.Viewer, UserRole.RaceDirector, false)]
    [InlineData(UserRole.Viewer, UserRole.Director, false)]
    public async Task HasRoleAsync_WithVariousRoleCombinations_ReturnsExpectedResult(
        UserRole userRole, UserRole requiredRole, bool expectedResult)
    {
        // Arrange
        var user = new User("test-id", "testuser", "test@example.com", userRole, true);

        // Act
        var result = await _authorizationService.HasRoleAsync(user, requiredRole);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task HasRoleAsync_WithInactiveUser_ReturnsFalse()
    {
        // Arrange
        var user = new User("test-id", "testuser", "test@example.com", UserRole.Director, false);

        // Act
        var result = await _authorizationService.HasRoleAsync(user, UserRole.Viewer);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasAnyRoleAsync_WithMatchingRole_ReturnsTrue()
    {
        // Arrange
        var user = new User("test-id", "testuser", "test@example.com", UserRole.RaceDirector, true);

        // Act
        var result = await _authorizationService.HasAnyRoleAsync(user, UserRole.Viewer, UserRole.RaceDirector);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasAnyRoleAsync_WithNoMatchingRole_ReturnsFalse()
    {
        // Arrange
        var user = new User("test-id", "testuser", "test@example.com", UserRole.Viewer, true);

        // Act
        var result = await _authorizationService.HasAnyRoleAsync(user, UserRole.RaceDirector, UserRole.Director);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task LogUnauthorizedAttemptAsync_WithValidData_CallsAuthenticationService()
    {
        // Arrange
        var user = new User("test-id", "testuser", "test@example.com", UserRole.Viewer, true);
        var action = "test_action";
        var resource = "test_resource";
        var reason = "test_reason";

        // Act
        await _authorizationService.LogUnauthorizedAttemptAsync(user, action, resource, reason);

        // Assert
        _mockAuthService.Verify(
            x => x.LogAuthorizationAttemptAsync(user.Id, action, resource, false, reason, null, null),
            Times.Once);
    }

    [Theory]
    [InlineData("view")]
    [InlineData("read")]
    public async Task CanPerformActionAsync_WithViewerActions_AllowsAllRoles(string action)
    {
        // Arrange & Act & Assert
        foreach (UserRole role in Enum.GetValues<UserRole>())
        {
            var user = new User("test-id", "testuser", "test@example.com", role, true);
            var result = await _authorizationService.CanPerformActionAsync(user, action, "test_resource");
            Assert.True(result, $"Role {role} should be able to perform {action}");
        }
    }

    [Theory]
    [InlineData("assign_race")]
    [InlineData("clear_ring")]
    [InlineData("manage_assignments")]
    public async Task CanPerformActionAsync_WithRaceDirectorActions_AllowsRaceDirectorAndDirector(string action)
    {
        // Arrange & Act & Assert
        var viewerUser = new User("test-id", "viewer", "viewer@example.com", UserRole.Viewer, true);
        var viewerResult = await _authorizationService.CanPerformActionAsync(viewerUser, action, "test_resource");
        Assert.False(viewerResult, $"Viewer should NOT be able to perform {action}");

        var raceDirectorUser = new User("test-id", "racedirector", "racedirector@example.com", UserRole.RaceDirector, true);
        var raceDirectorResult = await _authorizationService.CanPerformActionAsync(raceDirectorUser, action, "test_resource");
        Assert.True(raceDirectorResult, $"Race Director should be able to perform {action}");

        var directorUser = new User("test-id", "director", "director@example.com", UserRole.Director, true);
        var directorResult = await _authorizationService.CanPerformActionAsync(directorUser, action, "test_resource");
        Assert.True(directorResult, $"Director should be able to perform {action}");
    }

    [Theory]
    [InlineData("create_tournament")]
    [InlineData("delete_tournament")]
    [InlineData("upload_csv")]
    [InlineData("configure_rings")]
    [InlineData("manage_tournaments")]
    [InlineData("manage_users")]
    public async Task CanPerformActionAsync_WithDirectorActions_OnlyAllowsDirector(string action)
    {
        // Arrange & Act & Assert
        var viewerUser = new User("test-id", "viewer", "viewer@example.com", UserRole.Viewer, true);
        var viewerResult = await _authorizationService.CanPerformActionAsync(viewerUser, action, "test_resource");
        Assert.False(viewerResult, $"Viewer should NOT be able to perform {action}");

        var raceDirectorUser = new User("test-id", "racedirector", "racedirector@example.com", UserRole.RaceDirector, true);
        var raceDirectorResult = await _authorizationService.CanPerformActionAsync(raceDirectorUser, action, "test_resource");
        Assert.False(raceDirectorResult, $"Race Director should NOT be able to perform {action}");

        var directorUser = new User("test-id", "director", "director@example.com", UserRole.Director, true);
        var directorResult = await _authorizationService.CanPerformActionAsync(directorUser, action, "test_resource");
        Assert.True(directorResult, $"Director should be able to perform {action}");
    }
}