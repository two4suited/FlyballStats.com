using flyballstats.ApiService.Models;
using flyballstats.ApiService.Services;
using flyballstats.ApiService.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Diagnostics;
using Xunit;

namespace flyballstats.Tests;

public class RealTimePerformanceTests
{
    private readonly FlyballStatsDbContext _context;
    private readonly Mock<IRealTimeNotificationService> _mockNotificationService;
    private readonly Mock<ILogger<RaceAssignmentService>> _mockLogger;
    private readonly TournamentDataService _tournamentDataService;
    private readonly RaceAssignmentService _raceAssignmentService;

    public RealTimePerformanceTests()
    {
        var options = new DbContextOptionsBuilder<FlyballStatsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new FlyballStatsDbContext(options);
        _mockNotificationService = new Mock<IRealTimeNotificationService>();
        _mockLogger = new Mock<ILogger<RaceAssignmentService>>();
        _tournamentDataService = new TournamentDataService(_context);
        _raceAssignmentService = new RaceAssignmentService(_context, _tournamentDataService, _mockNotificationService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task AssignRaceAsync_MeetsP95LatencyRequirement()
    {
        // Arrange
        var tournamentId = "performance-test-tournament";
        var tournament = CreateTestTournament(tournamentId);
        var ringConfig = CreateTestRingConfiguration(tournamentId);
        
        _tournamentDataService.CreateOrUpdateTournament(tournamentId, tournament.Name, tournament.Races);
        _tournamentDataService.SaveRingConfiguration(tournamentId, ringConfig.Rings);

        var latencies = new List<long>();
        const int testIterations = 100; // Test 100 assignments to calculate P95
        const long maxP95LatencyMs = 3000; // 3 seconds as per requirement

        // Act - Perform multiple race assignments and measure latency
        for (int i = 0; i < testIterations; i++)
        {
            var raceNumber = (i % tournament.Races.Count) + 1;
            var ringNumber = (i % ringConfig.Rings.Count) + 1;
            var status = (RingStatus)(i % 3); // Cycle through Current, OnDeck, InTheHole

            var stopwatch = Stopwatch.StartNew();
            var result = await _raceAssignmentService.AssignRaceAsync(tournamentId, raceNumber, ringNumber, status);
            stopwatch.Stop();

            latencies.Add(stopwatch.ElapsedMilliseconds);
            Assert.True(result.Success, $"Assignment {i} failed: {result.Message}");
        }

        // Assert - Check P95 latency requirement
        latencies.Sort();
        var p95Index = (int)Math.Ceiling(0.95 * latencies.Count) - 1;
        var p95Latency = latencies[p95Index];

        Assert.True(p95Latency <= maxP95LatencyMs, 
            $"P95 latency {p95Latency}ms exceeds requirement of {maxP95LatencyMs}ms. " +
            $"Min: {latencies.Min()}ms, Max: {latencies.Max()}ms, Avg: {latencies.Average():F2}ms");

        // Verify that notifications were sent for all assignments
        _mockNotificationService.Verify(x => x.NotifyRaceAssignmentUpdated(
            It.IsAny<string>(), It.IsAny<TournamentRaceAssignments>()), 
            Times.Exactly(testIterations));
    }

    [Fact]
    public async Task ClearRingAsync_MeetsP95LatencyRequirement()
    {
        // Arrange
        var tournamentId = "clear-performance-test";
        var tournament = CreateTestTournament(tournamentId);
        var ringConfig = CreateTestRingConfiguration(tournamentId);
        
        _tournamentDataService.CreateOrUpdateTournament(tournamentId, tournament.Name, tournament.Races);
        _tournamentDataService.SaveRingConfiguration(tournamentId, ringConfig.Rings);

        // Pre-populate with some assignments
        await _raceAssignmentService.AssignRaceAsync(tournamentId, 1, 1, RingStatus.Current);
        await _raceAssignmentService.AssignRaceAsync(tournamentId, 2, 1, RingStatus.OnDeck);

        var latencies = new List<long>();
        const int testIterations = 50; // Test 50 clear operations
        const long maxP95LatencyMs = 3000; // 3 seconds as per requirement

        // Act - Perform multiple ring clears and measure latency
        for (int i = 0; i < testIterations; i++)
        {
            var ringNumber = (i % ringConfig.Rings.Count) + 1;

            var stopwatch = Stopwatch.StartNew();
            var result = await _raceAssignmentService.ClearRingAsync(tournamentId, ringNumber);
            stopwatch.Stop();

            latencies.Add(stopwatch.ElapsedMilliseconds);
            Assert.True(result.Success, $"Clear operation {i} failed: {result.Message}");

            // Re-add assignments for next iteration
            if (i < testIterations - 1)
            {
                await _raceAssignmentService.AssignRaceAsync(tournamentId, 1, ringNumber, RingStatus.Current);
            }
        }

        // Assert - Check P95 latency requirement
        latencies.Sort();
        var p95Index = (int)Math.Ceiling(0.95 * latencies.Count) - 1;
        var p95Latency = latencies[p95Index];

        Assert.True(p95Latency <= maxP95LatencyMs, 
            $"P95 latency for clear operations {p95Latency}ms exceeds requirement of {maxP95LatencyMs}ms. " +
            $"Min: {latencies.Min()}ms, Max: {latencies.Max()}ms, Avg: {latencies.Average():F2}ms");

        // Verify that notifications were sent for all clear operations
        _mockNotificationService.Verify(x => x.NotifyRingCleared(
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TournamentRaceAssignments>()), 
            Times.Exactly(testIterations));
    }

    [Fact]
    public async Task NotificationService_RecordsPerformanceMetrics()
    {
        // Arrange
        var tournamentId = "metrics-test";
        var tournament = CreateTestTournament(tournamentId);
        var ringConfig = CreateTestRingConfiguration(tournamentId);
        
        _tournamentDataService.CreateOrUpdateTournament(tournamentId, tournament.Name, tournament.Races);
        _tournamentDataService.SaveRingConfiguration(tournamentId, ringConfig.Rings);

        // Act
        await _raceAssignmentService.AssignRaceAsync(tournamentId, 1, 1, RingStatus.Current);

        // Assert - Verify that performance metrics were recorded
        _mockNotificationService.Verify(x => x.RecordLatencyMetric(
            "RaceAssignmentUpdate", It.IsAny<long>()), Times.Once);
    }

    [Fact]
    public async Task HighConcurrency_MaintainsPerformance()
    {
        // Arrange
        var tournamentId = "concurrency-test";
        var tournament = CreateTestTournament(tournamentId, raceCount: 20);
        var ringConfig = CreateTestRingConfiguration(tournamentId, ringCount: 5);
        
        _tournamentDataService.CreateOrUpdateTournament(tournamentId, tournament.Name, tournament.Races);
        _tournamentDataService.SaveRingConfiguration(tournamentId, ringConfig.Rings);

        const int concurrentOperations = 20;
        const long maxLatencyMs = 5000; // Allow slightly higher latency for concurrent operations

        // Act - Perform concurrent race assignments
        var tasks = new List<Task<(bool Success, long ElapsedMs)>>();
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < concurrentOperations; i++)
        {
            var raceNumber = (i % tournament.Races.Count) + 1;
            var ringNumber = (i % ringConfig.Rings.Count) + 1;
            var status = (RingStatus)(i % 3);

            tasks.Add(Task.Run(async () =>
            {
                var opStopwatch = Stopwatch.StartNew();
                var result = await _raceAssignmentService.AssignRaceAsync(tournamentId, raceNumber, ringNumber, status);
                opStopwatch.Stop();
                return (result.Success, opStopwatch.ElapsedMilliseconds);
            }));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var successCount = results.Count(r => r.Success);
        var latencies = results.Select(r => r.ElapsedMs).ToList();
        var maxLatency = latencies.Max();
        var avgLatency = latencies.Average();

        // At least 80% should succeed under concurrent load
        Assert.True(successCount >= concurrentOperations * 0.8, 
            $"Only {successCount}/{concurrentOperations} operations succeeded under concurrent load");

        // Maximum latency should still be reasonable
        Assert.True(maxLatency <= maxLatencyMs, 
            $"Maximum latency {maxLatency}ms under concurrent load exceeds {maxLatencyMs}ms threshold. " +
            $"Average: {avgLatency:F2}ms, Total time: {stopwatch.ElapsedMilliseconds}ms");
    }

    private Tournament CreateTestTournament(string tournamentId, int raceCount = 10)
    {
        var races = new List<Race>();
        for (int i = 1; i <= raceCount; i++)
        {
            races.Add(new Race(i, $"Team-{i}A", $"Team-{i}B", "Open"));
        }

        return new Tournament(tournamentId, $"Test Tournament {tournamentId}", races);
    }

    private TournamentRingConfiguration CreateTestRingConfiguration(string tournamentId, int ringCount = 3)
    {
        var rings = new List<RingConfiguration>();
        var colors = new[] { "#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#FF00FF" };
        
        for (int i = 1; i <= ringCount; i++)
        {
            rings.Add(new RingConfiguration(i, colors[(i - 1) % colors.Length]));
        }

        return new TournamentRingConfiguration(tournamentId, rings);
    }
}