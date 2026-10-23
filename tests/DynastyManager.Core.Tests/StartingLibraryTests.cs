using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Import;
using DynastyManager.Data.Persistence;
using Xunit;

namespace DynastyManager.Core.Tests;

public class StartingLibraryTests
{
    [Fact]
    public void BuiltInFbsPresetsRemainValidStartingSetups()
    {
        var legacy = LegacyUniverseCsvImporter.Import(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "universe.csv"))).Teams;
        foreach (var teams in new[] { legacy, ModernFbsUniverseFactory.Create(legacy) })
            StartingLibraryService.Validate(new StartingLeagueSetup { Name = "FBS", Teams = teams });
    }

    [Fact]
    public void IndependentSchoolsCanPlayWithoutInventingConferenceGames()
    {
        var setup = Setup(4, 2, 4); var teams = setup.Teams.Select(t => t with { ConferenceName = "Independent" }).ToArray();
        var schedule = SeasonScheduleBuilder.BuildRegularSeason(teams, setup.Id, 2026, gamesPerTeam: 2, allowAutomaticConferenceFill: true);
        Assert.Equal(4, schedule.Count); Assert.All(schedule, g => Assert.Equal(ScheduledGameType.NonConference, g.GameType));
    }

    [Fact]
    public void ConferenceTitleGameOptionIsHonored()
    {
        var setup = Setup(8, 6, 4); var teams = setup.Teams.ToDictionary(t => t.Name);
        var state = new DynastyState { DynastyName = "League", UserTeamName = "School 1", SeasonYear = 2026, Week = 14, Phase = SeasonPhase.ConferenceChampionship, LeagueRules = setup.Rules };
        Assert.Single(ConferenceChampionshipService.ScheduleChampionships(state, teams).Schedule);
        Assert.Empty(ConferenceChampionshipService.ScheduleChampionships(state with { LeagueRules = setup.Rules with { ConferenceChampionships = false } }, teams).Schedule);
    }

    [Theory]
    [InlineData(2, 1)] [InlineData(3, 2)] [InlineData(4, 3)] [InlineData(8, 6)]
    [InlineData(13, 12)] [InlineData(16, 12)] [InlineData(24, 12)] [InlineData(64, 12)] [InlineData(160, 12)]
    public void CustomLeaguesScheduleWithoutFixedFbsTeamCounts(int count, int games)
    {
        var setup = Setup(count, games, count < 4 ? 2 : 4);
        StartingLibraryService.Validate(setup);
        var schedule = SeasonScheduleBuilder.BuildRegularSeason(setup.Teams, setup.Id, 2026,
            gamesPerTeam: games, allowAutomaticConferenceFill: true);
        foreach (var team in setup.Teams)
        {
            Assert.Equal(games, schedule.Count(g => g.InvolvesTeam(team.Name)));
            Assert.Equal(games, schedule.Where(g => g.InvolvesTeam(team.Name)).Select(g => g.HomeTeamName == team.Name ? g.AwayTeamName : g.HomeTeamName).Distinct().Count());
        }
        Assert.All(schedule.GroupBy(g => g.Week), week => Assert.Equal(week.Count() * 2,
            week.SelectMany(g => new[] { g.HomeTeamName, g.AwayTeamName }).Distinct().Count()));
    }

    [Theory]
    [InlineData(2)] [InlineData(4)] [InlineData(8)] [InlineData(12)]
    public void EverySupportedPlayoffSizeFinishesAndRecordsAChampion(int size)
    {
        var setup = Setup(size, size == 2 ? 1 : 2, size);
        var teams = setup.Teams.ToDictionary(t => t.Name);
        var state = new DynastyState { DynastyName = "Custom", UserTeamName = "School 1", SeasonYear = 2026,
            Phase = SeasonPhase.Postseason, Week = 15, LeagueTeams = setup.Teams, LeagueRules = setup.Rules };
        state = CollegeFootballPlayoffService.InitializePlayoff(state, teams);
        Assert.Equal(size, state.CollegeFootballPlayoffHistory.Count);
        for (var week = 15; week <= 18; week++)
        {
            state = state with { Week = week };
            state = CollegeFootballPlayoffService.SimulateCurrentRound(state, teams);
            state = CollegeFootballPlayoffService.ScheduleNextRound(state);
        }
        Assert.Equal(size - 1, state.Schedule.Count);
        Assert.All(state.Schedule, g => Assert.True(g.HasPlayed));
        Assert.Single(state.NationalChampionshipHistory);
        Assert.Equal(state.Schedule.Single(g => g.PostseasonRound == PostseasonRound.NationalChampionship).WinnerTeamName,
            state.NationalChampionshipHistory[0].ChampionTeamName);
        Assert.Equal(size - 1, CollegeFootballPlayoffService.InitializePlayoff(state with { Week = 15 }, teams).Schedule.Count);
    }

    [Fact]
    public void StarterRosterCopiesDoNotShareDynastyIdentityOrChangeExistingCareers()
    {
        var setup = Setup(4, 2, 4);
        var imported = new DynastyPlayer { PlayerId = Guid.NewGuid(), FullName = "Imported Player", TeamName = "School 1", Position = Position.QB, ClassYear = 1, OverallRating = 75 };
        var generated = new[] { imported with { FullName = "Generated Player" }, imported with { TeamName = "School 2", PlayerId = Guid.NewGuid() } };
        var pack = new StartingRosterPack { Name = "NEXT", Players = [imported] };
        var first = StartingLibraryService.CreateRoster(setup, pack, generated);
        var second = StartingLibraryService.CreateRoster(setup, pack, generated);
        Assert.Equal(2, first.Count); Assert.Contains(first, p => p.TeamName == "School 2");
        Assert.DoesNotContain(first, p => p.FullName == "Generated Player");
        Assert.NotEqual(imported.PlayerId, first[1].PlayerId); Assert.NotEqual(first[1].PlayerId, second[1].PlayerId);
        var dynasty = new DynastyState { DynastyName = "First", UserTeamName = "School 1", LeagueTeams = setup.Teams.ToArray(), ActiveRoster = first };
        var library = new StartingLibrary { Setups = [setup], Rosters = [pack], DefaultRosterBySetup = new Dictionary<Guid, Guid> { [setup.Id] = pack.Id } };
        var removed = StartingLibraryService.RemoveRoster(library, pack.Id);
        Assert.Empty(removed.Rosters); Assert.Empty(removed.DefaultRosterBySetup);
        Assert.Equal(2, dynasty.ActiveRoster.Count); Assert.Equal(4, dynasty.LeagueTeams.Count);
        Assert.Throws<ArgumentException>(() => StartingLibraryService.CreateRoster(setup, pack with { Players = [imported with { TeamName = "Unknown" }] }, generated));
    }

    [Fact]
    public void FullRosterPacksCanBeUsedBySmallerLeagueSubsets()
    {
        var setup = Setup(4, 2, 4);
        var player = new DynastyPlayer { FullName = "Imported", PlayerId = Guid.NewGuid(), TeamName = "School 1", Position = Position.QB };
        var pack = new StartingRosterPack { Name = "Full universe", Players = [player, player with { TeamName = "School 99", PlayerId = Guid.NewGuid() }] };
        Assert.True(StartingLibraryService.IsCompatible(pack, setup.Teams));
        var roster = StartingLibraryService.CreateRoster(setup, pack, []);
        Assert.Single(roster); Assert.Equal("School 1", roster[0].TeamName); Assert.Equal(2, pack.Players.Count);
    }

    [Fact]
    public async Task StartingLibraryPersistsSeparatelyWithRosterDefaultsAndHiddenBuiltIns()
    {
        var path = Path.Combine(Path.GetTempPath(), $"starters-{Guid.NewGuid():N}.json");
        try
        {
            var setup = Setup(4, 2, 4); var pack = new StartingRosterPack { Name = "Roster" };
            var data = new StartingLibrary { Setups = [setup], Rosters = [pack], HiddenBuiltIns = [Guid.NewGuid()], DefaultRosterBySetup = new Dictionary<Guid, Guid> { [setup.Id] = pack.Id } };
            await new StartingLibraryRepository(path).SaveAsync(data);
            var loaded = await new StartingLibraryRepository(path).LoadAsync();
            Assert.Equal(setup.Rules, loaded.Setups.Single().Rules);
            Assert.Equal(pack.Id, loaded.DefaultRosterBySetup[setup.Id]); Assert.Single(loaded.HiddenBuiltIns);
            var removed = StartingLibraryService.RemoveSetup(loaded, setup);
            Assert.Empty(removed.Setups); Assert.Empty(removed.DefaultRosterBySetup); Assert.Single(removed.Rosters);
        }
        finally { File.Delete(path); File.Delete(path + ".tmp"); }
    }

    [Fact]
    public void InvalidLeagueConfigurationsExplainTheirConstraints()
    {
        Assert.Throws<ArgumentException>(() => StartingLibraryService.Validate(Setup(3, 1, 2)));
        Assert.Throws<ArgumentException>(() => StartingLibraryService.Validate(Setup(4, 4, 4)));
        Assert.Throws<ArgumentException>(() => StartingLibraryService.Validate(Setup(4, 2, 8)));
        var setup = Setup(4, 2, 4);
        Assert.Throws<ArgumentException>(() => StartingLibraryService.Validate(setup with { Teams = [setup.Teams[0], setup.Teams[0]] }));
    }

    [Fact]
    public void ManuallyPlayedFinalIsRecordedWithoutSimulatingTheGameAgain()
    {
        var setup = Setup(2, 1, 2); var teams = setup.Teams.ToDictionary(t => t.Name);
        var state = new DynastyState { DynastyName = "Final", UserTeamName = "School 1", SeasonYear = 2026, Phase = SeasonPhase.Postseason, Week = 15, LeagueRules = setup.Rules };
        state = CollegeFootballPlayoffService.InitializePlayoff(state, teams);
        state = state with { Week = 18, Schedule = state.Schedule.Select(g => g with { HasPlayed = true, HomeScore = 24, AwayScore = 17 }).ToArray() };
        var recorded = CollegeFootballPlayoffService.SimulateCurrentRound(state, teams);
        Assert.Single(recorded.NationalChampionshipHistory);
        Assert.Equal(24, recorded.NationalChampionshipHistory[0].ChampionScore);
        Assert.Single(CollegeFootballPlayoffService.RecordNationalChampion(recorded).NationalChampionshipHistory);
    }

    private static StartingLeagueSetup Setup(int count, int games, int playoffs) => new()
    {
        Name = "Custom", Teams = Enumerable.Range(1, count).Select(i => new Team { Name = $"School {i}", Abbreviation = $"S{i}", ConferenceName = "Custom Conference", Prestige = 50 }).ToArray(),
        Rules = new LeagueRules { GamesPerTeam = games, PlayoffTeams = playoffs, AllowAutomaticConferenceFill = true }
    };
}
