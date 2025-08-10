var builder = DistributedApplication.CreateBuilder(args);

// Add Cosmos DB resource
#pragma warning disable ASPIRECOSMOSDB001


var cosmos = builder.AddAzureCosmosDB("cosmos-db").RunAsPreviewEmulator(
                     emulator =>
                     {
                         emulator.WithDataExplorer();
                     });
var database = cosmos.AddCosmosDatabase("flyballstats");
var tournamentsContainer = database.AddContainer("Tournaments", "/id");
var ringConfigurationsContainer = database.AddContainer("RingConfigurations", "/tournamentId");
var raceAssignmentsContainer = database.AddContainer("RaceAssignments", "/tournamentId");

#pragma warning restore ASPIRECOSMOSDB001


var apiService = builder.AddProject<Projects.flyballstats_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.flyballstats_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
