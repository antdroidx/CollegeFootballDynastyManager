using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Import;
using DynastyManager.Data.Persistence;
using Xunit;

namespace DynastyManager.Core.Tests;

public class ModernFbsTests
{
    private static IReadOnlyList<Team> Legacy() => LegacyUniverseCsvImporter.Import(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "universe.csv"))).Teams;

    [Fact]
    public void MembershipAndRosterGenerationCoverAll138Schools()
    {
        var legacy = Legacy();
        var teams = ModernFbsUniverseFactory.Create(legacy);
        Assert.Equal(138, teams.Count);
        Assert.Equal(138, teams.Select(team => team.Name).Distinct().Count());
        var expected = new Dictionary<string, int> { ["ACC"] = 17, ["Big Ten"] = 18, ["Big 12"] = 16,
            ["SEC"] = 16, ["American"] = 14, ["Conf USA"] = 10, ["MAC"] = 13, ["Mt West"] = 10,
            ["Pac-12"] = 8, ["Sun Belt"] = 14, ["Independent"] = 2 };
        foreach (var conference in expected)
            Assert.Equal(conference.Value, teams.Count(team => team.ConferenceName == conference.Key));
        Assert.Equal("Pac-12", legacy.Single(team => team.Name == "Washington").ConferenceName);
        Assert.Equal("Big Ten", teams.Single(team => team.Name == "Washington").ConferenceName);
        Assert.Equal("ACC", teams.Single(team => team.Name == "California").ConferenceName);
        Assert.Equal("SEC", teams.Single(team => team.Name == "Texas").ConferenceName);
        Assert.All(teams.Where(team => team.ConferenceName != "Sun Belt"), team => Assert.Null(team.DivisionName));
        var roster = LegacyDynastyRosterFactory.Create([], teams, Guid.Parse("11111111-2222-3333-4444-555555555555"));
        Assert.Equal(138 * 85, roster.Count);
        Assert.All(teams, team => Assert.Equal(85, roster.Count(player => player.TeamName == team.Name)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ModernLeagueSupportsSchedulesAndTenConferenceChampionships(int seed)
    {
        var teams = ModernFbsUniverseFactory.Create(Legacy());
        var id = new Guid(seed, 2222, 3333, 4, 4, 5, 5, 5, 5, 5, 5);
        var schedule = SeasonScheduleBuilder.BuildRegularSeason(teams, id, 2026);
        Assert.Equal(138 * 6, schedule.Count);
        foreach (var team in teams)
        {
            var games = schedule.Where(game => game.InvolvesTeam(team.Name)).ToArray();
            Assert.Equal(12, games.Length);
            Assert.Equal(team.ConferenceGamesPerSeason, games.Count(game => game.GameType == ScheduledGameType.Conference));
            Assert.Equal(12, games.Select(game => game.Week).Distinct().Count());
        }
        var state = new DynastyState { DynastyId = id, DynastyName = "Modern", UserTeamName = "Washington",
            SeasonYear = 2026, Week = 14, Phase = SeasonPhase.ConferenceChampionship,
            LeagueTeams = teams, Schedule = schedule };
        state = ConferenceChampionshipService.ScheduleChampionships(state, teams.ToDictionary(team => team.Name));
        Assert.Equal(10, state.Schedule.Count(game => game.GameType == ScheduledGameType.ConferenceChampionship));
        var sunBeltFinal = Assert.Single(state.Schedule, game => game.GameType == ScheduledGameType.ConferenceChampionship &&
            teams.Single(team => team.Name == game.HomeTeamName).ConferenceName == "Sun Belt");
        Assert.NotEqual(teams.Single(team => team.Name == sunBeltFinal.HomeTeamName).DivisionName,
            teams.Single(team => team.Name == sunBeltFinal.AwayTeamName).DivisionName);
    }

    [Fact]
    public async Task CareerHistoryKeepsStatsAndOriginalStarsAcrossTransferGraduationAndSave()
    {
        var teams = ModernFbsUniverseFactory.Create(Legacy());
        var player = new DynastyPlayer { PlayerId = Guid.NewGuid(), FullName = "Career QB", TeamName = "Washington",
            Position = Position.QB, ClassYear = 3, OriginalRecruitingStars = 4, OverallRating = 80 };
        var lineman = player with { PlayerId = Guid.NewGuid(), FullName = "Career Lineman", Position = Position.OL,
            OriginalRecruitingStars = 3 };
        var state = new DynastyState { DynastyName = "Career", UserTeamName = "Washington", SeasonYear = 2026,
            LeagueName = "Modern FBS (2026)", LeagueTeams = teams, ActiveRoster = [player, lineman],
            Schedule = [Game(2026, "Washington", "Oregon")] };
        var lookup = teams.ToDictionary(team => team.Name);
        state = DynastyHistoryService.FinalizeSeason(state, lookup);
        state = state with { SeasonYear = 2027, ActiveRoster = [player with { TeamName = "Oregon", ClassYear = 4,
            OverallRating = 95, TalentLevel = 10 }], Schedule = [Game(2027, "Oregon", "Washington")] };
        state = DynastyHistoryService.FinalizeSeason(state, lookup);
        state = state with { SeasonYear = 2028, ActiveRoster = [] };
        var path = Path.Combine(Path.GetTempPath(), $"modern-career-{Guid.NewGuid():N}.db");
        try
        {
            await using var repository = new SqliteDynastySaveRepository(path);
            var save = await repository.SaveAsync(state, SaveKind.Manual);
            var loaded = await repository.LoadAsync(save);
            Assert.NotNull(loaded);
            Assert.Equal(138, loaded.LeagueTeams.Count);
            Assert.Equal("Big Ten", loaded.LeagueTeams.Single(team => team.Name == "Washington").ConferenceName);
            var stats = loaded.PlayerSeasonStats.Where(line => line.PlayerId == player.PlayerId).ToArray();
            Assert.Equal(2, stats.Length);
            Assert.Equal(new[] { "Washington", "Oregon" }, stats.Select(line => line.TeamName));
            Assert.All(stats, line => Assert.Equal(4, line.OriginalRecruitingStars));
            Assert.Equal(600, stats.Sum(line => line.PassYards));
            var career = Assert.Single(loaded.PlayerCareerHistory, item => item.PlayerId == player.PlayerId);
            Assert.Equal(3, Assert.Single(loaded.PlayerCareerHistory, item => item.PlayerId == lineman.PlayerId).OriginalRecruitingStars);
            Assert.Equal(4, career.OriginalRecruitingStars);
            Assert.Equal(2026, career.FirstRecordedSeason);
            Assert.Equal(2027, career.LastRecordedSeason);
        }
        finally { foreach (var suffix in new[] { "", "-wal", "-shm" }) if (File.Exists(path + suffix)) File.Delete(path + suffix); }
    }

    [Fact]
    public void CurrentSeasonStatsAreAvailableBeforeSeasonFinalization()
    {
        var qb = new DynastyPlayer { PlayerId = Guid.NewGuid(), FullName = "Live QB", TeamName = "Washington",
            Position = Position.QB, ClassYear = 2, OriginalRecruitingStars = 4, OverallRating = 82, DepthChartOrder = 1 };
        var rb = qb with { PlayerId = Guid.NewGuid(), FullName = "Live RB", Position = Position.RB, DepthChartOrder = 1 };
        var state = new DynastyState { DynastyName = "Current stats", UserTeamName = "Washington", SeasonYear = 2026,
            ActiveRoster = [qb, rb], Schedule = [Game(2026, "Washington", "Oregon")] };

        var stats = DynastyHistoryService.BuildCurrentPlayerStats(state);

        var quarterback = Assert.Single(stats, line => line.PlayerId == qb.PlayerId);
        Assert.Equal(300, quarterback.PassYards);
        Assert.Equal(1, quarterback.Games);
        Assert.Contains(stats, line => line.PlayerId == rb.PlayerId && line.RushYards > 0);
        Assert.Empty(state.PlayerSeasonStats);
    }

    private static ScheduledGame Game(int year, string home, string away) => new()
    {
        GameId = $"{year}-game", SeasonYear = year, Week = 1, HomeTeamName = home, AwayTeamName = away,
        HasPlayed = true, HomeScore = 28, AwayScore = 14,
        HomeStats = new GameTeamStats { PassYards = 300, PassAttempts = 30, RushYards = 100, RushAttempts = 20 },
        AwayStats = new GameTeamStats { PassYards = 200, PassAttempts = 25, RushYards = 80, RushAttempts = 20 }
    };
}
