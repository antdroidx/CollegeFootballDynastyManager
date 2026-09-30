using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class CollegeFootballPlayoffTests
{
    [Fact]
    public void FieldUsesFiveHighestRankedConferenceChampionsAndSevenAtLarge()
    {
        var rankings = Enumerable.Range(1, 20)
            .Select(rank => new NationalRanking
            {
                Rank = rank,
                TeamName = $"Team {rank}",
                ConferenceName = $"Conference {rank}",
                Wins = 12 - rank / 5,
                Losses = rank / 5,
                Score = 1000 - rank
            })
            .ToArray();

        var champions = new[] { 1, 3, 8, 15, 18, 20 }
            .Select(rank => new ConferenceChampionRecord
            {
                SeasonYear = 2026,
                ConferenceName = $"Conference {rank}",
                ChampionTeamName = $"Team {rank}",
                RunnerUpTeamName = $"Runner {rank}",
                ChampionScore = 30,
                RunnerUpScore = 20
            })
            .ToArray();

        var field = CollegeFootballPlayoffService.SelectField(
            2026,
            rankings,
            champions);

        Assert.Equal(12, field.Count);
        Assert.Equal(5, field.Count(team => team.IsAutomaticBid));
        Assert.Contains(field, team => team.TeamName == "Team 18");
        Assert.DoesNotContain(field, team => team.TeamName == "Team 20");
        Assert.Equal(
            Enumerable.Range(1, 12),
            field.Select(team => team.Seed));
    }

    [Fact]
    public void InitialBracketGivesTopFourByesAndPairsFiveThroughTwelve()
    {
        var teams = Enumerable.Range(1, 12)
            .Select(index => Team($"Team {index}"))
            .ToArray();

        var lookup = teams.ToDictionary(
            team => team.Name,
            StringComparer.OrdinalIgnoreCase);

        var state = State() with
        {
            ConferenceChampionshipHistory = Enumerable.Range(1, 5)
                .Select(index => new ConferenceChampionRecord
                {
                    SeasonYear = 2026,
                    ConferenceName = $"Conference {index}",
                    ChampionTeamName = $"Team {index}",
                    RunnerUpTeamName = $"Runner {index}",
                    ChampionScore = 30,
                    RunnerUpScore = 20
                })
                .ToArray()
        };

        state = CollegeFootballPlayoffService.InitializePlayoff(
            state,
            lookup);

        var field = state.CollegeFootballPlayoffHistory
            .Where(record => record.SeasonYear == 2026)
            .OrderBy(record => record.Seed)
            .ToArray();

        Assert.Equal(12, field.Length);

        var games = state.Schedule
            .Where(game =>
                game.GameType == ScheduledGameType.CollegeFootballPlayoff &&
                game.PostseasonRound == PostseasonRound.FirstRound)
            .OrderBy(game => game.PlayoffBracketSlot)
            .ToArray();

        Assert.Equal(4, games.Length);
        Assert.Equal(5, games[0].HomeSeed);
        Assert.Equal(12, games[0].AwaySeed);
        Assert.Equal(6, games[1].HomeSeed);
        Assert.Equal(11, games[1].AwaySeed);
        Assert.Equal(7, games[2].HomeSeed);
        Assert.Equal(10, games[2].AwaySeed);
        Assert.Equal(8, games[3].HomeSeed);
        Assert.Equal(9, games[3].AwaySeed);
        Assert.All(games, game => Assert.False(game.IsNeutralSite));
    }

    [Fact]
    public void PlayedFirstRoundSchedulesNeutralQuarterfinals()
    {
        var field = Enumerable.Range(1, 12)
            .Select(seed => new CollegeFootballPlayoffSeedRecord
            {
                SeasonYear = 2026,
                Seed = seed,
                NationalRank = seed,
                TeamName = $"Team {seed}",
                ConferenceName = $"Conference {seed}"
            })
            .ToArray();

        var state = State() with
        {
            CollegeFootballPlayoffHistory = field,
            Schedule = new[]
            {
                PlayedFirstRound(1, 5, 12, 35, 14),
                PlayedFirstRound(2, 6, 11, 28, 17),
                PlayedFirstRound(3, 7, 10, 24, 21),
                PlayedFirstRound(4, 8, 9, 20, 27)
            }
        };

        state = CollegeFootballPlayoffService.ScheduleNextRound(state);

        var quarters = state.Schedule
            .Where(game =>
                game.PostseasonRound == PostseasonRound.Quarterfinal)
            .OrderBy(game => game.PlayoffBracketSlot)
            .ToArray();

        Assert.Equal(4, quarters.Length);
        Assert.Equal("Team 1", quarters[0].HomeTeamName);
        Assert.Equal("Team 9", quarters[0].AwayTeamName);
        Assert.Equal("Team 4", quarters[1].HomeTeamName);
        Assert.Equal("Team 5", quarters[1].AwayTeamName);
        Assert.All(quarters, game => Assert.True(game.IsNeutralSite));
    }

    private static DynastyState State() =>
        new()
        {
            DynastyId =
                Guid.Parse("12345678-1234-1234-1234-123456789abc"),
            DynastyName = "Playoff Test",
            UserTeamName = "Team 1",
            SeasonYear = 2026,
            Week = CollegeFootballPlayoffService.FirstRoundWeek,
            Phase = SeasonPhase.Postseason,
            Schedule = Array.Empty<ScheduledGame>()
        };

    private static Team Team(string name) =>
        new()
        {
            Name = name,
            Abbreviation = name,
            ConferenceName = name,
            Prestige = 90 - int.Parse(name.Split(' ')[1])
        };

    private static ScheduledGame PlayedFirstRound(
        int slot,
        int homeSeed,
        int awaySeed,
        int homeScore,
        int awayScore) =>
        new()
        {
            GameId = $"FR-{slot}",
            SeasonYear = 2026,
            Week = CollegeFootballPlayoffService.FirstRoundWeek,
            HomeTeamName = $"Team {homeSeed}",
            AwayTeamName = $"Team {awaySeed}",
            GameType = ScheduledGameType.CollegeFootballPlayoff,
            PostseasonRound = PostseasonRound.FirstRound,
            PlayoffBracketSlot = slot,
            HomeSeed = homeSeed,
            AwaySeed = awaySeed,
            SimulationSeed = slot,
            HasPlayed = true,
            HomeScore = homeScore,
            AwayScore = awayScore
        };
}
