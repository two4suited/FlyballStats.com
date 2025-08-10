using flyballstats.ApiService.Models;

namespace flyballstats.ApiService.Services;

/// <summary>
/// Interface for real-time notification services
/// </summary>
public interface IRealTimeNotificationService
{
    /// <summary>
    /// Notify clients about race assignment updates for a specific tournament
    /// </summary>
    Task NotifyRaceAssignmentUpdated(string tournamentId, TournamentRaceAssignments assignments);
    
    /// <summary>
    /// Notify clients about ring clearance for a specific tournament
    /// </summary>
    Task NotifyRingCleared(string tournamentId, int ringNumber, TournamentRaceAssignments assignments);
    
    /// <summary>
    /// Track performance metrics for notifications
    /// </summary>
    Task RecordLatencyMetric(string operation, long milliseconds);
}