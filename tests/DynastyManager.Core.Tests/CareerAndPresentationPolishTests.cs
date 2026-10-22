using System.Text.Json;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Import;
using Xunit;
using Xunit.Abstractions;

namespace DynastyManager.Core.Tests;

public class CareerAndPresentationPolishTests(ITestOutputHelper output)
{
    [Fact]
    public void EveryCatalogPlayHasSevenOnLineAndUncoveredEligibleEnds()
    {
        foreach (var play in PlaybookCatalog.OffensivePlays)
        {
            var roles = new Dictionary<Position,int>();
            var points = new[] { Position.QB, Position.OL, Position.OL, Position.OL, Position.OL, Position.OL }
                .Concat(GameDayAnimationPlanner.EligiblePositions(play.Personnel)).Select(position =>
                { var role = roles.GetValueOrDefault(position); roles[position] = role + 1;
                    return (Position:position, Point:GameDayAnimationPlanner.OffensiveAlignment(play,position,role)); }).ToArray();
            var line = points.Where(p => p.Point.X >= -.5).OrderBy(p => p.Point.Y).ToArray();
            Assert.Equal(11, points.Length); Assert.Equal(7,line.Length);
            Assert.NotEqual(Position.OL,line[0].Position); Assert.NotEqual(Position.OL,line[^1].Position);
            Assert.Equal(7,line.Select(p => p.Point.Y).Distinct().Count());
            Assert.All(points.Where(p => p.Position is Position.RB or Position.QB),p=>Assert.True(p.Point.X < -.5));
        }
    }
    [Fact]
    public void CarrierCutsAreSmoothAndKeepTheRecordedEndpoints()
    {
        var from = new GameFieldPoint(.3,.5); var to = new GameFieldPoint(.7,.5);
        Assert.Equal(from,GameDayAnimationPlanner.CarrierPoint(from,to,0,90,true));
        var end = GameDayAnimationPlanner.CarrierPoint(from,to,1,90,true);
        Assert.Equal(to.X,end.X,8);Assert.Equal(to.Y,end.Y,8);
        var prior = from; var deviation = 0d;
        for(var i=1;i<=100;i++)
        {
            var next=GameDayAnimationPlanner.CarrierPoint(from,to,i/100d,90,true);
            Assert.True(GameDayAnimationPlanner.DistanceYards(prior,next)<1);
            deviation=Math.Max(deviation,Math.Abs(next.Y-.5));prior=next;
        }
        Assert.True(deviation>.01);
    }
    [Theory]
    [InlineData(19,false)] [InlineData(20,true)] [InlineData(24,true)] [InlineData(25,true)]
    public void ExplosiveRunsReplayAtTwentyYards(int yards,bool replay)
    {
        var snap=new GameSnapEvent{OffenseTeamName="H",DefenseTeamName="A",OffensivePlayId="run",DefensivePlayId="def",Outcome=SnapOutcome.Run,Yards=yards};
        Assert.Equal(replay,GameDayLivePresentation.InstantReplay(snap));
    }
    [Fact]
    public void BrokenTacklesAreRecordedByTheEngineWithoutReplayRerolls()
    {
        var source=GameDayEngineTests.Session(GameDayMode.Play);GameSnapEvent? broken=null;
        for(var seed=0;seed<300 && broken is null;seed++)
        {
            var s=GameDayEngine.Start(source.Game with {SimulationSeed=seed},source.HomeProfile,source.AwayProfile,source.Roster,GameDayMode.Play);
            s.KickoffPending=false;var snap=GameDayEngine.Step(s,"off-spread-zone");
            if(snap.BrokeTackle)broken=snap;
        }
        Assert.NotNull(broken);Assert.True(broken.Yards>=12);
        var loaded=JsonSerializer.Deserialize<GameSnapEvent>(JsonSerializer.Serialize(broken))!;
        Assert.True(loaded.BrokeTackle);Assert.Equal(broken.Yards,loaded.Yards);Assert.Equal(broken.BallCarrierId,loaded.BallCarrierId);Assert.Equal(broken.TackleAttemptIds,loaded.TackleAttemptIds);
    }
    [Fact]
    public void SeasonBaselineSurvivesTrainingAndSaveButResetsNextYear()
    {
        var source=GameDayEngineTests.Session(GameDayMode.Play);
        var state=PlayerRatingService.EnsureProfiles(new DynastyState{DynastyName="Baseline",UserTeamName="Home",SeasonYear=2026,Phase=SeasonPhase.RegularSeason,ActiveRoster=source.HomeRoster});
        var baseline=state.ActiveRoster.ToDictionary(p=>p.PlayerId,p=>p.OverallRating);
        foreach(var week in new[]{3,6,9,12})
        {
            state=PlayerDevelopmentService.ApplyMidseasonDevelopment(state with{Week=week});
            Assert.Equal(state,PlayerDevelopmentService.ApplyMidseasonDevelopment(state));
            Assert.All(state.ActiveRoster,p=>{Assert.Equal(baseline[p.PlayerId],p.SeasonStartOverall);Assert.InRange(GameMoraleService.SeasonDelta(state,p),-2,2);Assert.Equal(p.OverallRating,PlayerRatingService.CalculateOverall(p));});
        }
        var restored=JsonSerializer.Deserialize<DynastyState>(JsonSerializer.Serialize(state))!;
        Assert.Equal(state.ActiveRoster,restored.ActiveRoster);
        var next=PlayerRatingService.EnsureProfiles(restored with{SeasonYear=2027});
        Assert.All(next.ActiveRoster,p=>{Assert.Equal(p.OverallRating,p.SeasonStartOverall);Assert.Equal(2027,p.RatingBaselineSeason);});
    }
    [Fact]
    public void InjuryAndIndividualPerformanceAffectMoraleWithoutChangingRatings()
    {
        var source=GameDayEngineTests.Session(GameDayMode.Play);var p=source.HomeRoster.First();
        var state=new DynastyState{DynastyName="Morale",UserTeamName="Home",SeasonYear=2026,ActiveRoster=source.HomeRoster};
        var morale=GameMoraleService.PlayerMorale(state,p);
        var injured=p with {CurrentInjury=new PlayerInjury{WeeksRemaining=3,Severity=InjurySeverity.Moderate}};
        Assert.True(GameMoraleService.PlayerMorale(state,injured)<morale);
        Assert.Equal(p.OverallRating,state.ActiveRoster.First().OverallRating);
    }
    [Fact]
    public void HallOfFameRequiresAnExceptionalCompletedCareerAndIsIdempotent()
    {
        var id=Guid.NewGuid();var departure=new PlayerDepartureRecord{PlayerId=id,FullName="Legend",TeamName="Home",Position=Position.QB,SeasonYear=2029,OverallRating=95};
        var state=new DynastyState{DynastyName="Hall",UserTeamName="Home",SeasonYear=2029,PlayerSeasonStats=Enumerable.Range(2026,4).Select(y=>new PlayerSeasonStatLine{SeasonYear=y,PlayerId=id,PlayerName="Legend",TeamName="Home",Position=Position.QB,Games=12,PassYards=4000,PassingTouchdowns=40,InterceptionsThrown=5}).ToArray()};
        var inducted=HallOfFameService.Induct(state,[departure]);Assert.Single(inducted.HallOfFame);
        Assert.Equal(inducted.HallOfFame,HallOfFameService.Induct(inducted,[departure]).HallOfFame);
        Assert.Empty(HallOfFameService.Induct(state,[departure with{Reason=PlayerDepartureReason.TransferPortal}]).HallOfFame);
        Assert.Empty(HallOfFameService.Induct(state with{PlayerSeasonStats=[]},[departure]).HallOfFame);
        Assert.Equal(inducted.HallOfFame,JsonSerializer.Deserialize<DynastyState>(JsonSerializer.Serialize(inducted))!.HallOfFame);
    }
    [Fact]
    public void GeneratedFreshmenAndFourYearDevelopmentKeepABroadRatingPopulation()
    {
        var dynastyId=Guid.Parse("b1000000-2222-3333-4444-555555555555");
        var pool=HighSchoolRecruitingPoolFactory.Create(dynastyId,2026,30,[]);
        var state=new DynastyState{DynastyId=dynastyId,DynastyName="Population",UserTeamName="Home",SeasonYear=2026,Phase=SeasonPhase.RegularSeason,
            ActiveRoster=pool.Take(750).Select((r,index)=>new DynastyPlayer{PlayerId=r.RecruitId,FullName=r.FullName,TeamName=$"Program {index/85}",Position=r.Position,ClassYear=1,OverallRating=r.TrueOverallRating,PotentialRating=r.PotentialRating,OriginalRecruitingStars=r.StarRating}).ToArray()};
        state=RosterManagementService.NormalizeAllDepthCharts(PlayerRatingService.EnsureProfiles(state));var first=state.ActiveRoster.Average(p=>p.OverallRating);
        var initial=state.ActiveRoster.ToDictionary(p=>p.PlayerId,p=>p.OverallRating);
        for(var year=2026;year<=2029;year++)
        {
            state=PlayerRatingService.EnsureProfiles(state with{SeasonYear=year,Phase=SeasonPhase.RegularSeason});
            foreach(var week in new[]{3,6,9,12})state=PlayerDevelopmentService.ApplyMidseasonDevelopment(state with{Week=week});
            if(year<2029) state=PlayerDevelopmentService.ApplyOffseasonDevelopment(state with{Phase=SeasonPhase.Offseason});
            output.WriteLine($"year={year} mean={state.ActiveRoster.Average(p=>p.OverallRating):F1} 90+={state.ActiveRoster.Count(p=>p.OverallRating>=90)/(double)state.ActiveRoster.Count:P1}");
            state=state with{ActiveRoster=state.ActiveRoster.Select(p=>p with{ClassYear=p.ClassYear+1}).ToArray()};
        }
        var mean=state.ActiveRoster.Average(p=>p.OverallRating);Assert.InRange(mean,first,first+18);
        var improved=state.ActiveRoster.Count(p=>p.OverallRating>initial[p.PlayerId]);
        output.WriteLine($"Initial mean={first:F1}, improved={improved}/{state.ActiveRoster.Count}");
        Assert.True(improved>state.ActiveRoster.Count*.60);
        Assert.True(state.ActiveRoster.Count(p=>p.OverallRating>=90)<state.ActiveRoster.Count*.20);
        Assert.True(state.ActiveRoster.Count(p=>p.OverallRating<75)>state.ActiveRoster.Count*.10);
        Assert.True(state.ActiveRoster.Max(p=>p.OverallRating)-state.ActiveRoster.Min(p=>p.OverallRating)>=25);
    }
    [Fact]
    public void GeneratedFillerReflectsPrestigeAndAgeWithoutBecomingAnEliteRoster()
    {
        var teams=new[]{new Team{Name="Strong",Abbreviation="S",ConferenceName="Test",Prestige=90},new Team{Name="Weak",Abbreviation="W",ConferenceName="Test",Prestige=35}};
        var roster=LegacyDynastyRosterFactory.Create([],teams,Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),2026);
        var state=PlayerRatingService.EnsureProfiles(new DynastyState{DynastyName="Filler",UserTeamName="Strong",SeasonYear=2026,ActiveRoster=roster});
        Assert.Equal(170,state.ActiveRoster.Count);
        Assert.True(state.ActiveRoster.Where(p=>p.TeamName=="Strong").Average(p=>p.OverallRating)>state.ActiveRoster.Where(p=>p.TeamName=="Weak").Average(p=>p.OverallRating));
        Assert.All(state.ActiveRoster,p=>{Assert.InRange(p.ClassYear,1,4);Assert.Equal(p.OverallRating,PlayerRatingService.CalculateOverall(p));});
        foreach(var team in teams)
        {
            var players=state.ActiveRoster.Where(p=>p.TeamName==team.Name).ToArray();
            Assert.True(players.Where(p=>p.ClassYear==4).Average(p=>p.OverallRating)>players.Where(p=>p.ClassYear==1).Average(p=>p.OverallRating));
        }
        Assert.True(state.ActiveRoster.Count(p=>p.OverallRating>=90)<state.ActiveRoster.Count*.35);
    }
    [Fact]
    public void EarlyProAndGraduationDeparturesAreRetainedWithoutDuplicates()
    {
        var roster=Enumerable.Range(1,80).Select(i=>new DynastyPlayer{PlayerId=new Guid(i,0,0,new byte[8]),FullName=$"Junior {i}",TeamName="Home",Position=Position.WR,ClassYear=3,OverallRating=96}).Append(new DynastyPlayer{PlayerId=Guid.NewGuid(),FullName="Senior",TeamName="Home",Position=Position.QB,ClassYear=4,OverallRating=70}).ToArray();
        var state=new DynastyState{DynastyId=Guid.Parse("11111111-2222-3333-4444-555555555555"),DynastyName="Departures",UserTeamName="Home",SeasonYear=2026,Phase=SeasonPhase.TransferPortal,ActiveRoster=roster};
        var result=OffseasonPlayerLifecycleService.EnterTransferPortal(state);
        Assert.Contains(result.RecentPlayerDepartures,p=>p.Reason==PlayerDepartureReason.EarlyProDeclaration);
        Assert.Contains(result.RecentPlayerDepartures,p=>p.Reason==PlayerDepartureReason.Graduation);
        Assert.All(result.RecentPlayerDepartures.Where(p=>p.Reason==PlayerDepartureReason.EarlyProDeclaration),p=>Assert.DoesNotContain(result.ActiveRoster,r=>r.PlayerId==p.PlayerId));
        Assert.Equal(result.PlayerDepartureHistory,OffseasonPlayerLifecycleService.EnterTransferPortal(result).PlayerDepartureHistory);
        Assert.Equal(result.PlayerDepartureHistory,JsonSerializer.Deserialize<DynastyState>(JsonSerializer.Serialize(result))!.PlayerDepartureHistory);
    }

    [Fact]
    public void AutomaticRosterRebalancingAlsoRecordsReleasedPlayers()
    {
        var team=new Team{Name="Home",Abbreviation="H",ConferenceName="Test",Prestige=60};
        var source=LegacyDynastyRosterFactory.Create([],[team],Guid.NewGuid(),2026);
        var roster=source.Select(p=>p.Position==Position.QB ? p with{Position=Position.WR} : p).ToArray();
        var state=new DynastyState{DynastyName="Rebalance",UserTeamName="Home",SeasonYear=2026,ActiveRoster=roster};
        var result=WalkOnRosterService.FillAllTeams(state,new Dictionary<string,Team>{{team.Name,team}});
        var removed=roster.Where(p=>result.ActiveRoster.All(r=>r.PlayerId!=p.PlayerId)).ToArray();
        Assert.NotEmpty(removed);
        foreach(var p in removed)Assert.Contains(result.PlayerDepartureHistory,d=>d.PlayerId==p.PlayerId && d.Reason==PlayerDepartureReason.Released);
        Assert.Equal(result.PlayerDepartureHistory,WalkOnRosterService.FillAllTeams(result,new Dictionary<string,Team>{{team.Name,team}}).PlayerDepartureHistory);
    }

}


