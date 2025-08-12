using flyballstats.ApiService.Models;

namespace flyballstats.Tests.Fixtures.TestDataBuilders;

public class CsvTestDataBuilder
{
    private readonly List<string> _headers = new() { "Race Number", "Left Team", "Right Team", "Division" };
    private readonly List<string[]> _rows = new();

    public static CsvTestDataBuilder Create()
    {
        return new CsvTestDataBuilder();
    }

    public CsvTestDataBuilder WithHeaders(params string[] headers)
    {
        _headers.Clear();
        _headers.AddRange(headers);
        return this;
    }

    public CsvTestDataBuilder WithRow(params string[] values)
    {
        _rows.Add(values);
        return this;
    }

    public CsvTestDataBuilder WithValidRace(int raceNumber, string leftTeam, string rightTeam, string division = "Regular")
    {
        _rows.Add(new[] { raceNumber.ToString(), leftTeam, rightTeam, division });
        return this;
    }

    public CsvTestDataBuilder WithInvalidRace(string raceNumber, string leftTeam, string rightTeam, string division = "Regular")
    {
        _rows.Add(new[] { raceNumber, leftTeam, rightTeam, division });
        return this;
    }

    public string Build()
    {
        var lines = new List<string> { string.Join(",", _headers.Select(h => $"\"{h}\"")) };
        lines.AddRange(_rows.Select(row => string.Join(",", row.Select(v => $"\"{v}\""))));
        return string.Join("\n", lines);
    }

    public string BuildWithoutQuotes()
    {
        var lines = new List<string> { string.Join(",", _headers) };
        lines.AddRange(_rows.Select(row => string.Join(",", row)));
        return string.Join("\n", lines);
    }
}

public class ValidationErrorBuilder
{
    private int _rowNumber = 1;
    private string _field = "Field";
    private string _value = "Value";
    private string _reason = "Reason";

    public static ValidationErrorBuilder Create()
    {
        return new ValidationErrorBuilder();
    }

    public ValidationErrorBuilder WithRowNumber(int rowNumber)
    {
        _rowNumber = rowNumber;
        return this;
    }

    public ValidationErrorBuilder WithField(string field)
    {
        _field = field;
        return this;
    }

    public ValidationErrorBuilder WithValue(string value)
    {
        _value = value;
        return this;
    }

    public ValidationErrorBuilder WithReason(string reason)
    {
        _reason = reason;
        return this;
    }

    public ValidationError Build()
    {
        return new ValidationError(_rowNumber, _field, _value, _reason);
    }
}

public class RaceBuilder
{
    private int _raceNumber = 1;
    private string _leftTeam = "Left Team";
    private string _rightTeam = "Right Team";
    private string _division = "Regular";

    public static RaceBuilder Create()
    {
        return new RaceBuilder();
    }

    public RaceBuilder WithRaceNumber(int raceNumber)
    {
        _raceNumber = raceNumber;
        return this;
    }

    public RaceBuilder WithLeftTeam(string leftTeam)
    {
        _leftTeam = leftTeam;
        return this;
    }

    public RaceBuilder WithRightTeam(string rightTeam)
    {
        _rightTeam = rightTeam;
        return this;
    }

    public RaceBuilder WithDivision(string division)
    {
        _division = division;
        return this;
    }

    public Race Build()
    {
        return new Race(_raceNumber, _leftTeam, _rightTeam, _division);
    }
}