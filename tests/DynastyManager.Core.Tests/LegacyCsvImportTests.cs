using DynastyManager.Core.Models;
using DynastyManager.Data.Import;
using Xunit;

namespace DynastyManager.Core.Tests;

public class LegacyCsvImportTests
{
    [Fact]
    public void ExistingUniverseCsvImportsIntoNewModels()
    {
        var csv = File.ReadAllText(DataPath("universe.csv"));
        var result = LegacyUniverseCsvImporter.Import(csv);

        Assert.True(result.Conferences.Count >= 10);
        Assert.True(result.Teams.Count >= 100);
        Assert.Contains(result.Teams, t => t.Name == "Washington" && t.Abbreviation == "WASH");
    }

    [Fact]
    public void ExistingCoachCsvImports()
    {
        var csv = File.ReadAllText(DataPath("coach.csv"));
        var coaches = LegacyCoachCsvImporter.Import(csv);

        Assert.True(coaches.Count >= 100);
        Assert.All(coaches, c => Assert.Equal(CoachRole.HeadCoach, c.Role));
    }

    [Fact]
    public void ExistingRosterCsvImportsAndConvertsGenericDefense()
    {
        var csv = File.ReadAllText(DataPath("roster.csv"));
        var result = LegacyRosterCsvImporter.Import(csv);

        Assert.True(result.Players.Count >= 1000);
        Assert.NotEmpty(result.Warnings);
        Assert.DoesNotContain(result.Players, p =>
            p.Position.ToString() is "DL" or "LB" or "S");

        Assert.Contains(result.Players, p => p.Position == Position.DE);
        Assert.Contains(result.Players, p => p.Position == Position.DT);
        Assert.Contains(result.Players, p => p.Position == Position.OLB);
        Assert.Contains(result.Players, p => p.Position == Position.MLB);
        Assert.Contains(result.Players, p => p.Position == Position.FS);
        Assert.Contains(result.Players, p => p.Position == Position.SS);
    }

    [Fact]
    public void LegacyPositionConversionIsDeterministic()
    {
        var first = LegacyPositionConverter.Convert("DL", "Washington|Example Player|2|7", out _);
        var second = LegacyPositionConverter.Convert("DL", "Washington|Example Player|2|7", out _);

        Assert.Equal(first, second);
    }

    private static string DataPath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "TestData", fileName);
}
