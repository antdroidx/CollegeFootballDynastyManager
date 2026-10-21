using System.Text.Json;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Import;
using DynastyManager.Data.Persistence;
using Xunit;

namespace DynastyManager.Core.Tests;

public sealed class CollegeSeasonFeatureTests
{
    private static Team Team(string name, string conference = "League", int prestige = 60) => new() { Name = name, Abbreviation = name[..Math.Min(4,name.Length)], ConferenceName = conference, Prestige = prestige };
    private static DynastyState State(params Team[] teams) => new() { DynastyId = Guid.Parse("57b858f6-a08c-4a81-9a69-3432aaa61d5a"), DynastyName = "Test", UserTeamName = teams[0].Name,
        SeasonYear = 2026, Week = 1, Phase = SeasonPhase.RegularSeason, LeagueTeams = teams };
    private static ScheduledGame Result(string home, string away, int week = 1, int homeScore = 28, int awayScore = 14) => new()
    { GameId = $"{week}-{home}-{away}", SeasonYear = 2026, Week = week, HomeTeamName = home, AwayTeamName = away, HasPlayed = true, HomeScore = homeScore, AwayScore = awayScore };

    [Fact]
    public void HumanBallotsUseTop25PointsAndAreDeterministic()
    {
        var teams = Enumerable.Range(1,32).Select(i => Team($"Team{i:D2}", prestige: 35+i)).ToArray(); var state = State(teams);
        foreach (var (kind,voters) in new[] { (PollKind.Media,65), (PollKind.Coaches,60) })
        {
            var ranks = CollegePollService.Build(state, teams.ToDictionary(t => t.Name), kind);
            Assert.Equal(325*voters, ranks.Sum(r => r.Score));
            Assert.Equal(ranks, CollegePollService.Build(state, teams.ToDictionary(t => t.Name), kind));
            Assert.Equal(32, ranks.Select(r => r.TeamName).Distinct().Count());
        }
    }

    [Fact]
    public void ComputerPollDoesNotUseBrandOrConferenceBias()
    {
        var a = Team("A", "Small", 5); var b = Team("B", "Power", 99); var c = Team("C");
        var state = State(a,b,c) with { Schedule = [Result("A","B"),Result("A","C",2),Result("C","B",3)] };
        var ranks = CollegePollService.Build(state, state.LeagueTeams.ToDictionary(t => t.Name), PollKind.Computer);
        Assert.Equal("A",ranks[0].TeamName); Assert.Equal("B",ranks[^1].TeamName);
        var swapped = state.LeagueTeams.Select(t => t with { Prestige = 100-t.Prestige }).ToDictionary(t => t.Name);
        Assert.Equal(ranks.Select(r => r.Score),CollegePollService.Build(state,swapped,PollKind.Computer).Select(r => r.Score));
    }

    [Fact]
    public void PollSnapshotsAreIdempotentAndCountFcsResults()
    {
        var a = Team("A"); var b = Team("B"); var state = State(a,b) with { Schedule = [Result("A","Montana") with { IsFcsGame = true }] };
        var teams = state.LeagueTeams.ToDictionary(t => t.Name);
        var recorded = CollegePollService.Record(state,teams); var twice = CollegePollService.Record(recorded,teams);
        Assert.Equal(3,twice.PollHistory.Count);
        Assert.All(twice.PollHistory,p => Assert.Equal(1,p.Rankings.Single(r => r.TeamName == "A").Wins));
        Assert.Equal("#1 A",CollegePollService.RankedName("A",twice.PollHistory[0].Rankings));
    }

