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
}