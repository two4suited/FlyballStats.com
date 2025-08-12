using flyballstats.ApiService.Data;
using flyballstats.ApiService.Models;
using flyballstats.ApiService.Services;
using flyballstats.Tests.Fixtures.TestDataBuilders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace flyballstats.Tests.Integration;

public class CsvImportEndToEndTests : IDisposable
{
    private readonly FlyballStatsDbContext _context;
    private readonly CsvValidationService _validationService;
    private readonly TournamentDataService _dataService;
    private readonly ApplicationMetrics _metrics;

    public CsvImportEndToEndTests()
    {
        // Use in-memory database for testing
        var options = new DbContextOptionsBuilder<FlyballStatsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new FlyballStatsDbContext(options);
        _metrics = new ApplicationMetrics();
        _validationService = new CsvValidationService();
        _dataService = new TournamentDataService(_context, _metrics);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region Successful Import Flow Tests

    [Fact]
    public async Task ImportCsv_WithValidData_SuccessfullyPersistsToDatabase()
    {
        // Arrange
        var tournamentId = "test-tournament-1";
        var tournamentName = "Test Tournament";
        var csv = CsvTestDataBuilder.Create()
            .WithValidRace(1, "Team Alpha", "Team Beta")
            .WithValidRace(2, "Team Gamma", "Team Delta")
            .WithValidRace(3, "Team Echo", "Team Foxtrot")
            .Build();

        // Act
        var validationResult = _validationService.ValidateCsv(csv);
        Assert.True(validationResult.IsValid);

        var tournament = await _dataService.CreateOrUpdateTournamentAsync(
            tournamentId, tournamentName, validationResult.ValidRaces);

        // Assert
        Assert.NotNull(tournament);
        Assert.Equal(tournamentId, tournament.Id);
        Assert.Equal(tournamentName, tournament.Name);
        Assert.Equal(3, tournament.Races.Count);

        // Verify data persisted in database
        var persistedTournament = await _context.Tournaments
            .FirstOrDefaultAsync(t => t.Id == tournamentId);
        
        Assert.NotNull(persistedTournament);
        Assert.Equal(3, persistedTournament.Races.Count);
        
        var firstRace = persistedTournament.Races.OrderBy(r => r.RaceNumber).First();
        Assert.Equal(1, firstRace.RaceNumber);
        Assert.Equal("Team Alpha", firstRace.LeftTeam);
        Assert.Equal("Team Beta", firstRace.RightTeam);
    }

    [Fact]
    public async Task ImportCsv_WithLargeDataset_SuccessfullyPersists()
    {
        // Arrange
        var tournamentId = "large-tournament";
        var tournamentName = "Large Tournament";
        
        var builder = CsvTestDataBuilder.Create();
        for (int i = 1; i <= 500; i++)
        {
            builder.WithValidRace(i, $"Team Left {i}", $"Team Right {i}");
        }
        var csv = builder.Build();

        // Act
        var validationResult = _validationService.ValidateCsv(csv);
        Assert.True(validationResult.IsValid);

        var tournament = await _dataService.CreateOrUpdateTournamentAsync(
            tournamentId, tournamentName, validationResult.ValidRaces);

        // Assert
        Assert.Equal(500, tournament.Races.Count);
        
        var persistedTournament = await _context.Tournaments
            .FirstOrDefaultAsync(t => t.Id == tournamentId);
        Assert.NotNull(persistedTournament);
        Assert.Equal(500, persistedTournament.Races.Count);
    }

    [Fact]
    public async Task ImportCsv_UpdateExistingTournament_ReplacesRaces()
    {
        // Arrange
        var tournamentId = "update-tournament";
        var tournamentName = "Tournament to Update";
        
        // First import
        var initialCsv = CsvTestDataBuilder.Create()
            .WithValidRace(1, "Initial Team A", "Initial Team B")
            .WithValidRace(2, "Initial Team C", "Initial Team D")
            .Build();

        var initialValidation = _validationService.ValidateCsv(initialCsv);
        await _dataService.CreateOrUpdateTournamentAsync(
            tournamentId, tournamentName, initialValidation.ValidRaces);

        // Updated data
        var updatedCsv = CsvTestDataBuilder.Create()
            .WithValidRace(1, "Updated Team A", "Updated Team B")
            .WithValidRace(2, "Updated Team C", "Updated Team D")
            .WithValidRace(3, "New Team E", "New Team F")
            .Build();

        // Act
        var updatedValidation = _validationService.ValidateCsv(updatedCsv);
        var updatedTournament = await _dataService.CreateOrUpdateTournamentAsync(
            tournamentId, tournamentName, updatedValidation.ValidRaces);

        // Assert
        Assert.Equal(3, updatedTournament.Races.Count);
        
        var persistedTournament = await _context.Tournaments
            .FirstOrDefaultAsync(t => t.Id == tournamentId);
        Assert.NotNull(persistedTournament);
        Assert.Equal(3, persistedTournament.Races.Count);
        
        // Verify data was replaced, not appended
        var firstRace = persistedTournament.Races.First(r => r.RaceNumber == 1);
        Assert.Equal("Updated Team A", firstRace.LeftTeam);
        Assert.Equal("Updated Team B", firstRace.RightTeam);
    }

    #endregion

    #region Failed Import Handling Tests

    [Fact]
    public async Task ImportCsv_WithInvalidData_DoesNotPersistAnyData()
    {
        // Arrange
        var tournamentId = "invalid-tournament";
        var tournamentName = "Invalid Tournament";
        var csv = CsvTestDataBuilder.Create()
            .WithValidRace(1, "Valid Team A", "Valid Team B")
            .WithInvalidRace("invalid", "", "Invalid Team D") // Invalid row
            .Build();

        // Act
        var validationResult = _validationService.ValidateCsv(csv);

        // Assert
        Assert.False(validationResult.IsValid);
        Assert.True(validationResult.Errors.Count > 0);
        Assert.Single(validationResult.ValidRaces); // Only one valid race

        // Verify no data persisted due to validation failure
        var persistedTournament = await _context.Tournaments
            .FirstOrDefaultAsync(t => t.Id == tournamentId);
        Assert.Null(persistedTournament);
    }

    [Fact]
    public async Task ImportCsv_DatabaseError_MaintenanceConsistentState()
    {
        // Arrange
        var tournamentId = "error-tournament";
        var tournamentName = "Error Tournament";
        var csv = CsvTestDataBuilder.Create()
            .WithValidRace(1, "Team A", "Team B")
            .Build();

        var validationResult = _validationService.ValidateCsv(csv);
        Assert.True(validationResult.IsValid);

        // Dispose the context to simulate database error
        _context.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
        {
            await _dataService.CreateOrUpdateTournamentAsync(
                tournamentId, tournamentName, validationResult.ValidRaces);
        });
    }

