using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public class CampRosterTests
{
    [Theory]
    [InlineData(ProPhase.Offseason,90)]
    [InlineData(ProPhase.Preseason,90)]
    [InlineData(ProPhase.RegularSeason,53)]
    [InlineData(ProPhase.Playoffs,53)]
    public void RosterLimitMatchesCalendar(ProPhase phase, int expected) => Assert.Equal(expected, ProRoster.Limit(ProLeagueService.Create("Seattle Seahawks") with { Phase = phase }));

    [Fact]
    public void CompletedPreseasonWaitsForUserCutdownAndTrimsCpu()
    {
        var s = ProLeagueService.Create("Seattle Seahawks") with { Week = 3 };
        s = s with { Games = s.Games.Select(g => g.Phase != ProPhase.Preseason ? g : g with { Football = g.Football with { HasPlayed = true, HomeScore = 14, AwayScore = 7 } }).ToList(),
            Players = s.Players.Select(p => p.Football.TeamName == s.UserTeam && p.PracticeSquad ? p with { PracticeSquad = false } : p).ToList() };
        Assert.True(ProRoster.Active(s,s.UserTeam)>53);
        var waiting = ProLeagueService.Advance(s);
        Assert.Equal(ProPhase.Preseason,waiting.Phase);
        Assert.Equal(3,waiting.Week);
        Assert.Equal(s.Games,waiting.Games);
        Assert.All(waiting.League.Teams.Where(t=>t.Name!=s.UserTeam),t=>Assert.True(ProRoster.Active(waiting,t.Name)<=53));
        var ready = ProLeagueService.Advance(ProCpu.Trim(waiting,waiting.UserTeam));
        Assert.Equal(ProPhase.RegularSeason,ready.Phase);
        Assert.Equal(1,ready.Week);
    }
}
