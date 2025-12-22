using flyballstats.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace flyballstats.ApiService.Data;

public class FlyballStatsDbContext : DbContext
{
    public FlyballStatsDbContext(DbContextOptions<FlyballStatsDbContext> options) : base(options)
    {
    }

    public DbSet<TournamentEntity> Tournaments { get; set; }
    public DbSet<TournamentRingConfigurationEntity> RingConfigurations { get; set; }
    public DbSet<TournamentRaceAssignmentsEntity> RaceAssignments { get; set; }
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<AuthorizationLogEntity> AuthorizationLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Check if we're using Cosmos DB provider
        var isCosmosDb = Database.ProviderName == "Microsoft.EntityFrameworkCore.Cosmos";
        
        if (isCosmosDb)
        {
            // Configure Tournament entity for Cosmos DB
            modelBuilder.Entity<TournamentEntity>()
                .ToContainer("Tournaments")
                .HasPartitionKey(t => t.Id);
        }
        
        // Configure Tournament entity (common for all providers)
        modelBuilder.Entity<TournamentEntity>()
            .Property(t => t.Id)
            .ValueGeneratedNever();

        // Configure the Races as an owned collection
        modelBuilder.Entity<TournamentEntity>()
            .OwnsMany(t => t.Races, builder =>
            {
                if (isCosmosDb)
                {
                    builder.ToJsonProperty("races");
                }
                builder.Property(r => r.RaceNumber);
                builder.Property(r => r.LeftTeam);
                builder.Property(r => r.RightTeam);
                builder.Property(r => r.Division);
            });

        if (isCosmosDb)
        {
            // Configure TournamentRingConfiguration entity for Cosmos DB
            modelBuilder.Entity<TournamentRingConfigurationEntity>()
                .ToContainer("RingConfigurations")
                .HasPartitionKey(rc => rc.TournamentId);
        }
        
        // Configure TournamentRingConfiguration entity (common for all providers)
        modelBuilder.Entity<TournamentRingConfigurationEntity>()
            .Property(rc => rc.Id)
            .ValueGeneratedNever();

        // Configure the Rings as an owned collection
        modelBuilder.Entity<TournamentRingConfigurationEntity>()
            .OwnsMany(rc => rc.Rings, builder =>
            {
                if (isCosmosDb)
                {
                    builder.ToJsonProperty("rings");
                }
                builder.Property(r => r.RingNumber);
                builder.Property(r => r.Color);
            });

        if (isCosmosDb)
        {
            // Configure TournamentRaceAssignments entity for Cosmos DB
            modelBuilder.Entity<TournamentRaceAssignmentsEntity>()
                .ToContainer("RaceAssignments")
                .HasPartitionKey(ra => ra.TournamentId);
        }
        
        // Configure TournamentRaceAssignments entity (common for all providers)
        modelBuilder.Entity<TournamentRaceAssignmentsEntity>()
            .Property(ra => ra.Id)
            .ValueGeneratedNever();

        // Configure the Rings with complex nested structure as JSON
        var jsonOptions = new System.Text.Json.JsonSerializerOptions();
        
        modelBuilder.Entity<TournamentRaceAssignmentsEntity>()
            .Property(ra => ra.Rings)
            .HasConversion<string>(
                v => System.Text.Json.JsonSerializer.Serialize(v, jsonOptions),
                v => System.Text.Json.JsonSerializer.Deserialize<List<RingRaceAssignments>>(v, jsonOptions) ?? new()
            )
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<RingRaceAssignments>>(
                (c1, c2) => System.Text.Json.JsonSerializer.Serialize(c1, jsonOptions) == System.Text.Json.JsonSerializer.Serialize(c2, jsonOptions),
                c => c == null ? 0 : System.Text.Json.JsonSerializer.Serialize(c, jsonOptions).GetHashCode(),
                c => System.Text.Json.JsonSerializer.Deserialize<List<RingRaceAssignments>>(System.Text.Json.JsonSerializer.Serialize(c, jsonOptions), jsonOptions)!
            ));

        if (isCosmosDb)
        {
            // Configure User entity for Cosmos DB
            modelBuilder.Entity<UserEntity>()
                .ToContainer("Users")
                .HasPartitionKey(u => u.Id);
        }
        
        // Configure User entity (common for all providers)
        modelBuilder.Entity<UserEntity>()
            .Property(u => u.Id)
            .ValueGeneratedNever();

        if (isCosmosDb)
        {
            // Configure AuthorizationLog entity for Cosmos DB
            modelBuilder.Entity<AuthorizationLogEntity>()
                .ToContainer("AuthorizationLogs")
                .HasPartitionKey(al => al.UserId);
        }
        
        // Configure AuthorizationLog entity (common for all providers)
        modelBuilder.Entity<AuthorizationLogEntity>()
            .Property(al => al.Id)
            .ValueGeneratedNever();

        base.OnModelCreating(modelBuilder);
    }
}