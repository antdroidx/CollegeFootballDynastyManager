using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public class ReservePolicyTests
{
    static readonly ProFranchise Template=ProLeagueService.Create("Seattle Seahawks");
    static ProFranchise Games(ProFranchise s,int count,bool preseason=false)=>s with{Games=Enumerable.Range(1,count).Select(w=>new ProGame(new(){GameId="ir-"+w,SeasonYear=s.Year,Week=w,HomeTeamName=s.UserTeam,AwayTeamName="Arizona Cardinals",HasPlayed=true,HomeScore=7,AwayScore=0},preseason?ProPhase.Preseason:ProPhase.RegularSeason)).ToList()};
    [Fact]public void OpeningReservesHaveFourGameDefaultWithVerifiedSeasonEndingOverrides()
    {
        var s=Template;var medical=s.Players.Where(p=>p.Reserve&&p.ReserveInjury is not null).ToArray();Assert.NotEmpty(medical);Assert.All(medical,p=>Assert.Equal(4,p.ReserveInjury!.MinimumGames));
        Assert.All(new[]{"Jake Bobo","Jayden Reed","Jaxson Dart","Matt Hennessy","De'Von Achane","Brian Burns","Javon Hargrave","Tom Kennedy"},name=>{
            var p=medical.Single(p=>p.Football.FullName==name);Assert.True(p.ReserveInjury!.SeasonEnding);Assert.False(p.ReserveInjury.Estimated);Assert.NotNull(p.ReserveInjury.Source);Assert.Contains("Season-ending",ProReserve.ActivationReason(s,p.Football.TeamName,p.Football.PlayerId));});
        var unknown=medical.First(p=>p.ReserveInjury!.Estimated);Assert.False(unknown.ReserveInjury!.SeasonEnding);Assert.Contains("4 team games",ProReserve.ActivationReason(s,unknown.Football.TeamName,unknown.Football.PlayerId));
    }
    [Fact]public void PreseasonAndByesDoNotConsumeMinimumAndFourCompletedGamesPermitReturn()
    {
        var s=Template with{Phase=ProPhase.RegularSeason,Week=6,SalaryCap=900_000_000,TeamCapAllocations=[]};
        var p=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&p.ReserveInjury is {Estimated:true});
        s=s with{Players=s.Players.Where(x=>x.Football.TeamName!=s.UserTeam||x.Reserve||x.PracticeSquad||x.Football.PlayerId==s.Players.First(a=>a.Football.TeamName==s.UserTeam&&!a.Reserve&&!a.PracticeSquad).Football.PlayerId).ToList()};
        Assert.Contains("4 team games",ProReserve.ActivationReason(Games(s,3,true),s.UserTeam,p.Football.PlayerId));
        Assert.Contains("1 team games",ProReserve.ActivationReason(Games(s,3),s.UserTeam,p.Football.PlayerId));
        var ready=Games(s,4);Assert.Null(ProReserve.ActivationReason(ready,s.UserTeam,p.Football.PlayerId));
        var moved=ProReserve.Activate(ready,s.UserTeam,p.Football.PlayerId);Assert.False(moved.Players.Single(x=>x.Football.PlayerId==p.Football.PlayerId).Reserve);Assert.Equal(System.Text.Json.JsonSerializer.Serialize(ready.Contracts),System.Text.Json.JsonSerializer.Serialize(moved.Contracts));
        var ending=s.Players.Single(p=>p.Football.FullName=="Jake Bobo");Assert.Contains("Season-ending",ProReserve.ActivationReason(Games(s,17),s.UserTeam,ending.Football.PlayerId));
    }
    [Fact]public async Task MigrationIsIdempotentAndDoesNotRelockCompletedGamesOrDraftRights()
    {
        var s=Games(Template,6) with{Phase=ProPhase.RegularSeason,Week=8,ReservePolicyVersion=0,Players=Template.Players.Select(p=>p with{ReserveInjury=null,RosterStatus=p.Reserve&&p.RosterStatus=="IR"?"RES":p.RosterStatus}).ToList()};
        var p=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&p.Reserve&&p.RosterStatus=="RES");
        s=s with{UnsignedRookies=[new(p.Football.PlayerId,s.UserTeam,s.Year,1,1_000_000,0)]};var next=ProReserve.InitializeOpening(s);
        Assert.Null(next.Players.Single(x=>x.Football.PlayerId==p.Football.PlayerId).ReserveInjury);Assert.Equal(next,ProReserve.InitializeOpening(next));Assert.Equal(s.Contracts,next.Contracts);
        var dir=Path.Combine(Path.GetTempPath(),"ir-policy-"+Guid.NewGuid());
        try{var store=new ProSaveStore(dir);await store.Save(next);var loaded=await store.Load(next.Id);Assert.Equal(next.Players,loaded.Players);Assert.Equal(1,loaded.ReservePolicyVersion);}
        finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
    }
    [Fact]public void OversizedPracticeSquadsReleaseLowestRatingsAndRetainCapObligations()
    {
        var s=Template;var ps=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&p.PracticeSquad);
        var players=Enumerable.Range(0,19).Select(i=>ps with{Reserve=false,Football=ps.Football with{PlayerId=Guid.NewGuid(),OverallRating=40+i}}).ToArray();
        s=s with{Players=s.Players.Where(p=>p.Football.TeamName!=s.UserTeam||!p.PracticeSquad).Concat(players).ToList(),Contracts=s.Contracts.Concat(players.Select(p=>new ProContract(p.Football.PlayerId,s.UserTeam,s.Year,1,100_000,[new(s.Year,500_000,250_000)]))).ToList(),InternationalPracticeDesignations=[]};
        var next=ProRoster.TrimOversizedPractice(s);Assert.Equal(16,ProInternationalPractice.OrdinaryPlayers(next,s.UserTeam));
        Assert.All(players.Take(3),p=>Assert.Equal("Free agent",next.Players.Single(x=>x.Football.PlayerId==p.Football.PlayerId).Football.TeamName));
        Assert.All(players.Skip(3),p=>Assert.Equal(s.UserTeam,next.Players.Single(x=>x.Football.PlayerId==p.Football.PlayerId).Football.TeamName));
        Assert.All(players.Take(3),p=>Assert.Contains(next.DeadCap,d=>d.PlayerId==p.Football.PlayerId&&d.Amount>0));Assert.Equal(next,ProRoster.TrimOversizedPractice(next));
    }
    [Fact]public void CpuWaitsForFourGamesThenReturnsClearedInternalDepth()
    {
        var s=Template with{Phase=ProPhase.RegularSeason,Week=5,SalaryCap=900_000_000,TeamCapAllocations=[]};var p=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&p.ReserveInjury is {Estimated:true});
        s=s with{Players=s.Players.Where(x=>x.Football.TeamName!=s.UserTeam||x.Reserve||x.PracticeSquad).Select(x=>x.Football.PlayerId==p.Football.PlayerId?x with{Football=x.Football with{OverallRating=99}}:x).ToList()};
        var before=ProCpuRoster.Manage(Games(s,3),s.UserTeam);Assert.True(before.Players.Single(x=>x.Football.PlayerId==p.Football.PlayerId).Reserve);
        var after=ProCpuRoster.Manage(Games(s,4),s.UserTeam);Assert.False(after.Players.Single(x=>x.Football.PlayerId==p.Football.PlayerId).Reserve);
        Assert.True(after.Players.Single(x=>x.Football.FullName=="Jake Bobo").Reserve);
    }

    [Fact]public void CompletedGameRecoveryIsAppliedOnceAndDoesNotCountPreseason()
    {
        var s=Template with{Phase=ProPhase.RegularSeason,Week=4,SalaryCap=900_000_000,TeamCapAllocations=[]};var p=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&p.ReserveInjury is {Estimated:true});
        s=Games(s,3);s=s with{Players=s.Players.Select(x=>x==p?x with{Football=x.Football with{CurrentInjury=new(){WeeksRemaining=2}}}:x).ToList()};
        var game=new ProGame(new(){GameId="ir-final",SeasonYear=s.Year,Week=4,HomeTeamName=s.UserTeam,AwayTeamName="Arizona Cardinals"},ProPhase.RegularSeason);s=s with{Games=s.Games.Append(game).ToList()};
        var session=ProLeagueService.Start(s,game,GameDayMode.Simulate);Assert.DoesNotContain(session.Roster,x=>x.PlayerId==p.Football.PlayerId);GameDayEngine.SimulateToEnd(session);
        var next=ProLeagueService.Finish(s,session);Assert.Equal(1,next.Players.Single(x=>x.Football.PlayerId==p.Football.PlayerId).Football.CurrentInjury!.WeeksRemaining);Assert.Same(next,ProLeagueService.Finish(next,session));
        Assert.Equal(next.Players,ProReserve.AfterGame(next,game with{Phase=ProPhase.Preseason}).Players);
        Assert.Contains(ProInbox.Build(next),i=>i.PlayerId==p.Football.PlayerId&&i.Title.Contains("injured reserve"));
    }

}
