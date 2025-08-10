var builder = DistributedApplication.CreateBuilder(args);

// Add Cosmos DB resource
var cosmosDb = builder.AddAzureCosmosDB("cosmosdb")
    .RunAsEmulator();

var apiService = builder.AddProject("apiservice", "../flyballstats.ApiService/flyballstats.ApiService.csproj")
    .WithReference(cosmosDb);

builder.AddProject("webfrontend", "../flyballstats.Web/flyballstats.Web.csproj")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
