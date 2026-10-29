using DynastyManager.Core.Models;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public class CoordinatorRosterTests
{
    static (ProFranchise State,ProPlayer Candidate) Setup(StaffRole role)
    {
        var s=ProLeagueService.Create("Seattle Seahawks",role:role);
        var removed=s.Players.Where(p=>p.Football.TeamName==s.UserTeam&&!p.Retired&&!p.Reserve&&!p.PracticeSquad&&!ProCareer.Controls(s,p.Football.Position)).Take(3).Select(p=>p.Football.PlayerId).ToHashSet();
        var candidate=s.Players.First(p=>removed.Contains(p.Football.PlayerId));
        candidate=candidate with{Age=25,Experience=3,CreditedSeasons=3,Football=candidate.Football with{TeamName="Free agent",OverallRating=55,PotentialRating=55}};
        s=s with{Phase=ProPhase.RegularSeason,Week=10,SalaryCap=2_000_000_000,TeamCapAllocations=[],
            Players=s.Players.Where(p=>!removed.Contains(p.Football.PlayerId)).Select(p=>p.Football.TeamName=="Free agent"?p with{Retired=true}:p).Append(candidate).ToList(),
            Contracts=s.Contracts.Where(c=>!removed.Contains(c.PlayerId)).ToList()};
        // Isolate the one-player market using legal, full opposing rosters, not an invalid one-team league.
        foreach(var team in s.League.Teams.Where(t=>t.Name!=s.UserTeam))
            while(ProRoster.Active(s,team.Name)<53)
            {
                var reserve=s.Players.First(p=>p.Football.TeamName==team.Name&&p.PracticeSquad&&!p.Reserve&&!p.Retired);
                s=ProRoster.Practice(s,team.Name,reserve.Football.PlayerId,false);
            }
        // Isolate this one-player market from unrelated CPU reserve-return waivers.
        s=s with{Players=s.Players.Select(p=>p.Reserve&&p.Football.TeamName!=s.UserTeam?p with{RosterStatus="PUP"}:p).ToList()};
        // Isolate bidding from legacy veteran-squad corrections that would add other free agents.
        s=s with{Players=s.Players.Select(p=>p.PracticeSquad&&p.Football.TeamName!=s.UserTeam?p with{Experience=0,CreditedSeasons=0}:p).ToList()};
        // This fixture isolates market bidding. Healthy internal depth now correctly takes priority over free agents.
        s=s with{Players=s.Players.Select(p=>p.Football.TeamName==s.UserTeam&&!ProCareer.Controls(s,p.Football.Position)&&(p.Reserve||p.PracticeSquad)?p with{Football=p.Football with{CurrentInjury=new(){WeeksRemaining=2}}}:p).ToList()};
        return(s,candidate);
    }
    static void Fixture(ProFranchise s,ProPlayer p,StaffRole role,string kind)
    {
        if(Environment.GetEnvironmentVariable("PRO_COORDINATOR_UI_FIXTURES") is not string directory)return;
        var fixture=s with{Id=Guid.NewGuid(),Name="Synthetic staff "+kind+" QA "+role,Games=[]};
        ProLeagueService.Validate(fixture.League);
        using var file=File.Create(Path.Combine(directory,"pro-staff-"+kind+"-ui-"+role+".json.gz"));
        using var gzip=new System.IO.Compression.GZipStream(file,System.IO.Compression.CompressionLevel.SmallestSize);
        System.Text.Json.JsonSerializer.Serialize(gzip,new{franchise=fixture,player=p.Football.PlayerId,controlledPlayers=s.Players.Where(x=>x.Football.TeamName==s.UserTeam&&ProCareer.Controls(s,x.Football.Position)).Select(x=>x.Football.PlayerId).ToArray()},new System.Text.Json.JsonSerializerOptions{PropertyNamingPolicy=System.Text.Json.JsonNamingPolicy.CamelCase});
    }
    [Theory] [InlineData(StaffRole.OffensiveCoordinator)] [InlineData(StaffRole.DefensiveCoordinator)]
    public void AutomaticRosterFillSignsOtherUnitWithoutChangingControlledContractsOrCareer(StaffRole role)
    {
        var(s,p)=Setup(role);var own=s.Players.Where(x=>x.Football.TeamName==s.UserTeam&&ProCareer.Controls(s,x.Football.Position)).Select(x=>x.Football.PlayerId).ToHashSet();
        Fixture(s,p,role,"roster");
        var next=ProCpu.Manage(s);
        Assert.Equal(s.UserTeam,next.Players.Single(x=>x.Football.PlayerId==p.Football.PlayerId).Football.TeamName);
        Assert.Equal(s.Contracts.Where(c=>own.Contains(c.PlayerId)),next.Contracts.Where(c=>own.Contains(c.PlayerId)));
        Assert.All(own,id=>Assert.Equal(s.UserTeam,next.Players.Single(x=>x.Football.PlayerId==id).Football.TeamName));
        Assert.Same(s.Career,next.Career);Assert.Contains(next.GmDecisions,d=>d.Approved&&d.Action.StartsWith("Staff free-agent offer"));
        Assert.InRange(ProRoster.Active(next,s.UserTeam),0,53);Assert.True(ProEconomy.Room(next,s.UserTeam)>=0);
    }
    [Theory] [InlineData(StaffRole.OffensiveCoordinator)] [InlineData(StaffRole.DefensiveCoordinator)]
    public void AutomaticOfferCannotBypassFutureOtherUnitBudget(StaffRole role)
    {
        var(s,p)=Setup(role);var other=s.Players.First(x=>x.Football.TeamName==s.UserTeam&&!ProCareer.Controls(s,x.Football.Position));
        var nextYear=s.Year+1;var cap=(long)(ProFinance.Limit(s,s.UserTeam,nextYear)*.55);
        s=s with{Contracts=[new(other.Football.PlayerId,s.UserTeam,s.Year,2,0,[new(s.Year,1_500_000,0),new(nextYear,cap-500_000,0)])]};
        var next=ProCpu.Manage(s);
        Assert.Equal("Free agent",next.Players.Single(x=>x.Football.PlayerId==p.Football.PlayerId).Football.TeamName);
        Assert.Equal(s.Contracts,next.Contracts);Assert.Empty(next.Bids);
        Assert.Contains(next.GmDecisions,d=>!d.Approved&&d.Reason.Contains(nextYear.ToString()));
    }
    [Fact] public void ManualHeadCoachRosterIsNotFilledAutomatically()
    {
        var(s,p)=Setup(StaffRole.OffensiveCoordinator);s=s with{Career=s.Career! with{Role=StaffRole.HeadCoach}};
        var next=ProCpu.Manage(s);Assert.Equal("Free agent",next.Players.Single(x=>x.Football.PlayerId==p.Football.PlayerId).Football.TeamName);
        Assert.DoesNotContain(next.GmDecisions,d=>d.Action.StartsWith("Staff free-agent"));
    }
    static (ProFranchise State,ProPlayer Other,ProPlayer Own) Draft(StaffRole role)
    {
        var(s,candidate)=Setup(role);
        var own=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&ProCareer.Controls(s,p.Football.Position));
        candidate=candidate with{Football=candidate.Football with{PlayerId=Guid.NewGuid(),TeamName="Draft class",OverallRating=95},Experience=0,CreditedSeasons=0,Age=21};
        own=own with{Football=own.Football with{PlayerId=Guid.NewGuid(),TeamName="Draft class",OverallRating=40},Experience=0,CreditedSeasons=0,Age=21};
        return(s with{Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.Draft,Prospects=[candidate,own],DraftIndex=0,
            Picks=[new(Guid.NewGuid(),s.Year,1,1,s.UserTeam,s.UserTeam),new(Guid.NewGuid(),s.Year,1,2,s.UserTeam,s.UserTeam)]},candidate,own);
    }
    [Theory] [InlineData(StaffRole.OffensiveCoordinator)] [InlineData(StaffRole.DefensiveCoordinator)]
    public void GmDraftsOtherUnitThenStopsForCoordinatorsOwnRecommendation(StaffRole role)
    {
        var(s,other,own)=Draft(role);Fixture(s,other,role,"draft");var next=ProDraft.AutoUntilUser(s);
        Assert.Equal(1,next.DraftIndex);Assert.Equal(other.Football.PlayerId,Assert.Single(next.DraftHistory).Player.Football.PlayerId);
        Assert.Contains(next.Prospects,p=>p.Football.PlayerId==own.Football.PlayerId);Assert.Same(s.Career,next.Career);
        Assert.True(Assert.Single(next.GmDecisions).Approved);Assert.Empty(next.UnsignedRookies);
        Assert.Equal(4,next.Contracts.Single(c=>c.PlayerId==other.Football.PlayerId).Years.Count);
        var hc=s with{Career=s.Career! with{Role=StaffRole.HeadCoach}};Assert.Equal(0,ProDraft.AutoUntilUser(hc).DraftIndex);
    }
    [Theory] [InlineData(StaffRole.OffensiveCoordinator)] [InlineData(StaffRole.DefensiveCoordinator)]
    public void UnaffordableStaffRookieKeepsDraftRightsWithoutBypassingFutureBudget(StaffRole role)
    {
        var(s,other,_)=Draft(role);var veteran=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&!ProCareer.Controls(s,p.Football.Position));
        var year=s.Year+1;var cap=(long)(ProFinance.Limit(s,s.UserTeam,year)*.55);
        s=s with{Contracts=[new(veteran.Football.PlayerId,s.UserTeam,s.Year,2,0,[new(s.Year,1_500_000,0),new(year,cap-500_000,0)])]};
        var next=ProDraft.AutoUntilUser(s);Assert.Equal(1,next.DraftIndex);Assert.Equal(other.Football.PlayerId,Assert.Single(next.UnsignedRookies).PlayerId);
        Assert.DoesNotContain(next.Contracts,c=>c.PlayerId==other.Football.PlayerId);Assert.False(Assert.Single(next.GmDecisions).Approved);
        next=ProDraft.SignRookies(next,s.UserTeam);Assert.Single(next.UnsignedRookies);Assert.Equal(s.Contracts,next.Contracts);
        var funded=ProDraft.SignRookies(next with{Contracts=[]},s.UserTeam);Assert.Empty(funded.UnsignedRookies);
        Assert.False(funded.Players.Single(p=>p.Football.PlayerId==other.Football.PlayerId).Reserve);
        Assert.Equal(4,Assert.Single(funded.Contracts).Years.Count);Assert.Same(s.Career,funded.Career);
    }
    [Theory] [InlineData(StaffRole.OffensiveCoordinator)] [InlineData(StaffRole.DefensiveCoordinator)]
    public void WaiverClaimsAndSigningRespectOtherUnitAuthorityAndRecheckBudget(StaffRole role)
    {
        var(s,p)=Setup(role);s=s with{Players=s.Players.Where(x=>x.Football.TeamName!=s.UserTeam||x.Football.Position!=p.Football.Position).ToList(),Waivers=[new(p.Football.PlayerId,0,[])]};
        var claimed=ProCpu.Manage(s);Assert.Contains(s.UserTeam,claimed.Waivers.Single(w=>w.PlayerId==p.Football.PlayerId).Claims);
        var signed=ProRoster.ResolveWaivers(claimed);Assert.Equal(s.UserTeam,signed.Players.Single(x=>x.Football.PlayerId==p.Football.PlayerId).Football.TeamName);
        var other=s.Players.First(x=>x.Football.TeamName==s.UserTeam&&!ProCareer.Controls(s,x.Football.Position));
        var ceiling=(long)(ProFinance.Limit(s,s.UserTeam,s.Year)*.55);
        var changed=claimed with{Contracts=[new(other.Football.PlayerId,s.UserTeam,s.Year,1,0,[new(s.Year,ceiling-500_000,0)])]};
        var rejected=ProRoster.ResolveWaivers(changed);Assert.Equal("Free agent",rejected.Players.Single(x=>x.Football.PlayerId==p.Football.PlayerId).Football.TeamName);
        Assert.Equal(changed.Contracts,rejected.Contracts);Assert.False(rejected.GmDecisions.Last().Approved);Assert.Same(s.Career,rejected.Career);
    }
    [Theory] [InlineData(StaffRole.OffensiveCoordinator)] [InlineData(StaffRole.DefensiveCoordinator)]
    public void FinalCutsRemoveOnlyStaffSurplusAndPreserveControlledContracts(StaffRole role)
    {
        var(s,_)=Setup(role);var position=role==StaffRole.OffensiveCoordinator?Position.DE:Position.WR;
        var template=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&p.Football.Position==position&&!p.PracticeSquad&&!p.Reserve&&!p.Retired);
        var players=s.Players.ToList();var contracts=s.Contracts.ToList();
        while(players.Count(p=>p.Football.TeamName==s.UserTeam&&!p.PracticeSquad&&!p.Reserve&&!p.Retired)<54)
        {
            var p=template with{Football=template.Football with{PlayerId=Guid.NewGuid(),OverallRating=40}};players.Add(p);
            contracts.Add(new(p.Football.PlayerId,s.UserTeam,s.Year,1,0,[new(s.Year,1_500_000,0)]));
        }
        s=s with{Players=players,Contracts=contracts};var own=s.Players.Where(p=>p.Football.TeamName==s.UserTeam&&ProCareer.Controls(s,p.Football.Position)).Select(p=>p.Football.PlayerId).ToHashSet();
        var next=ProCpu.FinalizeStaffRoster(s);Assert.Equal(53,ProRoster.Active(next,s.UserTeam));
        Assert.Equal(s.Contracts.Where(c=>own.Contains(c.PlayerId)),next.Contracts.Where(c=>own.Contains(c.PlayerId)));
        Assert.All(own,id=>Assert.Equal(s.UserTeam,next.Players.Single(p=>p.Football.PlayerId==id).Football.TeamName));
        Assert.True(Assert.Single(next.GmDecisions).Approved);Assert.Same(s.Career,next.Career);
    }
    [Theory] [InlineData(StaffRole.OffensiveCoordinator)] [InlineData(StaffRole.DefensiveCoordinator)]
    public void RejectedStaffRestructureRollsBackWithoutTouchingControlledContracts(StaffRole role)
    {
        var(s,_)=Setup(role);var own=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&ProCareer.Controls(s,p.Football.Position));
        var other=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&!ProCareer.Controls(s,p.Football.Position)&&p.Experience>=4);
        var future=(long)(ProFinance.Limit(s,s.UserTeam,s.Year+1)*.55);
        s=s with{Contracts=[new(own.Football.PlayerId,s.UserTeam,s.Year-1,2,0,[new(s.Year,1_100_000_000,0)]),
            new(other.Football.PlayerId,s.UserTeam,s.Year-1,3,0,[new(s.Year,1_000_000_000,0),new(s.Year+1,future,0)])]};
        var next=ProCpu.FinalizeStaffRoster(s);
        Assert.Equal(s.Contracts,next.Contracts);Assert.Equal(s.Players,next.Players);Assert.Equal(s.DeadCap,next.DeadCap);Assert.Equal(s.SalaryCap,next.SalaryCap);
        Assert.True(ProEconomy.Room(next,s.UserTeam)<0);Assert.Contains(next.GmDecisions,d=>!d.Approved&&d.Reason.Contains((s.Year+1).ToString()));
    }
    [Fact] public async Task ScopedDeviceFixturesPassTheRealSaveLoader()
    {
        var root=Path.Combine(Path.GetTempPath(),"pro-coordinator-fixture-"+Guid.NewGuid());
        try
        {
            var store=new ProSaveStore(root);
            foreach(var role in new[]{StaffRole.OffensiveCoordinator,StaffRole.DefensiveCoordinator})
            {
                var(roster,_)=Setup(role);var(draft,_,_)=Draft(role);
                foreach(var state in new[]{roster,draft})
                {
                    await store.Save(state);var loaded=await store.Load(state.Id);
                    Assert.Equal(32,loaded.League.Teams.Count);Assert.Equal(state.Career,loaded.Career);
                    Assert.Equal(state.Prospects.Count,loaded.Prospects.Count);Assert.Equal(state.Phase,loaded.Phase);
                }
            }
        }
        finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }
}
