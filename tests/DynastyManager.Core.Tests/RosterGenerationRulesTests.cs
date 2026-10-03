using DynastyManager.Core.Models;
using DynastyManager.Data.Import;
using Xunit;

namespace DynastyManager.Core.Tests;

public class RosterGenerationRulesTests
{
    [Fact]
    public void PrestigeGeneratorBuildsExactlyEightyFivePlayersPerTeam()
    {
        var strong = Team("Strong", 90);
        var weak = Team("Weak", 40);

        var roster = LegacyDynastyRosterFactory.Create(
            Array.Empty<ImportedPlayerRow>(),
            new[] { strong, weak },
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            2026);

        foreach (var team in new[] { strong, weak })
        {
            var teamRoster = roster
                .Where(player => player.TeamName == team.Name)
                .ToArray();

            Assert.Equal(
                DynastyRosterRules.MaximumRosterSize,
                teamRoster.Length);

            foreach (var target in DynastyRosterRules.TargetPositionCounts)
            {
                Assert.Equal(
                    target.Value,
                    teamRoster.Count(player =>
                        player.Position == target.Key));
            }
        }

        var strongAverage = roster
            .Where(player => player.TeamName == strong.Name)
            .Average(player => player.OverallRating);

        var weakAverage = roster
            .Where(player => player.TeamName == weak.Name)
            .Average(player => player.OverallRating);

        Assert.True(strongAverage > weakAverage);
    }

    [Fact]
    public void RecruitingPoolScalesWithFbsTeamCount()
    {
        Assert.Equal(
            3250,
            RecruitingPoolRules.GetTargetPoolSize(130));

        Assert.Equal(
            3450,
            RecruitingPoolRules.GetTargetPoolSize(138));
    }

    private static Team Team(string name, int prestige) =>
        new()
        {
            Name = name,
            Abbreviation = name,
            ConferenceName = "Test",
            Prestige = prestige
        };
}
