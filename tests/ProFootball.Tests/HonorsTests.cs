using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public class HonorsTests
{
    static ProFranchise Complete()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");var g=s.Games.First(g=>g.Phase==ProPhase.RegularSeason&&g.Football.InvolvesTeam(s.UserTeam));var qb=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&p.Football.Position==Position.QB);
        var stats=new PlayerSeasonStatLine{PlayerId=qb.Football.PlayerId,PlayerName=qb.Football.FullName,TeamName=s.UserTeam,Position=Position.QB,Games=17,PassAttempts=500,PassCompletions=350,PassYards=5000,PassingTouchdowns=45,SeasonYear=s.Year};
        return s with{Phase=ProPhase.Offseason,Games=s.Games.Select(x=>x.Phase!=ProPhase.RegularSeason?x:x with{Football=x.Football with{HasPlayed=true,HomeScore=24,AwayScore=17,PlayerStats=x==g?[stats]:[]}}).ToList()};
    }
    [Fact] public void AwardsRequireCompletedSeasonAndPublishOnlyOnce()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");Assert.Same(s,ProHonors.Publish(s));
        var complete=Complete();var published=ProHonors.Publish(complete);var honors=Assert.Single(published.Honors);
        Assert.Contains(honors.Awards,a=>a.Title=="Most Valuable Player"&&a.Team==s.UserTeam);
        Assert.Same(published,ProHonors.Publish(published));
        Assert.Equal(2,honors.Selections.Select(p=>p.Conference).Distinct().Count());
        Assert.Equal(honors.Selections.Count,honors.Selections.Select(p=>p.Player.Football.PlayerId).Distinct().Count());
        foreach(var conference in honors.Selections.GroupBy(p=>p.Conference))Assert.All(Enumerable.Range(5,5),code=>Assert.Contains(conference,p=>p.Player.Football.NcaaPositionCode==code));
        var news=ProNews.Refresh(published);Assert.Contains(news.News,n=>n.Category=="Awards");Assert.Equal(news.News.Count,ProNews.Refresh(news).News.Count);
    }
    [Fact] public void ShowcaseUsesSharedEngineAndDoesNotAlterLeaguePlayersOrStats()
    {
        var s=ProHonors.Publish(Complete());var session=ProHonors.StartShowcase(s,s.Year,GameDayMode.Simulate);
        Assert.Equal("NFC All-Stars",session.Game.HomeTeamName);Assert.Equal(StaffRole.HeadCoach,session.UserCoachRole);
        Assert.Throws<InvalidOperationException>(()=>ProHonors.FinishShowcase(s,s.Year,session));
        GameDayEngine.SimulateToEnd(session);var completed=ProHonors.FinishShowcase(s,s.Year,session);
        Assert.True(completed.Honors.Single().Showcase!.HasPlayed);Assert.Same(s.Players,completed.Players);Assert.Same(s.Games,completed.Games);Assert.Same(s.Contracts,completed.Contracts);
        Assert.Same(completed,ProHonors.FinishShowcase(completed,s.Year,session));Assert.Throws<InvalidOperationException>(()=>ProHonors.StartShowcase(completed,s.Year,GameDayMode.Watch));
    }
}
