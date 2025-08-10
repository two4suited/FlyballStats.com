using flyballstats.ApiService.Models;
using flyballstats.ApiService.Services;
using flyballstats.ApiService.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace flyballstats.Tests;

public class RaceAssignmentServiceTests
{
    private readonly FlyballStatsDbContext _context;
    private readonly TournamentDataService _tournamentDataService;
    private readonly RaceAssignmentService _raceAssignmentService;
    private readonly Mock<IRealTimeNotificationService> _mockNotificationService;
    private readonly Mock<ILogger<RaceAssignmentService>> _mockLogger;

    public RaceAssignmentServiceTests()
    {
        var options = new DbContextOptionsBuilder<FlyballStatsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new FlyballStatsDbContext(options);
        _tournamentDataService = new TournamentDataService(_context);
        _mockNotificationService = new Mock<IRealTimeNotificationService>();
        _mockLogger = new Mock<ILogger<RaceAssignmentService>>();
        _raceAssignmentService = new RaceAssignmentService(_context, _tournamentDataService, _mockNotificationService.Object, _mockLogger.Object);
    }

    [Fact]
    public void AssignRace_WithValidInput_ReturnsSuccess()
    {
        // Arrange
        var tournamentId = "test-tournament";
        var tournament = CreateTestTournament(tournamentId);
        var ringConfig = CreateTestRingConfiguration(tournamentId);
        
        _tournamentDataService.CreateOrUpdateTournament(tournamentId, tournament.Name, tournament.Races);
        _tournamentDataService.SaveRingConfiguration(tournamentId, ringConfig.Rings);

        // Act
        var result = _raceAssignmentService.AssignRace(tournamentId, 1, 1, RingStatus.Current);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Assignments);
        Assert.Equal(1, result.Assignments.Rings.First(r => r.RingNumber == 1).Current?.RaceNumber);
    }

    [Fact]
    public void AssignRace_RaceAlreadyCurrentInAnotherRing_ReturnsConflict()
    {
        // Arrange
        var tournamentId = "test-tournament";
        var tournament = CreateTestTournament(tournamentId);
        var ringConfig = CreateTestRingConfiguration(tournamentId);
        
        _tournamentDataService.CreateOrUpdateTournament(tournamentId, tournament.Name, tournament.Races);
        _tournamentDataService.SaveRingConfiguration(tournamentId, ringConfig.Rings);

        // Assign race 1 to ring 1 as current
        _raceAssignmentService.AssignRace(tournamentId, 1, 1, RingStatus.Current);

        // Act - Try to assign the same race to ring 2 as current
        var result = _raceAssignmentService.AssignRace(tournamentId, 1, 2, RingStatus.Current);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Conflicts);
        Assert.Contains("Race 1 is already current in ring(s): 1", result.Conflicts);
    }

    [Fact]
    public void AssignRace_WithConflictOverride_ReturnsSuccess()
    {
        // Arrange
        var tournamentId = "test-tournament";
        var tournament = CreateTestTournament(tournamentId);
        var ringConfig = CreateTestRingConfiguration(tournamentId);
        
        _tournamentDataService.CreateOrUpdateTournament(tournamentId, tournament.Name, tournament.Races);
        _tournamentDataService.SaveRingConfiguration(tournamentId, ringConfig.Rings);

        // Assign race 1 to ring 1 as current
        _raceAssignmentService.AssignRace(tournamentId, 1, 1, RingStatus.Current);

        // Act - Try to assign the same race to ring 2 as current with override
        var result = _raceAssignmentService.AssignRace(tournamentId, 1, 2, RingStatus.Current, allowConflictOverride: true);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Assignments);
        
        // Race should be assigned to ring 2 and removed from ring 1
        Assert.Equal(1, result.Assignments.Rings.First(r => r.RingNumber == 2).Current?.RaceNumber);
        Assert.Null(result.Assignments.Rings.First(r => r.RingNumber == 1).Current);
    }

    [Fact]
    public void AssignRace_RingSlotAlreadyOccupied_ReturnsConflict()
    {
        // Arrange
        var tournamentId = "test-tournament";
        var tournament = CreateTestTournament(tournamentId);
        var ringConfig = CreateTestRingConfiguration(tournamentId);
        
        _tournamentDataService.CreateOrUpdateTournament(tournamentId, tournament.Name, tournament.Races);
        _tournamentDataService.SaveRingConfiguration(tournamentId, ringConfig.Rings);

        // Assign race 1 to ring 1 as current
        _raceAssignmentService.AssignRace(tournamentId, 1, 1, RingStatus.Current);

        // Act - Try to assign race 2 to ring 1 current slot (already occupied)
        var result = _raceAssignmentService.AssignRace(tournamentId, 2, 1, RingStatus.Current);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Conflicts);
        Assert.Contains("Ring 1 Current slot is already occupied by race 1", result.Conflicts);
    }

    [Fact]
    public void ClearRing_WithValidRing_ReturnsSuccess()
    {
        // Arrange
        var tournamentId = "test-tournament";
        var tournament = CreateTestTournament(tournamentId);
        var ringConfig = CreateTestRingConfiguration(tournamentId);
        
        _tournamentDataService.CreateOrUpdateTournament(tournamentId, tournament.Name, tournament.Races);
        _tournamentDataService.SaveRingConfiguration(tournamentId, ringConfig.Rings);

        // Assign some races
        _raceAssignmentService.AssignRace(tournamentId, 1, 1, RingStatus.Current);
        _raceAssignmentService.AssignRace(tournamentId, 2, 1, RingStatus.OnDeck);

        // Act
        var result = _raceAssignmentService.ClearRing(tournamentId, 1);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Assignments);
        
        var ring = result.Assignments.Rings.First(r => r.RingNumber == 1);
        Assert.Null(ring.Current);
        Assert.Null(ring.OnDeck);
        Assert.Null(ring.InTheHole);
    }

    [Fact]
    public void AssignRace_NonexistentTournament_ReturnsError()
    {
        // Act
        var result = _raceAssignmentService.AssignRace("nonexistent", 1, 1, RingStatus.Current);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Tournament not found", result.Message);
    }

    [Fact]
    public void AssignRace_NonexistentRace_ReturnsError()
    {
        // Arrange
        var tournamentId = "test-tournament";
        var tournament = CreateTestTournament(tournamentId);
        var ringConfig = CreateTestRingConfiguration(tournamentId);
        
        _tournamentDataService.CreateOrUpdateTournament(tournamentId, tournament.Name, tournament.Races);
        _tournamentDataService.SaveRingConfiguration(tournamentId, ringConfig.Rings);

        // Act
        var result = _raceAssignmentService.AssignRace(tournamentId, 999, 1, RingStatus.Current);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Race 999 not found in tournament", result.Message);
    }

    [Fact]
    public void AssignRace_NonexistentRing_ReturnsError()
    {
        // Arrange
        var tournamentId = "test-tournament";
        var tournament = CreateTestTournament(tournamentId);
        var ringConfig = CreateTestRingConfiguration(tournamentId);
        
        _tournamentDataService.CreateOrUpdateTournament(tournamentId, tournament.Name, tournament.Races);
        _tournamentDataService.SaveRingConfiguration(tournamentId, ringConfig.Rings);

        // Act
        var result = _raceAssignmentService.AssignRace(tournamentId, 1, 999, RingStatus.Current);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Ring 999 not configured for this tournament", result.Message);
    }

    [Fact]
    public void GetTournamentAssignments_WithAssignments_ReturnsAssignments()
    {
        // Arrange
        var tournamentId = "test-tournament";
        var tournament = CreateTestTournament(tournamentId);
        var ringConfig = CreateTestRingConfiguration(tournamentId);
        
        _tournamentDataService.CreateOrUpdateTournament(tournamentId, tournament.Name, tournament.Races);
        _tournamentDataService.SaveRingConfiguration(tournamentId, ringConfig.Rings);

        // Assign a race
        _raceAssignmentService.AssignRace(tournamentId, 1, 1, RingStatus.Current);

        // Act
        var assignments = _raceAssignmentService.GetTournamentAssignments(tournamentId);

        // Assert
        Assert.NotNull(assignments);
        Assert.Equal(tournamentId, assignments.TournamentId);
        Assert.Equal(2, assignments.Rings.Count);
        Assert.Equal(1, assignments.Rings.First(r => r.RingNumber == 1).Current?.RaceNumber);
    }

    private Tournament CreateTestTournament(string tournamentId)
    {
        var races = new List<Race>
        {
            new(1, "Team A", "Team B", "Division 1"),
            new(2, "Team C", "Team D", "Division 1"),
            new(3, "Team E", "Team F", "Division 2"),
            new(4, "Team G", "Team H", "Division 2")
        };

        return new Tournament(tournamentId, "Test Tournament", races);
    }

    private TournamentRingConfiguration CreateTestRingConfiguration(string tournamentId)
    {
        var rings = new List<RingConfiguration>
        {
            new(1, "#ff0000"),
            new(2, "#00ff00")
        };

        return new TournamentRingConfiguration(tournamentId, rings);
    }
}