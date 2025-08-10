using flyballstats.ApiService.Models;
using flyballstats.ApiService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Add custom services
builder.Services.AddSingleton<CsvValidationService>();
builder.Services.AddSingleton<TournamentDataService>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Tournament and CSV upload endpoints
app.MapPost("/tournaments/upload-csv", async (CsvUploadRequest request, CsvValidationService validationService, TournamentDataService dataService) =>
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

app.MapGet("/tournaments", (TournamentDataService dataService) =>
{
    return Results.Ok(dataService.GetAllTournaments());
})
.WithName("GetTournaments");

app.MapGet("/tournaments/{tournamentId}", (string tournamentId, TournamentDataService dataService) =>
{
    var tournament = dataService.GetTournament(tournamentId);
    return tournament != null ? Results.Ok(tournament) : Results.NotFound();
})
.WithName("GetTournament");

app.MapDefaultEndpoints();

app.Run();


