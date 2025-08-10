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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure Tournament entity
        modelBuilder.Entity<TournamentEntity>()
            .ToContainer("Tournaments")
            .HasPartitionKey(t => t.Id)
            .Property(t => t.Id)
            .ValueGeneratedNever();

        // Configure the Races as an owned collection (stored as JSON in Cosmos DB)
        modelBuilder.Entity<TournamentEntity>()
            .OwnsMany(t => t.Races, builder =>
            {
                builder.ToJsonProperty("races");
                builder.Property(r => r.RaceNumber);
                builder.Property(r => r.LeftTeam);
                builder.Property(r => r.RightTeam);
                builder.Property(r => r.Division);
            });

        // Configure TournamentRingConfiguration entity
        modelBuilder.Entity<TournamentRingConfigurationEntity>()
            .ToContainer("RingConfigurations")
            .HasPartitionKey(rc => rc.TournamentId)
            .Property(rc => rc.Id)
            .ValueGeneratedNever();

        // Configure the Rings as an owned collection
        modelBuilder.Entity<TournamentRingConfigurationEntity>()
            .OwnsMany(rc => rc.Rings, builder =>
            {
                builder.ToJsonProperty("rings");
                builder.Property(r => r.RingNumber);
                builder.Property(r => r.Color);
            });

        // Configure TournamentRaceAssignments entity
        modelBuilder.Entity<TournamentRaceAssignmentsEntity>()
            .ToContainer("RaceAssignments")
            .HasPartitionKey(ra => ra.TournamentId)
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

        base.OnModelCreating(modelBuilder);
    }
}