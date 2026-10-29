using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public class ScheduleDepthPresentationTests
{
    [Theory][InlineData(ProPhase.Preseason,3)][InlineData(ProPhase.RegularSeason,18)][InlineData(ProPhase.Playoffs,4)]
    public void PhaseHasOnlyItsOwnWeeks(ProPhase phase,int count){var rows=ProScheduleFilters.Weeks(ProPhase.Preseason,2,phase);Assert.Equal(count+1,rows.Count);Assert.All(rows.Skip(1),r=>Assert.Equal(phase,r.Phase));Assert.Equal(phase==ProPhase.Preseason?1:0,rows.Count(r=>r.Label.Contains("Current")));}
    [Fact]public void AllPhasesDoNotConfuseMatchingWeekNumbers(){var rows=ProScheduleFilters.Weeks(ProPhase.Preseason,2,null);var current=Assert.Single(rows,r=>r.Label.Contains("Current"));Assert.Equal(ProPhase.Preseason,current.Phase);Assert.Equal(2,current.Week);Assert.Equal(26,rows.Count);Assert.Contains(rows,r=>r.Phase==ProPhase.RegularSeason&&r.Week==2&&!r.Label.Contains("Current"));}
    [Fact]public void OffseasonDoesNotHighlightAnInSeasonWeek()=>Assert.DoesNotContain(ProScheduleFilters.Weeks(ProPhase.Offseason,2,null),r=>r.Label.Contains("Current"));
    [Fact]public void DepthPositionsFollowCollegeNumbersIncludingGroupedLineRoles()=>Assert.Equal(new[]{"QB","RB","WR","TE","OT","OG","OC","OL","DE","DT","OLB","MLB","CB","FS","SS","K"},ProDepthPositions.Sort(new[]{"SS","QB","OC","OT","OG","CB","DT","DE","OLB","MLB","WR","RB","TE","K","FS","OL"}));
}
