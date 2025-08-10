using flyballstats.ApiService.Models;
using flyballstats.ApiService.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace flyballstats.ApiService.Services;

public class RaceAssignmentService
{
    private readonly FlyballStatsDbContext _context;
    private readonly TournamentDataService _tournamentDataService;
    private readonly IRealTimeNotificationService _notificationService;
    private readonly ILogger<RaceAssignmentService> _logger;

    public RaceAssignmentService(FlyballStatsDbContext context, TournamentDataService tournamentDataService, IRealTimeNotificationService notificationService, ILogger<RaceAssignmentService> logger)
    {
        _context = context;
        _tournamentDataService = tournamentDataService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<TournamentRaceAssignments?> GetTournamentAssignmentsAsync(string tournamentId)
    {
        var entity = await _context.RaceAssignments.FirstOrDefaultAsync(ra => ra.TournamentId == tournamentId);
        return entity != null ? new TournamentRaceAssignments(entity.TournamentId, entity.Rings, entity.LastUpdated) : null;
    }

    public TournamentRaceAssignments? GetTournamentAssignments(string tournamentId)
    {
        var entity = _context.RaceAssignments.FirstOrDefault(ra => ra.TournamentId == tournamentId);
        return entity != null ? new TournamentRaceAssignments(entity.TournamentId, entity.Rings, entity.LastUpdated) : null;
    }

    public async Task<AssignRaceResponse> AssignRaceAsync(string tournamentId, int raceNumber, int ringNumber, RingStatus status, bool allowConflictOverride = false)
    {
        var operationStopwatch = Stopwatch.StartNew();
        try
        {
            // Validate tournament exists
            var tournament = await _tournamentDataService.GetTournamentAsync(tournamentId);
            if (tournament == null)
            {
                return new AssignRaceResponse(false, "Tournament not found", null, null);
            }

            // Validate race exists
            var race = tournament.Races.FirstOrDefault(r => r.RaceNumber == raceNumber);
            if (race == null)
            {
                return new AssignRaceResponse(false, $"Race {raceNumber} not found in tournament", null, null);
            }

            // Validate ring configuration exists
            var ringConfig = await _tournamentDataService.GetRingConfigurationAsync(tournamentId);
            if (ringConfig == null || !ringConfig.Rings.Any(r => r.RingNumber == ringNumber))
            {
                return new AssignRaceResponse(false, $"Ring {ringNumber} not configured for this tournament", null, null);
            }

            // Get or create tournament assignments
            var assignments = await GetOrCreateTournamentAssignmentsAsync(tournamentId, ringConfig);

            // Check for conflicts
            var conflicts = CheckForConflicts(assignments, raceNumber, ringNumber, status);
            if (conflicts.Any() && !allowConflictOverride)
            {
                return new AssignRaceResponse(false, "Race assignment conflicts detected", conflicts, assignments);
            }

            // Remove any existing assignments for this race
            RemoveRaceFromAllRings(assignments, raceNumber);

            // Assign the race to the specified ring and status
            var targetRing = assignments.Rings.First(r => r.RingNumber == ringNumber);
            var newAssignment = new RaceAssignment(raceNumber, ringNumber, status);

            var updatedRing = status switch
            {
                RingStatus.Current => targetRing with { Current = newAssignment },
                RingStatus.OnDeck => targetRing with { OnDeck = newAssignment },
                RingStatus.InTheHole => targetRing with { InTheHole = newAssignment },
                _ => targetRing
            };

            // Update the ring in the assignments
            var ringIndex = assignments.Rings.FindIndex(r => r.RingNumber == ringNumber);
            assignments.Rings[ringIndex] = updatedRing;

            // Update timestamp
            var updatedAssignments = assignments with { LastUpdated = DateTime.UtcNow };
            
            // Save to database
            await SaveTournamentAssignmentsAsync(updatedAssignments);

            // Send real-time notification
            await _notificationService.NotifyRaceAssignmentUpdated(tournamentId, updatedAssignments);

            operationStopwatch.Stop();
            _logger.LogInformation("Race assignment completed in {ElapsedMs}ms for tournament {TournamentId}", operationStopwatch.ElapsedMilliseconds, tournamentId);

            return new AssignRaceResponse(true, "Race assigned successfully", null, updatedAssignments);
        }
        catch (Exception ex)
        {
            operationStopwatch.Stop();
            _logger.LogError(ex, "Error assigning race {RaceNumber} to ring {RingNumber} in tournament {TournamentId} after {ElapsedMs}ms", 
                raceNumber, ringNumber, tournamentId, operationStopwatch.ElapsedMilliseconds);
            return new AssignRaceResponse(false, $"An error occurred: {ex.Message}", null, null);
        }
    }

    // Keep synchronous version for backwards compatibility
    public AssignRaceResponse AssignRace(string tournamentId, int raceNumber, int ringNumber, RingStatus status, bool allowConflictOverride = false)
    {
        return AssignRaceAsync(tournamentId, raceNumber, ringNumber, status, allowConflictOverride).GetAwaiter().GetResult();
    }

    public async Task<ClearRingResponse> ClearRingAsync(string tournamentId, int ringNumber)
    {
        var operationStopwatch = Stopwatch.StartNew();
        try
        {
            // Validate tournament exists
            var tournament = await _tournamentDataService.GetTournamentAsync(tournamentId);
            if (tournament == null)
            {
                return new ClearRingResponse(false, "Tournament not found", null);
            }

            // Get assignments
            var assignments = await GetTournamentAssignmentsAsync(tournamentId);
            if (assignments == null)
            {
                return new ClearRingResponse(false, "No assignments found for tournament", null);
            }

            // Find and clear the ring
            var ringIndex = assignments.Rings.FindIndex(r => r.RingNumber == ringNumber);
            if (ringIndex == -1)
            {
                return new ClearRingResponse(false, $"Ring {ringNumber} not found", null);
            }

            var ring = assignments.Rings[ringIndex];
            var clearedRing = ring with { Current = null, OnDeck = null, InTheHole = null };
            assignments.Rings[ringIndex] = clearedRing;

            // Update timestamp
            var updatedAssignments = assignments with { LastUpdated = DateTime.UtcNow };
            
            // Save to database
            await SaveTournamentAssignmentsAsync(updatedAssignments);

            // Send real-time notification
            await _notificationService.NotifyRingCleared(tournamentId, ringNumber, updatedAssignments);

            operationStopwatch.Stop();
            _logger.LogInformation("Ring clear completed in {ElapsedMs}ms for ring {RingNumber} in tournament {TournamentId}", 
                operationStopwatch.ElapsedMilliseconds, ringNumber, tournamentId);

            return new ClearRingResponse(true, "Ring cleared successfully", updatedAssignments);
        }
        catch (Exception ex)
        {
            operationStopwatch.Stop();
            _logger.LogError(ex, "Error clearing ring {RingNumber} in tournament {TournamentId} after {ElapsedMs}ms", 
                ringNumber, tournamentId, operationStopwatch.ElapsedMilliseconds);
            return new ClearRingResponse(false, $"An error occurred: {ex.Message}", null);
        }
    }

    // Keep synchronous version for backwards compatibility
    public ClearRingResponse ClearRing(string tournamentId, int ringNumber)
    {
        return ClearRingAsync(tournamentId, ringNumber).GetAwaiter().GetResult();
    }

    private async Task<TournamentRaceAssignments> GetOrCreateTournamentAssignmentsAsync(string tournamentId, TournamentRingConfiguration ringConfig)
    {
        var entity = await _context.RaceAssignments.FirstOrDefaultAsync(ra => ra.TournamentId == tournamentId);
        
        if (entity != null)
        {
            return new TournamentRaceAssignments(entity.TournamentId, entity.Rings, entity.LastUpdated);
        }

        // Create new assignments
        var rings = ringConfig.Rings.Select(r => new RingRaceAssignments(
            r.RingNumber,
            r.Color,
            null, // Current
            null, // OnDeck
            null  // InTheHole
        )).ToList();

        var newAssignments = new TournamentRaceAssignments(tournamentId, rings, DateTime.UtcNow);
        await SaveTournamentAssignmentsAsync(newAssignments);
        
        return newAssignments;
    }

    private async Task SaveTournamentAssignmentsAsync(TournamentRaceAssignments assignments)
    {
        var existingEntity = await _context.RaceAssignments.FirstOrDefaultAsync(ra => ra.TournamentId == assignments.TournamentId);
        
        if (existingEntity != null)
        {
            existingEntity.Rings = assignments.Rings;
            existingEntity.LastUpdated = assignments.LastUpdated;
            _context.RaceAssignments.Update(existingEntity);
        }
        else
        {
            var newEntity = new TournamentRaceAssignmentsEntity
            {
                Id = assignments.TournamentId,
                TournamentId = assignments.TournamentId,
                Rings = assignments.Rings,
                LastUpdated = assignments.LastUpdated
            };
            _context.RaceAssignments.Add(newEntity);
        }

        await _context.SaveChangesAsync();
    }

    private List<string> CheckForConflicts(TournamentRaceAssignments assignments, int raceNumber, int ringNumber, RingStatus status)
    {
        var conflicts = new List<string>();

        // Check if race is already assigned as Current in another ring
        if (status == RingStatus.Current)
        {
            var existingCurrentRings = assignments.Rings
                .Where(r => r.RingNumber != ringNumber && r.Current?.RaceNumber == raceNumber)
                .ToList();

            if (existingCurrentRings.Any())
            {
                var ringNumbers = string.Join(", ", existingCurrentRings.Select(r => r.RingNumber));
                conflicts.Add($"Race {raceNumber} is already current in ring(s): {ringNumbers}");
            }
        }

        // Check if the target ring status slot is already occupied
        var targetRing = assignments.Rings.FirstOrDefault(r => r.RingNumber == ringNumber);
        if (targetRing != null)
        {
            var existingAssignment = status switch
            {
                RingStatus.Current => targetRing.Current,
                RingStatus.OnDeck => targetRing.OnDeck,
                RingStatus.InTheHole => targetRing.InTheHole,
                _ => null
            };

            if (existingAssignment != null)
            {
                conflicts.Add($"Ring {ringNumber} {status} slot is already occupied by race {existingAssignment.RaceNumber}");
            }
        }

        return conflicts;
    }

    private void RemoveRaceFromAllRings(TournamentRaceAssignments assignments, int raceNumber)
    {
        for (int i = 0; i < assignments.Rings.Count; i++)
        {
            var ring = assignments.Rings[i];
            var updatedRing = ring;

            if (ring.Current?.RaceNumber == raceNumber)
                updatedRing = updatedRing with { Current = null };
            if (ring.OnDeck?.RaceNumber == raceNumber)
                updatedRing = updatedRing with { OnDeck = null };
            if (ring.InTheHole?.RaceNumber == raceNumber)
                updatedRing = updatedRing with { InTheHole = null };

            assignments.Rings[i] = updatedRing;
        }
    }
}