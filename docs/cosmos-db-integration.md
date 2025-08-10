# Azure Cosmos DB Integration

This document describes the Azure Cosmos DB integration in FlyballStats.com using Entity Framework Core and .NET Aspire.

## Overview

The application has been updated to use Azure Cosmos DB as the primary data store, replacing the previous in-memory storage. This integration uses:

- **Azure Cosmos DB**: NoSQL document database for scalable data storage
- **Entity Framework Core**: ORM with Cosmos DB provider
- **.NET Aspire**: For orchestration and service discovery
- **Cosmos DB Emulator**: For local development

## Architecture

### Data Models

The application uses the following Cosmos DB containers:

1. **Tournaments** (`TournamentEntity`)
   - Partition Key: `Id`
   - Contains tournament metadata and embedded race data
   - Document structure optimized for Cosmos DB

2. **RingConfigurations** (`TournamentRingConfigurationEntity`)
   - Partition Key: `TournamentId`
   - Stores ring configuration per tournament

3. **RaceAssignments** (`TournamentRaceAssignmentsEntity`)
   - Partition Key: `TournamentId`
   - Stores race assignments to rings with real-time updates

### Service Integration

- **AppHost**: Provisions Cosmos DB resource with emulator support
- **ApiService**: Configured with Entity Framework Cosmos DB provider
- **TournamentDataService**: Updated to use DbContext instead of in-memory collections
- **RaceAssignmentService**: Enhanced with persistent storage

## Configuration

### AppHost Configuration

```csharp
// Add Cosmos DB resource
var cosmosDb = builder.AddAzureCosmosDB("cosmosdb")
    .RunAsEmulator();

var flyballstatsDb = cosmosDb.AddCosmosDatabase("flyballstats");

var apiService = builder.AddProject<Projects.flyballstats_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(flyballstatsDb);
```

### ApiService Configuration

```csharp
// Add Cosmos DB via Aspire
builder.AddCosmosDbContext<FlyballStatsDbContext>("flyballstats");
```

## Development Setup

### Prerequisites

1. .NET 9.0 SDK
2. Azure Cosmos DB Emulator (for local development)
3. .NET Aspire workload

### Running Locally

1. **Start Cosmos DB Emulator**:
   - The Aspire orchestrator will automatically start the emulator
   - Access emulator dashboard at: https://localhost:8081/_explorer/index.html

2. **Run the Application**:
   ```bash
   cd src
   aspire run
   ```

3. **Access Application**:
   - Aspire Dashboard: Automatically opens in browser
   - Web Frontend: Available through Aspire dashboard
   - API Service: Internal service discovery

## API Endpoints

All existing API endpoints continue to work with the new Cosmos DB backend:

### Tournament Management
- `POST /tournaments/upload-csv` - Upload tournament data
- `GET /tournaments` - List all tournaments
- `GET /tournaments/{id}` - Get specific tournament
- `GET /tournaments/{id}/exists` - Check tournament existence

### Ring Configuration
- `POST /tournaments/{id}/rings` - Configure rings
- `GET /tournaments/{id}/rings` - Get ring configuration

### Race Assignments
- `POST /tournaments/{id}/races/assign` - Assign race to ring
- `GET /tournaments/{id}/assignments` - Get all assignments
- `POST /tournaments/{id}/rings/{ringNumber}/clear` - Clear ring

## Data Migration

### From In-Memory to Cosmos DB

The migration from in-memory storage to Cosmos DB is transparent to API consumers. Key changes:

1. **Data Persistence**: Data survives application restarts
2. **Scalability**: Supports distributed deployment
3. **Consistency**: Strong consistency within partition
4. **Performance**: Optimized queries with proper partition keys

### Cosmos DB Design Patterns

1. **Document Embedding**: Races are embedded within tournament documents
2. **Partition Strategy**: Each tournament gets its own partition
3. **JSON Serialization**: Complex nested objects stored as JSON
4. **Value Comparers**: Proper change detection for collections

## Performance Considerations

### Partition Key Strategy
- **Tournaments**: Partitioned by tournament ID
- **Configurations**: Partitioned by tournament ID
- **Assignments**: Partitioned by tournament ID

This ensures related data is co-located and queries are efficient.

### Query Optimization
- All queries include partition key for optimal performance
- Cross-partition queries are avoided
- Document structure minimizes round trips

## Monitoring and Diagnostics

### Health Checks
- Entity Framework health checks included
- Cosmos DB connectivity verification
- Available at `/health` endpoint

### Logging
- Entity Framework query logging
- Cosmos DB operation metrics
- Performance tracking with stopwatch

## Troubleshooting

### Common Issues

1. **Emulator Connection Issues**:
   - Ensure Cosmos DB emulator is running
   - Check firewall settings
   - Verify SSL certificates

2. **Entity Framework Errors**:
   - Check model configuration
   - Verify partition key settings
   - Review JSON serialization settings

3. **Performance Issues**:
   - Review query patterns
   - Check partition key usage
   - Monitor RU consumption

### Debugging

Enable detailed logging in `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore": "Debug",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

## Testing

### Unit Tests
- In-memory Entity Framework provider for testing
- Mock DbContext for isolated testing
- Test coverage for all CRUD operations

### Integration Tests
- Cosmos DB emulator for integration testing
- End-to-end API testing
- Performance testing scenarios

## Future Enhancements

### Potential Improvements

1. **Change Feed**: Implement change feed for real-time updates
2. **Stored Procedures**: Add server-side logic for complex operations
3. **Caching**: Implement read-through caching for frequently accessed data
4. **Backup Strategy**: Automated backup and restore procedures
5. **Multi-Region**: Configure global distribution for high availability

### Scaling Considerations

1. **Throughput**: Monitor and adjust RU/s allocation
2. **Partitioning**: Review partition strategy as data grows
3. **Indexing**: Optimize indexing policies for query patterns
4. **Connection Pooling**: Optimize Entity Framework connection management

---

For more information about Cosmos DB with Entity Framework, see:
- [Microsoft Docs: Cosmos DB Provider](https://docs.microsoft.com/en-us/ef/core/providers/cosmos/)
- [Aspire Cosmos DB Integration](https://learn.microsoft.com/en-us/dotnet/aspire/database/azure-cosmos-db-entity-framework-integration)