    [Fact]
    public void SeasonHonorsRewardProductionAndIncludeFullLinesAndConferenceTeams()
    {
        var a = Team("A"); var b = Team("B"); var state = State(a,b);
        var roster = new List<DynastyPlayer>(); var lines = new List<PlayerSeasonStatLine>();
        foreach (var team in state.LeagueTeams)
        foreach (var position in Enum.GetValues<Position>())
        for (var i=0; i<(position==Position.OL?7:3);i++)
        {
            var p = new DynastyPlayer { PlayerId = Guid.NewGuid(), FullName = $"{team.Name} {position} {i}", TeamName=team.Name,Position=position,OverallRating=95-i*5,ClassYear=3 };
            roster.Add(p); lines.Add(new() { SeasonYear=2026,PlayerId=p.PlayerId,PlayerName=p.FullName,TeamName=p.TeamName,Position=position,Games=1,
                PassYards=position==Position.QB?200+i*50:0,PassingTouchdowns=position==Position.QB?i+1:0,Tackles=i*3,RushYards=position==Position.RB?i*100:0,
                ReceivingYards=position==Position.WR?i*100:0,FieldGoalsMade=position==Position.K?i:0,FieldGoalsAttempted=position==Position.K?i:0 });
        }
        state=state with { ActiveRoster=roster,Schedule=[Result("A","B") with { PlayerStats=lines }] };
        var awards=SeasonAwardsService.Build(state,false);
        Assert.Contains(awards,a => a.AwardName=="Heisman Trophy");
        Assert.Equal(5,awards.Count(a => a.IsAllAmerican && a.Position==Position.OL && a.SelectionTeam==1));
        Assert.Equal(2,awards.Count(a => a.IsAllAmerican && a.Position==Position.CB && a.SelectionTeam==1));
        Assert.Contains(awards,a => a.ConferenceName=="League" && a.AwardName=="First Team All-Conference");
        Assert.EndsWith("2",awards.Single(a => a.AwardName=="Davey O’Brien Award").PlayerName);
        var pre=SeasonAwardsService.EnsurePreseason(state); Assert.All(pre.PlayerAwardHistory,a => Assert.True(a.IsPreseason));
        Assert.Equal(pre.PlayerAwardHistory.Count,SeasonAwardsService.EnsurePreseason(pre).PlayerAwardHistory.Count);
        var final=DynastyHistoryService.FinalizeSeason(pre,state.LeagueTeams.ToDictionary(t=>t.Name));
        Assert.Contains(final.PlayerAwardHistory,a => !a.IsPreseason && a.AwardName=="Heisman Trophy");
        Assert.Equal(final.PlayerAwardHistory.Count,DynastyHistoryService.FinalizeSeason(final,state.LeagueTeams.ToDictionary(t=>t.Name)).PlayerAwardHistory.Count);
        var restored=JsonSerializer.Deserialize<DynastyState>(JsonSerializer.Serialize(final))!;
        Assert.Equal(final.PlayerAwardHistory,restored.PlayerAwardHistory);
    }

    [Fact]
    public void AnUnusedHighlyRatedBenchPlayerCannotWinFinalHonors()
    {
        var state=State(Team("A"),Team("B"));
        var starter=new DynastyPlayer { PlayerId=Guid.NewGuid(),FullName="Starter",TeamName="A",Position=Position.QB,OverallRating=70 };
        var bench=starter with { PlayerId=Guid.NewGuid(),FullName="Bench",OverallRating=99 };
        state=state with { ActiveRoster=[starter,bench],Schedule=[Result("A","B") with { PlayerStats=[new() { SeasonYear=2026,PlayerId=starter.PlayerId,PlayerName=starter.FullName,TeamName="A",Position=Position.QB,Games=1,PassYards=300 }] }] };
        Assert.DoesNotContain(SeasonAwardsService.Build(state,false),a=>a.PlayerId==bench.PlayerId);
    }

