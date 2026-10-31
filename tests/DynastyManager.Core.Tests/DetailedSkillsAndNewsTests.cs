using System.Text.Json;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Persistence;
using Xunit;

namespace DynastyManager.Core.Tests;

public sealed class DetailedSkillsAndNewsTests
{
    private static DynastyState State() => new() { DynastyName = "Skills", UserTeamName = "A", SeasonYear = 2026, Week = 6 };
    private static DynastyPlayer Player(Position position = Position.QB) => new() { PlayerId = Guid.Parse("99999999-1111-2222-3333-444444444444"),
        FullName = "Test Player", TeamName = "A", Position = position, OverallRating = 75, PotentialRating = 90,
        SpeedRating = 75, StrengthRating = 75, AgilityRating = 75, AwarenessRating = 75, InjuryRating = 75 };
    private static ScheduledGame Game(string id, int week = 6, int home = 24, int away = 21) => new() {
        GameId = id, SeasonYear = 2026, Week = week, HomeTeamName = "A", AwayTeamName = "B", HasPlayed = true, HomeScore = home, AwayScore = away };

    [Fact]
    public void GeneratedSkillsDetermineOverallAndRemainStableAcrossYearsAndTransfers()
    {
        var state = State(); var original = Player();
        var player = PlayerRatingService.EnsureInitialized(state, original);
        Assert.Equal(PlayerRatingService.CalculateOverall(player), player.OverallRating);
        Assert.Equal(original.SpeedRating, player.SpeedRating);
        Assert.Equal(player, PlayerRatingService.EnsureInitialized(state with { SeasonYear = 2030 }, player));
        Assert.Equal(player.Skills, PlayerSkillService.EnsureInitialized(original with { TeamName = "Transferred" }).Skills);
        Assert.True(player.Skills!.PassingAccuracy > PlayerSkillService.EnsureInitialized(Player(Position.OL)).Skills!.PassingAccuracy);
        foreach (var property in typeof(PlayerSkillRatings).GetProperties()) Assert.InRange((int)property.GetValue(player.Skills)!, 1, 99);
        var partial = player with { Skills = player.Skills with { PassingAccuracy = 91, Catching = 0 } };
        var repaired = PlayerSkillService.EnsureInitialized(partial);
        Assert.Equal(91, repaired.Skills!.PassingAccuracy); Assert.True(repaired.Skills.Catching > 0);
    }

