using ProFootball.Core;
using DynastyManager.Core.Models;
using Xunit;
namespace ProFootball.Tests;
public class SeasonEligibilityTests
{
    static ProFranchise OverCap(ProPhase phase)
    {
        var s=ProLeagueService.Create("Seattle Seahawks") with{Phase=phase};return s with{DeadCap=s.DeadCap.Append(new(s.UserTeam,s.Year,Guid.NewGuid(),s.SalaryCap)).ToList()};
    }
    [Theory]
    [InlineData(GameDayMode.Play)] [InlineData(GameDayMode.Watch)] [InlineData(GameDayMode.Simulate)]
    public void ExistingOverCapRegularSeasonSavesCannotStartUserGames(GameDayMode mode)
    {
        var s=OverCap(ProPhase.RegularSeason);var g=s.Games.First(g=>g.Phase==s.Phase&&g.Football.Week==s.Week&&g.Football.InvolvesTeam(s.UserTeam));
        var error=Assert.Throws<InvalidOperationException>(()=>ProLeagueService.Start(s,g,mode));Assert.Contains("over the salary cap",error.Message);
    }
    [Fact] public void OverCapSaveCannotAdvanceRegularSeasonOrConsumeCalendarTick()
    {var s=OverCap(ProPhase.RegularSeason);Assert.Throws<InvalidOperationException>(()=>ProLeagueService.Advance(s));Assert.Equal(0,s.TransactionTick);Assert.False(s.Games.Any(g=>g.Football.HasPlayed));}
    [Fact] public void CompletedPreseasonRequiresCapComplianceBeforeSeasonEntry()
    {
        var s=OverCap(ProPhase.Preseason) with{Week=3};s=s with{Games=s.Games.Select(g=>g.Phase!=ProPhase.Preseason?g:g with{Football=g.Football with{HasPlayed=true,HomeScore=14,AwayScore=7}}).ToList()};
        Assert.Throws<InvalidOperationException>(()=>ProLeagueService.Advance(s));
        var fixedState=s with{DeadCap=[]};var advanced=ProLeagueService.Advance(fixedState);Assert.Equal(ProPhase.RegularSeason,advanced.Phase);Assert.Equal(1,advanced.Week);
    }
    [Fact] public void ExactlyAtCapIsAllowed()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");s=s with{TeamCapAllocations=[],SalaryCap=ProEconomy.Used(s,s.UserTeam)};Assert.Null(ProSeasonEligibility.Issue(s,s.UserTeam));
    }
}
