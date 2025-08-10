using flyballstats.ApiService.Models;
using System.Collections.Concurrent;

namespace flyballstats.ApiService.Services;

public class TournamentDataService
{
    private readonly ConcurrentDictionary<string, Tournament> _tournaments = new();
    private readonly ConcurrentDictionary<string, TournamentRingConfiguration> _ringConfigurations = new();

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

    public TournamentRingConfiguration SaveRingConfiguration(string tournamentId, List<RingConfiguration> rings)
    {
        var configuration = new TournamentRingConfiguration(tournamentId, rings);
        _ringConfigurations.AddOrUpdate(tournamentId, configuration, (key, oldValue) => configuration);
        return configuration;
    }

    public TournamentRingConfiguration? GetRingConfiguration(string tournamentId)
    {
        _ringConfigurations.TryGetValue(tournamentId, out var configuration);
        return configuration;
    }
}