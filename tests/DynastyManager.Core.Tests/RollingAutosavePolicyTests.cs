using DynastyManager.Data.Persistence;
using Xunit;

namespace DynastyManager.Core.Tests;

public sealed class RollingAutosavePolicyTests
{
    [Theory]
    [InlineData(1, SaveKind.WeeklyAuto1)]
    [InlineData(2, SaveKind.WeeklyAuto2)]
    [InlineData(3, SaveKind.WeeklyAuto3)]
    [InlineData(4, SaveKind.WeeklyAuto1)]
    [InlineData(5, SaveKind.WeeklyAuto2)]
    [InlineData(6, SaveKind.WeeklyAuto3)]
    public void WeeklyAutosavesRotateAcrossThreeSlots(int week, SaveKind expected)
    {
        Assert.Equal(expected, RollingAutosavePolicy.GetSlotForWeek(week));
    }

    [Fact]
    public void PreseasonCannotUseWeeklyAutosaveSlot()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RollingAutosavePolicy.GetSlotForWeek(0));
    }
}
