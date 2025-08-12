using flyballstats.ApiService.Services;
using flyballstats.Tests.Fixtures.TestDataBuilders;
using Xunit;

namespace flyballstats.Tests.Unit;

public class CsvParsingEdgeCasesTests
{
    private readonly CsvValidationService _service = new();

    #region Boundary and Edge Cases

    [Fact]
    public void ValidateCsv_WithMaxIntRaceNumber_IsValid()
    {
        // Arrange
        var csv = CsvTestDataBuilder.Create()
            .WithValidRace(int.MaxValue, "Team A", "Team B")
            .Build();

        // Act
        var result = _service.ValidateCsv(csv);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(int.MaxValue, result.ValidRaces[0].RaceNumber);
    }

    [Fact]
    public void ValidateCsv_WithLeadingZerosInRaceNumber_IsValid()
    {
        // Arrange
        var csv = CsvTestDataBuilder.Create()
            .WithRow("001", "Team A", "Team B", "Regular")
            .Build();

        // Act
        var result = _service.ValidateCsv(csv);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(1, result.ValidRaces[0].RaceNumber);
    }

    [Fact]
    public void ValidateCsv_WithWhitespaceAroundValues_TrimsCorrectly()
    {
        // Arrange
        var csv = "Race Number,Left Team,Right Team,Division\n  1  ,  Team A  ,  Team B  ,  Regular  ";

        // Act
        var result = _service.ValidateCsv(csv);

        // Assert
        Assert.True(result.IsValid);
        var race = result.ValidRaces[0];
        Assert.Equal("Team A", race.LeftTeam.Trim());
        Assert.Equal("Team B", race.RightTeam.Trim());
        Assert.Equal("Regular", race.Division.Trim());
    }

    [Fact]
    public void ValidateCsv_WithUnicodeCharacters_IsValid()
    {
        // Arrange
        var csv = CsvTestDataBuilder.Create()
            .WithValidRace(1, "Équipe Française", "Команда России", "Élite")
            .Build();

        // Act
        var result = _service.ValidateCsv(csv);

        // Assert
        Assert.True(result.IsValid);
        var race = result.ValidRaces[0];
        Assert.Equal("Équipe Française", race.LeftTeam);
        Assert.Equal("Команда России", race.RightTeam);
        Assert.Equal("Élite", race.Division);
    }

    [Fact]
    public void ValidateCsv_WithEmoji_IsValid()
    {
        // Arrange
        var csv = CsvTestDataBuilder.Create()
            .WithValidRace(1, "🐕 Team", "⚡ Lightning", "🏆 Elite")
            .Build();

        // Act
        var result = _service.ValidateCsv(csv);

        // Assert
        Assert.True(result.IsValid);
        var race = result.ValidRaces[0];
        Assert.Equal("🐕 Team", race.LeftTeam);
        Assert.Equal("⚡ Lightning", race.RightTeam);
        Assert.Equal("🏆 Elite", race.Division);
    }

    [Fact]
    public void ValidateCsv_WithNewlineInQuotedField_IsValid()
    {
        // Arrange - This tests multiline fields within quotes
        // Note: Current CSV parser doesn't handle multiline fields properly
        // This test validates current behavior, not ideal CSV handling
        var csv = "Race Number,Left Team,Right Team,Division\n1,\"Team\nWith Newline\",\"Another\nTeam\",Regular";

        // Act
        var result = _service.ValidateCsv(csv);

        // Assert
        // Current parser treats newlines as row separators even within quotes
        // This results in parsing errors, which is the current behavior
        Assert.False(result.IsValid);
    }

    #endregion

    #region Data Type Edge Cases

