using flyballstats.ApiService.Hubs;
using flyballstats.ApiService.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

namespace flyballstats.ApiService.Services;

/// <summary>
/// Service for sending real-time notifications via SignalR
/// </summary>
public class SignalRNotificationService : IRealTimeNotificationService
{
    private readonly IHubContext<RaceAssignmentHub> _hubContext;
    private readonly ILogger<SignalRNotificationService> _logger;
    private readonly ApplicationMetrics _metrics;

    public SignalRNotificationService(IHubContext<RaceAssignmentHub> hubContext, ILogger<SignalRNotificationService> logger, ApplicationMetrics metrics)
    {
        _hubContext = hubContext;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task NotifyRaceAssignmentUpdated(string tournamentId, TournamentRaceAssignments assignments)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        try
        {
            await _hubContext.Clients.Group($"tournament_{tournamentId}")
                .SendAsync("RaceAssignmentUpdated", assignments);
            
            success = true;
            _logger.LogInformation("Notified clients about race assignment update for tournament {TournamentId}", tournamentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send race assignment notification for tournament {TournamentId}", tournamentId);
        }
        finally
        {
            stopwatch.Stop();
            _metrics.RecordNotification(success, "race_assignment");
            _metrics.RecordOperationDuration("notification_race_assignment", stopwatch.ElapsedMilliseconds);
            await RecordLatencyMetric("RaceAssignmentUpdate", stopwatch.ElapsedMilliseconds);
        }
    }

    public async Task NotifyRingCleared(string tournamentId, int ringNumber, TournamentRaceAssignments assignments)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;
        try
        {
            await _hubContext.Clients.Group($"tournament_{tournamentId}")
                .SendAsync("RingCleared", new { RingNumber = ringNumber, Assignments = assignments });
            
            success = true;
            _logger.LogInformation("Notified clients about ring {RingNumber} cleared for tournament {TournamentId}", ringNumber, tournamentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ring cleared notification for ring {RingNumber} in tournament {TournamentId}", ringNumber, tournamentId);
        }
        finally
        {
            stopwatch.Stop();
            _metrics.RecordNotification(success, "ring_clear");
            _metrics.RecordOperationDuration("notification_ring_clear", stopwatch.ElapsedMilliseconds);
            await RecordLatencyMetric("RingClear", stopwatch.ElapsedMilliseconds);
        }
    }

    public async Task RecordLatencyMetric(string operation, long milliseconds)
    {
        try
        {
            // Log performance metrics for monitoring P95 latency requirement
            _logger.LogInformation("Performance: {Operation} notification took {Milliseconds}ms", operation, milliseconds);
            
            // In a production environment, you would send this to a metrics service like Application Insights, Prometheus, etc.
            // For now, we'll just log it for monitoring
            if (milliseconds > 3000) // Alert if over 3 seconds (our P95 requirement)
            {
                _logger.LogWarning("PERFORMANCE ALERT: {Operation} notification took {Milliseconds}ms, exceeding 3 second target", operation, milliseconds);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record latency metric for operation {Operation}", operation);
        }
        
        await Task.CompletedTask;
    }
}