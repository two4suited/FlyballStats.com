using System.Text;
using System.Text.Json;

namespace flyballstats.Web;

public class TournamentApiClient(HttpClient httpClient)
{
    public async Task<Tournament[]> GetTournamentsAsync(CancellationToken cancellationToken = default)
    {
        var tournaments = await httpClient.GetFromJsonAsync<Tournament[]>("/tournaments", cancellationToken);
        return tournaments ?? [];
    }

    public async Task<Tournament?> GetTournamentAsync(string tournamentId, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<Tournament>($"/tournaments/{tournamentId}", cancellationToken);
    }

    public async Task<CsvUploadResponse> UploadCsvAsync(string tournamentId, string tournamentName, string csvContent, CancellationToken cancellationToken = default)
    {
        var request = new CsvUploadRequest(tournamentId, tournamentName, csvContent);
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await httpClient.PostAsync("/tournaments/upload-csv", content, cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<CsvUploadResponse>(cancellationToken);
            return result ?? new CsvUploadResponse(false, "Failed to parse response", null, null);
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return new CsvUploadResponse(false, $"HTTP {response.StatusCode}: {errorContent}", null, null);
        }
    }

    public async Task<RingConfigurationResponse> SaveRingConfigurationAsync(string tournamentId, List<RingConfiguration> rings, CancellationToken cancellationToken = default)
    {
        var request = new RingConfigurationRequest(tournamentId, rings);
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await httpClient.PostAsync($"/tournaments/{tournamentId}/rings", content, cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<RingConfigurationResponse>(cancellationToken);
            return result ?? new RingConfigurationResponse(false, "Failed to parse response", null);
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return new RingConfigurationResponse(false, $"HTTP {response.StatusCode}: {errorContent}", null);
        }
    }

    public async Task<TournamentRingConfiguration?> GetRingConfigurationAsync(string tournamentId, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<TournamentRingConfiguration>($"/tournaments/{tournamentId}/rings", cancellationToken);
    }

    public async Task<bool> TournamentExistsAsync(string tournamentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<TournamentExistsResponse>($"/tournaments/{tournamentId}/exists", cancellationToken);
            return response?.Exists ?? false;
        }
        catch
        {
            return false;
        }
    }

    // Race Assignment methods
    public async Task<AssignRaceResponse> AssignRaceAsync(string tournamentId, int raceNumber, int ringNumber, RingStatus status, bool allowConflictOverride = false, CancellationToken cancellationToken = default)
    {
        var request = new AssignRaceRequest(tournamentId, raceNumber, ringNumber, status, allowConflictOverride);
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await httpClient.PostAsync($"/tournaments/{tournamentId}/races/assign", content, cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<AssignRaceResponse>(cancellationToken);
            return result ?? new AssignRaceResponse(false, "Failed to parse response", null, null);
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return new AssignRaceResponse(false, $"HTTP {response.StatusCode}: {errorContent}", null, null);
        }
    }

    public async Task<TournamentRaceAssignments?> GetTournamentAssignmentsAsync(string tournamentId, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<TournamentRaceAssignments>($"/tournaments/{tournamentId}/assignments", cancellationToken);
    }

    public async Task<ClearRingResponse> ClearRingAsync(string tournamentId, int ringNumber, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync($"/tournaments/{tournamentId}/rings/{ringNumber}/clear", null, cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ClearRingResponse>(cancellationToken);
            return result ?? new ClearRingResponse(false, "Failed to parse response", null);
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return new ClearRingResponse(false, $"HTTP {response.StatusCode}: {errorContent}", null);
        }
    }
}