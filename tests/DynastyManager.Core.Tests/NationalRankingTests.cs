using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class NationalRankingTests
{
    [Fact]
    public void PreseasonRankingUsesPrestigeAndRosterStrength()
    {
        var strong = Team("Strong", 90);
        var average = Team("Average", 70);
        var teams = new[] { strong, average };
        var lookup = teams.ToDictionary(
            team => team.Name,
            StringComparer.OrdinalIgnoreCase);

        var profiles = new Dictionary<string, TeamSimulationProfile>(
            StringComparer.OrdinalIgnoreCase)
        {
            [strong.Name] = Profile(strong.Name, 90),
            [average.Name] = Profile(average.Name, 70)
        };

        var state = State(Array.Empty<ScheduledGame>());

        var rankings = NationalRankingService.Build(
            state,
            lookup,
            profiles);

        Assert.Equal("Strong", rankings[0].TeamName);
        Assert.Equal(1, rankings[0].Rank);
        Assert.Equal(2, rankings[1].Rank);
    }

    [Fact]
    public void EqualTeamsRewardTheHeadToHeadWinner()
    {
        var first = Team("First", 75);
        var second = Team("Second", 75);
        var teams = new[] { first, second };
        var lookup = teams.ToDictionary(
            team => team.Name,
            StringComparer.OrdinalIgnoreCase);

        var game = new ScheduledGame
        {
            GameId = "ranking-result",
            SeasonYear = 2026,
            Week = 1,
            HomeTeamName = first.Name,
            AwayTeamName = second.Name,
            GameType = ScheduledGameType.NonConference,
            SimulationSeed = 1,
            HasPlayed = true,
            HomeScore = 17,
            AwayScore = 24,
            HomeStats = Stats(350),
            AwayStats = Stats(410)
        };

        var rankings = NationalRankingService.Build(
            State(new[] { game }),
            lookup);

        var firstRank = rankings.Single(
            ranking => ranking.TeamName == first.Name).Rank;
        var secondRank = rankings.Single(
            ranking => ranking.TeamName == second.Name).Rank;

        Assert.True(secondRank < firstRank);
    }

    [Fact]
    public void ConferenceChampionReceivesLegacyPollBonus()
    {
        var first = Team("First", 75);
        var second = Team("Second", 75);
        var lookup = new[] { first, second }.ToDictionary(
            team => team.Name,
            StringComparer.OrdinalIgnoreCase);

        var state = State(Array.Empty<ScheduledGame>()) with
        {
            ConferenceChampionshipHistory = new[]
            {
                new ConferenceChampionRecord
                {
                    SeasonYear = 2026,
                    ConferenceName = "Test",
                    ChampionTeamName = second.Name,
                    RunnerUpTeamName = first.Name,
                    ChampionScore = 27,
                    RunnerUpScore = 24
                }
            }
        };

        var rankings = NationalRankingService.Build(state, lookup);

        Assert.Equal(second.Name, rankings[0].TeamName);
    }

    [Fact]
    public void FinalPollPlacesNationalChampionFirstAndRunnerUpSecond()
    {
        var champion = Team("Champion", 70);
        var runnerUp = Team("Runner Up", 95);
        var other = Team("Other", 90);

        var lookup = new[] { champion, runnerUp, other }.ToDictionary(
            team => team.Name,
            StringComparer.OrdinalIgnoreCase);

        var state = State(Array.Empty<ScheduledGame>()) with
        {
            NationalChampionshipHistory = new[]
            {
                new NationalChampionRecord
                {
                    SeasonYear = 2026,
                    ChampionTeamName = champion.Name,
                    RunnerUpTeamName = runnerUp.Name,
                    ChampionScore = 24,
                    RunnerUpScore = 21
                }
            }
        };

        var rankings = NationalRankingService.Build(state, lookup);

        Assert.Equal(champion.Name, rankings[0].TeamName);
        Assert.Equal(1, rankings[0].Rank);
        Assert.Equal(runnerUp.Name, rankings[1].TeamName);
        Assert.Equal(2, rankings[1].Rank);
        Assert.Equal(other.Name, rankings[2].TeamName);
        Assert.Equal(3, rankings[2].Rank);
    }

    private static DynastyState State(
        IReadOnlyList<ScheduledGame> schedule) =>
        new()
        {
            DynastyName = "Ranking Test",
            UserTeamName = "First",
            SeasonYear = 2026,
            Week = 1,
            Phase = SeasonPhase.RegularSeason,
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

    private static TeamSimulationProfile Profile(
        string teamName,
        double rating) =>
        new()
        {
            TeamName = teamName,
            PassOffenseRating = rating,
            RushOffenseRating = rating,
            PassDefenseRating = rating,
            RushDefenseRating = rating,
            SpecialTeamsRating = rating,
            RosterSize = 85
        };

    private static GameTeamStats Stats(int totalYards) =>
        new()
        {
            Possessions = 12,
            PassAttempts = 30,
            RushAttempts = 35,
            PassYards = totalYards / 2,
            RushYards = totalYards - totalYards / 2,
            Turnovers = 1
        };
}