    #endregion

    #region Tournament Association Tests

    [Fact]
    public async Task ImportCsv_WithTournamentScope_IsolatesDataCorrectly()
    {
        // Arrange
        var tournament1Id = "tournament-1";
        var tournament2Id = "tournament-2";
        
        var csv1 = CsvTestDataBuilder.Create()
            .WithValidRace(1, "T1 Team A", "T1 Team B")
            .WithValidRace(2, "T1 Team C", "T1 Team D")
            .Build();
            
        var csv2 = CsvTestDataBuilder.Create()
            .WithValidRace(1, "T2 Team A", "T2 Team B")
            .WithValidRace(2, "T2 Team C", "T2 Team D")
            .Build();

        // Act
        var validation1 = _validationService.ValidateCsv(csv1);
        var validation2 = _validationService.ValidateCsv(csv2);
        
        await _dataService.CreateOrUpdateTournamentAsync(
            tournament1Id, "Tournament 1", validation1.ValidRaces);
        await _dataService.CreateOrUpdateTournamentAsync(
            tournament2Id, "Tournament 2", validation2.ValidRaces);

        // Assert
        var tournament1 = await _dataService.GetTournamentAsync(tournament1Id);
        var tournament2 = await _dataService.GetTournamentAsync(tournament2Id);
        
        Assert.NotNull(tournament1);
        Assert.NotNull(tournament2);
        Assert.Equal(2, tournament1.Races.Count);
        Assert.Equal(2, tournament2.Races.Count);
        
        // Verify data isolation
        Assert.Equal("T1 Team A", tournament1.Races.First().LeftTeam);
        Assert.Equal("T2 Team A", tournament2.Races.First().LeftTeam);
    }

