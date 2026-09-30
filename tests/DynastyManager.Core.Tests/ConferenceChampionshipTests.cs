using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class ConferenceChampionshipTests
{
    [Fact]
    public void TwoTeamConferenceWinsTieUsesHeadToHead()
    {
        var teams = new[]
        {
            Team("A", 80),
            Team("B", 90),
            Team("C", 70),
            Team("D", 70)
        };
        var lookup = teams.ToDictionary(
            team => team.Name,
            StringComparer.OrdinalIgnoreCase);

        var state = State(
            SeasonPhase.RegularSeason,
            13,
            new[]
            {
                PlayedConference("A-B", "A", "B", 24, 17),
                PlayedConference("A-D", "D", "A", 21, 14),
                PlayedConference("B-C", "B", "C", 28, 14)
            });

        var standings = ConferenceStandings.Build(
            state,
            lookup,
            "Test");

        var indexA = standings
            .Select((standing, index) => (standing.TeamName, index))
            .Single(item => item.TeamName == "A")
            .index;
        var indexB = standings
            .Select((standing, index) => (standing.TeamName, index))
            .Single(item => item.TeamName == "B")
            .index;

        Assert.True(indexA < indexB);
        Assert.Equal(1, standings[indexA].ConferenceWins);
        Assert.Equal(1, standings[indexB].ConferenceWins);
    }

    [Fact]
    public void ChampionshipSchedulesTopTwoAndRecordsChampion()
    {
        var teams = Enumerable.Range(1, 8)
            .Select(index => Team(
                $"Team {index}",
                90 - index))
            .ToArray();

        var lookup = teams.ToDictionary(
            team => team.Name,
            StringComparer.OrdinalIgnoreCase);

        var regularSeason = new[]
        {
            PlayedConference("1-3", "Team 1", "Team 3", 31, 10),
            PlayedConference("1-4", "Team 1", "Team 4", 28, 14),
            PlayedConference("1-5", "Team 1", "Team 5", 35, 17),
            PlayedConference("2-3", "Team 2", "Team 3", 27, 20),
            PlayedConference("2-4", "Team 2", "Team 4", 24, 21),
            PlayedConference("3-4", "Team 3", "Team 4", 20, 13)
        };

        var state = State(
            SeasonPhase.ConferenceChampionship,
            14,
            regularSeason);

        state = ConferenceChampionshipService
            .ScheduleChampionships(state, lookup);

        var titleGame = Assert.Single(
            state.Schedule.Where(game =>
                game.GameType ==
                    ScheduledGameType.ConferenceChampionship));

        Assert.Equal("Team 1", titleGame.HomeTeamName);
        Assert.Equal("Team 2", titleGame.AwayTeamName);
        Assert.False(titleGame.HasPlayed);

        state = ConferenceChampionshipService
            .SimulateChampionships(state, lookup);

        titleGame = Assert.Single(
            state.Schedule.Where(game =>
                game.GameType ==
                    ScheduledGameType.ConferenceChampionship));

        Assert.True(titleGame.HasPlayed);

        var champion = Assert.Single(
            state.ConferenceChampionshipHistory);

        Assert.Equal(2026, champion.SeasonYear);
        Assert.Equal("Test", champion.ConferenceName);
        Assert.Contains(
            champion.ChampionTeamName,
            new[] { "Team 1", "Team 2" });
        Assert.NotEqual(
            champion.ChampionScore,
            champion.RunnerUpScore);
    }

    private static DynastyState State(
        SeasonPhase phase,
        int week,
        IReadOnlyList<ScheduledGame> schedule) =>
        new()
        {
            DynastyId =
                Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            DynastyName = "Test Dynasty",
            UserTeamName = "Team 1",
            SeasonYear = 2026,
            Week = week,
            Phase = phase,
            Schedule = schedule
        };

    private static Team Team(string name, int prestige) =>
        new()
        {
            Name = name,
            Abbreviation = name,
            ConferenceName = "Test",
            Prestige = prestige
        };

    private static ScheduledGame PlayedConference(
        string id,
        string home,
        string away,
        int homeScore,
        int awayScore) =>
        new()
        {
            GameId = id,
            SeasonYear = 2026,
            Week = 1,
            HomeTeamName = home,
            AwayTeamName = away,
            GameType = ScheduledGameType.Conference,
            SimulationSeed = id.GetHashCode(),
            HasPlayed = true,
            HomeScore = homeScore,
            AwayScore = awayScore
        };
}