    [Fact]
    public async Task SkillsPersistAcrossSqliteReloadAndTransfer()
    {
        var path = Path.Combine(Path.GetTempPath(), $"skills-{Guid.NewGuid():N}.db");
        try {
            await using var repository = new SqliteDynastySaveRepository(path);
            var player = PlayerRatingService.EnsureInitialized(State(), Player());
            var id = await repository.SaveAsync(State() with { ActiveRoster = [player] }, SaveKind.Manual);
            var loaded = (await repository.LoadAsync(id))!;
            Assert.Equal(player, Assert.Single(loaded.ActiveRoster));
            Assert.Equal(DynastyState.CurrentSchemaVersion, loaded.SchemaVersion);
            Assert.Equal(player.Skills, PlayerSkillService.EnsureInitialized(player with { TeamName = "B" }).Skills);
        } finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void InjuryAttributeChangesCarryToRelatedSkills()
    {
        var player = PlayerSkillService.EnsureInitialized(Player());
        var updated = PlayerSkillService.ApplyInjuryRegression(player, player with { AwarenessRating = 80, SpeedRating = 78, StrengthRating = 77 });
        Assert.Equal(player.Skills!.PassingAccuracy + 5, updated!.PassingAccuracy);
        Assert.Equal(player.Skills.Acceleration + 3, updated.Acceleration);
        Assert.Equal(player.Skills.PassingPower + 2, updated.PassingPower);
        Assert.Equal(player.Skills.Discipline, updated.Discipline);
        Assert.Equal(player.Skills.Leadership, updated.Leadership);
    }

    [Fact]
    public void PassingAndBlockingSkillsAffectTheirRelevantSimulationProfiles()
    {
        var qb = PlayerSkillService.EnsureInitialized(Player());
        Assert.True(PlayerSkillService.GameRating(qb with { Skills = qb.Skills! with { PassingAccuracy = 99 } }) >
            PlayerSkillService.GameRating(qb with { Skills = qb.Skills! with { PassingAccuracy = 20 } }));
        var ol = PlayerSkillService.EnsureInitialized(Player(Position.OL));
        var good = ol with { Skills = ol.Skills! with { PassBlocking = 99, RunBlocking = 20 } };
        var bad = good with { Skills = good.Skills! with { PassBlocking = 20 } };
        Team[] teams = [new() { Name = "A", Abbreviation = "A", ConferenceName = "Test", Prestige = 60 }];
        var high = DynastyRosterSimulationProfileBuilder.Build(State() with { ActiveRoster = [good] }, teams)["A"];
        var low = DynastyRosterSimulationProfileBuilder.Build(State() with { ActiveRoster = [bad] }, teams)["A"];
        Assert.True(high.PassOffenseRating > low.PassOffenseRating);
        Assert.Equal(high.RushOffenseRating, low.RushOffenseRating);
    }

    [Fact]
    public void UpsetsUseRecordedPregameStrengthAndDoNotChangeWhenCurrentRosterChanges()
    {
        var state = State() with { Schedule = [Game("upset") with { HomePregameRating = 65, AwayPregameRating = 85 }] };
        var story = Assert.Single(DynastyNewsService.Build(state));
        Assert.Equal("Upsets", story.Category); Assert.Contains("A", story.Headline); Assert.Contains("B", story.Headline); Assert.Contains("65.0", story.Body);
        Assert.Equal(story, Assert.Single(DynastyNewsService.Build(state with { ActiveRoster = [Player() with { OverallRating = 99 }] })));
        Assert.Equal(story, Assert.Single(DynastyNewsService.Build(JsonSerializer.Deserialize<DynastyState>(JsonSerializer.Serialize(state))!)));
        Assert.Equal("Results", Assert.Single(DynastyNewsService.Build(State() with { Schedule = [Game("ordinary")] })).Category);
    }

    [Fact]
    public void StreaksStopAtLossesAndTiesAndPostseasonTitlesAreSpecific()
    {
        var wins = Enumerable.Range(1, 5).Select(i => Game("win" + i, i)).ToArray();
        var story = DynastyNewsService.Build(State() with { Schedule = wins }).First();
        Assert.Equal("Streaks", story.Category); Assert.Contains("5 straight", story.Headline);
        Assert.DoesNotContain(DynastyNewsService.Build(State() with { Schedule = wins.Select(g => g.Week == 3 ? g with { HomeScore = 14, AwayScore = 14 } : g).ToArray() }), s => s.Category == "Streaks");
        var title = Assert.Single(DynastyNewsService.Build(State() with { Schedule = [Game("title") with {
            GameType = ScheduledGameType.CollegeFootballPlayoff, PostseasonRound = PostseasonRound.NationalChampionship }] }));
        Assert.Contains("national championship", title.Headline);
        Assert.Equal("Shutouts", Assert.Single(DynastyNewsService.Build(State() with { Schedule = [Game("zero", home: 42, away: 0)] })).Category);
        Assert.Equal("Blowouts", Assert.Single(DynastyNewsService.Build(State() with { Schedule = [Game("big", home: 49, away: 7)] })).Category);
    }
    [Fact]
    public void LegacyUpsetsUseOnlyEarlierSeasonResultsAndFutureGamesDoNotChangeTheStory()
    {
        var prior = Enumerable.Range(1, 4).Select(i => Game("b" + i, i, 7, 28) with { HomeTeamName = "Other" + i }).ToArray();
        var losses = Enumerable.Range(1, 4).Select(i => Game("a" + i, i, 7, 28) with { AwayTeamName = "Opponent" + i }).ToArray();
        var result = Game("legacy");
        var state = State() with { Schedule = prior.Concat(losses).Append(result).ToArray() };
        var story = DynastyNewsService.Build(state).Single(s => s.Id == "game/legacy");
        Assert.Equal("Upsets", story.Category);
        Assert.Contains("ends B's 4-game winning streak", story.Body);
        Assert.Equal(story, DynastyNewsService.Build(state with { Schedule = state.Schedule.Append(Game("future", 10)).ToArray() }).Single(s => s.Id == story.Id));
    }

    [Fact]
    public void SimulationStoresTheActualPregameProfilesForLaterNews()
    {
        var home = new Team { Name = "A", Abbreviation = "A", ConferenceName = "Test", Prestige = 50 };
        var away = home with { Name = "B", Abbreviation = "B" };
        var played = DeterministicGameSimulator.Simulate(Game("snapshot") with { HasPlayed = false, HomeScore = null, AwayScore = null }, home, away,
            new TeamSimulationProfile { TeamName = "A", PassOffenseRating = 60, RushOffenseRating = 60, PassDefenseRating = 70, RushDefenseRating = 70, SpecialTeamsRating = 65 },
            new TeamSimulationProfile { TeamName = "B", PassOffenseRating = 80, RushOffenseRating = 80, PassDefenseRating = 90, RushDefenseRating = 90, SpecialTeamsRating = 85 });
        Assert.Equal(65d, played.HomePregameRating);
        Assert.Equal(85d, played.AwayPregameRating);
        Assert.Equal(played, DeterministicGameSimulator.Simulate(played, home, away));
    }

    [Theory]
    [InlineData(Position.QB)]
    [InlineData(Position.OL)]
    [InlineData(Position.CB)]
    [InlineData(Position.K)]
    [InlineData(Position.P)]
    public void RelevantSkillsDriveOverallWithoutDependingOnStoredOverall(Position position)
    {
        var player = PlayerRatingService.EnsureInitialized(State(), Player(position));
        var skills = player.Skills!;
        PlayerSkillRatings Set(int value) => position switch
        {
            Position.QB => skills with { PassingAccuracy = value },
            Position.OL => skills with { PassBlocking = value },
            Position.CB => skills with { Tackling = value },
            _ => skills with { KickAccuracy = value }
        };
        Assert.True(PlayerRatingService.CalculateOverall(player with { Skills = Set(95) }) >
            PlayerRatingService.CalculateOverall(player with { Skills = Set(30) }));
        Assert.Equal(PlayerRatingService.CalculateOverall(player with { OverallRating = 1 }),
            PlayerRatingService.CalculateOverall(player with { OverallRating = 99 }));
        Assert.Equal(PlayerSkillService.GameRating(player with { OverallRating = 1 }),
            PlayerSkillService.GameRating(player with { OverallRating = 99 }));
    }

    [Fact]
    public void PracticeIsIndependentRepeatableAndPositionFocused()
    {
        var qb = PlayerRatingService.EnsureInitialized(State(), Player());
        var trained = PlayerSkillService.Train(State() with { DynastyId = Guid.Empty }, qb, 3, false);
        Assert.Equal(trained, PlayerSkillService.Train(State() with { DynastyId = Guid.Empty }, qb, 3, false));
        var deltas = typeof(PlayerSkillRatings).GetProperties().Select(property =>
            (int)property.GetValue(trained)! - (int)property.GetValue(qb.Skills)!).ToArray();
        Assert.True(deltas.Distinct().Count() > 1);
        Assert.True(trained.PassingAccuracy > qb.Skills!.PassingAccuracy);
        Assert.InRange(trained.Tackling - qb.Skills.Tackling, 0, 2);
        var midseason = PlayerSkillService.Train(State(), qb, 1, true);
        Assert.Equal(qb.Skills.Leadership, midseason.Leadership);
        Assert.Equal(qb.Skills.Discipline, midseason.Discipline);
    }

}
