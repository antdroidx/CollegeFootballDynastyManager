using DynastyManager.Core.Models;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public class ManagementRefinementTests
{
    [Fact] public void BracketProjectsThirteenSlotsAndProtectsTopSeedBye()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");var tree=ProPlayoffTree.Build(s,s.Year);
        Assert.Equal(13,tree.Count);Assert.Equal(6,tree.Count(x=>x.Round==1));
        Assert.All(tree.Where(x=>x.Round==1),x=>{Assert.NotEqual(1,x.HomeSeed);Assert.Equal(9,x.HomeSeed+x.AwaySeed);});
        Assert.Equal(2,tree.Count(x=>x.Round==2&&x.HomeSeed==1));Assert.All(tree.Where(x=>x.Round>=2),x=>Assert.Null(x.Away));
    }
    [Fact] public void AutomaticScoutingIsBudgetedAndIdempotent()
    {
        var s=ProLeagueService.Create("Seattle Seahawks") with{Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.FreeAgency};s=ProDraft.Generate(s);
        Assert.Equal(40,s.Scouting.Count(r=>r.Team==s.UserTeam));var next=ProDraft.AutoScout(s);Assert.Equal(s.Scouting,next.Scouting);
        Assert.All(s.Scouting,r=>Assert.Equal(75,r.Confidence));
    }
    [Fact] public void RookieSourceCoverageKeepsCurrentCapAndMarksEstimatedFuture()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");var source=ProContractData.Load();
        var rookie=s.Players.First(p=>p.RookieYear==s.Year-1&&p.DraftOverall is >0&&!p.PracticeSquad&&source.Any(c=>c.PlayerId==p.Football.PlayerId&&c.Years.Count==1));
        var original=source.Single(c=>c.PlayerId==rookie.Football.PlayerId);var migrated=ProContractData.Apply(s);var contract=migrated.Contracts.Single(c=>c.PlayerId==rookie.Football.PlayerId);
        Assert.Equal(ProEconomy.CapHit(original,s.Year),ProEconomy.CapHit(contract,s.Year));Assert.Equal(3,contract.Years.Count);Assert.True(contract.FutureTermsEstimated);
        Assert.Equal(contract,ProContractData.CompleteRookieTerms(migrated).Contracts.Single(c=>c.PlayerId==rookie.Football.PlayerId));
    }
    [Fact] public void LegacyEstimatedRookieRepairPreservesCurrentYearCharge()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");var p=s.Players.First(p=>p.RookieYear==s.Year&&p.DraftOverall is >0&&!p.PracticeSquad);
        var c=new ProContract(p.Football.PlayerId,p.Football.TeamName,s.Year,1,400_000,[new(s.Year,1_000_000,200_000)]);
        s=s with{Contracts=s.Contracts.Where(x=>x.PlayerId!=p.Football.PlayerId).Append(c).ToList()};
        var next=ProContractData.CompleteRookieTerms(s);var repaired=next.Contracts.Single(x=>x.PlayerId==p.Football.PlayerId);
        Assert.Equal(4,repaired.Years.Count);Assert.Equal(ProEconomy.CapHit(c,s.Year),ProEconomy.CapHit(repaired,s.Year));Assert.Equal(400_000,repaired.SigningBonus);
    }
    [Fact] public void InboxResolvesCapAlertsAndReadStateSurvivesRefresh()
    {
        var s=ProLeagueService.Create("Seattle Seahawks") with{TeamCapAllocations=[],SalaryCap=0};var notices=ProInbox.Build(s);var cap=Assert.Single(notices,n=>n.Id.StartsWith("cap-"));
        s=s with{ReadNotificationIds=[cap.Id]};Assert.Equal(notices,ProInbox.Build(s));Assert.Contains(cap.Id,s.ReadNotificationIds);
        Assert.DoesNotContain(ProInbox.Build(s with{TeamCapAllocations=[],SalaryCap=1_000_000_000}),n=>n.Id==cap.Id);
    }
    [Fact] public void RetentionRespectsLetExpireAndCanRenewUnderBudget()
    {
        var s=ProLeagueService.Create("Seattle Seahawks") with{Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.Retention,TeamCapAllocations=[],SalaryCap=1_000_000_000};
        var players=s.Players.Where(p=>p.Football.TeamName==s.UserTeam&&!p.Retired).Take(2).ToArray();var ids=players.Select(p=>p.Football.PlayerId).ToArray();
        s=s with{Contracts=s.Contracts.Where(c=>!ids.Contains(c.PlayerId)).Concat(players.Select(p=>new ProContract(p.Football.PlayerId,s.UserTeam,s.Year,1,0,[new(s.Year,1_000_000,0)]))).ToList(),LetExpire=[ids[0]]};
        var next=ProRetention.Negotiate(s,s.UserTeam);
        Assert.Contains(ProRetention.Expiring(next,s.UserTeam),p=>p.Football.PlayerId==ids[0]);Assert.DoesNotContain(ProRetention.Expiring(next,s.UserTeam),p=>p.Football.PlayerId==ids[1]);
        Assert.Equal(s.Players.Select(p=>p.Football.PlayerId),next.Players.Select(p=>p.Football.PlayerId));
    }
}
