using DynastyManager.Core.Models;
using ProFootball.Core;
using Xunit;

namespace ProFootball.Tests;

public class SpecialistAndDepthTests
{
    [Fact]
    public void SourceKickerWithPunterCodeMigratesToPunter()
    {
        var s = ProLeagueService.Create("Seattle Seahawks");
        var dickson = s.Players.Single(p => p.Football.FullName == "Michael Dickson");
        Assert.Equal(Position.P, dickson.Football.Position);
    }

    [Fact]
    public void DepthChartCapsSpecialistSlots()
    {
        Assert.Equal(1, ProDepthChart.MaxForCode(19));
        Assert.Equal(1, ProDepthChart.MaxForCode(20));
        Assert.Equal(9, ProDepthChart.MaxForCode(5));
    }

    [Fact]
    public void AutoAssignProducesDepthWithinCaps()
    {
        var s = ProLeagueService.Create("Seattle Seahawks", generatedPlayers: true, applyOpeningTrades: false);
        var assigned = ProDepthChart.AutoAssign(s, s.UserTeam);
        foreach (var group in assigned.DepthChartAssignments.Where(a => a.TeamName == s.UserTeam).GroupBy(a => a.PositionCode))
            Assert.True(group.Count() <= ProDepthChart.MaxForCode(group.Key), $"code {group.Key} over cap");
        Assert.Contains(assigned.DepthChartAssignments, a => a.TeamName == s.UserTeam && a.PositionCode == 20);
        Assert.Contains(assigned.DepthChartAssignments, a => a.TeamName == s.UserTeam && a.PositionCode == 19);
    }

    [Fact]
    public void NewsOrdersPlayoffsAndAwardsAfterRegularSeason()
    {
        var s = ProLeagueService.Create("Seattle Seahawks", generatedPlayers: true, applyOpeningTrades: false);
        var team = s.League.Teams.Single(t => t.Name == s.UserTeam);
        var story = (string id, int week, string category) => new ProNewsStory(id, s.Year, week, category, id, id);
        s = s with { News = [story("regular-18", 18, "Results"), story("playoff-round", 2, "Playoffs"), story("award", 18, "Awards")] };
        var refreshed = ProNews.Refresh(s);
        Assert.Equal("award", refreshed.News[0].Id);
        Assert.Equal("playoff-round", refreshed.News[1].Id);
        Assert.Equal("regular-18", refreshed.News[2].Id);
    }
}