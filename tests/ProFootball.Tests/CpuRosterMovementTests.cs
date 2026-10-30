using DynastyManager.Core.Models;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public class CpuRosterMovementTests
{
    static readonly ProFranchise Template=ProLeagueService.Create("Seattle Seahawks",preserveLegacyPracticeForAudit:true);
    static ProFranchise Setup(int active=52)
    {
        var source=Template.Players.First();var team=Template.UserTeam;
        var players=Enumerable.Range(0,active+2).Select(i=>source with{Age=23,Experience=1,CreditedSeasons=1,Reserve=i==active+1,PracticeSquad=i==active,ReserveInjury=null,RosterStatus=i==active+1?"RES":i==active?"PS":"ACT",Football=source.Football with{PlayerId=Guid.NewGuid(),TeamName=team,Position=Position.QB,OverallRating=i>=active?90:65,CurrentInjury=null}}).ToList();
        var contracts=players.Select(p=>new ProContract(p.Football.PlayerId,team,2026,2,0,[new(2026,p.PracticeSquad?400_000:1_500_000,0),new(2027,1_500_000,0)])).ToList();
        return Template with{Phase=ProPhase.RegularSeason,Week=6,Players=players,Contracts=contracts,TeamCapAllocations=[],DeadCap=[],SalaryCap=900_000_000,Bids=[],Waivers=[]};
    }
    [Fact]public void InternalPracticeDepthPromotesBeforeMarketAndFullRosterSwapKeepsContracts()
    {
        var s=Setup(53);var ps=s.Players.Single(p=>p.PracticeSquad);var reserve=s.Players.Single(p=>p.Reserve);
        s=s with{Players=s.Players.Select(p=>p==reserve?p with{RosterStatus="PUP"}:p).ToList()};
        var moved=ProCpuRoster.Manage(s,s.UserTeam);
        Assert.Equal(53,ProRoster.Active(moved,s.UserTeam));Assert.False(moved.Players.Single(p=>p.Football.PlayerId==ps.Football.PlayerId).PracticeSquad);
        Assert.Single(moved.Players,p=>p.PracticeSquad);Assert.True(moved.Players.Single(p=>p.Football.PlayerId==reserve.Football.PlayerId).Reserve);
        Assert.All(s.Contracts,c=>Assert.Contains(moved.Contracts,x=>x.PlayerId==c.PlayerId&&x.Active&&x.SigningBonus==c.SigningBonus));Assert.Equal(s.DeadCap,moved.DeadCap);
        Assert.True(ProEconomy.Room(moved,s.UserTeam)>=0);Assert.Equal("ACT",moved.Players.Single(p=>p.Football.PlayerId==ps.Football.PlayerId).RosterStatus);
    }
    [Fact]public void EligibleReserveReturnsButInjuredAndUnclearedReservesStayParked()
    {
        var s=Setup(51);var ps=s.Players.Single(p=>p.PracticeSquad);s=s with{Players=s.Players.Select(p=>p==ps?p with{Football=p.Football with{CurrentInjury=new(){WeeksRemaining=2}}}:p).ToList()};
        var reserve=s.Players.Single(p=>p.Reserve);var next=ProCpuRoster.Manage(s,s.UserTeam);
        Assert.False(next.Players.Single(p=>p.Football.PlayerId==reserve.Football.PlayerId).Reserve);Assert.True(next.Players.Single(p=>p.Football.PlayerId==ps.Football.PlayerId).PracticeSquad);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(s.Contracts),System.Text.Json.JsonSerializer.Serialize(next.Contracts));Assert.Equal(52,ProRoster.Active(next,s.UserTeam));
    }
    [Fact]public void UnaffordableFullRosterSwapRollsBackEveryMove()
    {
        var s=Setup(53);s=s with{SalaryCap=1,TeamCapAllocations=[]};var next=ProCpuRoster.Manage(s,s.UserTeam);
        Assert.Equal(s.Players,next.Players);Assert.Equal(s.Contracts,next.Contracts);Assert.Equal(s.Transactions,next.Transactions);
    }
    [Fact]public void FinalCutsRetainSignedDepthAndRespectPracticeVeteranLimit()
    {
        var s=Setup(56);s=s with{Players=s.Players.Where(p=>!p.Reserve&&!p.PracticeSquad).ToList()};
        var next=ProCpu.Trim(s,s.UserTeam);Assert.Equal(53,ProRoster.Active(next,s.UserTeam));Assert.Equal(3,next.Players.Count(p=>p.PracticeSquad));Assert.Equal(s.Contracts,next.Contracts);Assert.Empty(next.DeadCap);
        var veterans=next.Players.Take(6).Select(p=>p with{PracticeSquad=true,Experience=5}).ToArray();
        var candidate=next.Players.Last() with{PracticeSquad=false,Experience=5};var full=next with{Players=veterans.Append(candidate).ToList()};
        Assert.Throws<InvalidOperationException>(()=>ProRoster.Practice(full,full.UserTeam,candidate.Football.PlayerId,true));
    }
    [Fact]public void RoleFilterPreservesUserControlledUnitAndRepeatedManagementDoesNotChurn()
    {
        var s=Setup();var next=ProCpuRoster.Manage(s,s.UserTeam,_=>false);Assert.Equal(s,next);
        next=ProCpuRoster.Manage(s,s.UserTeam);Assert.Equal(next,ProCpuRoster.Manage(next,next.UserTeam));
    }
    [Fact]public void PracticeRecruitmentStopsAtSixteenAndExcludesWaiversAndStars()
    {
        var s=Setup(53);var template=s.Players[0];
        var free=Enumerable.Range(0,20).Select(i=>template with{Experience=0,CreditedSeasons=0,Football=template.Football with{PlayerId=Guid.NewGuid(),TeamName="Free agent",OverallRating=i==0?99:40},Reserve=false,PracticeSquad=false}).ToArray();
        s=s with{Players=s.Players.Where(p=>!p.PracticeSquad&&!p.Reserve).Concat(free).ToList(),Contracts=s.Contracts.Where(c=>s.Players.Take(53).Any(p=>p.Football.PlayerId==c.PlayerId)).ToList(),Waivers=[new(free[1].Football.PlayerId,2,[])]};
        var next=ProCpuRoster.FillPractice(s,s.UserTeam);Assert.Equal(16,ProInternationalPractice.OrdinaryPlayers(next,s.UserTeam));Assert.Equal(53,ProRoster.Active(next,s.UserTeam));
        Assert.Equal("Free agent",next.Players.Single(p=>p.Football.PlayerId==free[0].Football.PlayerId).Football.TeamName);Assert.Equal("Free agent",next.Players.Single(p=>p.Football.PlayerId==free[1].Football.PlayerId).Football.TeamName);
        Assert.True(ProEconomy.Room(next,s.UserTeam)>=0);Assert.All(next.Players.Where(p=>p.PracticeSquad),p=>Assert.Single(next.Contracts,c=>c.Active&&c.PlayerId==p.Football.PlayerId));
    }
    [Fact]public void PracticePromotionRequiresContractAndPendingCapAndHealth()
    {
        var s=Setup();var p=s.Players.Single(p=>p.PracticeSquad);
        Assert.Throws<InvalidOperationException>(()=>ProRoster.Practice(s with{Contracts=s.Contracts.Where(c=>c.PlayerId!=p.Football.PlayerId).ToList()},s.UserTeam,p.Football.PlayerId,false));
        Assert.Throws<InvalidOperationException>(()=>ProRoster.Practice(s with{Bids=[new(Guid.NewGuid(),s.UserTeam,1,900_000_000,0,0)]},s.UserTeam,p.Football.PlayerId,false));
        var injured=s with{Players=s.Players.Select(x=>x==p?x with{Football=x.Football with{CurrentInjury=new(){WeeksRemaining=1}}}:x).ToList()};
        Assert.Throws<InvalidOperationException>(()=>ProRoster.Practice(injured,s.UserTeam,p.Football.PlayerId,false));
    }

    [Fact]public async Task WholeLeagueCpuUsesInternalDepthWithoutChangingUserTeamAndSurvivesSave()
    {
        var s=Template with{Phase=ProPhase.RegularSeason,Week=12,SalaryCap=900_000_000,TeamCapAllocations=[]};
        var team=s.League.Teams.First(t=>t.Name!=s.UserTeam&&s.Players.Any(p=>p.Football.TeamName==t.Name&&p.Reserve&&p.ReserveInjury is {SeasonEnding:false})).Name;
        var ps=s.Players.First(p=>p.Football.TeamName==team&&p.PracticeSquad);var reserve=s.Players.First(p=>p.Football.TeamName==team&&p.Reserve&&p.ReserveInjury is {SeasonEnding:false});
        foreach(var p in s.Players.Where(p=>p.Football.TeamName==team&&!p.Reserve&&!p.PracticeSquad).Take(4).ToArray())s=ProEconomy.Release(s,team,p.Football.PlayerId,false);
        s=s with{Players=s.Players.Select(p=>p==ps||p==reserve?p with{ReserveInjury=null,RosterStatus=p.Reserve?"RES":p.RosterStatus,Football=p.Football with{OverallRating=99,CurrentInjury=null}}:p).ToList()};
        var user=s.Players.Where(p=>p.Football.TeamName==s.UserTeam).ToArray();var contracts=s.Contracts.Where(c=>c.Team==s.UserTeam).ToArray();
        var next=ProCpu.Manage(s);Assert.All(new[]{ps,reserve},p=>{var moved=next.Players.Single(x=>x.Football.PlayerId==p.Football.PlayerId);Assert.False(moved.PracticeSquad);Assert.False(moved.Reserve);});
        Assert.Equal(user,next.Players.Where(p=>p.Football.TeamName==s.UserTeam));Assert.Equal(contracts,next.Contracts.Where(c=>c.Team==s.UserTeam));
        Assert.All(next.League.Teams.Where(t=>t.Name!=s.UserTeam),t=>{Assert.InRange(ProRoster.Active(next,t.Name),0,53);Assert.InRange(ProInternationalPractice.OrdinaryPlayers(next,t.Name),0,16);Assert.InRange(next.Players.Count(p=>p.Football.TeamName==t.Name&&p.PracticeSquad&&p.Experience>2),0,6);});
        var dir=Path.Combine(Path.GetTempPath(),"cpu-roster-"+Guid.NewGuid());
        next=ProPracticeRetirement.Apply(next);
        try{var store=new ProSaveStore(dir);await store.Save(next);var loaded=await store.Load(next.Id);Assert.Equal(next.Players,loaded.Players);Assert.Equal(System.Text.Json.JsonSerializer.Serialize(next.Contracts),System.Text.Json.JsonSerializer.Serialize(loaded.Contracts));}
        finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
    }

    [Fact]public void DemotingAndRepromotingDraftedRookieDoesNotRemoveOriginalDealProtection()
    {
        var s=Setup(51);var p=s.Players[0] with{RookieYear=s.Year,DraftOverall=20,Experience=0,CreditedSeasons=0};
        var c=new ProContract(p.Football.PlayerId,s.UserTeam,s.Year,4,1_000_000,Enumerable.Range(s.Year,4).Select(y=>new ContractYear(y,2_000_000,0)).ToList()){OriginalRookieYear=s.Year};
        s=s with{Players=s.Players.Select(x=>x.Football.PlayerId==p.Football.PlayerId?p:x).ToList(),Contracts=s.Contracts.Select(x=>x.PlayerId==p.Football.PlayerId?c:x).ToList()};
        var next=ProRoster.Practice(ProRoster.Practice(s,s.UserTeam,p.Football.PlayerId,true),s.UserTeam,p.Football.PlayerId,false);
        var deal=next.Contracts.Single(x=>x.PlayerId==p.Football.PlayerId);Assert.False(deal.PracticeOrigin);Assert.True(ProContractInsights.RookieDeal(next,p,deal));Assert.Equal(s.Year+3,ProContractInsights.PlayingEnd(deal));
    }

    [Fact]public void FullPracticeSquadDoesNotStrandSuperiorClearedReserve()
    {
        var s=Setup(53);var reserve=s.Players.Single(p=>p.Reserve);var ps=s.Players.Single(p=>p.PracticeSquad);
        var practice=Enumerable.Range(0,15).Select(_=>ps with{Football=ps.Football with{PlayerId=Guid.NewGuid(),OverallRating=40}}).ToArray();
        s=s with{Players=s.Players.Select(p=>p==ps?p with{Football=p.Football with{OverallRating=40}}:p).Concat(practice).ToList(),Contracts=s.Contracts.Concat(practice.Select(p=>new ProContract(p.Football.PlayerId,s.UserTeam,s.Year,1,0,[new(s.Year,400_000,0)]))).ToList()};
        var next=ProCpuRoster.Manage(s,s.UserTeam);Assert.False(next.Players.Single(p=>p.Football.PlayerId==reserve.Football.PlayerId).Reserve);Assert.Equal(53,ProRoster.Active(next,s.UserTeam));Assert.Equal(16,ProInternationalPractice.OrdinaryPlayers(next,s.UserTeam));
        Assert.Single(next.Contracts,c=>!c.Active);Assert.Single(next.Waivers);Assert.True(ProEconomy.Room(next,s.UserTeam)>=0);
        var practiceDeal=next.Contracts.First(c=>c.Active) with{PracticeOrigin=true,OriginalRookieYear=s.Year-3};Assert.Null(ProRookieOptions.Origin(ps,practiceDeal));
    }

}
