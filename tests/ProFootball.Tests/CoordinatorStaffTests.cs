using DynastyManager.Core.Models;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public class CoordinatorStaffTests
{
    static (ProFranchise State,ProPlayer Own,ProPlayer Other) Setup(StaffRole role)
    {
        var s=ProLeagueService.Create("Seattle Seahawks",role:role,coachName:"Coordinator QA");
        var ownPos=role==StaffRole.DefensiveCoordinator?Position.DE:Position.QB;var otherPos=ownPos==Position.DE?Position.QB:Position.DE;
        ProPlayer Player(Position pos)
        {
            var p=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&!p.Retired&&!p.PracticeSquad&&!p.Reserve&&p.Experience>=4&&p.Football.Position==pos);
            return p with{Age=27,Football=p.Football with{OverallRating=80,PotentialRating=85,CurrentInjury=null}};
        }
        var own=Player(ownPos);var other=Player(otherPos);
        s=s with{Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.Retention,SalaryCap=2_000_000_000,TeamCapAllocations=[],Players=s.Players.Select(p=>p.Football.PlayerId==own.Football.PlayerId?own:p.Football.PlayerId==other.Football.PlayerId?other:p).ToList(),
            Contracts=new[]{own,other}.Select(p=>new ProContract(p.Football.PlayerId,s.UserTeam,s.Year-1,2,0,[new(s.Year,1_000_000,0)])).ToList()};
        return(s,own,other);
    }
    [Theory] [InlineData(StaffRole.OffensiveCoordinator)] [InlineData(StaffRole.DefensiveCoordinator)]
    public void AutomaticStaffNegotiatesOnlyTheUncontrolledUnitAndKeepsCareerIdentity(StaffRole role)
    {
        var(s,own,other)=Setup(role);var next=ProRetention.NegotiateStaffUnit(s);
        Assert.Contains(ProRetention.Expiring(next,s.UserTeam),p=>p.Football.PlayerId==own.Football.PlayerId);
        Assert.DoesNotContain(ProRetention.Expiring(next,s.UserTeam),p=>p.Football.PlayerId==other.Football.PlayerId);
        Assert.Same(s.Career,next.Career);Assert.Equal(role,next.Career!.Role);Assert.True(Assert.Single(next.GmDecisions).Approved);
        Assert.Same(s.Contracts[0],next.Contracts[0]);Assert.Equal(ProFinance.ContractChangeYear(s),next.Contracts[1].LastChangedYear);
        Assert.Throws<InvalidOperationException>(()=>ProManagement.Submit(s,"Direct other-unit extension",[other.Football.PlayerId],0,x=>x));
    }
    [Theory] [InlineData(StaffRole.OffensiveCoordinator,false)] [InlineData(StaffRole.DefensiveCoordinator,false)]
    [InlineData(StaffRole.OffensiveCoordinator,true)] [InlineData(StaffRole.DefensiveCoordinator,true)]
    public void OffseasonAdvancementHandlesOtherUnitWithoutTakingManualOwnUnitDecisions(StaffRole role,bool delegateOwn)
    {
        var(s,own,other)=Setup(role);s=s with{AutoNegotiateExpiring=delegateOwn};
        if(!delegateOwn&&Environment.GetEnvironmentVariable("PRO_COORDINATOR_UI_FIXTURES") is string directory)
        {
            var fixture=s with{Id=Guid.NewGuid(),Name="Synthetic retention QA "+role,Games=[]};
            var path=Path.Combine(directory,"pro-coordinator-ui-"+role+".json.gz");
            using var file=File.Create(path);using var gzip=new System.IO.Compression.GZipStream(file,System.IO.Compression.CompressionLevel.SmallestSize);
            System.Text.Json.JsonSerializer.Serialize(gzip,new{franchise=fixture,ownPlayer=own.Football.PlayerId,otherPlayer=other.Football.PlayerId},new System.Text.Json.JsonSerializerOptions{PropertyNamingPolicy=System.Text.Json.JsonNamingPolicy.CamelCase});
        }
        var next=ProOffseason.Advance(s);
        Assert.Equal(2027,next.Year);Assert.Equal(OffseasonStep.FreeAgency,next.OffseasonStep);
        Assert.Equal(s.UserTeam,next.Players.Single(p=>p.Football.PlayerId==other.Football.PlayerId).Football.TeamName);
        Assert.Equal(delegateOwn?s.UserTeam:"Free agent",next.Players.Single(p=>p.Football.PlayerId==own.Football.PlayerId).Football.TeamName);
        Assert.Equal(role,next.Career!.Role);Assert.True(ProFinance.Budget(next,s.UserTeam).Available>=0);
    }
    [Fact] public void UnemployedFormerTeamRetainsPlayersWithoutUserDelegationOrAuthority()
    {
        var(s,own,other)=Setup(StaffRole.OffensiveCoordinator);s=s with{Career=s.Career! with{Unemployed=true},AutoNegotiateExpiring=false};
        var next=ProOffseason.Advance(s);Assert.True(next.Career!.Unemployed);
        Assert.All(new[]{own,other},p=>Assert.Equal(s.UserTeam,next.Players.Single(x=>x.Football.PlayerId==p.Football.PlayerId).Football.TeamName));
        Assert.Throws<InvalidOperationException>(()=>ProManagement.Submit(next,"User command",[],0,x=>x));
    }
    [Theory] [InlineData(false)] [InlineData(true)] public void HeadCoachManualRetentionAndLetExpireChoicesArePreserved(bool delegated)
    {
        var(s,own,other)=Setup(StaffRole.HeadCoach);s=s with{AutoNegotiateExpiring=delegated};Assert.Same(s,ProRetention.NegotiateStaffUnit(s));
        var next=ProOffseason.Advance(s);Assert.All(new[]{own,other},p=>Assert.Equal(delegated?s.UserTeam:"Free agent",next.Players.Single(x=>x.Football.PlayerId==p.Football.PlayerId).Football.TeamName));
        var(c,_,opposite)=Setup(StaffRole.OffensiveCoordinator);c=c with{LetExpire=[opposite.Football.PlayerId]};
        Assert.Same(c,ProRetention.NegotiateStaffUnit(c));
    }
    [Fact] public void ThreeCoordinatorSeasonsRetainLegalRostersAndBudgetsWithExplicitGmDelegation()
    {
        var s=ProLeagueService.Create("Seattle Seahawks",role:StaffRole.OffensiveCoordinator) with{AutoNegotiateExpiring=true,AutoManageRookieOptions=true};
        for(var season=0;season<3;season++)
        {
            s=ProCpu.Manage(s,true);
            for(var tick=0;tick<26&&s.Phase!=ProPhase.Offseason;tick++)
            {
                if(s.Phase==ProPhase.Preseason&&s.Week==3)s=ProCpu.Trim(s,s.UserTeam);
                s=ProLeagueService.Advance(s);
            }
            Assert.Equal(ProPhase.Offseason,s.Phase);s=ProOffseason.Advance(s);s=ProOffseason.Advance(s);s=ProDraft.AutoUntilUser(s,true);
            s=ProOffseason.Advance(s);s=ProCpu.Trim(s,s.UserTeam);
            Assert.True(ProEconomy.Room(s,s.UserTeam)>=0,$"Before camp {s.Year}: active {ProRoster.Active(s,s.UserTeam)}, roster {ProRoster.CountForLimit(s,s.UserTeam)}, room {ProEconomy.Room(s,s.UserTeam)}, debt {s.DeadCap.Where(d=>d.Team==s.UserTeam&&d.Year==s.Year).Sum(d=>d.Amount)}");
            s=ProOffseason.Advance(s);s=ProCpu.Manage(s,true);
            Assert.Equal(StaffRole.OffensiveCoordinator,s.Career!.Role);Assert.False(s.Career.Unemployed);Assert.Empty(s.UnsignedRookies);
            Assert.All(s.League.Teams,t=>{Assert.True(ProEconomy.Room(s,t.Name)>=0,t.Name);Assert.InRange(ProRoster.Active(s,t.Name),45,90);});
        }
    }
    [Theory] [InlineData(StaffRole.OffensiveCoordinator)] [InlineData(StaffRole.DefensiveCoordinator)]
    public void StaffFutureUnitBudgetRejectionDiscardsTheProposedContract(StaffRole role)
    {
        var(s,own,other)=Setup(role);var reserve=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&!p.Retired&&!p.PracticeSquad&&!p.Reserve&&!ProCareer.Controls(s,p.Football.Position)&&p.Football.PlayerId!=other.Football.PlayerId);
        var salary=(long)(ProFinance.Limit(s,s.UserTeam,s.Year+1)*.55)-500_000;
        s=s with{Contracts=s.Contracts.Append(new ProContract(reserve.Football.PlayerId,s.UserTeam,s.Year,3,0,[new(s.Year,1_500_000,0),new(s.Year+1,salary,0),new(s.Year+2,salary,0)])).ToList()};
        var next=ProRetention.NegotiateStaffUnit(s);Assert.Same(s.Contracts,next.Contracts);Assert.Same(s.Players,next.Players);
        var decision=Assert.Single(next.GmDecisions);Assert.False(decision.Approved);Assert.Contains("unit commitments",decision.Reason);
        Assert.Equal(role,next.Career!.Role);Assert.Equal(0,next.Contracts.Single(c=>c.PlayerId==other.Football.PlayerId).LastChangedYear);
    }
}
