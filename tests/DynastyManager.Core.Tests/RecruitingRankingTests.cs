using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public sealed class RecruitingRankingTests
{
    [Fact]
    public void HighSchoolRatingsStayWithinStarBands()
    {
        var recruits = new[]
        {
            Recruit("Five", 5, 86, 96),
            Recruit("Four", 4, 80, 92),
            Recruit("Three", 3, 74, 88),
            Recruit("Two", 2, 66, 80),
            Recruit("One", 1, 58, 72)
        };

        var ratings = recruits
            .Select(RecruitingRankingService.GetHighSchoolRating)
            .ToArray();

        Assert.InRange(ratings[0], 98.0, 100.0);
        Assert.InRange(ratings[1], 90.0, 97.99);
        Assert.InRange(ratings[2], 80.0, 89.99);
        Assert.InRange(ratings[3], 72.0, 79.99);
        Assert.InRange(ratings[4], 70.0, 71.99);
    }

    [Fact]
    public void GaussianClassScoreRewardsQualityOverRawClassSize()
    {
        var sigma = 8.0;

        var strongTwentyFive =
            Enumerable.Repeat(92.0, 25);
        var weakerThirtyEight =
            Enumerable.Repeat(82.0, 38);

        var strongScore =
            RecruitingRankingService.CalculateTeamScore(
                strongTwentyFive,
                sigma);
        var weakScore =
            RecruitingRankingService.CalculateTeamScore(
                weakerThirtyEight,
                sigma);

        Assert.True(
            strongScore > weakScore,
            $"Expected quality class to win: {strongScore} vs {weakScore}");
    }

    [Fact]
    public void BestRecruitReceivesFull247StyleWeight()
    {
        var score =
            RecruitingRankingService.CalculateTeamScore(
                new[] { 95.0 },
                8.0);

        Assert.Equal(25.0, score);
    }

    [Fact]
    public void BreakoutSeasonCreatesImmediateRecruitingMomentum()
    {
        var team = Team("User", 65);
        var state = new DynastyState
        {
            DynastyName = "Momentum",
            UserTeamName = team.Name,
            SeasonYear = 2026,
            TeamSeasonHistory = new[]
            {
                new TeamSeasonHistoryRecord
                {
                    SeasonYear = 2026,
                    TeamName = team.Name,
                    Wins = 12,
                    Losses = 3,
                    ConferenceWins = 7,
                    ConferenceLosses = 1,
                    FinalRanking = 6
                }
            }
        };

        var bonus =
            RecruitPreferenceService.GetRecentSuccessBonus(
                state,
                team);

        Assert.True(
            bonus >= 8,
            $"Expected strong momentum after 12-3/#6, got {bonus}.");
    }

    [Fact]
    public void TeamRankingUsesGaussianScoreInsteadOfCommitCount()
    {
        var strongTeam = Team("Strong", 80);
        var volumeTeam = Team("Volume", 80);

        var strongCommits = Enumerable.Range(1, 25)
            .Select(index => Commitment(
                strongTeam.Name,
                $"Strong {index}",
                92))
            .ToArray();

        var volumeCommits = Enumerable.Range(1, 38)
            .Select(index => Commitment(
                volumeTeam.Name,
                $"Volume {index}",
                68))
            .ToArray();

        var state = new DynastyState
        {
            DynastyName = "Rankings",
            UserTeamName = strongTeam.Name,
            SeasonYear = 2026,
            RecruitingCommitments =
                strongCommits
                    .Concat(volumeCommits)
                    .ToArray()
        };

        var rankings =
            RecruitingRankingService.BuildTeamRankings(
                state,
                new[]
                {
                    strongTeam.Name,
                    volumeTeam.Name
                });

        Assert.Equal(
            strongTeam.Name,
            rankings[0].TeamName);
        Assert.True(
            rankings[0].TeamScore >
            rankings[1].TeamScore);
    }

    private static HighSchoolRecruit Recruit(
        string name,
        int stars,
        int overall,
        int potential) => new()
    {
        RecruitId = Guid.NewGuid(),
        SeasonYear = 2026,
        FullName = name,
        Position = Position.WR,
        StarRating = stars,
        TrueOverallRating = overall,
        PotentialRating = potential,
        HomeState = 1,
        HomeRegion = 1
    };

    private static Team Team(
        string name,
        int prestige) => new()
    {
        Name = name,
        Abbreviation =
            name[..Math.Min(3, name.Length)]
                .ToUpperInvariant(),
        ConferenceName = "Test",
        Prestige = prestige,
        LegacyRegionId = 1
    };

    private static RecruitingCommitmentRecord Commitment(
        string teamName,
        string playerName,
        int overall) => new()
    {
        SeasonYear = 2026,
        JoinSeasonYear = 2027,
        CommittedWeek = 24,
        ProspectId = Guid.NewGuid(),
        Source = RecruitingSource.HighSchool,
        PlayerName = playerName,
        TeamName = teamName,
        Position = Position.WR,
        OverallRating = overall,
        WasCpuAssisted = true
    };
}
