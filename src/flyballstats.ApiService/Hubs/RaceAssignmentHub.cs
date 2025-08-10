using Microsoft.AspNetCore.SignalR;

namespace flyballstats.ApiService.Hubs;

/// <summary>
/// SignalR hub for real-time race assignment updates
/// </summary>
public class RaceAssignmentHub : Hub
{
    /// <summary>
    /// Join a tournament group to receive updates for that specific tournament
    /// </summary>
    /// <param name="tournamentId">The tournament ID to join</param>
    public async Task JoinTournament(string tournamentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"tournament_{tournamentId}");
    }

    /// <summary>
    /// Leave a tournament group
    /// </summary>
    /// <param name="tournamentId">The tournament ID to leave</param>
    public async Task LeaveTournament(string tournamentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tournament_{tournamentId}");
    }

    /// <summary>
    /// Handle client disconnection
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}