    [Fact]
    public void ConductIsProcessedOnceAndCanBeDisabledOrImprovedOnce()
    {
        var state=State(Team("A"),Team("B"));
        var next=ProgramDisciplineService.AdvanceWeek(state); var twice=ProgramDisciplineService.AdvanceWeek(next);
        Assert.Equal(next.ProgramConduct,twice.ProgramConduct); Assert.Equal(next.DisciplineHistory,twice.DisciplineHistory);
        var disabled=state with { LeagueRules=new() { ProgramDiscipline=false } }; Assert.Same(disabled,ProgramDisciplineService.AdvanceWeek(disabled));
        var improved=ProgramDisciplineService.ImproveOversight(next,"A"); var again=ProgramDisciplineService.ImproveOversight(improved,"A");
        Assert.Equal(improved.ProgramConduct,again.ProgramConduct); Assert.Equal(improved.DisciplineHistory,again.DisciplineHistory);
        Assert.All(next.ProgramConduct,p=>Assert.InRange(p.Score,0,100));
    }

    [Fact]
    public void RepeatedMajorFailuresCanCauseARarePostseasonBan()
    {
        var state=State(Team("A"),Team("B")) with
        {
            ProgramConduct=[new() {TeamName="A",Score=5}],
            DisciplineHistory=Enumerable.Range(0,3).Select(i=>new DisciplineIncident { SeasonYear=2025,TeamName="A",EventId="compliance",Title="Review",Description="Review",Severity=12 }).ToArray()
        };
        var sanctioned=false;
        for(var week=1;week<=13;week++)
        {
            state=ProgramDisciplineService.AdvanceWeek(state with { Week=week });
            sanctioned|=!ProgramDisciplineService.PostseasonEligible(state,"A");
        }
        Assert.True(sanctioned); Assert.Single(state.DisciplineHistory,i=>i.SanctionIssued);
        Assert.True(ProgramDisciplineService.PostseasonEligible(state with { SeasonYear=2027 },"A"));
        Assert.True(ProgramDisciplineService.PostseasonEligible(state with { LeagueRules=state.LeagueRules with { ProgramDiscipline=false } },"A"));
    }

    [Fact]
    public void HealthyProgramsVeryRarelyReceiveSanctions()
    {
        var teams=Enumerable.Range(0,64).Select(i=>Team($"T{i}")).ToArray();var state=State(teams);
        for(var year=2026;year<2036;year++) for(var week=1;week<=13;week++) state=ProgramDisciplineService.AdvanceWeek(state with { SeasonYear=year,Week=week });
        Assert.InRange(state.DisciplineHistory.Count(i=>i.SanctionIssued),0,3);
        Assert.Contains(state.DisciplineHistory,i=>i.Severity<0); Assert.Contains(state.DisciplineHistory,i=>i.Severity>0);
    }

    [Theory]
    [InlineData(16,8)] [InlineData(9,5)] [InlineData(15,9)]
    public void FcsSchedulesPreserveCountsConferenceGamesAndOneOpponentLimit(int count,int games)
    {
        var teams=Enumerable.Range(0,count).Select(i=>Team($"T{i:D2}",count==16?$"C{i/8}":"Independent")).ToArray();
        var state=State(teams);var rules=new LeagueRules { GamesPerTeam=games,FcsOpponents=true,AllowAutomaticConferenceFill=true };
        var slate=FcsLeagueService.BuildSchedule(teams,state.DynastyId,2026,rules);
        foreach(var t in teams)
        {
            var own=slate.Where(g=>g.InvolvesTeam(t.Name)).ToArray();Assert.Equal(games,own.Length);
            Assert.InRange(own.Count(g=>g.IsFcsGame),0,1);Assert.Equal(own.Length,own.Select(g=>g.Week).Distinct().Count());
            Assert.All(own.Where(g=>g.IsFcsGame),g=>Assert.Equal(ScheduledGameType.NonConference,g.GameType));
        }
        foreach(var group in slate.SelectMany(g=>new[]{(Team:g.HomeTeamName,g.Week),(Team:g.AwayTeamName,g.Week)}).GroupBy(x=>x)) Assert.Single(group);
        Assert.Equal(slate.Count,slate.Select(g=>g.GameId).Distinct().Count());
        Assert.DoesNotContain(FcsLeagueService.Available(teams.Append(Team("Montana"))),t=>t.Name=="Montana");
    }

