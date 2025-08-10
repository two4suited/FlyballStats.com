using flyballstats.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace flyballstats.ApiService.Data;

public class FlyballStatsDbContext : DbContext
{
    public FlyballStatsDbContext(DbContextOptions<FlyballStatsDbContext> options) : base(options)
    {
    }

    public DbSet<TournamentEntity> Tournaments { get; set; }
    public DbSet<RaceEntity> Races { get; set; }
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

        // Configure Race entity
        modelBuilder.Entity<RaceEntity>()
            .ToContainer("Races")
            .HasPartitionKey(r => r.TournamentId)
            .Property(r => r.Id)
            .ValueGeneratedOnAdd();

        // Configure TournamentRingConfiguration entity
        modelBuilder.Entity<TournamentRingConfigurationEntity>()
            .ToContainer("RingConfigurations")
            .HasPartitionKey(rc => rc.TournamentId)
            .Property(rc => rc.Id)
            .ValueGeneratedNever();

        // Configure TournamentRaceAssignments entity
        modelBuilder.Entity<TournamentRaceAssignmentsEntity>()
            .ToContainer("RaceAssignments")
            .HasPartitionKey(ra => ra.TournamentId)
            .Property(ra => ra.Id)
            .ValueGeneratedNever();

        base.OnModelCreating(modelBuilder);
    }
}