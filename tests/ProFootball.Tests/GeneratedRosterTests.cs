using System.Text.Json;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public class GeneratedRosterTests
{
    [Fact] public void FictionalOpeningBudgetIncludesWorstCaseOneYearSigningBonuses()
    {
        var league=ProLeagueService.Load();
        var players=ProGeneratedRoster.Create(league,new Random(20261030))
            .Select(p=>p with {Age=34, Experience=12, RookieYear=2014}).ToList();
        var s=ProCapInitialization.Initialize(ProEconomy.Initialize(new ProFranchise {
            UserTeam="Seattle Seahawks", FictionalPlayers=true, League=league, Players=players }));
        Assert.All(league.Teams,t=>Assert.True(ProEconomy.Room(s,t.Name)>0,t.Name));
        Assert.All(s.Contracts,c=>Assert.Equal(1,c.BonusYears));
    }
    [Fact]public async Task FictionalStartsAreUniqueBalancedAndPersistWithoutRealContractOrTradeImports()
    {
        var a=ProLeagueService.Create("Seattle Seahawks",sourcedContracts:true,generatedPlayers:true);var b=ProLeagueService.Create("Seattle Seahawks",generatedPlayers:true);
        Assert.True(a.FictionalPlayers);Assert.Empty(a.Players.Select(p=>p.Football.PlayerId).Intersect(b.Players.Select(p=>p.Football.PlayerId)));Assert.Null(a.OpeningTransactionsSnapshot);Assert.Null(a.OpeningPickSnapshot);
        var means=a.Players.Where(p=>!p.PracticeSquad).GroupBy(p=>p.Football.TeamName).Select(g=>g.Average(p=>p.Football.OverallRating)).ToArray();Assert.InRange(means.Max()-means.Min(),0,9);
        Assert.All(a.Players,p=>{Assert.True(p.Football.HeightInches>60);Assert.True(p.Football.WeightPounds>140);});
        Assert.All(a.League.Teams,t=>{Assert.Equal(53,ProRoster.Active(a,t.Name));Assert.Equal(16,a.Players.Count(p=>p.Football.TeamName==t.Name&&p.PracticeSquad));Assert.True(ProEconomy.Room(a,t.Name)>0);});
        Assert.All(a.Players.Where(p=>p.Experience==0&&!p.PracticeSquad&&p.DraftOverall>0),p=>Assert.Equal(2029,ProContractInsights.PlayingEnd(a.Contracts.Single(c=>c.Active&&c.PlayerId==p.Football.PlayerId))));
        var path=Path.Combine(Path.GetTempPath(),"generated-"+Guid.NewGuid());try{var store=new ProSaveStore(path);await store.Save(a);var restored=await store.Load(a.Id);Assert.Equal(JsonSerializer.Serialize(a.Players),JsonSerializer.Serialize(restored.Players));Assert.Equal(JsonSerializer.Serialize(a.Contracts),JsonSerializer.Serialize(restored.Contracts));}finally{if(Directory.Exists(path))Directory.Delete(path,true);}
    }
    [Fact]public void ProfessionalSacksAreSeparateFromRushingAndTeamTotalsReconcile()
    {
        var s=ProLeagueService.Create("Seattle Seahawks",generatedPlayers:true);var g=s.Games.First();var session=ProLeagueService.Start(s,g,GameDayMode.Simulate);GameDayEngine.SimulateToEnd(session);var game=GameDayEngine.Finish(session);
        foreach(var team in new[]{g.Football.HomeTeamName,g.Football.AwayTeamName})
        {
            var stats=team==g.Football.HomeTeamName?game.HomeStats!:game.AwayStats!;var sacks=session.Events.Where(e=>e.OffenseTeamName==team&&e.Outcome==SnapOutcome.Sack).ToArray();
            Assert.Equal(-sacks.Sum(e=>e.Yards),stats.SackYardsLost);Assert.Equal(stats.PassYards+stats.RushYards-stats.SackYardsLost,stats.TotalYards);Assert.Equal(stats.RushYards,game.PlayerStats.Where(p=>p.TeamName==team).Sum(p=>p.RushYards));
        }
        Assert.All(session.Events.Where(GameDayEngine.IsScrimmage),e=>{Assert.Equal(11,e.OffensePersonnel.Distinct().Count());Assert.Equal(11,e.DefensePersonnel.Distinct().Count());});
    }
}
