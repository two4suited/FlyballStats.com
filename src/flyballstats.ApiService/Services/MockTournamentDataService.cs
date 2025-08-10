using flyballstats.ApiService.Models;

namespace flyballstats.ApiService.Services;

public class MockTournamentDataService
{
    private readonly List<Tournament> _tournaments = new()
    {
        new Tournament(
            "demo-tournament-2024",
            "Spring Championship 2024",
            new List<Race>
            {
                new Race(1, "Lightning Bolts", "Thunder Paws", "Open"),
                new Race(2, "Speed Demons", "Flying Aces", "Veteran"),
                new Race(3, "Rocket Dogs", "Wind Runners", "Open"),
                new Race(4, "Swift Paws", "Storm Chasers", "Multibreed"),
                new Race(5, "Fire Flies", "Wave Riders", "Open"),
                new Race(6, "Night Hawks", "Desert Storm", "Veteran"),
                new Race(7, "Golden Retrievers", "Border Collies", "Regular"),
                new Race(8, "Fast Track", "Speed Zone", "Open"),
                new Race(9, "Turbo Tails", "Dash Hounds", "Multibreed"),
                new Race(10, "Elite Squad", "Champion Pack", "Veteran")
            }
        ),
        new Tournament(
            "summer-classic-2024",
            "Summer Classic Tournament 2024",
            new List<Race>
            {
                new Race(1, "Beach Runners", "Surf Dogs", "Open"),
                new Race(2, "Sun Chasers", "Heat Wave", "Veteran"),
                new Race(3, "Summer Sprinters", "Coastal Pack", "Regular"),
                new Race(4, "Ocean Breeze", "Tide Runners", "Multibreed"),
                new Race(5, "Solar Flare", "Bright Lights", "Open")
            }
        ),
        new Tournament(
            "fall-invitational-2024",
            "Fall Invitational 2024",
            new List<Race>
            {
                new Race(1, "Autumn Leaves", "Harvest Moon", "Open"),
                new Race(2, "Crisp Air", "Cool Breeze", "Veteran"),
                new Race(3, "Falling Stars", "Golden Hour", "Regular")
            }
        )
    };

    private readonly Dictionary<string, TournamentRaceAssignments> _assignments = new()
    {
        ["demo-tournament-2024"] = new TournamentRaceAssignments(
            "demo-tournament-2024",
            new List<RingRaceAssignments>
            {
                new RingRaceAssignments(1, "Red", 
                    new RaceAssignment(3, 1, RingStatus.Current),
                    new RaceAssignment(4, 1, RingStatus.OnDeck),
                    new RaceAssignment(5, 1, RingStatus.InTheHole)),
                new RingRaceAssignments(2, "Blue",
                    new RaceAssignment(1, 2, RingStatus.Current),
                    new RaceAssignment(2, 2, RingStatus.OnDeck),
                    null),
                new RingRaceAssignments(3, "Green",
                    null, null, null)
            },
            DateTime.UtcNow
        )
    };

    public IEnumerable<Tournament> GetAllTournaments()
    {
        return _tournaments;
    }

    public Tournament? GetTournament(string tournamentId)
    {
        return _tournaments.FirstOrDefault(t => t.Id == tournamentId);
    }

    public bool TournamentExists(string tournamentId)
    {
        return _tournaments.Any(t => t.Id == tournamentId);
    }

    public TournamentRaceAssignments? GetTournamentAssignments(string tournamentId)
    {
        _assignments.TryGetValue(tournamentId, out var assignments);
        return assignments;
    }
}