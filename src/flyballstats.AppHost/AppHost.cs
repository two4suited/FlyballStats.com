var builder = DistributedApplication.CreateBuilder(args);

// Add Cosmos DB resource
var cosmosDb = builder.AddAzureCosmosDB("cosmosdb")
    .RunAsEmulator();
    
var apiService = builder.AddProject<Projects.flyballstats_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.flyballstats_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
