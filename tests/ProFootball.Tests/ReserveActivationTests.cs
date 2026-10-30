using System.Text.Json;
using DynastyManager.Core.Models;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public sealed class ReserveActivationTests
{
    static readonly ProFranchise Template=ProLeagueService.Create("Seattle Seahawks",preserveLegacyPracticeForAudit:true);
    static (ProFranchise State,ProPlayer Player,ProContract Contract) Setup()
    {
        var s=Template with{Phase=ProPhase.RegularSeason,Week=6,SalaryCap=900_000_000,TeamCapAllocations=[]};
        var p=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&p.Reserve&&p.ReserveInjury is {SeasonEnding:false});
        p=p with{ReserveInjury=null,RosterStatus="RES",Football=p.Football with{CurrentInjury=null}};
        var c=s.Contracts.Last(c=>c.Active&&c.PlayerId==p.Football.PlayerId);
        c=c with{LastChangedYear=s.Year,Years=c.Years.Select(y=>y.Year==s.Year?y with{Salary=Math.Max(y.Salary,ProFinance.Minimum(s,p,y.Year,practice:false))}:y).ToList()};
        var players=s.Players.Select(x=>x.Football.PlayerId==p.Football.PlayerId?p:x).ToList();
        var excess=Math.Max(0,ProRoster.Active(s,s.UserTeam)-52);
        var cuts=players.Where(x=>x.Football.TeamName==s.UserTeam&&!x.Reserve&&!x.PracticeSquad).Take(excess).Select(x=>x.Football.PlayerId).ToHashSet();
        players=players.Select(x=>cuts.Contains(x.Football.PlayerId)?x with{Reserve=true}:x).ToList();
        s=s with{Players=players,Contracts=s.Contracts.Select(x=>x.PlayerId==p.Football.PlayerId?c:x).ToList(),Bids=[]};
        return(s,p,c);
    }
    [Theory][InlineData("RES")][InlineData("INA")][InlineData("DEV")][InlineData("")]
    public void AvailableReserveActivatesWithoutRenegotiatingAndSurvivesSerialization(string status)
    {
        var(s,p,c)=Setup();s=s with{Players=s.Players.Select(x=>x==p?x with{RosterStatus=status}:x).ToList()};
        Assert.Null(ProReserve.ActivationReason(s,s.UserTeam,p.Football.PlayerId));
        var next=ProReserve.Activate(s,s.UserTeam,p.Football.PlayerId);
        var moved=next.Players.Single(x=>x.Football.PlayerId==p.Football.PlayerId);
        Assert.False(moved.Reserve);Assert.False(moved.PracticeSquad);Assert.Equal("ACT",moved.RosterStatus);Assert.Equal(99,moved.Football.DepthChartOrder);
        Assert.Equal(JsonSerializer.Serialize(s.Contracts),JsonSerializer.Serialize(next.Contracts));
        Assert.Equal(ProRoster.Active(s,s.UserTeam)+1,ProRoster.Active(next,s.UserTeam));
        var loaded=JsonSerializer.Deserialize<ProFranchise>(JsonSerializer.Serialize(next))!;
        Assert.False(loaded.Players.Single(x=>x.Football.PlayerId==p.Football.PlayerId).Reserve);
        Assert.Equal(c.LastChangedYear,loaded.Contracts.Last(x=>x.PlayerId==p.Football.PlayerId).LastChangedYear);
        Assert.NotNull(ProReserve.ActivationReason(next,next.UserTeam,p.Football.PlayerId));
    }
    [Theory][InlineData("EXE")][InlineData("SUSP")][InlineData("IR")][InlineData("PUP")]
    public void UnknownClearanceCannotBeBypassed(string status)
    {
        var(s,p,_)=Setup();s=s with{Players=s.Players.Select(x=>x==p?x with{RosterStatus=status}:x).ToList()};
        Assert.Contains("clearance",ProReserve.ActivationReason(s,s.UserTeam,p.Football.PlayerId));
        Assert.Throws<InvalidOperationException>(()=>ProReserve.Activate(s,s.UserTeam,p.Football.PlayerId));
    }
    [Fact] public void FullRosterAndPendingCapBlockWithoutMutatingContract()
    {
        var(s,p,_)=Setup();var extra=s.Players.First(x=>x.Football.TeamName==s.UserTeam&&x.PracticeSquad&&!x.Reserve);
        s=s with{Players=s.Players.Select(x=>x==extra?x with{PracticeSquad=false}:x).ToList()};
        Assert.Contains("53-player",ProReserve.ActivationReason(s,s.UserTeam,p.Football.PlayerId));
        var original=JsonSerializer.Serialize(s);Assert.Throws<InvalidOperationException>(()=>ProReserve.Activate(s,s.UserTeam,p.Football.PlayerId));Assert.Equal(original,JsonSerializer.Serialize(s));
        (s,p,_)=Setup();s=s with{Bids=[new(p.Football.PlayerId,s.UserTeam,1,s.SalaryCap,0,0)]};
        Assert.Contains("cap space",ProReserve.ActivationReason(s,s.UserTeam,p.Football.PlayerId));
    }
    [Fact] public void MissingContractUnsignedAndOtherTeamCannotActivate()
    {
        var(s,p,_)=Setup();Assert.NotNull(ProReserve.ActivationReason(s,"Other team",p.Football.PlayerId));
        var missing=s with{Contracts=s.Contracts.Where(c=>c.PlayerId!=p.Football.PlayerId).ToList()};
        Assert.Contains("signed playing contract",ProReserve.ActivationReason(missing,s.UserTeam,p.Football.PlayerId));
        // Reserved draft rights must use the draft signing path, never a free activation.
        var unsigned=s with{UnsignedRookies=[new(p.Football.PlayerId,s.UserTeam,s.Year,1,1_000_000,0)]};
        Assert.Contains("Sign this rookie",ProReserve.ActivationReason(unsigned,s.UserTeam,p.Football.PlayerId));
    }
    [Fact] public async Task InjuryMinimumSalaryAndRealSaveLoaderRemainConsistent()
    {
        var(s,p,c)=Setup();
        var injured=p with{Football=p.Football with{CurrentInjury=new PlayerInjury{WeeksRemaining=2}}};
        var blocked=s with{Players=s.Players.Select(x=>x==p?injured:x).ToList()};
        Assert.Contains("2 weeks",ProReserve.ActivationReason(blocked,s.UserTeam,p.Football.PlayerId));
        Assert.Throws<InvalidOperationException>(()=>ProReserve.Activate(blocked,s.UserTeam,p.Football.PlayerId));
        var low=c with{Years=c.Years.Select(y=>y.Year==s.Year?y with{Salary=100_000}:y).ToList()};
        s=s with{Contracts=s.Contracts.Select(x=>x==c?low:x).ToList()};
        var next=ProReserve.Activate(s,s.UserTeam,p.Football.PlayerId);
        Assert.Equal(ProFinance.Minimum(s,p,practice:false),next.Contracts.Last(x=>x.PlayerId==p.Football.PlayerId).Years.Single(y=>y.Year==s.Year).Salary);
        Assert.Equal(c.LastChangedYear,next.Contracts.Last(x=>x.PlayerId==p.Football.PlayerId).LastChangedYear);
        var directory=Path.Combine(Path.GetTempPath(),"pro-reserve-"+Guid.NewGuid());
        try{var store=new ProSaveStore(directory);await store.Save(next);var loaded=await store.Load(next.Id);Assert.False(loaded.Players.Single(x=>x.Football.PlayerId==p.Football.PlayerId).Reserve);}
        finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
    }
    [Fact] public void CampRosterCannotExceedNinetyAndCoordinatorCannotManageOtherUnit()
    {
        var(s,p,_)=Setup();s=s with{Phase=ProPhase.Preseason};
        var count=ProRoster.CountForLimit(s,s.UserTeam);
        var extra=Enumerable.Range(0,90-count).Select(i=>p with{Reserve=false,Football=p.Football with{PlayerId=Guid.NewGuid()}});
        var full=s with{Players=s.Players.Concat(extra).ToList()};
        Assert.Contains("90-player",ProReserve.ActivationReason(full,s.UserTeam,p.Football.PlayerId));
        var role=p.Football.Position is Position.QB or Position.RB or Position.WR or Position.TE or Position.OL?StaffRole.DefensiveCoordinator:StaffRole.OffensiveCoordinator;
        s=s with{Career=s.Career! with{Role=role}};
        Assert.Throws<InvalidOperationException>(()=>ProManagement.Submit(s,"Reserve activation",[p.Football.PlayerId],0,x=>ProReserve.Activate(x,x.UserTeam,p.Football.PlayerId)));
    }
    [Fact] public async Task ExportedDeviceFixtureLoadsAndContainsAvailableAndBlockedReserve()
    {
        var(s,p,_)=Setup();p=p with{Football=p.Football with{FullName="Reserve Ready"}};
        var blocked=s.Players.First(x=>x.Football.TeamName==s.UserTeam&&x.Reserve&&x.Football.PlayerId!=p.Football.PlayerId);
        blocked=blocked with{ReserveInjury=null,RosterStatus="RES",Football=blocked.Football with{FullName="Reserve Injured",CurrentInjury=new PlayerInjury{WeeksRemaining=2}}};
        var fixture=s with{Id=Guid.NewGuid(),Name="Synthetic reserve QA",Games=[],Players=s.Players.Select(x=>x.Football.PlayerId==p.Football.PlayerId?p:x.Football.PlayerId==blocked.Football.PlayerId?blocked:x).ToList()};
        var four=fixture.Players.First(x=>x.Football.TeamName==s.UserTeam&&x.ReserveInjury is {Estimated:true}&&x.Football.PlayerId!=p.Football.PlayerId&&x.Football.PlayerId!=blocked.Football.PlayerId);
        var ending=fixture.Players.Single(x=>x.Football.FullName=="Jake Bobo");
        fixture=fixture with{Players=fixture.Players.Select(x=>x==four?x with{Football=x.Football with{FullName="Reserve Four"}}:x==ending?x with{Football=x.Football with{FullName="Reserve Season"}}:x).ToList()};
        var directory=Path.Combine(Path.GetTempPath(),"pro-reserve-fixture-"+Guid.NewGuid());
        fixture=ProPracticeRetirement.Apply(fixture);
        try{var store=new ProSaveStore(directory);await store.Save(fixture);var loaded=await store.Load(fixture.Id);Assert.Equal(JsonSerializer.Serialize(fixture.Contracts),JsonSerializer.Serialize(loaded.Contracts));Assert.Null(ProReserve.ActivationReason(loaded,loaded.UserTeam,p.Football.PlayerId));Assert.Contains("2 weeks",ProReserve.ActivationReason(loaded,loaded.UserTeam,blocked.Football.PlayerId));}
        finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        if(Environment.GetEnvironmentVariable("PRO_COORDINATOR_UI_FIXTURES") is string output)
        {
            using var file=File.Create(Path.Combine(output,"pro-staff-reserve-ui-fixture.json.gz"));
            using var gzip=new System.IO.Compression.GZipStream(file,System.IO.Compression.CompressionLevel.SmallestSize);
            JsonSerializer.Serialize(gzip,new{franchise=fixture,readyPlayer=p.Football.PlayerId,blockedPlayer=blocked.Football.PlayerId},new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase});
        }
    }

}
