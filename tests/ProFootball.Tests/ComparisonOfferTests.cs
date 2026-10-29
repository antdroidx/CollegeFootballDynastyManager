using ProFootball.Core;
using DynastyManager.Core.Models;
using Xunit;
namespace ProFootball.Tests;
public class ComparisonOfferTests
{
    [Fact] public void AgentDefaultsAreRoundedAgeSensitiveAndRespondToCompetingBids()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");var p=s.Players.First(p=>p.Football.TeamName=="Free agent");
        var demand=ProOfferTerms.AgentDemand(s,p);Assert.Equal(0,demand.Salary%250_000);Assert.Equal(0,demand.Bonus%250_000);Assert.InRange(demand.Years,1,5);
        var old=ProOfferTerms.AgentDemand(s,p with{Age=35});Assert.Equal(1,old.Years);
        Assert.Equal(demand,ProOfferTerms.AgentDemand(s with{Bids=[new(p.Football.PlayerId,s.UserTeam,3,demand.Salary,0,s.Week)]},p));
        var bidders=s.League.Teams.Take(3).Select(t=>new FreeAgentBid(p.Football.PlayerId,t.Name,3,demand.Salary,0,s.Week)).ToList();
        Assert.True(ProOfferTerms.AgentDemand(s with{Bids=bidders},p).Salary>demand.Salary);
        Assert.True(ProNegotiation.Evaluate(p,demand.Years,demand.Salary,demand.Bonus).Accepted);
    }
    [Fact] public void RoundedCounterNeverUnderstatesRequiredSalary()
    { Assert.Equal(7_250_000,ProOfferTerms.RoundUp(7_001_000));Assert.Equal(7_000_000,ProOfferTerms.RoundUp(7_000_000)); }
    [Fact] public void PerformanceComparisonsUsePerGameValuesAndExcludePreseason()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");var other=s.League.Teams.First(t=>t.Name!=s.UserTeam).Name;
        ProGame Game(string id,int score,ProPhase phase)=>new(new(){GameId=id,SeasonYear=s.Year,Week=1,HomeTeamName=s.UserTeam,AwayTeamName=other,HasPlayed=true,HomeScore=score,AwayScore=10,HomeStats=new(){PassYards=200,RushYards=100},AwayStats=new(){PassYards=100,RushYards=50}},phase);
        s=s with{Games=[Game("a",20,ProPhase.RegularSeason),Game("b",40,ProPhase.RegularSeason),Game("ignore",99,ProPhase.Preseason)]};
        var comparison=Assert.Single(ProLeagueComparisons.Build(s),c=>c.Metric=="Points scored");
        Assert.Equal(30,comparison.Team);Assert.Equal(20,comparison.Average);Assert.Equal(1,comparison.Rank);Assert.Equal(2,comparison.Teams);
        Assert.Equal(300,Assert.Single(ProLeagueComparisons.Build(s),c=>c.Metric=="Offensive yards").Team);
    }
    [Fact] public void RankingsShareTiesAndNoGamesHaveNoPerformanceRanks()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");Assert.DoesNotContain(ProLeagueComparisons.Build(s),c=>c.Unit=="pts/g");
        s=s with{Contracts=[],DeadCap=[]};var cap=Assert.Single(ProLeagueComparisons.Build(s),c=>c.Metric==s.Year+" committed cap");Assert.Equal(1,cap.Rank);Assert.Equal(32,cap.Teams);Assert.True(cap.LowerIsBetter);
    }
}
