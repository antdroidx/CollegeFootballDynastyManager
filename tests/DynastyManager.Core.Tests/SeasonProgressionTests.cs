using DynastyManager.Core.Models;
using DynastyManager.Core.Seasons;
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

    [Theory]
    [InlineData(SeasonPhase.ConferenceChampionship, SeasonPhase.Postseason)]
    [InlineData(SeasonPhase.Postseason, SeasonPhase.TransferPortal)]
    [InlineData(SeasonPhase.TransferPortal, SeasonPhase.Recruiting)]
    [InlineData(SeasonPhase.Recruiting, SeasonPhase.RosterManagement)]
    [InlineData(SeasonPhase.RosterManagement, SeasonPhase.Offseason)]
    public void LaterSeasonPhasesAdvanceInOrder(
        SeasonPhase current,
        SeasonPhase expected)
    {
        var next = SeasonProgression.Advance(State(current, 14));

        Assert.Equal(expected, next.Phase);
        Assert.Equal(15, next.Week);
    }

    [Fact]
    public void OffseasonStartsNextYearPreseason()
    {
        var next = SeasonProgression.Advance(
            State(SeasonPhase.Offseason, 19));

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
