using flyballstats.ApiService.Models;
using System.Collections.Concurrent;

namespace flyballstats.ApiService.Services;

public class TournamentDataService
{
    private readonly ConcurrentDictionary<string, Tournament> _tournaments = new();

    public Tournament? GetTournament(string tournamentId)
    {
        _tournaments.TryGetValue(tournamentId, out var tournament);
        return tournament;
    }

    public IEnumerable<Tournament> GetAllTournaments()
    {
        return _tournaments.Values;
    }

    public Tournament CreateOrUpdateTournament(string tournamentId, string tournamentName, List<Race> races)
    {
        var tournament = new Tournament(tournamentId, tournamentName, races);
        _tournaments.AddOrUpdate(tournamentId, tournament, (key, oldValue) => tournament);
        return tournament;
    }

    public bool TournamentExists(string tournamentId)
    {
        return _tournaments.ContainsKey(tournamentId);
    }
}