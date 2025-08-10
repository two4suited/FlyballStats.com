using flyballstats.ApiService.Models;
using System.Text;

namespace flyballstats.ApiService.Services;

public class CsvValidationService
{
    private static readonly string[] RequiredHeaders = ["race number", "left team", "right team", "division"];

    public CsvValidationResult ValidateCsv(string csvContent)
    {
        var errors = new List<ValidationError>();
        var validRaces = new List<Race>();

        if (string.IsNullOrWhiteSpace(csvContent))
        {
            errors.Add(new ValidationError(0, "File", "", "CSV content is empty"));
            return new CsvValidationResult(false, errors, validRaces);
        }

        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length == 0)
        {
            errors.Add(new ValidationError(0, "File", "", "CSV file is empty"));
            return new CsvValidationResult(false, errors, validRaces);
        }

        // Validate headers
        var headerLine = lines[0].Trim();
        var headers = ParseCsvLine(headerLine);
        var headerValidation = ValidateHeaders(headers);
        if (!headerValidation.IsValid)
        {
            errors.AddRange(headerValidation.Errors);
            return new CsvValidationResult(false, errors, validRaces);
        }

        // Get header positions
        var raceNumberIndex = GetHeaderIndex(headers, "race number");
        var leftTeamIndex = GetHeaderIndex(headers, "left team");
        var rightTeamIndex = GetHeaderIndex(headers, "right team");
        var divisionIndex = GetHeaderIndex(headers, "division");

        // Validate data rows
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var values = ParseCsvLine(line);
            var rowNumber = i + 1;

            var rowValidation = ValidateDataRow(values, rowNumber, raceNumberIndex, leftTeamIndex, rightTeamIndex, divisionIndex);
            errors.AddRange(rowValidation.Errors);
            
            if (rowValidation.IsValid)
            {
                var race = new Race(
                    int.Parse(values[raceNumberIndex]),
                    values[leftTeamIndex].Trim(),
                    values[rightTeamIndex].Trim(),
                    values[divisionIndex].Trim());
                validRaces.Add(race);
            }
        }

        return new CsvValidationResult(errors.Count == 0, errors, validRaces);
    }

    private static string[] ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        
        values.Add(current.ToString());
        return values.ToArray();
    }

    private static CsvValidationResult ValidateHeaders(string[] headers)
    {
        var errors = new List<ValidationError>();
        
        foreach (var requiredHeader in RequiredHeaders)
        {
            if (!headers.Any(h => string.Equals(h.Trim(), requiredHeader, StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add(new ValidationError(1, "Headers", string.Join(",", headers), $"Missing required header: '{requiredHeader}'"));
            }
        }

        return new CsvValidationResult(errors.Count == 0, errors, []);
    }

    private static int GetHeaderIndex(string[] headers, string headerName)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            if (string.Equals(headers[i].Trim(), headerName, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    private static CsvValidationResult ValidateDataRow(string[] values, int rowNumber, int raceNumberIndex, int leftTeamIndex, int rightTeamIndex, int divisionIndex)
    {
        var errors = new List<ValidationError>();

        // Check if we have enough columns
        var maxIndex = Math.Max(Math.Max(raceNumberIndex, leftTeamIndex), Math.Max(rightTeamIndex, divisionIndex));
        if (values.Length <= maxIndex)
        {
            errors.Add(new ValidationError(rowNumber, "Row", string.Join(",", values), "Row has insufficient columns"));
            return new CsvValidationResult(false, errors, []);
        }

        // Validate race number
        if (string.IsNullOrWhiteSpace(values[raceNumberIndex]))
        {
            errors.Add(new ValidationError(rowNumber, "Race Number", values[raceNumberIndex], "Race number cannot be empty"));
        }
        else if (!int.TryParse(values[raceNumberIndex], out int raceNumber) || raceNumber <= 0)
        {
            errors.Add(new ValidationError(rowNumber, "Race Number", values[raceNumberIndex], "Race number must be a positive integer"));
        }

        // Validate left team
        if (string.IsNullOrWhiteSpace(values[leftTeamIndex]))
        {
            errors.Add(new ValidationError(rowNumber, "Left Team", values[leftTeamIndex], "Left team cannot be empty"));
        }

        // Validate right team
        if (string.IsNullOrWhiteSpace(values[rightTeamIndex]))
        {
            errors.Add(new ValidationError(rowNumber, "Right Team", values[rightTeamIndex], "Right team cannot be empty"));
        }

        // Validate division
        if (string.IsNullOrWhiteSpace(values[divisionIndex]))
        {
            errors.Add(new ValidationError(rowNumber, "Division", values[divisionIndex], "Division cannot be empty"));
        }

        return new CsvValidationResult(errors.Count == 0, errors, []);
    }
}