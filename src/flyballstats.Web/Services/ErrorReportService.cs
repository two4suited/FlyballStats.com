using System.Text;

namespace flyballstats.Web.Services;

public class ErrorReportService
{
    public ErrorSummary GenerateErrorSummary(List<ValidationError> errors)
    {
        if (errors == null || errors.Count == 0)
            return new ErrorSummary(0, []);

        var errorsByType = errors
            .GroupBy(e => e.Field)
            .Select(g => new ErrorTypeCount(g.Key, g.Count()))
            .OrderByDescending(etc => etc.Count)
            .ToList();

        return new ErrorSummary(errors.Count, errorsByType);
    }

    public string GenerateErrorReportCsv(List<ValidationError> errors)
    {
        if (errors == null || errors.Count == 0)
            return "No errors to report";

        var csv = new StringBuilder();
        csv.AppendLine("Row Number,Field,Value,Reason");

        foreach (var error in errors.OrderBy(e => e.RowNumber))
        {
            csv.AppendLine($"{error.RowNumber},\"{EscapeCsvValue(error.Field)}\",\"{EscapeCsvValue(error.Value)}\",\"{EscapeCsvValue(error.Reason)}\"");
        }

        return csv.ToString();
    }

    public string GenerateErrorReportText(List<ValidationError> errors)
    {
        if (errors == null || errors.Count == 0)
            return "No errors to report";

        var report = new StringBuilder();
        report.AppendLine("CSV Validation Error Report");
        report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"Total Errors: {errors.Count}");
        report.AppendLine();

        var errorsByType = errors.GroupBy(e => e.Field).OrderByDescending(g => g.Count());
        
        report.AppendLine("Error Summary by Field:");
        foreach (var group in errorsByType)
        {
            report.AppendLine($"  {group.Key}: {group.Count()} error(s)");
        }
        report.AppendLine();

        report.AppendLine("Detailed Errors:");
        foreach (var error in errors.OrderBy(e => e.RowNumber))
        {
            report.AppendLine($"Row {error.RowNumber}: {error.Field} - {error.Reason}");
            if (!string.IsNullOrWhiteSpace(error.Value))
            {
                report.AppendLine($"  Value: {error.Value}");
            }
            report.AppendLine();
        }

        return report.ToString();
    }

    private static string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        // Escape double quotes by doubling them
        return value.Replace("\"", "\"\"");
    }
}

public record ErrorSummary(
    int TotalErrors,
    List<ErrorTypeCount> ErrorsByType);

public record ErrorTypeCount(
    string FieldName,
    int Count);