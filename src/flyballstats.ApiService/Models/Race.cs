namespace flyballstats.ApiService.Models;

public record Race(
    int RaceNumber,
    string LeftTeam,
    string RightTeam,
    string Division);

public record Tournament(
    string Id,
    string Name,
    List<Race> Races);

public record ValidationError(
    int RowNumber,
    string Field,
    string Value,
    string Reason);

public record CsvValidationResult(
    bool IsValid,
    List<ValidationError> Errors,
    List<Race> ValidRaces);

public record CsvUploadRequest(
    string TournamentId,
    string TournamentName,
    string CsvContent);

public record CsvUploadResponse(
    bool Success,
    string? Message,
    List<ValidationError>? Errors,
    int? RacesImported);

public record RingConfiguration(
    int RingNumber,
    string Color);

public record TournamentRingConfiguration(
    string TournamentId,
    List<RingConfiguration> Rings);

public record RingConfigurationRequest(
    string TournamentId,
    List<RingConfiguration> Rings);

public record RingConfigurationResponse(
    bool Success,
    string? Message,
    TournamentRingConfiguration? Configuration);

public record TournamentExistsResponse(
    bool Exists);

// Race Assignment Models
public enum RingStatus
{
    Idle,
    Current,
    OnDeck,
    InTheHole
}

public record RaceAssignment(
    int RaceNumber,
    int RingNumber,
    RingStatus Status);

public record RingRaceAssignments(
    int RingNumber,
    string Color,
    RaceAssignment? Current,
    RaceAssignment? OnDeck,
    RaceAssignment? InTheHole);

public record TournamentRaceAssignments(
    string TournamentId,
    List<RingRaceAssignments> Rings,
    DateTime LastUpdated);

public record AssignRaceRequest(
    string TournamentId,
    int RaceNumber,
    int RingNumber,
    RingStatus Status,
    bool AllowConflictOverride = false);

public record AssignRaceResponse(
    bool Success,
    string? Message,
    List<string>? Conflicts,
    TournamentRaceAssignments? Assignments);

public record ClearRingRequest(
    string TournamentId,
    int RingNumber);

public record ClearRingResponse(
    bool Success,
    string? Message,
    TournamentRaceAssignments? Assignments);