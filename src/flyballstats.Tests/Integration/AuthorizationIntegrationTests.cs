using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using flyballstats.ApiService.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using flyballstats.ApiService.Data;
using Microsoft.EntityFrameworkCore;

namespace flyballstats.Tests.Integration;

public class AuthorizationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthorizationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<FlyballStatsDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<FlyballStatsDbContext>(options =>
                {
                    options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString());
                });
            });
        });

        _client = _factory.CreateClient();
    }

    private async Task<string> GetAuthTokenAsync(string username, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FlyballStatsDbContext>();

        // Ensure test users exist
        if (!await context.Users.AnyAsync(u => u.Username == username))
        {
            UserRole role = username switch
            {
                "director" => UserRole.Director,
                "racedirector" => UserRole.RaceDirector,
                "viewer" => UserRole.Viewer,
                _ => UserRole.Viewer
            };

            var user = new UserEntity
            {
                Id = Guid.NewGuid().ToString(),
                Username = username,
                Email = $"{username}@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role,
                IsActive = true
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        var loginRequest = new LoginRequest(username, password);
        var json = JsonSerializer.Serialize(loginRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/auth/login", content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return loginResponse?.Token ?? throw new InvalidOperationException("Login failed");
    }

    private void SetAuthToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task GetTournaments_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/tournaments");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetTournaments_WithViewerAuth_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthTokenAsync("viewer", "viewer123");
        SetAuthToken(token);

        // Act
        var response = await _client.GetAsync("/tournaments");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTournaments_WithRaceDirectorAuth_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthTokenAsync("racedirector", "racedirector123");
        SetAuthToken(token);

        // Act
        var response = await _client.GetAsync("/tournaments");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTournaments_WithDirectorAuth_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthTokenAsync("director", "director123");
        SetAuthToken(token);

        // Act
        var response = await _client.GetAsync("/tournaments");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SetRingConfiguration_WithViewerAuth_ReturnsForbidden()
    {
        // Arrange
        var token = await GetAuthTokenAsync("viewer", "viewer123");
        SetAuthToken(token);

        var request = new RingConfigurationRequest("test-tournament", new List<RingConfiguration>
        {
            new(1, "Red")
        });
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/tournaments/test-tournament/rings", content);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SetRingConfiguration_WithRaceDirectorAuth_ReturnsForbidden()
    {
        // Arrange
        var token = await GetAuthTokenAsync("racedirector", "racedirector123");
        SetAuthToken(token);

        var request = new RingConfigurationRequest("test-tournament", new List<RingConfiguration>
        {
            new(1, "Red")
        });
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/tournaments/test-tournament/rings", content);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SetRingConfiguration_WithDirectorAuth_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthTokenAsync("director", "director123");
        SetAuthToken(token);

        var request = new RingConfigurationRequest("test-tournament", new List<RingConfiguration>
        {
            new(1, "Red")
        });
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/tournaments/test-tournament/rings", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AssignRaceToRing_WithViewerAuth_ReturnsForbidden()
    {
        // Arrange
        var token = await GetAuthTokenAsync("viewer", "viewer123");
        SetAuthToken(token);

        var request = new AssignRaceRequest("test-tournament", 1, 1, RingStatus.Current);
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/tournaments/test-tournament/races/assign", content);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AssignRaceToRing_WithRaceDirectorAuth_ReturnsOkOrBadRequest()
    {
        // Arrange
        var token = await GetAuthTokenAsync("racedirector", "racedirector123");
        SetAuthToken(token);

        var request = new AssignRaceRequest("test-tournament", 1, 1, RingStatus.Current);
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/tournaments/test-tournament/races/assign", content);

        // Assert
        // Should not return Forbidden - either OK or BadRequest depending on data validation
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AssignRaceToRing_WithDirectorAuth_ReturnsOkOrBadRequest()
    {
        // Arrange
        var token = await GetAuthTokenAsync("director", "director123");
        SetAuthToken(token);

        var request = new AssignRaceRequest("test-tournament", 1, 1, RingStatus.Current);
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/tournaments/test-tournament/races/assign", content);

        // Assert
        // Should not return Forbidden - either OK or BadRequest depending on data validation
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ClearRing_WithViewerAuth_ReturnsForbidden()
    {
        // Arrange
        var token = await GetAuthTokenAsync("viewer", "viewer123");
        SetAuthToken(token);

        // Act
        var response = await _client.PostAsync("/tournaments/test-tournament/rings/1/clear", new StringContent("", Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ClearRing_WithRaceDirectorAuth_ReturnsOkOrBadRequest()
    {
        // Arrange
        var token = await GetAuthTokenAsync("racedirector", "racedirector123");
        SetAuthToken(token);

        // Act
        var response = await _client.PostAsync("/tournaments/test-tournament/rings/1/clear", new StringContent("", Encoding.UTF8, "application/json"));

        // Assert
        // Should not return Forbidden - either OK or BadRequest depending on data validation
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AuthLogin_WithValidCredentials_ReturnsTokenAndUserInfo()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FlyballStatsDbContext>();

        var user = new UserEntity
        {
            Id = "test-login-user",
            Username = "logintest",
            Email = "logintest@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("testpassword"),
            Role = UserRole.RaceDirector,
            IsActive = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var loginRequest = new LoginRequest("logintest", "testpassword");
        var json = JsonSerializer.Serialize(loginRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/auth/login", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(loginResponse);
        Assert.True(loginResponse.Success);
        Assert.NotNull(loginResponse.Token);
        Assert.NotNull(loginResponse.User);
        Assert.Equal("logintest", loginResponse.User.Username);
        Assert.Equal(UserRole.RaceDirector, loginResponse.User.Role);
    }

    [Fact]
    public async Task AuthLogin_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest("nonexistent", "wrongpassword");
        var json = JsonSerializer.Serialize(loginRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/auth/login", content);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}