using ProFootball.Core;
using DynastyManager.Core.Models;
using Xunit;
namespace ProFootball.Tests;
public class FuturePickOwnershipTests
{
    static ProFranchise New()=>ProLeagueService.Create("Seattle Seahawks");
    static ProDraftPick Pick(ProFranchise s,int year,int round,string original)=>s.Picks.Single(p=>p.Year==year&&p.Round==round&&!p.Compensatory&&p.OriginalTeam==original);
    [Fact]public void DatedLedgerCoversAllStandardPicksAndDoesNotDuplicateAcquiredRights()
    {
        var s=New();Assert.Equal("2026-10-28",s.OpeningPickSnapshot);Assert.Equal(672,s.Picks.Count);Assert.Equal(672,s.Picks.Select(p=>p.Id).Distinct().Count());
        foreach(var year in new[]{2027,2028,2029})foreach(var team in s.League.Teams)Assert.Equal(7,s.Picks.Count(p=>p.Year==year&&p.OriginalTeam==team.Name&&!p.Compensatory));
        Assert.Equal("New York Jets",Pick(s,2027,1,"Indianapolis Colts").Owner);Assert.Equal("Cleveland Browns",Pick(s,2029,3,"Los Angeles Rams").Owner);
        Assert.Equal("New York Jets",Pick(s,2029,7,"Las Vegas Raiders").Owner);Assert.Equal("Miami Dolphins",Pick(s,2029,7,"New England Patriots").Owner);
        Assert.Equal("Las Vegas Raiders",Pick(s,2027,6,"San Francisco 49ers").Owner);Assert.Equal("San Francisco 49ers",Pick(s,2027,7,"Kansas City Chiefs").Owner);
        Assert.Equal("Arizona Cardinals",Pick(s,2028,7,"New Orleans Saints").Owner);Assert.Equal("Pittsburgh Steelers",Pick(s,2028,6,"Jacksonville Jaguars").Owner);
        Assert.All(s.Picks.Where(p=>p.AllocationPending),p=>Assert.False(ProTrade.Available(s,p)));Assert.Equal(4,s.PickAllocationConditions.Count);
    }
    [Theory][InlineData(true)][InlineData(false)]public void EarlierLaterRightsResolveByFinalSlotsAndRemainStableAfterTrading(bool dallasEarlier)
    {
        var s=New() with{Year=2027,Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.FreeAgency};
        var order=s.League.Teams.OrderBy(t=>t.Name=="Dallas Cowboys"?(dallasEarlier?0:2):t.Name=="Green Bay Packers"?(dallasEarlier?2:0):1).ThenBy(t=>t.Name).Select((t,i)=>(t.Name,Slot:i+1)).ToDictionary(x=>x.Name,x=>x.Slot);
        s=s with{Picks=s.Picks.Select(p=>p.Year==2027&&!p.Compensatory?p with{Slot=order[p.OriginalTeam]}:p).ToList()};
        var resolved=ProFutureDraftOwnership.ResolveDraftYear(s);Assert.Equal("New York Jets",Pick(resolved,2027,1,dallasEarlier?"Dallas Cowboys":"Green Bay Packers").Owner);
        Assert.Equal("Dallas Cowboys",Pick(resolved,2027,1,dallasEarlier?"Green Bay Packers":"Dallas Cowboys").Owner);Assert.DoesNotContain(resolved.Picks,p=>p.Year==2027&&p.AllocationPending);
        Assert.Contains(resolved.Picks,p=>p.Year==2028&&p.AllocationPending);Assert.All(resolved.PickAllocationConditions.Where(c=>c.Year==2027),c=>Assert.True(c.Resolved));
        var traded=Pick(resolved,2027,1,"Dallas Cowboys");resolved=resolved with{Picks=resolved.Picks.Select(p=>p.Id==traded.Id?p with{Owner="Miami Dolphins"}:p).ToList()};
        Assert.Equal("Miami Dolphins",ProFutureDraftOwnership.ResolveDraftYear(resolved).Picks.Single(p=>p.Id==traded.Id).Owner);
    }
    [Theory][InlineData(0,"Dallas Cowboys")][InlineData(2,"Pittsburgh Steelers")]
    public void DisclosedPorterConditionUsesPriorSeasonAppearances(int appearances,string owner)
    {
        var s=New();var p=s.Players.Single(p=>p.Football.FullName=="Joey Porter Jr.");
        var games=Enumerable.Range(1,2).Select(week=>new ProGame(new ScheduledGame{GameId="condition-"+week,SeasonYear=2026,Week=week,HomeTeamName="Dallas Cowboys",AwayTeamName="Miami Dolphins",HasPlayed=true,HomeScore=7,AwayScore=0,PlayerStats=week<=appearances?[new(){PlayerId=p.Football.PlayerId,PlayerName=p.Football.FullName,TeamName="Dallas Cowboys"}]:[]},ProPhase.RegularSeason)).ToList();
        s=s with{Year=2027,Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.FreeAgency,History=[new(2026,"Dallas Cowboys",games)]};
        Assert.Equal(owner,Pick(ProDraft.Generate(s),2027,6,"Dallas Cowboys").Owner);
    }
    [Fact]public async Task ExistingSavesKeepOwnershipAndConditionalResolutionAcrossLoad()
    {
        var s=New();var asset=Pick(s,2028,1,"New England Patriots");s=s with{Picks=s.Picks.Select(p=>p.Id==asset.Id?p with{Owner="Miami Dolphins"}:p).ToList()};
        var dir=Path.Combine(Path.GetTempPath(),"future-rights-"+Guid.NewGuid());
        try{var store=new ProSaveStore(dir);await store.Save(s);var loaded=await store.Load(s.Id);Assert.Equal("Miami Dolphins",loaded.Picks.Single(p=>p.Id==asset.Id).Owner);Assert.Equal(s.PickAllocationConditions,loaded.PickAllocationConditions);Assert.Equal(s.OpeningPickSnapshot,loaded.OpeningPickSnapshot);Assert.Equal(s.Picks,loaded.Picks);}
        finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
    }
    [Fact]public void MinimumReplacementContractsDoNotRequireArtificialQuarterMillionPremium()
    {
        var s=New() with{Year=2029,SalaryCap=355_300_000};var p=s.Players.First() with{Age=21,Experience=0,CreditedSeasons=0,Football=s.Players.First().Football with{OverallRating=40}};
        Assert.Equal(ProFinance.Minimum(s,p,practice:false),ProCpu.SigningSalary(s,p));
    }

}