    [Fact]
    public void FcsGamesUseTheSameEngineAndDoNotAddExternalPlayersToRecruitingRoster()
    {
        var teams=new[]{Team("A"),Team("B")};var state=State(teams) with { Schedule=[Result("A","Montana") with { IsFcsGame=true,HasPlayed=false,HomeScore=null,AwayScore=null }] };
        state=WalkOnRosterService.FillAllTeams(state,teams.ToDictionary(t=>t.Name)); var count=state.ActiveRoster.Count;
        var played=WeekSimulation.SimulateCurrentRegularSeasonWeek(state,teams.ToDictionary(t=>t.Name));
        Assert.True(played.Schedule[0].HasPlayed);Assert.NotEmpty(played.Schedule[0].PlayerStats); Assert.Equal(count,played.ActiveRoster.Count);
        Assert.NotEmpty(played.Schedule[0].Highlights); Assert.Equal(played.Schedule[0].HomeScore,played.Schedule[0].PeriodScores.Sum(p=>p.HomePoints));
    }

    [Fact]
    public void FullModernLeagueSchedulesWithFcsAndCanGrowAcrossOffseasons()
    {
        var legacy=LegacyUniverseCsvImporter.Import(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"TestData","universe.csv"))).Teams;
        var state=State(ModernFbsUniverseFactory.Create(legacy).ToArray()) with { Phase=SeasonPhase.Preseason,LeagueRules=new() { FcsOpponents=true,FcsPromotion=true,ConferenceRealignment=true } };
        var promoted=false;
        for(var year=2026;year<2056;year++)
        {
            state=LeagueEvolutionService.AdvanceOffseason(state with {SeasonYear=year});
            var schedule=FcsLeagueService.BuildSchedule(state.LeagueTeams,state.DynastyId,year,state.LeagueRules);
            Assert.Contains(schedule,g=>g.IsFcsGame);
            foreach(var team in state.LeagueTeams)
            {
                var games=schedule.Where(g=>g.InvolvesTeam(team.Name)).ToArray();
                Assert.Equal(12,games.Length);Assert.InRange(games.Count(g=>g.IsFcsGame),0,1);
                Assert.Equal(12,games.Select(g=>g.Week).Distinct().Count());
                Assert.DoesNotContain(games,g=>g.IsFcsGame && state.LeagueTeams.Any(t=>FcsLeagueService.Identity(t.Name)==FcsLeagueService.Identity(g.AwayTeamName)));
            }
            promoted|=state.LeagueMovementHistory.Any(m=>m.Promotion);
            state=state with {Schedule=schedule};
            if(promoted && year>=2030) break;
        }
        Assert.True(promoted);
    }

    [Fact]
    public void SanctionedTeamsCannotReceivePlayoffOrBowlInvitations()
    {
        var teams=Enumerable.Range(0,16).Select(i=>Team($"T{i:D2}")).ToArray();
        var state=State(teams) with {Phase=SeasonPhase.Postseason,Week=15,ProgramConduct=[new() {TeamName="T00",PostseasonBanSeason=2026}],
            Schedule=Enumerable.Range(1,6).Select(i=>Result("T00",$"T{i:D2}",i)).ToArray()};
        var lookup=teams.ToDictionary(t=>t.Name);
        state=CollegeFootballPlayoffService.InitializePlayoff(state,lookup);
        Assert.DoesNotContain(state.CollegeFootballPlayoffHistory,s=>s.TeamName=="T00");
        state=BowlService.InitializeBowls(state,lookup);
        Assert.DoesNotContain(state.Schedule,g=>g.GameType==ScheduledGameType.Bowl && g.InvolvesTeam("T00"));
        var ranking=NationalRankingService.Build(state with {Schedule=[Result("T00","Montana") with {IsFcsGame=true}]},lookup).Single(r=>r.TeamName=="T00");
        Assert.Equal(1,ranking.Wins);
    }

    [Fact]
    public void OptionalEvolutionIsOffByDefaultAndIdempotentWhenEnabled()
    {
        var teams=Enumerable.Range(0,16).Select(i=>Team($"T{i}",$"C{i/8}")).ToArray();var state=State(teams) with { Phase=SeasonPhase.Preseason };
        Assert.Equal(teams,LeagueEvolutionService.AdvanceOffseason(state).LeagueTeams);
        var moved=false;var promoted=false;
        for(var year=2027;year<2057;year++)
        {
            var evolved=LeagueEvolutionService.AdvanceOffseason(state with { SeasonYear=year,LeagueRules=new() {ConferenceRealignment=true,FcsOpponents=true,FcsPromotion=true} });
            moved|=evolved.LeagueMovementHistory.Any(m=>!m.Promotion);promoted|=evolved.LeagueMovementHistory.Any(m=>m.Promotion);
            Assert.Equal(evolved.LeagueTeams.Count,evolved.LeagueTeams.Select(t=>FcsLeagueService.Identity(t.Name)).Distinct().Count());
            Assert.Same(evolved,LeagueEvolutionService.AdvanceOffseason(evolved));
            if(evolved.LeagueTeams.Count>teams.Length) Assert.NotEmpty(evolved.ActiveRoster);
        }
        Assert.True(moved);Assert.True(promoted);
    }

    [Fact]
    public void ProjectedLinesReactToRatingsAndHomeFieldWithoutReadingResults()
    {
        TeamSimulationProfile P(string name,int rating)=>new() { TeamName=name,PassOffenseRating=rating,RushOffenseRating=rating,PassDefenseRating=rating,RushDefenseRating=rating };
        var game=Result("A","B");var equal=GameOutlookService.Build(game,P("A",80),P("B",80));
        Assert.Equal("A",equal.Favorite);Assert.True(equal.HomeWinProbability>.5);
        Assert.Equal(.5,GameOutlookService.Build(game with {IsNeutralSite=true},P("A",80),P("B",80)).HomeWinProbability);
        Assert.Equal("B",GameOutlookService.Build(game,P("A",60),P("B",90)).Favorite);
        Assert.Equal(equal,GameOutlookService.Build(game with { HomeScore=0,AwayScore=90 },P("A",80),P("B",80)));
    }

    [Fact]
    public void HighlightTapeRetainsParticipantsAndNeverChangesTheGame()
    {
        var session=GameDayEngineTests.Session(GameDayMode.Simulate,includeOpeningKickoff:true);GameDayEngine.SimulateToEnd(session);
        var result=GameDayEngine.Finish(session);var score=session.HomeScore;var before=JsonSerializer.Serialize(result);
        Assert.InRange(result.Highlights.Count,1,5);Assert.NotEmpty(result.HighlightPlayers);
        var ids=result.HighlightPlayers.Select(p=>p.PlayerId).ToHashSet();
        Assert.All(result.Highlights.SelectMany(s=>s.OffensePersonnel.Concat(s.DefensePersonnel)),id=>Assert.Contains(id,ids));
        var replay=GameHighlightService.ReplayRoster(result);Assert.Equal(ids.Count,replay.Count);
        Assert.Equal(result.Highlights.OrderBy(s=>s.Sequence),result.Highlights);
        Assert.Equal(before,JsonSerializer.Serialize(result));Assert.Equal(score,session.HomeScore);
        Assert.Equal(result.HomeScore,result.PeriodScores.Sum(p=>p.HomePoints));Assert.Equal(result.AwayScore,result.PeriodScores.Sum(p=>p.AwayPoints));
        Assert.Contains(result.PlayerStats,p=>p.Position==Position.OL && p.Games==1);
    }
}
