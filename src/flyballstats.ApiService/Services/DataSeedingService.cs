using flyballstats.ApiService.Data;
using flyballstats.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace flyballstats.ApiService.Services;

public interface IDataSeedingService
{
    Task SeedInitialDataAsync();
}

public class DataSeedingService : IDataSeedingService
{
    private readonly FlyballStatsDbContext _context;
    private readonly ILogger<DataSeedingService> _logger;

    public DataSeedingService(FlyballStatsDbContext context, ILogger<DataSeedingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedInitialDataAsync()
    {
        try
        {
            // Check if users already exist
            if (await _context.Users.AnyAsync())
            {
                _logger.LogInformation("Users already exist, skipping seed data");
                return;
            }

            var users = new[]
            {
                new UserEntity
                {
                    Id = "director-001",
                    Username = "director",
                    Email = "director@flyballstats.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("director123"),
                    Role = UserRole.Director,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new UserEntity
                {
                    Id = "race-director-001",
                    Username = "racedirector",
                    Email = "racedirector@flyballstats.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("racedirector123"),
                    Role = UserRole.RaceDirector,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new UserEntity
                {
                    Id = "viewer-001",
                    Username = "viewer",
                    Email = "viewer@flyballstats.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("viewer123"),
                    Role = UserRole.Viewer,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Seeded {UserCount} initial users: {Usernames}", 
                users.Length, 
                string.Join(", ", users.Select(u => u.Username)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed initial data");
            throw;
        }
    }
}