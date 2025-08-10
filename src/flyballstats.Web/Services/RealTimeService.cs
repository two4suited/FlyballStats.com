using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;

namespace flyballstats.Web.Services;

/// <summary>
/// Service for managing real-time SignalR connection and events
/// </summary>
public class RealTimeService : IAsyncDisposable
{
    private HubConnection? _connection;
    private readonly ILogger<RealTimeService> _logger;
    private readonly IConfiguration _configuration;
    private string? _currentTournamentId;

    // Events for UI components to subscribe to
    public event Func<TournamentRaceAssignments, Task>? RaceAssignmentUpdated;
    public event Func<int, TournamentRaceAssignments, Task>? RingCleared;
    public event Func<bool, Task>? ConnectionStateChanged;

    public RealTimeService(ILogger<RealTimeService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public async Task<bool> ConnectAsync(string baseUrl)
    {
        var connectionStopwatch = Stopwatch.StartNew();
        try
        {
            if (_connection != null)
            {
                await _connection.DisposeAsync();
            }

            var hubUrl = $"{baseUrl.TrimEnd('/')}/racehub";
            _logger.LogInformation("Connecting to SignalR hub at {HubUrl}", hubUrl);

            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
                .Build();

            // Register event handlers
            _connection.On<TournamentRaceAssignments>("RaceAssignmentUpdated", async (assignments) =>
            {
                var eventStopwatch = Stopwatch.StartNew();
                try
                {
                    _logger.LogInformation("Received race assignment update for tournament {TournamentId}", assignments.TournamentId);
                    await (RaceAssignmentUpdated?.Invoke(assignments) ?? Task.CompletedTask);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling RaceAssignmentUpdated event");
                }
                finally
                {
                    eventStopwatch.Stop();
                    _logger.LogInformation("RaceAssignmentUpdated event handled in {ElapsedMs}ms", eventStopwatch.ElapsedMilliseconds);
                }
            });

            _connection.On<object>("RingCleared", async (data) =>
            {
                var eventStopwatch = Stopwatch.StartNew();
                try
                {
                    // Handle the ring cleared event - data contains RingNumber and Assignments
                    if (data is System.Text.Json.JsonElement element)
                    {
                        var ringNumber = element.GetProperty("RingNumber").GetInt32();
                        var assignmentsJson = element.GetProperty("Assignments").GetRawText();
                        var assignments = System.Text.Json.JsonSerializer.Deserialize<TournamentRaceAssignments>(assignmentsJson);
                        
                        if (assignments != null)
                        {
                            _logger.LogInformation("Received ring clear event for ring {RingNumber} in tournament {TournamentId}", 
                                ringNumber, assignments.TournamentId);
                            await (RingCleared?.Invoke(ringNumber, assignments) ?? Task.CompletedTask);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling RingCleared event");
                }
                finally
                {
                    eventStopwatch.Stop();
                    _logger.LogInformation("RingCleared event handled in {ElapsedMs}ms", eventStopwatch.ElapsedMilliseconds);
                }
            });

            // Connection state change handlers
            _connection.Reconnecting += async (error) =>
            {
                _logger.LogWarning("SignalR connection lost, attempting to reconnect: {Error}", error?.Message);
                await (ConnectionStateChanged?.Invoke(false) ?? Task.CompletedTask);
            };

            _connection.Reconnected += async (connectionId) =>
            {
                _logger.LogInformation("SignalR reconnected with connection ID: {ConnectionId}", connectionId);
                await (ConnectionStateChanged?.Invoke(true) ?? Task.CompletedTask);
                
                // Rejoin tournament if we were in one
                if (!string.IsNullOrEmpty(_currentTournamentId))
                {
                    await JoinTournamentAsync(_currentTournamentId);
                }
            };

            _connection.Closed += async (error) =>
            {
                _logger.LogWarning("SignalR connection closed: {Error}", error?.Message);
                await (ConnectionStateChanged?.Invoke(false) ?? Task.CompletedTask);
            };

            await _connection.StartAsync();
            connectionStopwatch.Stop();
            
            _logger.LogInformation("Connected to SignalR hub in {ElapsedMs}ms", connectionStopwatch.ElapsedMilliseconds);
            await (ConnectionStateChanged?.Invoke(true) ?? Task.CompletedTask);
            
            return true;
        }
        catch (Exception ex)
        {
            connectionStopwatch.Stop();
            _logger.LogError(ex, "Failed to connect to SignalR hub after {ElapsedMs}ms", connectionStopwatch.ElapsedMilliseconds);
            await (ConnectionStateChanged?.Invoke(false) ?? Task.CompletedTask);
            return false;
        }
    }

    public async Task<bool> JoinTournamentAsync(string tournamentId)
    {
        try
        {
            if (_connection?.State != HubConnectionState.Connected)
            {
                _logger.LogWarning("Cannot join tournament - not connected to SignalR hub");
                return false;
            }

            _logger.LogInformation("Joining tournament {TournamentId}", tournamentId);
            await _connection.InvokeAsync("JoinTournament", tournamentId);
            _currentTournamentId = tournamentId;
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join tournament {TournamentId}", tournamentId);
            return false;
        }
    }

    public async Task<bool> LeaveTournamentAsync(string tournamentId)
    {
        try
        {
            if (_connection?.State != HubConnectionState.Connected)
            {
                return false;
            }

            _logger.LogInformation("Leaving tournament {TournamentId}", tournamentId);
            await _connection.InvokeAsync("LeaveTournament", tournamentId);
            
            if (_currentTournamentId == tournamentId)
            {
                _currentTournamentId = null;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to leave tournament {TournamentId}", tournamentId);
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }
    }
}