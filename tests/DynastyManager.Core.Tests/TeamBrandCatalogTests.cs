using Xunit;
using DynastyManager.Data.Branding;
using DynastyManager.Data.Import;

namespace DynastyManager.Core.Tests;

public sealed class TeamBrandCatalogTests
{
    [Fact]
    public void EveryLegacyAndModernSchoolHasAValidOfflinePalette()
    {
        var csv = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "universe.csv"));
        var legacy = LegacyUniverseCsvImporter.Import(csv).Teams;
        var modern = ModernFbsUniverseFactory.Create(legacy);
        Assert.Equal(138, modern.Count);
        foreach (var team in legacy.Concat(modern))
        {
            Assert.True(TeamBrandCatalog.Contains(team.Name), team.Name);
            var colors = TeamBrandCatalog.Get(team.Name);
            Assert.Matches("^#[0-9a-fA-F]{6}$", colors.Primary);
            Assert.Matches("^#[0-9a-fA-F]{6}$", colors.Secondary);
        }
    }

    [Theory]
    [InlineData("Washington", "#33006f", "#e8d3a2")]
    [InlineData("Alabama", "#9e1b32", "#ffffff")]
    public void PaletteLookupPreservesSchoolIdentityAndIgnoresCase(string school, string primary, string secondary)
    {
        var colors = TeamBrandCatalog.Get(" " + school.ToUpperInvariant() + " ");
        Assert.Equal(primary, colors.Primary, ignoreCase: true);
        Assert.Equal(secondary, colors.Secondary, ignoreCase: true);
    }

    [Fact]
    public void UnknownAndMissingSchoolsHaveADeterministicNeutralFallback()
    {
        Assert.Equal(TeamBrandCatalog.Get(null), TeamBrandCatalog.Get("Custom school"));
        Assert.False(TeamBrandCatalog.Contains("Custom school"));
    }
}
