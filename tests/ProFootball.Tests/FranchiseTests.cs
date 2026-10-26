using Xunit;
using ProFootball.Core;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
namespace ProFootball.Tests;
public class FranchiseTests
{
    static ProFranchise New()=>ProLeagueService.Create("Seattle Seahawks");
    [Fact] public void DraftOrderUsesCompletedSeasonAfterCalendarAdvances()
    {
        var s=New();var completed=s.Games.Select(g=>
        {
            if(g.Phase!=ProPhase.RegularSeason)return g;
            var f=g.Football;var winner=f.InvolvesTeam("Buffalo Bills")?"Buffalo Bills":f.InvolvesTeam(s.UserTeam)?f.HomeTeamName==s.UserTeam?f.AwayTeamName:f.HomeTeamName:f.HomeTeamName;
            return g with{Football=f with{HasPlayed=true,HomeScore=winner==f.HomeTeamName?21:7,AwayScore=winner==f.AwayTeamName?21:7}};
        }).ToList();
        s=ProDraft.Generate(s with{Year=s.Year+1,Games=[],History=[new(s.Year,"Buffalo Bills",completed)]});
        var round=s.Picks.Where(p=>p.Year==s.Year&&p.Round==1).OrderBy(p=>p.Slot).ToArray();
        Assert.Equal("Seattle Seahawks",round[0].OriginalTeam);Assert.Equal("Buffalo Bills",round[^1].OriginalTeam);
    }
    [Fact] public void WaiverClockContinuesAcrossPlayoffWeekReset()
    {
        var s=New() with{Phase=ProPhase.RegularSeason,Week=18,TransactionTick=20};var p=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&!p.Reserve&&!p.PracticeSquad&&p.Experience<4);
        s=ProEconomy.Release(s,s.UserTeam,p.Football.PlayerId);Assert.Contains(s.Waivers,w=>w.PlayerId==p.Football.PlayerId);
        var other=s.League.Teams.First(t=>t.Name!=s.UserTeam).Name;var cut=s.Players.First(x=>x.Football.TeamName==other&&!x.PracticeSquad&&!x.Reserve&&!x.Retired);s=ProEconomy.Release(s,other,cut.Football.PlayerId,false);s=ProRoster.Claim(s,other,p.Football.PlayerId);
        s=ProRoster.ResolveWaivers(s with{Phase=ProPhase.Playoffs,Week=1,TransactionTick=21});Assert.DoesNotContain(s.Waivers,w=>w.PlayerId==p.Football.PlayerId);
        Assert.Equal(other,s.Players.Single(x=>x.Football.PlayerId==p.Football.PlayerId).Football.TeamName);
    }
    [Fact] public async Task LegacyFictionalSaveMigratesWithoutReplacingItsLeagueOrRoster()
    {
        using var stream=typeof(ProLeagueService).Assembly.GetManifestResourceStream("ProFootball.Core.fictional-league.json")!;
        var league=System.Text.Json.JsonSerializer.Deserialize<ProLeague>(stream)!;
        var legacy=new ProFranchise{SchemaVersion=1,League=league,UserTeam=league.Teams[0].Name,Players=ProLeagueService.GeneratePlayers(league),Games=ProSchedule.Build(league,2026)};
        var dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString());
        try{var store=new ProSaveStore(dir);await store.Save(legacy);var restored=await store.Load(legacy.Id);Assert.Equal(2,restored.SchemaVersion);Assert.Equal(legacy.UserTeam,restored.UserTeam);Assert.Equal(legacy.Players[0].Football.PlayerId,restored.Players[0].Football.PlayerId);Assert.Equal(legacy.Games.Count,restored.Games.Count);Assert.NotEmpty(restored.Contracts);}
        finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
    }
    [Fact] public void StaffCanChangeDuringSeasonAndPersistVacancies()
    {
        var s=New();
        s=ProStaff.Fire(s,s.UserTeam,StaffRole.OffensiveCoordinator);Assert.Contains(s.Coaches,c=>c.TeamId=="SEA"&&c.Role==StaffRole.OffensiveCoordinator&&c.Vacant);
        var candidate=s.CoachCandidates.First();s=ProStaff.Hire(s,s.UserTeam,StaffRole.OffensiveCoordinator,candidate.Name);Assert.Contains(s.Coaches,c=>c.TeamId=="SEA"&&c.Name==candidate.Name&&!c.Vacant);Assert.DoesNotContain(s.CoachCandidates,c=>c.Name==candidate.Name);
    }
    [Fact] public void SnapshotHasRealIdentitiesStaffAndHonestRatingSources()
    {
        var s=New();Assert.Contains(s.Players,p=>p.Football.FullName=="Patrick Mahomes"&&p.RatingSource.StartsWith("Madden"));
        Assert.All(s.League.Teams,t=>{Assert.InRange(t.Abbreviation.Length,2,4);Assert.Equal(4,s.Coaches.Count(c=>c.TeamId==t.Id));Assert.True(ProEconomy.Room(s,t.Name)>0);});
        Assert.Contains(s.Coaches,c=>c.TeamId=="SEA"&&c.Name=="Mike Macdonald");Assert.NotEmpty(s.CoachCandidates);
        Assert.All(s.Players,p=>Assert.InRange(p.Football.OverallRating,1,99));
        Assert.All(Enumerable.Range(5,5),code=>Assert.True(s.Players.Count(p=>p.Football.Position==Position.OL&&p.Football.NcaaPositionCode==code)>=40,"Missing specific offensive line roles in the source pack"));
    }
    [Fact] public void BonusProrationAndReleaseConserveObligations()
    {
        var s=New();var p=s.Players.First(p=>p.Football.TeamName==s.UserTeam);var c=new ProContract(p.Football.PlayerId,s.UserTeam,s.Year,3,10,[new(s.Year,12,4),new(s.Year+1,12,3),new(s.Year+2,12,0)]);
        Assert.Equal(10,Enumerable.Range(s.Year,3).Sum(y=>ProEconomy.BonusCharge(c,y)));Assert.Equal(16,ProEconomy.CapHit(c,s.Year));
        s=s with{Contracts=[c]};var released=ProEconomy.Release(s,s.UserTeam,p.Football.PlayerId,false);Assert.Equal(14,ProEconomy.Used(released,s.UserTeam));Assert.Equal(3,released.DeadCap.Where(d=>d.Year==s.Year+1).Sum(d=>d.Amount));Assert.Equal(17,released.DeadCap.Sum(d=>d.Amount));Assert.False(released.Contracts[0].Active);
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
    [Fact] public void TwentySeasonsPreserveCapRosterDraftAndAging()
    {
        var s=New();var first=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&p.Age<28);var age=first.Age;
        for(var season=0;season<20;season++)
        {
            for(var week=0;week<26&&s.Phase!=ProPhase.Offseason;week++)
            {
                if(s.Phase==ProPhase.Preseason&&s.Week==3)s=ProCpu.Trim(s,s.UserTeam);
                s=ProLeagueService.Advance(s);
                if(s.Phase==ProPhase.RegularSeason)Assert.All(s.League.Teams,t=>Assert.True(ProRoster.Active(s,t.Name)<=53,t.Name));
            }
            Assert.Equal(ProPhase.Offseason,s.Phase);Assert.Equal(season+1,s.History.Count);Assert.Equal(season+1,s.Honors.Count);Assert.All(s.Honors,h=>Assert.Equal(2,h.Selections.Select(p=>p.Conference).Distinct().Count()));s=ProOffseason.Advance(s);Assert.Equal(320,s.Prospects.Count);
            s=ProOffseason.Advance(s);s=ProDraft.AutoUntilUser(s,true);Assert.Null(ProDraft.Current(s));Assert.Equal(224,s.DraftHistory.Count(p=>p.Year==s.Year));
            s=ProOffseason.Advance(s);s=ProCpu.Trim(s,s.UserTeam);s=ProOffseason.Advance(s);s=ProCpu.Manage(s,true);
            Assert.Equal(ProPhase.Preseason,s.Phase);Assert.All(s.League.Teams,t=>{Assert.True(ProRoster.Active(s,t.Name)>=45&&ProRoster.CountForLimit(s,t.Name)<=90,$"{s.Year} {t.Name}: {ProRoster.Active(s,t.Name)} active, room {ProEconomy.Room(s,t.Name):N0}, dead {s.DeadCap.Where(d=>d.Team==t.Name&&d.Year==s.Year).Sum(d=>d.Amount):N0}");Assert.True(ProEconomy.Room(s,t.Name)>=0,t.Name);});
            Assert.Equal(s.Players.Count,s.Players.Select(p=>p.Football.PlayerId).Distinct().Count());
        }
        Assert.Equal(age+20,s.Players.Single(p=>p.Football.PlayerId==first.Football.PlayerId).Age);
    }
}
