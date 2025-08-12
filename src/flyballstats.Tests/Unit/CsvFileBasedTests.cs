using flyballstats.ApiService.Services;
using Xunit;

namespace flyballstats.Tests.Unit;

public class CsvFileBasedTests
{
    private readonly CsvValidationService _service = new();

    [Fact]
    public void ValidateCsv_WithSmallValidFile_IsValid()
    {
        // Arrange
        var csv = File.ReadAllText("Fixtures/ValidCsvSamples/small_valid_10_races.csv");

        // Act
        var result = _service.ValidateCsv(csv);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal(10, result.ValidRaces.Count);
        Assert.Equal(1, result.ValidRaces[0].RaceNumber);
        Assert.Equal(10, result.ValidRaces[9].RaceNumber);
    }

    [Fact]
    public void ValidateCsv_WithMediumValidFile_IsValid()
    {
        // Arrange
        var csv = File.ReadAllText("Fixtures/ValidCsvSamples/medium_valid_100_races.csv");

        // Act
        var result = _service.ValidateCsv(csv);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal(100, result.ValidRaces.Count);
        Assert.Equal(1, result.ValidRaces[0].RaceNumber);
        Assert.Equal(100, result.ValidRaces[99].RaceNumber);
    }

    [Fact]
    public void ValidateCsv_WithSingleRaceFile_IsValid()
    {
        // Arrange
        var csv = File.ReadAllText("Fixtures/ValidCsvSamples/single_race.csv");

        // Act
        var result = _service.ValidateCsv(csv);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Single(result.ValidRaces);
        Assert.Equal(1, result.ValidRaces[0].RaceNumber);
    }

    [Fact]
    public void ValidateCsv_WithSpecialCharactersFile_IsValid()
    {
        // Arrange
        var csv = File.ReadAllText("Fixtures/ValidCsvSamples/special_characters.csv");

        // Act
        var result = _service.ValidateCsv(csv);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal(3, result.ValidRaces.Count);
        
        // Check special characters are preserved
        Assert.Contains("comma in name", result.ValidRaces[0].LeftTeam);
        Assert.Contains("Special_Characters@Team!", result.ValidRaces[1].LeftTeam);
        Assert.Contains("VeryLongTeamName", result.ValidRaces[2].LeftTeam);
    }

    [Fact]
    public void ValidateCsv_WithCaseInsensitiveHeadersFile_IsValid()
    {
        // Arrange
        var csv = File.ReadAllText("Fixtures/ValidCsvSamples/case_insensitive_headers.csv");

        // Act
        var result = _service.ValidateCsv(csv);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal(2, result.ValidRaces.Count);
    }

    [Fact]
    public void ValidateCsv_WithMissingHeadersFile_ReturnsError()
    {
        // Arrange
        var csv = File.ReadAllText("Fixtures/InvalidCsvSamples/missing_headers.csv");

        // Act
        var result = _service.ValidateCsv(csv);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Reason.Contains("Missing required header: 'race number'"));
    }

    [Fact]
    public void ValidateCsv_WithWrongDataTypesFile_ReturnsErrors()
    {
        // Arrange
        var csv = File.ReadAllText("Fixtures/InvalidCsvSamples/wrong_data_types.csv");

        // Act
        var result = _service.ValidateCsv(csv);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 4); // Multiple validation errors
        
        // Check specific errors
        Assert.Contains(result.Errors, e => e.Field == "Race Number" && e.Value == "abc");
        Assert.Contains(result.Errors, e => e.Field == "Left Team" && e.Reason.Contains("cannot be empty"));
        Assert.Contains(result.Errors, e => e.Field == "Right Team" && e.Reason.Contains("cannot be empty"));
        Assert.Contains(result.Errors, e => e.Field == "Division" && e.Reason.Contains("cannot be empty"));
    }

    [Fact]
    public void ValidateCsv_WithDuplicateRaceNumbersFile_AllowsDuplicates()
    {
        // Arrange
        var csv = File.ReadAllText("Fixtures/InvalidCsvSamples/duplicate_race_numbers.csv");

        // Act
        var result = _service.ValidateCsv(csv);

        // Assert
        // Current implementation doesn't check for duplicate race numbers
        // This test documents current behavior
        Assert.True(result.IsValid);
        Assert.Equal(3, result.ValidRaces.Count);
        Assert.Equal(1, result.ValidRaces[0].RaceNumber);
        Assert.Equal(1, result.ValidRaces[2].RaceNumber); // Duplicate allowed
    }

    [Fact]
    public void ValidateCsv_WithInsufficientColumnsFile_ReturnsError()
    {
        // Arrange
        var csv = File.ReadAllText("Fixtures/InvalidCsvSamples/insufficient_columns.csv");

        // Act
        var result = _service.ValidateCsv(csv);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Reason.Contains("insufficient columns"));
    }

    [Fact]
    public void ValidateCsv_WithEmptyFile_ReturnsError()
    {
        // Arrange
        var csv = File.ReadAllText("Fixtures/InvalidCsvSamples/empty_file.csv");

        // Act
        var result = _service.ValidateCsv(csv);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("CSV content is empty", result.Errors[0].Reason);
    }
}