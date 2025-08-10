var builder = DistributedApplication.CreateBuilder(args);

// Add Cosmos DB resource
var cosmosDb = builder.AddAzureCosmosDB("cosmosdb")
    .RunAsEmulator();

var flyballstatsDb = cosmosDb.AddCosmosDatabase("flyballstats");

var apiService = builder.AddProject<Projects.flyballstats_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(flyballstatsDb);

builder.AddProject<Projects.flyballstats_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
