using DynastyManager.Core.Models;
using DynastyManager.Core.Seasons;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class SeasonProgressionTests
{
    private static DynastyState State(
        SeasonPhase phase,
        int week,
        int year = 2026) =>
        new()
        {
            DynastyName = "Test Dynasty",
            UserTeamName = "Washington",
            SeasonYear = year,
            Week = week,
            Phase = phase
        };

    [Fact]
    public void PreseasonOpensRegularSeasonAtWeekZero()
    {
        var next = SeasonProgression.Advance(
            State(SeasonPhase.Preseason, 0));

        Assert.Equal(SeasonPhase.RegularSeason, next.Phase);
        Assert.Equal(0, next.Week);
        Assert.Equal(2026, next.SeasonYear);
    }

    [Fact]
    public void WeekZeroAdvancesToWeekOne()
    {
        var next = SeasonProgression.Advance(
            State(SeasonPhase.RegularSeason, 0));

        Assert.Equal(SeasonPhase.RegularSeason, next.Phase);
        Assert.Equal(1, next.Week);
    }

    [Fact]
    public void RegularSeasonAdvancesUntilLegacyWeekThirteenBoundary()
    {
        var next = SeasonProgression.Advance(
            State(SeasonPhase.RegularSeason, 12));

        Assert.Equal(SeasonPhase.RegularSeason, next.Phase);
        Assert.Equal(13, next.Week);
    }

    [Fact]
    public void WeekThirteenAdvancesToConferenceChampionship()
    {
        var next = SeasonProgression.Advance(
            State(SeasonPhase.RegularSeason, 13));

        Assert.Equal(SeasonPhase.ConferenceChampionship, next.Phase);
        Assert.Equal(14, next.Week);
    }

    [Fact]
    public void ConferenceChampionshipAdvancesToPostseasonWeekFifteen()
    {
        var next = SeasonProgression.Advance(
            State(SeasonPhase.ConferenceChampionship, 14));

        Assert.Equal(SeasonPhase.Postseason, next.Phase);
        Assert.Equal(15, next.Week);
    }

    [Theory]
    [InlineData(15, 16)]
    [InlineData(16, 17)]
    [InlineData(17, 18)]
    public void PostseasonAdvancesThroughFourPlayoffWeeks(
        int currentWeek,
        int expectedWeek)
    {
        var next = SeasonProgression.Advance(
            State(SeasonPhase.Postseason, currentWeek));

        Assert.Equal(SeasonPhase.Postseason, next.Phase);
        Assert.Equal(expectedWeek, next.Week);
    }

    [Fact]
    public void ChampionshipWeekAdvancesToTransferPortal()
    {
        var next = SeasonProgression.Advance(
            State(
                SeasonPhase.Postseason,
                CollegeFootballPlayoffService.NationalChampionshipWeek));

        Assert.Equal(SeasonPhase.TransferPortal, next.Phase);
        Assert.Equal(19, next.Week);
    }

    [Fact]
    public void PortalEntryWindowAdvancesToRecruitingWeekOne()
    {
        var next = SeasonProgression.Advance(
            State(SeasonPhase.TransferPortal, 19));

        Assert.Equal(SeasonPhase.Recruiting, next.Phase);
        Assert.Equal(
            SeasonProgression.FirstRecruitingWeek,
            next.Week);
        Assert.Equal(
            1,
            SeasonProgression.GetRecruitingWeekNumber(next));
    }

    [Theory]
    [InlineData(20, 21, 2)]
    [InlineData(21, 22, 3)]
    [InlineData(22, 23, 4)]
    [InlineData(23, 24, 5)]
    [InlineData(24, 25, 6)]
    public void RecruitingAdvancesThroughSixWeeks(
        int currentWeek,
        int expectedWeek,
        int expectedRecruitingWeek)
    {
        var next = SeasonProgression.Advance(
            State(SeasonPhase.Recruiting, currentWeek));

        Assert.Equal(SeasonPhase.Recruiting, next.Phase);
        Assert.Equal(expectedWeek, next.Week);
        Assert.Equal(
            expectedRecruitingWeek,
            SeasonProgression.GetRecruitingWeekNumber(next));
    }

    [Fact]
    public void FinalRecruitingWeekAdvancesToRosterManagement()
    {
        var state = State(
            SeasonPhase.Recruiting,
            SeasonProgression.LastRecruitingWeek);

        Assert.True(
            SeasonProgression.IsFinalRecruitingWeek(state));

        var next = SeasonProgression.Advance(state);

        Assert.Equal(
            SeasonPhase.RosterManagement,
            next.Phase);
        Assert.Equal(
            SeasonProgression.LastRecruitingWeek + 1,
            next.Week);
    }

    [Fact]
    public void RosterManagementAdvancesToOffseason()
    {
        var currentWeek =
            SeasonProgression.LastRecruitingWeek + 1;

        var next = SeasonProgression.Advance(
            State(
                SeasonPhase.RosterManagement,
                currentWeek));

        Assert.Equal(SeasonPhase.Offseason, next.Phase);
        Assert.Equal(currentWeek + 1, next.Week);
    }

    [Fact]
    public void OffseasonStartsNextYearPreseason()
    {
        var next = SeasonProgression.Advance(
            State(SeasonPhase.Offseason, 27));

        Assert.Equal(SeasonPhase.Preseason, next.Phase);
        Assert.Equal(0, next.Week);
        Assert.Equal(2027, next.SeasonYear);
    }

    [Fact]
    public void RegularSeasonLengthCanBeConfigured()
    {
        var next = SeasonProgression.Advance(
            State(SeasonPhase.RegularSeason, 9),
            regularSeasonWeeks: 9);

        Assert.Equal(SeasonPhase.ConferenceChampionship, next.Phase);
        Assert.Equal(10, next.Week);
    }
}
