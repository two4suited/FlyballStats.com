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



// Add Azure SignalR resource (emulator)
var signalr = builder.AddAzureSignalR("signalr").RunAsEmulator();

var apiService = builder.AddProject<Projects.flyballstats_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(database)
    .WithReference(signalr)
    .WaitFor(database)
    .WaitFor(signalr);

var webfrontend = builder.AddProject<Projects.flyballstats_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WithReference(signalr)
    .WaitFor(apiService)
    .WaitFor(database)
    .WaitFor(signalr);

builder.Build().Run();
