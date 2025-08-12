using flyballstats.ApiService.Models;
using flyballstats.ApiService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using flyballstats.ApiService.Data;

namespace flyballstats.Tests.Unit;

public class AuthenticationServiceTests : IDisposable
{
    private readonly FlyballStatsDbContext _context;
    private readonly AuthenticationService _authService;

    public AuthenticationServiceTests()
    {
        var options = new DbContextOptionsBuilder<FlyballStatsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new FlyballStatsDbContext(options);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test-secret-key-that-is-long-enough-for-jwt-signing-minimum-32-characters",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience"
            })
            .Build();

        var logger = new LoggerFactory().CreateLogger<AuthenticationService>();
        _authService = new AuthenticationService(_context, configuration, logger);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccessfulResponse()
    {
        // Arrange
        var user = new UserEntity
        {
            Id = "test-user-1",
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = UserRole.Viewer,
            IsActive = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var loginRequest = new LoginRequest("testuser", "password123");

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Token);
        Assert.NotNull(result.User);
        Assert.Equal("testuser", result.User.Username);
        Assert.Equal(UserRole.Viewer, result.User.Role);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidUsername_ReturnsFailure()
    {
        // Arrange
        var loginRequest = new LoginRequest("nonexistent", "password123");

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Token);
        Assert.Null(result.User);
        Assert.Equal("Invalid username or password", result.Message);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsFailure()
    {
        // Arrange
        var user = new UserEntity
        {
            Id = "test-user-2",
            Username = "testuser2",
            Email = "test2@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctpassword"),
            Role = UserRole.Viewer,
            IsActive = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var loginRequest = new LoginRequest("testuser2", "wrongpassword");

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Token);
        Assert.Null(result.User);
        Assert.Equal("Invalid username or password", result.Message);
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ReturnsFailure()
    {
        // Arrange
        var user = new UserEntity
        {
            Id = "test-user-3",
            Username = "inactiveuser",
            Email = "inactive@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = UserRole.Viewer,
            IsActive = false
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var loginRequest = new LoginRequest("inactiveuser", "password123");

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Token);
        Assert.Null(result.User);
        Assert.Equal("Invalid username or password", result.Message);
    }

    [Fact]
    public void GenerateToken_WithValidUser_ReturnsValidJwtToken()
    {
        // Arrange
        var user = new User("test-id", "testuser", "test@example.com", UserRole.Director);

        // Act
        var token = _authService.GenerateToken(user);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        
        // Verify it's a valid JWT format (3 parts separated by dots)
        var parts = token.Split('.');
        Assert.Equal(3, parts.Length);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var user = new User("test-id", "testuser", "test@example.com", UserRole.Director);
        var token = _authService.GenerateToken(user);

        // Act
        var isValid = await _authService.ValidateTokenAsync(token);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithInvalidToken_ReturnsFalse()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var isValid = await _authService.ValidateTokenAsync(invalidToken);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task GetUserFromTokenAsync_WithValidToken_ReturnsUser()
    {
        // Arrange
        var originalUser = new UserEntity
        {
            Id = "test-user-4",
            Username = "tokenuser",
            Email = "token@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = UserRole.RaceDirector,
            IsActive = true
        };
        _context.Users.Add(originalUser);
        await _context.SaveChangesAsync();

        var user = new User(originalUser.Id, originalUser.Username, originalUser.Email, originalUser.Role, true);
        var token = _authService.GenerateToken(user);

        // Act
        var retrievedUser = await _authService.GetUserFromTokenAsync(token);

        // Assert
        Assert.NotNull(retrievedUser);
        Assert.Equal(originalUser.Id, retrievedUser.Id);
        Assert.Equal(originalUser.Username, retrievedUser.Username);
        Assert.Equal(originalUser.Role, retrievedUser.Role);
    }

    [Fact]
    public async Task SimpleJwtTest()
    {
        // Very simple test to isolate the JWT issue
        var user = new User("simple-id", "simple-user", "simple@test.com", UserRole.Viewer, true);
        var token = _authService.GenerateToken(user);
        
        // Just check if validation works at all
        var isValid = await _authService.ValidateTokenAsync(token);
        Assert.True(isValid);
    }

    [Fact]
    public async Task LogAuthorizationAttemptAsync_WithValidData_LogsToDatabase()
    {
        // Arrange
        var userId = "test-user-5";
        var action = "test_action";
        var resource = "test_resource";
        var success = true;
        var reason = "test_reason";

        // Act
        await _authService.LogAuthorizationAttemptAsync(userId, action, resource, success, reason);

        // Assert
        var logEntry = await _context.AuthorizationLogs.FirstOrDefaultAsync(l => l.UserId == userId);
        Assert.NotNull(logEntry);
        Assert.Equal(action, logEntry.Action);
        Assert.Equal(resource, logEntry.Resource);
        Assert.Equal(success, logEntry.Success);
        Assert.Equal(reason, logEntry.Reason);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}