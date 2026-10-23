using System.Text.Json;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class GameConditionsAndAwardsTests
{
    private static GameDaySession Session(GameWeather weather, int seed = 7123, GameDayMode mode = GameDayMode.Play)
    {
        var s = GameDayEngineTests.Session(mode, seed:seed);
        var result = GameDayEngine.Start(s.Game with { Weather=weather },s.HomeProfile,s.AwayProfile,s.Roster,mode);
        result.KickoffPending=false; return result;
    }
    [Fact]
    public void WeatherIsRepeatableStoredAndOverridesAreNormalized()
    {
        var game = GameDayEngineTests.Session(GameDayMode.Play).Game with { HomeTeamName="Washington", Week=10 };
        Assert.Equal(GameWeatherService.ForGame(game),GameWeatherService.ForGame(game));
        var indoor = GameWeatherService.ForGame(game with { HomeTeamName="Syracuse" });
        Assert.True(indoor.Indoors);Assert.Equal(0,indoor.WindMph);
        var s=Session(new() {WindMph=100,TemperatureFahrenheit=-100,WindFieldDirection=9});
        Assert.Equal(45,s.Weather.WindMph);Assert.Equal(-10,s.Weather.TemperatureFahrenheit);Assert.Equal(1,s.Weather.WindFieldDirection);
        GameDayEngine.SimulateToEnd(s);var result=GameDayEngine.Finish(s);
        Assert.Equal(s.Weather,result.Weather);
        Assert.Equal(result.Weather,JsonSerializer.Deserialize<ScheduledGame>(JsonSerializer.Serialize(result))!.Weather);
    }
    [Fact]
    public void WindReversesWithEndsAndIndoorConditionsIgnoreWindAndRain()
    {
        var s=Session(new() {WindMph=20,WindFieldDirection=1});
        Assert.Equal(20,GameWeatherService.Tailwind(s,"Home"));Assert.Equal(-20,GameWeatherService.Tailwind(s,"Away"));
        s.Quarter=2;Assert.Equal(-20,GameWeatherService.Tailwind(s,"Home"));
        var indoor=Session(new() {Indoors=true,WindMph=40,Precipitation=GamePrecipitation.Rain});
        Assert.Equal(0,GameWeatherService.Tailwind(indoor,"Home"));Assert.Equal(0,GameWeatherService.CatchPenalty(indoor.Weather,true));
        Assert.Equal(.8,GameWeatherService.KickProbability(indoor,.8));
    }
    [Fact]
    public void PoorWeatherHurtsDeepThrowsBallSecurityAndKickingWithoutInvalidProbabilities()
    {
        var clear=new GameWeather();var wet=new GameWeather {Precipitation=GamePrecipitation.Rain,WindMph=25,WindFieldDirection=-1};
        Assert.True(GameWeatherService.CatchPenalty(wet,true)>GameWeatherService.CatchPenalty(wet,false));
        Assert.True(GameWeatherService.FumblePenalty(wet)>GameWeatherService.FumblePenalty(clear));
        var calm=Session(clear);var wind=Session(wet);calm.YardLine=wind.YardLine=65;
        Assert.True(GameDayEngine.FieldGoalProbability(wind)<GameDayEngine.FieldGoalProbability(calm));
        Assert.InRange(GameDayEngine.FieldGoalProbability(wind),.01,.995);
    }
    [Fact]
    public void WeatherActuallyChangesSeededPassResultsAndAllThreeModesStayIdentical()
    {
        var source=GameDayEngineTests.Session(GameDayMode.Play);
        GameDaySession Start(int seed,GameWeather w,GameDayMode mode)=>GameDayEngine.Start(source.Game with {SimulationSeed=seed,Weather=w},source.HomeProfile,source.AwayProfile,source.Roster,mode);
        var clearWins=0;var stormWins=0;
        for(var seed=1;seed<=400;seed++)
        {
            var clear=Start(seed,new(),GameDayMode.Play);var storm=Start(seed,new() {Precipitation=GamePrecipitation.Snow,WindMph=35},GameDayMode.Play);
            clear.KickoffPending=storm.KickoffPending=false;
            if(GameDayEngine.Step(clear,"off-spread-verts").Outcome is SnapOutcome.Completion or SnapOutcome.Touchdown)clearWins++;
            if(GameDayEngine.Step(storm,"off-spread-verts").Outcome is SnapOutcome.Completion or SnapOutcome.Touchdown)stormWins++;
        }
        Assert.True(clearWins>stormWins,$"calm={clearWins}; storm={stormWins}");
        var games=Enum.GetValues<GameDayMode>().Select(mode=>{var s=Start(441,new(){WindMph=15,Precipitation=GamePrecipitation.Rain},mode);GameDayEngine.SimulateToEnd(s);return GameDayEngine.Finish(s);}).ToArray();
        foreach(var game in games.Skip(1)){Assert.Equal(games[0].HomeStats,game.HomeStats);Assert.Equal(games[0].AwayStats,game.AwayStats);Assert.Equal(games[0].PlayerStats,game.PlayerStats);Assert.Equal(games[0].PlayerOfGame,game.PlayerOfGame);}
    }
    [Theory]
    [InlineData(49,40,false)] [InlineData(50,45,false)] [InlineData(51,10,true)] [InlineData(99,90,true)] [InlineData(100,95,true)]
    public void KickoffReplayRequiresCrossingMidfieldNotJustReturnDistance(int spot,int yards,bool expected)
    {
        var e=new GameSnapEvent {OffenseTeamName="H",DefenseTeamName="A",OffensivePlayId="",DefensivePlayId="",Outcome=SnapOutcome.Kickoff,BallEndYardLine=spot,ReturnYards=yards};
        Assert.Equal(expected,GameDayLivePresentation.InstantReplay(e));
        Assert.False(GameDayLivePresentation.InstantReplay(e with{IsOnsideKick=true}));
        Assert.False(GameDayLivePresentation.InstantReplay(e with{IsTouchback=true}));
    }
    [Fact]
    public void KickReturnsCanBreakPastMidfieldAndScoreWithMatchingStatsAndConversion()
    {
        var source=GameDayEngineTests.Session(GameDayMode.Play);var crossed=0;GameDaySession? scored=null;
        for(var seed=1;seed<=2000 && (scored is null || crossed==0);seed++)
        {
            var s=GameDayEngine.Start(source.Game with{SimulationSeed=seed,Weather=new()},source.HomeProfile,source.AwayProfile,source.Roster,GameDayMode.Play);
            var e=GameDayEngine.Step(s);Assert.Equal(e.BallEndYardLine-e.KickLandingYardLine,e.ReturnYards);
            if(e.BallEndYardLine>50)crossed++;
            if(e.Points>=6)scored=s;
        }
        Assert.True(crossed>0);Assert.NotNull(scored);Assert.True(scored.TryPending);Assert.Equal(6,scored.PossessionTeamName=="Home"?scored.HomeScore:scored.AwayScore);
        var snap=Assert.Single(scored.Events);Assert.Null(snap.PrimaryDefenderId);
        var line=GameDayEngine.BoxScore(scored).Players.Single(p=>p.PlayerId==snap.ReturnerId);
        Assert.Equal(1,line.KickReturnTouchdowns);Assert.Equal(snap.ReturnYards,line.KickReturnYards);
        Assert.Equal(6,GameDayEngine.BoxScore(scored).Periods.Sum(p=>scored.PossessionTeamName=="Home"?p.HomePoints:p.AwayPoints));
        Assert.True(GameDayAnimationPlanner.CarrierDestinationYardLine(snap)>100);
    }
    [Fact]
    public void MoraleReactsToSuccessAndAdversityButRemainsBoundedAndReadOnly()
    {
        var s=Session(new());var baseline=GameMoraleService.InGame(s,"Home");
        GameSnapEvent E(string team,SnapOutcome outcome)=>new(){OffenseTeamName=team,DefenseTeamName=team=="Home"?"Away":"Home",OffensivePlayId="",DefensivePlayId="",Outcome=outcome};
        s.Events.Add(E("Home",SnapOutcome.Touchdown));Assert.True(GameMoraleService.InGame(s,"Home")>baseline);
        Assert.True(GameMoraleService.InGame(s,"Away")<baseline);
        s.Events.Clear();for(var i=0;i<100;i++)s.Events.Add(E("Home",SnapOutcome.Interception));
        Assert.InRange(GameMoraleService.InGame(s,"Home"),20,80);Assert.InRange(GameMoraleService.MatchupShift(s,"Home","Away"),-3,3);
        var count=s.Events.Count;var clock=s.ClockSeconds;for(var i=0;i<10;i++)GameMoraleService.InGame(s,"Home");
        Assert.Equal(count,s.Events.Count);Assert.Equal(clock,s.ClockSeconds);
    }
    [Fact]
    public void SustainedTeamMoraleHasASmallerPositiveOrNegativeDevelopmentEffect()
    {
        var source=GameDayEngineTests.Session(GameDayMode.Play);
        var state=new DynastyState{DynastyName="Training",UserTeamName="Home",SeasonYear=2026,Phase=SeasonPhase.Offseason,ActiveRoster=source.HomeRoster};
        ScheduledGame G(int week,bool win)=>source.Game with{GameId=$"week-{week}",Week=week,HasPlayed=true,HomeScore=win?30:10,AwayScore=win?10:30};
        var wins=state with{Schedule=Enumerable.Range(1,4).Select(w=>G(w,true)).ToArray()};
        var losses=state with{Schedule=Enumerable.Range(1,4).Select(w=>G(w,false)).ToArray()};
        Assert.True(GameMoraleService.SeasonMorale(wins,"Home")>GameMoraleService.SeasonMorale(losses,"Home"));
        Assert.Equal(1,GameMoraleService.TrainingModifier(GameMoraleService.SeasonMorale(wins,"Home")));
        Assert.Equal(-1,GameMoraleService.TrainingModifier(GameMoraleService.SeasonMorale(losses,"Home")));
        var up=PlayerDevelopmentService.ApplyOffseasonDevelopment(wins);var down=PlayerDevelopmentService.ApplyOffseasonDevelopment(losses);
        Assert.True(up.ActiveRoster.Sum(p=>p.OverallRating)>down.ActiveRoster.Sum(p=>p.OverallRating));
        Assert.All(up.ActiveRoster,p=>Assert.InRange(p.OverallRating,40,99));
        Assert.Equal(up.ActiveRoster.Select(p=>p.PlayerId),down.ActiveRoster.Select(p=>p.PlayerId));
    }
    [Fact]
    public void PlayerOfGameCanRecognizeDefenseAndSurvivesSeasonArchival()
    {
        var game=GameDayEngineTests.Session(GameDayMode.Play).Game with{HasPlayed=true,HomeScore=21,AwayScore=7};
        var db=new PlayerSeasonStatLine{PlayerId=Guid.NewGuid(),PlayerName="Impact Corner",TeamName="Home",Position=Position.CB,Interceptions=3,PassesDefended=4,Tackles=5,Games=1};
        var qb=new PlayerSeasonStatLine{PlayerId=Guid.NewGuid(),PlayerName="Quarterback",TeamName="Home",Position=Position.QB,PassYards=150,PassingTouchdowns=1,Games=1};
        var award=PlayerOfGameService.Select(game,[qb,db]);Assert.NotNull(award);Assert.Equal(db.PlayerId,award.PlayerId);Assert.Contains("PBU",award.Summary);
        game=game with{PlayerStats=[db,qb],PlayerOfGame=award};
        var state=new DynastyState{DynastyName="Awards",UserTeamName="Home",SeasonYear=2026,Schedule=[game]};
        var archived=DynastyHistoryService.FinalizeSeason(state,new Dictionary<string,Team>());
        Assert.Equal(award,Assert.Single(PlayerOfGameService.All(archived with{Schedule=[]})));
        var round=JsonSerializer.Deserialize<DynastyState>(JsonSerializer.Serialize(archived))!;
        Assert.Equal(award,Assert.Single(round.PlayerGameAwardHistory));
    }
    [Fact]
    public void FinishedGameStoresTheSamePlayerOfGameAsTheFinalLiveBox()
    {
        var s=Session(new(),mode:GameDayMode.Simulate);GameDayEngine.SimulateToEnd(s);
        var game=GameDayEngine.Finish(s);Assert.NotNull(game.PlayerOfGame);
        Assert.Equal(game.PlayerOfGame,GameDayEngine.BoxScore(s).PlayerOfGame);
        Assert.Contains(game.PlayerStats,p=>p.PlayerId==game.PlayerOfGame.PlayerId);
    }
    [Theory]
    [InlineData(2)] [InlineData(4)]
    public void FinalHeaveCanSnapInTheLastSecondsWithARunningClockAndAllEligibleRoutesGoDeep(int quarter)
    {
        var s=Session(new());s.Quarter=quarter;s.ClockSeconds=3;s.YardLine=45;s.HomeScore=0;s.AwayScore=7;s.ClockStopped=false;s.TwoMinuteTimeoutHalf=quarter==2?1:2;
        var e=GameDayEngine.Step(s,"off-shotgunspread-hail-mary");Assert.NotEqual(SnapOutcome.ClockStoppage,e.Outcome);
        Assert.InRange(e.PreSnapSeconds,0,1);Assert.Equal("off-shotgunspread-hail-mary",e.OffensivePlayId);
        var play=PlaybookCatalog.OffensivePlays.Single(p=>p.Id==e.OffensivePlayId);
        foreach(var role in new[]{Position.RB,Position.TE,Position.WR})Assert.Equal(GameDayRouteFamily.Verticals,GameDayAnimationPlanner.AssignedRoute(play,role,0));
    }
}