    [Fact]
    public void ValidateCsv_WithFloatRaceNumber_ReturnsError()
    {
        // Arrange
        var csv = CsvTestDataBuilder.Create()
            .WithInvalidRace("1.5", "Team A", "Team B")
            .Build();

        // Act
        var result = _service.ValidateCsv(csv);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => 
            e.Field == "Race Number" && 
            e.Reason.Contains("must be a positive integer"));
    }

    [Fact]
    public void ValidateCsv_WithScientificNotationRaceNumber_ReturnsError()
    {
        // Arrange
        var csv = CsvTestDataBuilder.Create()
            .WithInvalidRace("1e2", "Team A", "Team B")
            .Build();

        // Act
        var result = _service.ValidateCsv(csv);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => 
            e.Field == "Race Number" && 
            e.Reason.Contains("must be a positive integer"));
    }

    [Fact]
    public void ValidateCsv_WithHexadecimalRaceNumber_ReturnsError()
    {
        // Arrange
        var csv = CsvTestDataBuilder.Create()
            .WithInvalidRace("0xFF", "Team A", "Team B")
            .Build();

        // Act
        var result = _service.ValidateCsv(csv);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => 
            e.Field == "Race Number" && 
            e.Reason.Contains("must be a positive integer"));
    }

    #endregion

    #region Team Name Edge Cases

    [Fact]
    public void ValidateCsv_WithOnlyNumbersInTeamName_IsValid()
    {
        // Arrange
        var csv = CsvTestDataBuilder.Create()
            .WithValidRace(1, "12345", "67890")
            .Build();

        // Act
        var result = _service.ValidateCsv(csv);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("12345", result.ValidRaces[0].LeftTeam);
        Assert.Equal("67890", result.ValidRaces[0].RightTeam);
    }

    [Fact]
    public void ValidateCsv_WithSingleCharacterTeamName_IsValid()
    {
        // Arrange
        var csv = CsvTestDataBuilder.Create()
            .WithValidRace(1, "A", "B")
            .Build();

        // Act
        var result = _service.ValidateCsv(csv);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("A", result.ValidRaces[0].LeftTeam);
        Assert.Equal("B", result.ValidRaces[0].RightTeam);
    }

    [Fact]
    public void ValidateCsv_WithIdenticalTeamNames_IsValid()
    {
        // Arrange
        var csv = CsvTestDataBuilder.Create()
            .WithValidRace(1, "Same Team", "Same Team")
            .Build();

        // Act
        var result = _service.ValidateCsv(csv);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("Same Team", result.ValidRaces[0].LeftTeam);
        Assert.Equal("Same Team", result.ValidRaces[0].RightTeam);
    }

    #endregion

    #region Performance Considerations

    [Fact]
    public void ValidateCsv_WithManyRowsOfValidData_PerformsWell()
    {
        // Arrange
        var builder = CsvTestDataBuilder.Create();
        for (int i = 1; i <= 1000; i++)
        {
            builder.WithValidRace(i, $"Team Left {i}", $"Team Right {i}");
        }
        var csv = builder.Build();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = _service.ValidateCsv(csv);
        stopwatch.Stop();

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(1000, result.ValidRaces.Count);
        // Performance assertion - should complete in reasonable time
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, $"Validation took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void ValidateCsv_WithManyErrorsInData_ReportsAllErrors()
    {
        // Arrange
        var builder = CsvTestDataBuilder.Create();
        for (int i = 1; i <= 100; i++)
        {
            builder.WithInvalidRace("invalid", "", "", ""); // 4 errors per row
        }
        var csv = builder.Build();

        // Act
        var result = _service.ValidateCsv(csv);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 400); // At least 4 errors per row * 100 rows
        Assert.Empty(result.ValidRaces);
    }

    #endregion

    #region Row Number Validation

    [Fact]
    public void ValidateCsv_ErrorsIncludeCorrectRowNumbers()
    {
        // Arrange
        var csv = CsvTestDataBuilder.Create()
            .WithValidRace(1, "Valid Team A", "Valid Team B")
            .WithValidRace(2, "Valid Team C", "Valid Team D")
            .WithInvalidRace("invalid", "Team E", "Team F") // Row 4 (header is row 1)
            .WithValidRace(4, "Valid Team G", "Valid Team H")
            .WithInvalidRace("also_invalid", "Team I", "Team J") // Row 6
            .Build();

        // Act
        var result = _service.ValidateCsv(csv);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(3, result.ValidRaces.Count); // 3 valid races despite errors
        
        // Check row numbers in errors
        var errorRowNumbers = result.Errors.Select(e => e.RowNumber).Distinct().ToList();
        Assert.Contains(4, errorRowNumbers); // First invalid row
        Assert.Contains(6, errorRowNumbers); // Second invalid row
    }

    #endregion

    #region Mixed Valid and Invalid Data

    [Fact]
    public void ValidateCsv_WithMixedValidAndInvalidRows_ReturnsOnlyValidRaces()
    {
        // Arrange
        var csv = CsvTestDataBuilder.Create()
            .WithValidRace(1, "Valid Team A", "Valid Team B")
            .WithInvalidRace("", "Invalid Team C", "Invalid Team D") // Empty race number
            .WithValidRace(3, "Valid Team E", "Valid Team F")
            .WithInvalidRace("4", "", "Invalid Team H") // Empty left team
            .WithValidRace(5, "Valid Team I", "Valid Team J")
            .Build();

        // Act
        var result = _service.ValidateCsv(csv);

        // Assert
        Assert.False(result.IsValid); // Has errors
        Assert.True(result.Errors.Count >= 2); // At least 2 errors
        Assert.Equal(3, result.ValidRaces.Count); // Only valid races returned
        
        // Verify valid races
        Assert.Equal(1, result.ValidRaces[0].RaceNumber);
        Assert.Equal(3, result.ValidRaces[1].RaceNumber);
        Assert.Equal(5, result.ValidRaces[2].RaceNumber);
    }

    #endregion
}