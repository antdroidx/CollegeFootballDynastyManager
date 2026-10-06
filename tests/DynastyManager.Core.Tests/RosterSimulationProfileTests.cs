using DynastyManager.Core.Models;
using DynastyManager.Data.Import;
using Xunit;

namespace DynastyManager.Core.Tests;

public class RosterSimulationProfileTests
{
    [Fact]
    public void HigherLegacyTalentProducesHigherSimulationRatings()
    {
        var teams = new[]
        {
            Team("High"),
            Team("Low")
        };

        var players = BuildRoster("High", 9)
            .Concat(BuildRoster("Low", 5))
            .ToArray();

        var profiles = LegacyRosterSimulationProfileBuilder.Build(players, teams);

        Assert.True(
            profiles["High"].OffenseRating >
            profiles["Low"].OffenseRating);

        Assert.True(
            profiles["High"].DefenseRating >
            profiles["Low"].DefenseRating);

        Assert.True(
            profiles["High"].SpecialTeamsRating >
            profiles["Low"].SpecialTeamsRating);
    }

    [Fact]
    public void EveryUniverseTeamGetsAProfileEvenWithoutRosterRows()
    {
        var teams = new[] { Team("No Roster", prestige: 75) };

        var profiles = LegacyRosterSimulationProfileBuilder.Build(
            Array.Empty<ImportedPlayerRow>(),
            teams);

        var profile = Assert.Single(profiles).Value;
        Assert.Equal("No Roster", profile.TeamName);
        Assert.Equal(0, profile.RosterSize);
        Assert.InRange(profile.OffenseRating, 55, 95);
    }

    private static IEnumerable<ImportedPlayerRow> BuildRoster(
        string teamName,
        int talent)
    {
        var positions = new[]
        {
            Position.QB, Position.QB,
            Position.RB, Position.RB, Position.RB,
            Position.WR, Position.WR, Position.WR, Position.WR, Position.WR,
            Position.TE, Position.TE,
            Position.OL, Position.OL, Position.OL, Position.OL, Position.OL,
            Position.OL, Position.OL,
            Position.DE, Position.DE, Position.DE, Position.DE,
            Position.DT, Position.DT, Position.DT, Position.DT,
            Position.OLB, Position.OLB, Position.OLB, Position.OLB,
            Position.MLB, Position.MLB, Position.MLB,
            Position.CB, Position.CB, Position.CB, Position.CB, Position.CB,
            Position.FS, Position.FS,
            Position.SS, Position.SS,
            Position.K
        };

        return positions.Select((position, index) => new ImportedPlayerRow
        {
            TeamName = teamName,
            FullName = $"Player {index}",
            Position = position,
            Year = 2,
            LegacyTalentLevel = talent
        });
    }

    private static Team Team(string name, int prestige = 70) =>
        new()
        {
            Name = name,
            Abbreviation = name,
            ConferenceName = "Test",
            Prestige = prestige
        };
}
