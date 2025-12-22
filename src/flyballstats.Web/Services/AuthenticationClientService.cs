namespace flyballstats.Web.Services;

public interface IAuthenticationClientService
{
    event EventHandler<bool> AuthenticationStateChanged;
    Task<bool> LoginAsync(string username, string password);
    Task LogoutAsync();
    bool IsAuthenticated { get; }
    User? CurrentUser { get; }
    string? Token { get; }
    Task InitializeAsync();
    bool CanPerformAction(string action);
    bool HasRole(UserRole role);
    bool HasAnyRole(params UserRole[] roles);
}

public class AuthenticationClientService : IAuthenticationClientService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthenticationClientService> _logger;
    private User? _currentUser;
    private string? _token;

    public event EventHandler<bool>? AuthenticationStateChanged;

    public bool IsAuthenticated => _currentUser != null && !string.IsNullOrEmpty(_token);
    public User? CurrentUser => _currentUser;
    public string? Token => _token;

    public AuthenticationClientService(HttpClient httpClient, ILogger<AuthenticationClientService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            var request = new LoginRequest(username, password);
            var response = await _httpClient.PostAsJsonAsync("/auth/login", request);

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (loginResponse?.Success == true && loginResponse.User != null && !string.IsNullOrEmpty(loginResponse.Token))
                {
                    _currentUser = loginResponse.User;
                    _token = loginResponse.Token;
                    
                    // Set the authorization header for future requests
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);

                    _logger.LogInformation("User {Username} logged in successfully", username);
                    AuthenticationStateChanged?.Invoke(this, true);
                    return true;
                }
            }

            _logger.LogWarning("Login failed for user {Username}", username);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Username}", username);
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        _currentUser = null;
        _token = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;
        
        _logger.LogInformation("User logged out");
        AuthenticationStateChanged?.Invoke(this, false);
        await Task.CompletedTask;
    }

    public async Task InitializeAsync()
    {
        // This would typically load from local storage in a real app
        // For now, we'll start with no authentication
        await Task.CompletedTask;
    }

    public bool CanPerformAction(string action)
    {
        if (!IsAuthenticated || _currentUser == null)
            return false;

        return action.ToLowerInvariant() switch
        {
            // Viewer permissions
            "view" => _currentUser.Role >= UserRole.Viewer,
            "read" => _currentUser.Role >= UserRole.Viewer,
            
            // Race Director permissions
            "assign_race" => _currentUser.Role >= UserRole.RaceDirector,
            "clear_ring" => _currentUser.Role >= UserRole.RaceDirector,
            "manage_assignments" => _currentUser.Role >= UserRole.RaceDirector,
            
            // Director permissions
            "create_tournament" => _currentUser.Role >= UserRole.Director,
            "delete_tournament" => _currentUser.Role >= UserRole.Director,
            "upload_csv" => _currentUser.Role >= UserRole.Director,
            "configure_rings" => _currentUser.Role >= UserRole.Director,
            "manage_tournaments" => _currentUser.Role >= UserRole.Director,
            "manage_users" => _currentUser.Role >= UserRole.Director,
            
            _ => false // Default deny for unknown actions
        };
    }

    public bool HasRole(UserRole role)
    {
        return IsAuthenticated && _currentUser != null && _currentUser.Role >= role;
    }

    public bool HasAnyRole(params UserRole[] roles)
    {
        return IsAuthenticated && _currentUser != null && roles.Any(role => _currentUser.Role >= role);
    }
}