    [Fact]
    public async Task ImportCsv_RetrieveRacesAfterImport_VerifiesCorrectData()
    {
        // Arrange
        var tournamentId = "retrieve-tournament";
        var csv = CsvTestDataBuilder.Create()
            .WithValidRace(5, "Retrieval Team A", "Retrieval Team B", "Elite")
            .WithValidRace(10, "Retrieval Team C", "Retrieval Team D", "Regular")
            .Build();

        // Act
        var validationResult = _validationService.ValidateCsv(csv);
        await _dataService.CreateOrUpdateTournamentAsync(
            tournamentId, "Retrieval Tournament", validationResult.ValidRaces);

        var retrievedTournament = await _dataService.GetTournamentAsync(tournamentId);

        // Assert
        Assert.NotNull(retrievedTournament);
        Assert.Equal(2, retrievedTournament.Races.Count);
        
        var race5 = retrievedTournament.Races.First(r => r.RaceNumber == 5);
        var race10 = retrievedTournament.Races.First(r => r.RaceNumber == 10);
        
        Assert.Equal("Retrieval Team A", race5.LeftTeam);
        Assert.Equal("Elite", race5.Division);
        Assert.Equal("Retrieval Team C", race10.LeftTeam);
        Assert.Equal("Regular", race10.Division);
    }

    #endregion

    #region Data Consistency Tests

    [Fact]
    public async Task ImportCsv_VerifyRaceOrderPreservation()
    {
        // Arrange
        var tournamentId = "order-tournament";
        var csv = CsvTestDataBuilder.Create()
            .WithValidRace(10, "Team J", "Team K")
            .WithValidRace(5, "Team E", "Team F")
            .WithValidRace(1, "Team A", "Team B")
            .WithValidRace(15, "Team O", "Team P")
            .Build();

        // Act
        var validationResult = _validationService.ValidateCsv(csv);
        await _dataService.CreateOrUpdateTournamentAsync(
            tournamentId, "Order Tournament", validationResult.ValidRaces);

        var tournament = await _dataService.GetTournamentAsync(tournamentId);

        // Assert
        Assert.NotNull(tournament);
        Assert.Equal(4, tournament.Races.Count);
        
        // Verify races are stored in the order they appear in CSV, not sorted by race number
        var raceNumbers = tournament.Races.Select(r => r.RaceNumber).ToList();
        Assert.Equal(new[] { 10, 5, 1, 15 }, raceNumbers);
    }

    [Fact]
    public async Task ImportCsv_VerifyDataTypes_PreservesAllInformation()
    {
        // Arrange
        var tournamentId = "datatype-tournament";
        var csv = CsvTestDataBuilder.Create()
            .WithValidRace(42, "Special-Characters_123", "Team with Spaces", "Custom Division Name")
            .Build();

        // Act
        var validationResult = _validationService.ValidateCsv(csv);
        await _dataService.CreateOrUpdateTournamentAsync(
            tournamentId, "DataType Tournament", validationResult.ValidRaces);

        var tournament = await _dataService.GetTournamentAsync(tournamentId);

        // Assert
        Assert.NotNull(tournament);
        var race = tournament.Races.Single();
        
        Assert.Equal(42, race.RaceNumber);
        Assert.Equal("Special-Characters_123", race.LeftTeam);
        Assert.Equal("Team with Spaces", race.RightTeam);
        Assert.Equal("Custom Division Name", race.Division);
    }

    #endregion
}