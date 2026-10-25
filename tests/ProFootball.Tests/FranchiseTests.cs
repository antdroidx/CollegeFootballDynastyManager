using Xunit;
using ProFootball.Core;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
namespace ProFootball.Tests;
public class FranchiseTests
{
    static ProFranchise New()=>ProLeagueService.Create("Seattle Seahawks");
    [Fact] public async Task LegacyFictionalSaveMigratesWithoutReplacingItsLeagueOrRoster()
    {
        using var stream=typeof(ProLeagueService).Assembly.GetManifestResourceStream("ProFootball.Core.fictional-league.json")!;
        var league=System.Text.Json.JsonSerializer.Deserialize<ProLeague>(stream)!;
        var legacy=new ProFranchise{SchemaVersion=1,League=league,UserTeam=league.Teams[0].Name,Players=ProLeagueService.GeneratePlayers(league),Games=ProSchedule.Build(league,2026)};
        var dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString());
        try{var store=new ProSaveStore(dir);await store.Save(legacy);var restored=await store.Load(legacy.Id);Assert.Equal(2,restored.SchemaVersion);Assert.Equal(legacy.UserTeam,restored.UserTeam);Assert.Equal(legacy.Players[0].Football.PlayerId,restored.Players[0].Football.PlayerId);Assert.Equal(legacy.Games.Count,restored.Games.Count);Assert.NotEmpty(restored.Contracts);}
        finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
    }
    [Fact] public void StaffMovesAreLimitedToOffseasonAndPersistVacancies()
    {
        var s=New();Assert.Throws<InvalidOperationException>(()=>ProStaff.Fire(s,s.UserTeam,StaffRole.OffensiveCoordinator));
        s=ProStaff.Fire(s with{Phase=ProPhase.Offseason},s.UserTeam,StaffRole.OffensiveCoordinator);Assert.Contains(s.Coaches,c=>c.TeamId=="SEA"&&c.Role==StaffRole.OffensiveCoordinator&&c.Vacant);
        var candidate=s.CoachCandidates.First();s=ProStaff.Hire(s,s.UserTeam,StaffRole.OffensiveCoordinator,candidate.Name);Assert.Contains(s.Coaches,c=>c.TeamId=="SEA"&&c.Name==candidate.Name&&!c.Vacant);Assert.DoesNotContain(s.CoachCandidates,c=>c.Name==candidate.Name);
    }
    [Fact] public void SnapshotHasRealIdentitiesStaffAndHonestRatingSources()
    {
        var s=New();Assert.Contains(s.Players,p=>p.Football.FullName=="Patrick Mahomes"&&p.RatingSource.StartsWith("Madden"));
        Assert.All(s.League.Teams,t=>{Assert.NotNull(t.LogoFile);Assert.Equal(4,s.Coaches.Count(c=>c.TeamId==t.Id));Assert.True(ProEconomy.Room(s,t.Name)>0);});
        Assert.Contains(s.Coaches,c=>c.TeamId=="SEA"&&c.Name=="Mike Macdonald");Assert.NotEmpty(s.CoachCandidates);
        Assert.All(s.Players,p=>Assert.InRange(p.Football.OverallRating,1,99));
    }
    [Fact] public void BonusProrationAndReleaseConserveObligations()
    {
        var s=New();var p=s.Players.First(p=>p.Football.TeamName==s.UserTeam);var c=new ProContract(p.Football.PlayerId,s.UserTeam,s.Year,3,10,[new(s.Year,12,4),new(s.Year+1,12,3),new(s.Year+2,12,0)]);
        Assert.Equal(10,Enumerable.Range(s.Year,3).Sum(y=>ProEconomy.BonusCharge(c,y)));Assert.Equal(16,ProEconomy.CapHit(c,s.Year));
        s=s with{Contracts=[c]};var released=ProEconomy.Release(s,s.UserTeam,p.Football.PlayerId,false);Assert.Equal(17,ProEconomy.Used(released,s.UserTeam));Assert.False(released.Contracts[0].Active);
    }
    [Fact] public void OffersCompeteAndCannotExceedReservedCap()
    {
        var s=New() with{Phase=ProPhase.Offseason};var p=s.Players.First(p=>p.Football.TeamName=="Free agent"&&!p.Retired);var other=s.League.Teams.First(t=>t.Name!=s.UserTeam).Name;
        var salary=Math.Max(1_000_000,ProEconomy.MarketSalary(p));s=ProEconomy.Offer(s,s.UserTeam,p.Football.PlayerId,2,salary,0);s=ProEconomy.Offer(s,other,p.Football.PlayerId,2,salary*2,0);s=ProEconomy.ResolveBids(s);
        Assert.Equal(other,s.Players.Single(x=>x.Football.PlayerId==p.Football.PlayerId).Football.TeamName);Assert.Empty(s.Bids);
        var poor=s with{SalaryCap=0};var free=poor.Players.First(p=>p.Football.TeamName=="Free agent"&&!p.Retired);Assert.Throws<InvalidOperationException>(()=>ProEconomy.Offer(poor,poor.UserTeam,free.Football.PlayerId,1,10_000_000,0));
    }
    [Fact] public void PickTradesTransferOwnershipAndRejectDuplicateAssets()
    {
        var s=New();var mine=s.Picks.First(p=>p.Owner==s.UserTeam&&p.Round==1);var theirs=s.Picks.First(p=>p.Owner!=s.UserTeam&&p.Round==7);
        var next=ProTrade.Propose(s,s.UserTeam,theirs.Owner,[],[],[mine.Id],[theirs.Id]);Assert.Equal(theirs.Owner,next.Picks.Single(p=>p.Id==mine.Id).Owner);Assert.Equal(s.UserTeam,next.Picks.Single(p=>p.Id==theirs.Id).Owner);
        Assert.Throws<InvalidOperationException>(()=>ProTrade.Propose(s,s.UserTeam,theirs.Owner,[],[],[mine.Id,mine.Id],[]));
    }
    [Fact] public void OnlyPrimaryAndSelectedAssistsPursueContact()
    {
        var primary=Guid.NewGuid();var a=Guid.NewGuid();var b=Guid.NewGuid();var c=Guid.NewGuid();var snap=new GameSnapEvent{OffenseTeamName="A",DefenseTeamName="B",OffensivePlayId="run",DefensivePlayId="base",Outcome=SnapOutcome.Run,Yards=10,PrimaryDefenderId=primary,TackleAttemptIds=[primary,a,b,c]};
        var assists=LiveFootballChoreography.AssistIds(snap);Assert.Equal(2,assists.Count);Assert.DoesNotContain(c,assists);
        var contact=new GameFieldPoint(.8,.5);var start=new GameFieldPoint(.4,.15);var hold=LiveFootballChoreography.DefensiveDestination(start,contact,.3,.8,0,false,false);Assert.NotEqual(contact,hold);Assert.Equal(.125,hold.Y,5);
        Assert.Equal(GameDayAnimationPlanner.PursuitTarget(contact,0,true),LiveFootballChoreography.DefensiveDestination(start,contact,.3,.8,0,true,false));
        Assert.Empty(LiveFootballChoreography.AssistIds(snap with{Outcome=SnapOutcome.Incompletion}));
    }
    [Fact] public void ThreeOffseasonsPreserveCapRosterDraftAndAging()
    {
        var s=New();var first=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&p.Age<28);var age=first.Age;
        for(var season=0;season<3;season++)
        {
            for(var week=0;week<25&&s.Phase!=ProPhase.Offseason;week++)s=ProLeagueService.Advance(s);
            Assert.Equal(ProPhase.Offseason,s.Phase);Assert.Equal(season+1,s.History.Count);s=ProOffseason.Advance(s);Assert.Equal(320,s.Prospects.Count);
            s=ProOffseason.Advance(s);s=ProDraft.AutoUntilUser(s,true);Assert.Null(ProDraft.Current(s));Assert.Equal(224,s.DraftHistory.Count(p=>p.Year==s.Year));
            s=ProOffseason.Advance(s);s=ProCpu.Trim(s,s.UserTeam);s=ProOffseason.Advance(s);s=ProCpu.Manage(s,true);
            Assert.Equal(ProPhase.Preseason,s.Phase);Assert.All(s.League.Teams,t=>{Assert.InRange(ProRoster.Active(s,t.Name),20,53);Assert.True(ProEconomy.Room(s,t.Name)>=0,t.Name);});
            Assert.Equal(s.Players.Count,s.Players.Select(p=>p.Football.PlayerId).Distinct().Count());
        }
        Assert.Equal(age+3,s.Players.Single(p=>p.Football.PlayerId==first.Football.PlayerId).Age);
    }
}
