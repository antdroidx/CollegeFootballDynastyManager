using System.Text.Json;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Presentation;
using Xunit;

namespace DynastyManager.Core.Tests;

public sealed class NewsAndTableOrderingTests
{
    [Fact]
    public void NumericHeadersSortValuesRatherThanFormattedStringsAndKeepMissingLast()
    {
        var values = new[] { "1,200", "—", "80", "9", "-2", "1.5" };
        Assert.Equal(new[] { "-2", "1.5", "9", "80", "1,200", "—" }, TableOrdering.Sort(values, v => v, false));
        Assert.Equal(new[] { "1,200", "80", "9", "1.5", "-2", "—" }, TableOrdering.Sort(values, v => v, true));
        Assert.Equal(new[] { "#2", "#10", "#100" }, TableOrdering.Sort(new[] { "#100", "#2", "#10" }, v => v, false));
        Assert.Equal(new[] { "2★", "5★" }, TableOrdering.Sort(new[] { "5★", "2★" }, v => v, false));
    }

    [Fact]
    public void SortingAllRowsBeforePagingPreservesGlobalOrderAndStableTies()
    {
        var rows = Enumerable.Range(0, 73).Select(i => (Id: i, Value: (i % 10).ToString())).ToArray();
        var sorted = TableOrdering.Sort(rows, row => row.Value, true);
        Assert.Equal(9, int.Parse(sorted[0].Value));
        Assert.Equal(new[] { 9, 19, 29, 39, 49, 59, 69 }, sorted.Take(7).Select(row => row.Id));
        Assert.Equal(73, sorted.Take(50).Concat(sorted.Skip(50)).Select(row => row.Id).Distinct().Count());
        Assert.Equal(new[] { "alpha", "Beta", "gamma" }, TableOrdering.Sort(new[] { "gamma", "Beta", "alpha" }, v => v, false));
    }

    [Fact]
    public void NewsUsesRecordedOutcomesAndSurvivesReloadWithoutMutatingTheSave()
    {
        var game = new ScheduledGame { GameId = "result", SeasonYear = 2026, Week = 2, HomeTeamName = "Washington",
            AwayTeamName = "Oregon", HasPlayed = true, HomeScore = 21, AwayScore = 24, IsNeutralSite = true };
        var state = State() with { Schedule = [game] };
        var before = JsonSerializer.Serialize(state);
        var stories = DynastyNewsService.Build(state);
        var result = Assert.Single(stories);
        Assert.Equal("Oregon edges Washington, 24–21", result.Headline);
        Assert.Contains("neutral site", result.Body);
        Assert.DoesNotContain("hosted", result.Body);
        Assert.Equal(before, JsonSerializer.Serialize(state));
        Assert.Equal(stories, DynastyNewsService.Build(JsonSerializer.Deserialize<DynastyState>(before)!));
    }

    [Fact]
    public void UpcomingGamesProducePreviewsRatherThanInventedScoresAndTiesHaveNoWinner()
    {
        var upcoming = new ScheduledGame { GameId = "next", SeasonYear = 2026, Week = 3, HomeTeamName = "Washington", AwayTeamName = "Oregon" };
        var story = Assert.Single(DynastyNewsService.Build(State() with { Schedule = [upcoming] }));
        Assert.Equal("Preview", story.Category);
        Assert.DoesNotContain("defeated", story.Body);
        var tied = upcoming with { HasPlayed = true, HomeScore = 14, AwayScore = 14 };
        var tie = Assert.Single(DynastyNewsService.Build(State() with { Schedule = [tied] }));
        Assert.Contains("finish level", tie.Headline);
        Assert.DoesNotContain("defeated", tie.Body);
    }

    [Fact]
    public void CommitmentsDescribeTheRecordedDestinationAndJoinYearWithoutInventingStars()
    {
        var state = State() with { RecruitingCommitments =
        [
            new RecruitingCommitmentRecord { ProspectId = Guid.Parse("11111111-1111-1111-1111-111111111111"), PlayerName = "Alex Smith",
                TeamName = "Washington", Position = Position.QB, SeasonYear = 2026, JoinSeasonYear = 2027,
                CommittedWeek = 22, Source = RecruitingSource.HighSchool }
        ] };
        var story = Assert.Single(DynastyNewsService.Build(state));
        Assert.Contains("Alex Smith commits to Washington", story.Headline);
        Assert.Contains("2027", story.Body);
        Assert.DoesNotContain("star", story.Body);
        Assert.Empty(DynastyNewsService.Build(state, 0));
    }

    [Fact]
    public void InjuryStoriesUseTheCurrentReportAndDisappearWhenThePlayerRecovers()
    {
        var player = new DynastyPlayer { PlayerId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            FullName = "Alex Jones", TeamName = "Washington", Position = Position.RB,
            CurrentInjury = new PlayerInjury { Severity = InjurySeverity.Moderate, BodyArea = InjuryBodyArea.Ankle, WeeksRemaining = 3 } };
        var state = State() with { ActiveRoster = [player] };
        var story = Assert.Single(DynastyNewsService.Build(state));
        Assert.Equal(player.PlayerId, story.PlayerId);
        Assert.Contains("Ankle", story.Body);
        Assert.Contains("3 week(s)", story.Body);
        Assert.Empty(DynastyNewsService.Build(state with { ActiveRoster = [player with { CurrentInjury = null }] }));
    }

    [Fact]
    public void OverviewHeadlinesPreferNotableResultsAndLimitInjuryCrowding()
    {
        var injuries = Enumerable.Range(1, 5).Select(index => new DynastyPlayer
        {
            PlayerId = Guid.Parse($"00000000-0000-0000-0000-{index:000000000000}"), FullName = $"Injured {index}",
            TeamName = "Washington", Position = Position.RB,
            CurrentInjury = new PlayerInjury { Severity = InjurySeverity.Minor, BodyArea = InjuryBodyArea.Ankle, WeeksRemaining = 1 }
        }).ToArray();
        var upset = new ScheduledGame { GameId = "upset-overview", SeasonYear = 2026, Week = 2,
            HomeTeamName = "Washington", AwayTeamName = "Oregon", HasPlayed = true, HomeScore = 28, AwayScore = 21,
            HomePregameRating = 70, AwayPregameRating = 90 };
        var stories = DynastyNewsService.BuildOverview(State() with { ActiveRoster = injuries, Schedule = [upset] });

        Assert.Contains(stories, story => story.Category == "Upsets");
        Assert.True(stories.Count(story => story.Category == "Injuries") <= 1);
        Assert.Equal(stories, DynastyNewsService.BuildOverview(State() with { ActiveRoster = injuries, Schedule = [upset] }));
    }

    private static DynastyState State() => new() { DynastyId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        DynastyName = "Test career", UserTeamName = "Washington", SeasonYear = 2026, Week = 2 };
}
