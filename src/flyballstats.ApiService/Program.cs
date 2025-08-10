using flyballstats.ApiService.Models;
using flyballstats.ApiService.Services;
using flyballstats.ApiService.Hubs;
using flyballstats.ApiService.Data;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add Cosmos DB via Aspire
 builder.AddCosmosDbContext<FlyballStatsDbContext>("cosmos-db", "flyballstats");

// Add services to the container.
builder.Services.AddProblemDetails();

// Add custom services
builder.Services.AddSingleton<CsvValidationService>();
builder.Services.AddSingleton<MockTournamentDataService>();
 builder.Services.AddScoped<TournamentDataService>();
builder.Services.AddScoped<RaceAssignmentService>();

// Add real-time services
builder.Services.AddSingleton<IRealTimeNotificationService, SignalRNotificationService>();
builder.Services.AddSignalR()
     .AddNamedAzureSignalR("signalr");

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Tournament and CSV upload endpoints
/*
app.MapPost("/tournaments/upload-csv", (CsvUploadRequest request, CsvValidationService validationService, TournamentDataService dataService) =>
{
    try
    {
        // Validate the CSV content
        var validationResult = validationService.ValidateCsv(request.CsvContent);

        if (!validationResult.IsValid)
        {
            return Results.BadRequest(new CsvUploadResponse(false, "CSV validation failed", validationResult.Errors, null));
        }

        // Store the tournament with races
        var tournament = dataService.CreateOrUpdateTournament(request.TournamentId, request.TournamentName, validationResult.ValidRaces);

        return Results.Ok(new CsvUploadResponse(true, $"Successfully imported {validationResult.ValidRaces.Count} races", null, validationResult.ValidRaces.Count));
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred: {ex.Message}");
    }
})
.WithName("UploadTournamentCsv");
*/

app.MapGet("/tournaments", (MockTournamentDataService dataService) =>
{
    return Results.Ok(dataService.GetAllTournaments());
})
.WithName("GetTournaments");

app.MapGet("/tournaments/{tournamentId}", (string tournamentId, MockTournamentDataService dataService) =>
{
    var tournament = dataService.GetTournament(tournamentId);
    return tournament != null ? Results.Ok(tournament) : Results.NotFound();
})
.WithName("GetTournament");

app.MapGet("/tournaments/{tournamentId}/exists", (string tournamentId, MockTournamentDataService dataService) =>
{
    var exists = dataService.TournamentExists(tournamentId);
    return Results.Ok(new { Exists = exists });
})
.WithName("CheckTournamentExists");



// Ring configuration endpoints
app.MapPost("/tournaments/{tournamentId}/rings", (string tournamentId, RingConfigurationRequest request, TournamentDataService dataService) =>
{
    try
    {
        // Validate ring configuration
        if (request.Rings.Count == 0 || request.Rings.Count > 10)
        {
            return Results.BadRequest(new RingConfigurationResponse(false, "Tournament must have between 1 and 10 rings", null));
        }

        // Check for duplicate colors
        var colors = request.Rings.Select(r => r.Color).ToList();
        if (colors.Distinct().Count() != colors.Count)
        {
            return Results.BadRequest(new RingConfigurationResponse(false, "Ring colors must be unique", null));
        }

        // Check for valid ring numbers (1-10)
        if (request.Rings.Any(r => r.RingNumber < 1 || r.RingNumber > 10))
        {
            return Results.BadRequest(new RingConfigurationResponse(false, "Ring numbers must be between 1 and 10", null));
        }

        // Check for duplicate ring numbers
        var ringNumbers = request.Rings.Select(r => r.RingNumber).ToList();
        if (ringNumbers.Distinct().Count() != ringNumbers.Count)
        {
            return Results.BadRequest(new RingConfigurationResponse(false, "Ring numbers must be unique", null));
        }

        var configuration = dataService.SaveRingConfiguration(tournamentId, request.Rings);
        return Results.Ok(new RingConfigurationResponse(true, "Ring configuration saved successfully", configuration));
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred: {ex.Message}");
    }
})
.WithName("SetRingConfiguration");

app.MapGet("/tournaments/{tournamentId}/rings", (string tournamentId, TournamentDataService dataService) =>
{
    var configuration = dataService.GetRingConfiguration(tournamentId);
    return configuration != null ? Results.Ok(configuration) : Results.NotFound();
})
.WithName("GetRingConfiguration");

// Race Assignment endpoints
app.MapPost("/tournaments/{tournamentId}/races/assign", async (string tournamentId, AssignRaceRequest request, RaceAssignmentService assignmentService) =>
{
    try
    {
        var result = await assignmentService.AssignRaceAsync(request.TournamentId, request.RaceNumber, request.RingNumber, request.Status, request.AllowConflictOverride);
        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred: {ex.Message}");
    }
})
.WithName("AssignRaceToRing");

app.MapGet("/tournaments/{tournamentId}/assignments", (string tournamentId, RaceAssignmentService assignmentService) =>
{
    var assignments = assignmentService.GetTournamentAssignments(tournamentId);
    return assignments != null ? Results.Ok(assignments) : Results.NotFound();
})
.WithName("GetTournamentAssignments");

app.MapPost("/tournaments/{tournamentId}/rings/{ringNumber}/clear", async (string tournamentId, int ringNumber, RaceAssignmentService assignmentService) =>
{
    try
    {
        var result = await assignmentService.ClearRingAsync(tournamentId, ringNumber);
        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred: {ex.Message}");
    }
})
.WithName("ClearRing");


// Map SignalR hub
 app.MapHub<RaceAssignmentHub>("/racehub");

app.MapDefaultEndpoints();

app.Run();


