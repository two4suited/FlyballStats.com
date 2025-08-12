using flyballstats.ApiService.Models;
using flyballstats.ApiService.Services;
using flyballstats.ApiService.Hubs;
using flyballstats.ApiService.Data;
using flyballstats.ApiService.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add dependency-specific health checks
builder.AddCosmosDbHealthCheck();
builder.AddSignalRHealthCheck();

// Add Cosmos DB via Aspire
builder.AddCosmosDbContext<FlyballStatsDbContext>("cosmos-db", "flyballstats");

// Add authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "your-256-bit-secret-key-that-is-long-enough-for-jwt-signing-minimum-32-characters";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "FlyballStats";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "FlyballStats";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("Authentication failed: {Error}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("Authentication challenge: {Error}", context.Error);
                return Task.CompletedTask;
            }
        };
    });

// Add authorization with custom policies
builder.Services.AddAuthorization(options =>
{
    // Director policies
    options.AddPolicy("Director_access_api", policy =>
        policy.Requirements.Add(new RoleRequirement(UserRole.Director, "access", "api")));
    options.AddPolicy("Director_manage_tournaments", policy =>
        policy.Requirements.Add(new RoleRequirement(UserRole.Director, "manage_tournaments", "tournaments")));
    options.AddPolicy("Director_upload_csv", policy =>
        policy.Requirements.Add(new RoleRequirement(UserRole.Director, "upload_csv", "tournaments")));
    options.AddPolicy("Director_configure_rings", policy =>
        policy.Requirements.Add(new RoleRequirement(UserRole.Director, "configure_rings", "rings")));

    // Race Director policies
    options.AddPolicy("RaceDirector_access_api", policy =>
        policy.Requirements.Add(new RoleRequirement(UserRole.RaceDirector, "access", "api")));
    options.AddPolicy("RaceDirector_assign_race", policy =>
        policy.Requirements.Add(new RoleRequirement(UserRole.RaceDirector, "assign_race", "assignments")));
    options.AddPolicy("RaceDirector_clear_ring", policy =>
        policy.Requirements.Add(new RoleRequirement(UserRole.RaceDirector, "clear_ring", "assignments")));

    // Viewer policies
    options.AddPolicy("Viewer_access_api", policy =>
        policy.Requirements.Add(new RoleRequirement(UserRole.Viewer, "access", "api")));
    options.AddPolicy("Viewer_view", policy =>
        policy.Requirements.Add(new RoleRequirement(UserRole.Viewer, "view", "tournaments")));
});

// Add custom authorization handler
builder.Services.AddSingleton<IAuthorizationHandler, RoleAuthorizationHandler>();

// Add HTTP context accessor for authorization logging
builder.Services.AddHttpContextAccessor();

// Add services to the container.
builder.Services.AddProblemDetails();

// Add custom services
builder.Services.AddSingleton<CsvValidationService>();
builder.Services.AddSingleton<MockTournamentDataService>();
builder.Services.AddScoped<TournamentDataService>();
builder.Services.AddScoped<RaceAssignmentService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<flyballstats.ApiService.Services.IAuthorizationService, flyballstats.ApiService.Services.AuthorizationService>();
builder.Services.AddScoped<IDataSeedingService, DataSeedingService>();

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

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Authentication endpoints
app.MapPost("/auth/login", async (LoginRequest request, IAuthenticationService authService) =>
{
    var result = await authService.LoginAsync(request);
    return result.Success ? Results.Ok(result) : Results.Unauthorized();
})
.WithName("Login")
.AllowAnonymous();

// Tournament and CSV upload endpoints (require Director role)
app.MapGet("/tournaments", [RequireViewer("view", "tournaments")] (MockTournamentDataService dataService) =>
{
    return Results.Ok(dataService.GetAllTournaments());
})
.WithName("GetTournaments");

app.MapGet("/tournaments/{tournamentId}", [RequireViewer("view", "tournaments")] (string tournamentId, MockTournamentDataService dataService) =>
{
    var tournament = dataService.GetTournament(tournamentId);
    return tournament != null ? Results.Ok(tournament) : Results.NotFound();
})
.WithName("GetTournament");

app.MapGet("/tournaments/{tournamentId}/exists", [RequireViewer("view", "tournaments")] (string tournamentId, MockTournamentDataService dataService) =>
{
    var exists = dataService.TournamentExists(tournamentId);
    return Results.Ok(new { Exists = exists });
})
.WithName("CheckTournamentExists");

// Ring configuration endpoints (require Director role)
app.MapPost("/tournaments/{tournamentId}/rings", [RequireDirector("configure_rings", "rings")] (string tournamentId, RingConfigurationRequest request, TournamentDataService dataService) =>
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

app.MapGet("/tournaments/{tournamentId}/rings", [RequireViewer("view", "rings")] (string tournamentId, TournamentDataService dataService) =>
{
    var configuration = dataService.GetRingConfiguration(tournamentId);
    return configuration != null ? Results.Ok(configuration) : Results.NotFound();
})
.WithName("GetRingConfiguration");

// Race Assignment endpoints (require Race Director role)
app.MapPost("/tournaments/{tournamentId}/races/assign", [RequireRaceDirector("assign_race", "assignments")] async (string tournamentId, AssignRaceRequest request, RaceAssignmentService assignmentService) =>
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

app.MapGet("/tournaments/{tournamentId}/assignments", [RequireViewer("view", "assignments")] (string tournamentId, RaceAssignmentService assignmentService) =>
{
    var assignments = assignmentService.GetTournamentAssignments(tournamentId);
    return assignments != null ? Results.Ok(assignments) : Results.NotFound();
})
.WithName("GetTournamentAssignments");

app.MapPost("/tournaments/{tournamentId}/rings/{ringNumber}/clear", [RequireRaceDirector("clear_ring", "assignments")] async (string tournamentId, int ringNumber, RaceAssignmentService assignmentService) =>
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

// Seed initial data
using (var scope = app.Services.CreateScope())
{
    var seedingService = scope.ServiceProvider.GetRequiredService<IDataSeedingService>();
    await seedingService.SeedInitialDataAsync();
}

app.Run();

// Make the implicit Program class public so test projects can access it
public partial class Program { }
