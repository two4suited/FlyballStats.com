using flyballstats.Web.Services;
using flyballstats.Web;
using Xunit;

namespace flyballstats.Tests;

public class ErrorReportServiceTests
{
    private readonly ErrorReportService _errorReportService = new();

    [Fact]
    public void GenerateErrorSummary_WithNoErrors_ReturnsEmptySummary()
    {
        // Arrange
        var errors = new List<ValidationError>();

        // Act
        var summary = _errorReportService.GenerateErrorSummary(errors);

        // Assert
        Assert.Equal(0, summary.TotalErrors);
        Assert.Empty(summary.ErrorsByType);
    }

    [Fact]
    public void GenerateErrorSummary_WithErrors_ReturnsCorrectSummary()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new(1, "Race Number", "abc", "Must be a number"),
            new(2, "Race Number", "", "Cannot be empty"),
            new(3, "Left Team", "", "Cannot be empty"),
            new(4, "Division", "", "Cannot be empty")
        };

        // Act
        var summary = _errorReportService.GenerateErrorSummary(errors);

        // Assert
        Assert.Equal(4, summary.TotalErrors);
        Assert.Equal(3, summary.ErrorsByType.Count);
        
        var raceNumberErrors = summary.ErrorsByType.First(e => e.FieldName == "Race Number");
        Assert.Equal(2, raceNumberErrors.Count);
    }

    [Fact]
    public void GenerateErrorReportCsv_WithErrors_ReturnsValidCsv()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new(1, "Race Number", "abc", "Must be a number"),
            new(2, "Left Team", "", "Cannot be empty")
        };

        // Act
        var csv = _errorReportService.GenerateErrorReportCsv(errors);

        // Assert
        Assert.Contains("Row Number,Field,Value,Reason", csv);
        Assert.Contains("1,\"Race Number\",\"abc\",\"Must be a number\"", csv);
        Assert.Contains("2,\"Left Team\",\"\",\"Cannot be empty\"", csv);
    }

    [Fact]
    public void GenerateErrorReportText_WithErrors_ReturnsFormattedReport()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new(1, "Race Number", "abc", "Must be a number"),
            new(2, "Left Team", "", "Cannot be empty")
        };

        // Act
        var report = _errorReportService.GenerateErrorReportText(errors);

        // Assert
        Assert.Contains("CSV Validation Error Report", report);
        Assert.Contains("Total Errors: 2", report);
        Assert.Contains("Row 1: Race Number - Must be a number", report);
        Assert.Contains("Row 2: Left Team - Cannot be empty", report);
    }

    [Fact]
    public void GenerateErrorReportCsv_WithNoErrors_ReturnsNoErrorsMessage()
    {
        // Arrange
        var errors = new List<ValidationError>();

        // Act
        var csv = _errorReportService.GenerateErrorReportCsv(errors);

        // Assert
        Assert.Equal("No errors to report", csv);
    }
}