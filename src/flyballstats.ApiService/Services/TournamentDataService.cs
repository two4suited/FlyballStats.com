using flyballstats.ApiService.Models;
using flyballstats.ApiService.Data;
using Microsoft.EntityFrameworkCore;

namespace flyballstats.ApiService.Services;

public class TournamentDataService
{
    private readonly FlyballStatsDbContext _context;

    public TournamentDataService(FlyballStatsDbContext context)
    {
        _context = context;
    }

    public async Task<Tournament?> GetTournamentAsync(string tournamentId)
    {
        var entity = await _context.Tournaments.FirstOrDefaultAsync(t => t.Id == tournamentId);
        return entity != null ? new Tournament(entity.Id, entity.Name, entity.Races) : null;
    }

    public Tournament? GetTournament(string tournamentId)
    {
        var entity = _context.Tournaments.FirstOrDefault(t => t.Id == tournamentId);
        return entity != null ? new Tournament(entity.Id, entity.Name, entity.Races) : null;
    }

    public async Task<IEnumerable<Tournament>> GetAllTournamentsAsync()
    {
        var entities = await _context.Tournaments.ToListAsync();
        return entities.Select(e => new Tournament(e.Id, e.Name, e.Races));
    }

    public IEnumerable<Tournament> GetAllTournaments()
    {
        var entities = _context.Tournaments.ToList();
        return entities.Select(e => new Tournament(e.Id, e.Name, e.Races));
    }

    public async Task<Tournament> CreateOrUpdateTournamentAsync(string tournamentId, string tournamentName, List<Race> races)
    {
        var existingEntity = await _context.Tournaments.FirstOrDefaultAsync(t => t.Id == tournamentId);
        
        if (existingEntity != null)
        {
            existingEntity.Name = tournamentName;
            existingEntity.Races = races;
            _context.Tournaments.Update(existingEntity);
        }
        else
        {
            var newEntity = new TournamentEntity
            {
                Id = tournamentId,
                Name = tournamentName,
                Races = races
            };
            _context.Tournaments.Add(newEntity);
        }

        await _context.SaveChangesAsync();
        return new Tournament(tournamentId, tournamentName, races);
    }

    public Tournament CreateOrUpdateTournament(string tournamentId, string tournamentName, List<Race> races)
    {
        var existingEntity = _context.Tournaments.FirstOrDefault(t => t.Id == tournamentId);
        
        if (existingEntity != null)
        {
            existingEntity.Name = tournamentName;
            existingEntity.Races = races;
            _context.Tournaments.Update(existingEntity);
        }
        else
        {
            var newEntity = new TournamentEntity
            {
                Id = tournamentId,
                Name = tournamentName,
                Races = races
            };
            _context.Tournaments.Add(newEntity);
        }

        _context.SaveChanges();
        return new Tournament(tournamentId, tournamentName, races);
    }

    public async Task<bool> TournamentExistsAsync(string tournamentId)
    {
        return await _context.Tournaments.AnyAsync(t => t.Id == tournamentId);
    }

    public bool TournamentExists(string tournamentId)
    {
        return _context.Tournaments.Any(t => t.Id == tournamentId);
    }

    public async Task<TournamentRingConfiguration> SaveRingConfigurationAsync(string tournamentId, List<RingConfiguration> rings)
    {
        var existingEntity = await _context.RingConfigurations.FirstOrDefaultAsync(rc => rc.TournamentId == tournamentId);
        
        if (existingEntity != null)
        {
            existingEntity.Rings = rings;
            _context.RingConfigurations.Update(existingEntity);
        }
        else
        {
            var newEntity = new TournamentRingConfigurationEntity
            {
                Id = tournamentId,
                TournamentId = tournamentId,
                Rings = rings
            };
            _context.RingConfigurations.Add(newEntity);
        }

        await _context.SaveChangesAsync();
        return new TournamentRingConfiguration(tournamentId, rings);
    }

    public TournamentRingConfiguration SaveRingConfiguration(string tournamentId, List<RingConfiguration> rings)
    {
        var existingEntity = _context.RingConfigurations.FirstOrDefault(rc => rc.TournamentId == tournamentId);
        
        if (existingEntity != null)
        {
            existingEntity.Rings = rings;
            _context.RingConfigurations.Update(existingEntity);
        }
        else
        {
            var newEntity = new TournamentRingConfigurationEntity
            {
                Id = tournamentId,
                TournamentId = tournamentId,
                Rings = rings
            };
            _context.RingConfigurations.Add(newEntity);
        }

        _context.SaveChanges();
        return new TournamentRingConfiguration(tournamentId, rings);
    }

    public async Task<TournamentRingConfiguration?> GetRingConfigurationAsync(string tournamentId)
    {
        var entity = await _context.RingConfigurations.FirstOrDefaultAsync(rc => rc.TournamentId == tournamentId);
        return entity != null ? new TournamentRingConfiguration(entity.TournamentId, entity.Rings) : null;
    }

    public TournamentRingConfiguration? GetRingConfiguration(string tournamentId)
    {
        var entity = _context.RingConfigurations.FirstOrDefault(rc => rc.TournamentId == tournamentId);
        return entity != null ? new TournamentRingConfiguration(entity.TournamentId, entity.Rings) : null;
    }
}