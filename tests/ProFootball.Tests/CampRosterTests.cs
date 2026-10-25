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
    public void CampLimitIncludesRetainedPracticePlayers()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");var template=s.Players.First();
        var players=Enumerable.Range(0,90).Select(i=>template with{Football=template.Football with{PlayerId=Guid.NewGuid(),TeamName=s.UserTeam},PracticeSquad=i<16,Reserve=false,Retired=false}).ToList();
        var free=template with{Football=template.Football with{PlayerId=Guid.NewGuid(),TeamName="Free agent"},PracticeSquad=false,Reserve=false,Retired=false};
        s=s with{Players=players.Append(free).ToList(),Contracts=[],Waivers=[],Bids=[]};
        Assert.Equal(74,ProRoster.Active(s,s.UserTeam));Assert.Equal(90,ProRoster.CountForLimit(s,s.UserTeam));
        Assert.Throws<InvalidOperationException>(()=>ProEconomy.Offer(s,s.UserTeam,free.Football.PlayerId,1,Math.Max(1_000_000,ProEconomy.MarketSalary(free)),0));
        var practice=players.First(p=>p.PracticeSquad);var promoted=ProRoster.Practice(s,s.UserTeam,practice.Football.PlayerId,false);Assert.Equal(90,ProRoster.CountForLimit(promoted,s.UserTeam));
        Assert.Equal(75,ProRoster.Active(promoted,promoted.UserTeam));
    }

    [Fact]
    public void CompletedPreseasonWaitsForUserCutdownAndTrimsCpu()
    {
        var s = ProLeagueService.Create("Seattle Seahawks") with { Week = 3 };
        s = s with { Games = s.Games.Select(g => g.Phase != ProPhase.Preseason ? g : g with { Football = g.Football with { HasPlayed = g.Football.Week < 3, HomeScore = g.Football.Week < 3 ? 14 : null, AwayScore = g.Football.Week < 3 ? 7 : null } }).ToList(),
            Players = s.Players.Select(p => p.Football.TeamName == s.UserTeam && p.PracticeSquad ? p with { PracticeSquad = false } : p).ToList() };
        Assert.True(ProRoster.Active(s,s.UserTeam)>53);
        var waiting = ProLeagueService.Advance(s);
        Assert.Equal(ProPhase.Preseason,waiting.Phase);
        Assert.Equal(3,waiting.Week);
        Assert.All(waiting.Games.Where(g=>g.Phase==ProPhase.Preseason),g=>Assert.True(g.Football.HasPlayed));
        Assert.Throws<InvalidOperationException>(()=>ProLeagueService.Advance(waiting));
        Assert.All(waiting.League.Teams.Where(t=>t.Name!=s.UserTeam),t=>Assert.True(ProRoster.Active(waiting,t.Name)<=53));
        var ready = ProLeagueService.Advance(ProCpu.Trim(waiting,waiting.UserTeam));
        Assert.Equal(ProPhase.RegularSeason,ready.Phase);
        Assert.Equal(1,ready.Week);
    }
}
