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