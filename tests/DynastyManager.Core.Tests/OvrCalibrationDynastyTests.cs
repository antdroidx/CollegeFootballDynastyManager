using DynastyManager.Core.Models;
using DynastyManager.Core.Seasons;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Import;
using Xunit;
using Xunit.Abstractions;

namespace DynastyManager.Core.Tests;

/// <summary>Full FBS roster lifecycle/market stress test, without the cost of playing 20 national schedules.
/// The separate DynastyHealthTests run complete schedules, stats, injuries and save/load.</summary>
public abstract class OvrCalibrationDynastyTests(ITestOutputHelper output)
{
    protected void RunCareer(int seed)
    {
        var teams = OvrCalibrationTests.Teams().ToDictionary(t => t.Name);
        var user = teams["Washington"];
        var state = OvrCalibrationTests.StartingState(seed) with { RecruitingAssistanceEnabled = true, RecruitingAssistanceWorkloadPercent = 100 };
        // The two promoted schools absent from NEXT26 receive the same filler path as a new dynasty.
        state = WalkOnRosterService.FillAllTeams(state, teams);
        state = StaffManagementService.EnsureLeagueStaff(state, teams.Values);
        var snapshots = new List<object>();
        var means = new List<double>();
        var checkpoints = new List<OvrSnapshot>();
        for (var season = 1; season <= 20; season++)
        {
            state = PlayerRatingService.EnsureProfiles(state);
            var metrics = OvrMetrics.Measure(state, teams.Values);
            means.Add(metrics.Average);
            snapshots.Add(new { season, metrics, walkons = state.ActiveRoster.Count(p => p.IsWalkOn), departures = state.PlayerDepartureHistory.Count });
            OvrMetrics.Write($"dynasty-seed-{seed}", snapshots);
            output.WriteLine($"seed={seed} season={season} avg={metrics.Average:F2} starters={metrics.StarterAverage:F2} elite90={metrics.Percent90:F2}% freshmen={metrics.FreshmanAverage:F2} seniors={metrics.SeniorAverage:F2}");
            if (season is 1 or 5 or 10 or 20)
            {
                checkpoints.Add(metrics);
                Assert.All(state.ActiveRoster, p => Assert.Equal(p.OverallRating, PlayerRatingService.CalculateOverall(p)));
            }
            if (season == 20) break;
            state = state with { Phase = SeasonPhase.RegularSeason };
            foreach (var week in new[] { 3, 6, 9, 12 })
            {
                state = state with { Week = week };
                state = PlayerDevelopmentService.ApplyMidseasonDevelopment(state);
                state = InteractiveRecruitingService.EnsurePhaseInitialized(state, user);
                state = CpuRecruitingService.ApplyWeeklyUserAssistance(state, user);
                state = InteractiveRecruitingService.ResolveCurrentPhase(state, user, teams);
            }
            state = state with { Phase = SeasonPhase.TransferPortal, Week = SeasonProgression.FirstRecruitingWeek - 1 };
            state = OffseasonPlayerLifecycleService.EnterTransferPortal(state);
            state = TransferPortalMarketService.MaterializePendingCommitments(state);
            state = InteractiveRecruitingService.EnsurePhaseInitialized(state, user);
            state = CpuRecruitingService.ApplyWeeklyUserAssistance(state, user);
            state = InteractiveRecruitingService.ResolveCurrentPhase(state, user, teams);
            state = CpuRecruitingService.ApplyPhaseAssistance(state, teams);
            state = SeasonProgression.Advance(state);
            state = state with { HighSchoolRecruitingPool = HighSchoolRecruitingPoolFactory.Create(state.DynastyId, state.SeasonYear, teams.Count, []) };
            for (var week = 0; week < SeasonProgression.RecruitingWeekCount; week++)
            {
                state = InteractiveRecruitingService.EnsurePhaseInitialized(state, user);
                state = CpuRecruitingService.ApplyWeeklyUserAssistance(state, user);
                state = InteractiveRecruitingService.ResolveCurrentPhase(state, user, teams);
                state = CpuRecruitingService.AdvanceCpuRecruitingWeek(state, teams);
                state = SeasonProgression.Advance(state);
            }
            Assert.Equal(SeasonPhase.RosterManagement, state.Phase);
            state = WalkOnRosterService.FillAllTeams(state, teams);
            state = SeasonProgression.Advance(state);
            state = PlayerDevelopmentService.ApplyOffseasonDevelopment(state);
            state = SeasonProgression.Advance(state);
            state = StaffManagementService.AdvanceSeason(state);
            state = state with { HighSchoolRecruitingPool = HighSchoolRecruitingPoolFactory.Create(state.DynastyId, state.SeasonYear, teams.Count, []) };
            state = RosterManagementService.NormalizeAllDepthCharts(state);
            Assert.All(state.ActiveRoster.GroupBy(p => p.TeamName), g => Assert.Equal(85, g.Count()));
        }
        Assert.True(Math.Abs(means[^1] - means[0]) < 3, "Long-term OVR drift exceeds three points");
        foreach (var metrics in checkpoints)
        {
            Assert.InRange(metrics.Average, 70, 76);
            Assert.InRange(metrics.StarterAverage, 76, 84);
            Assert.InRange(metrics.Percent90, .2, 4);
            Assert.InRange(metrics.Percent95, 0, .6);
            Assert.InRange(metrics.Percent99, 0, .05);
            Assert.True(metrics.EliteProgramAverage > metrics.WeakProgramAverage + 2);
            Assert.True(metrics.SeniorAverage > metrics.FreshmanAverage);
        }
    }
}

// Separate xUnit collections allow the independent seeds to run concurrently.
public sealed class OvrCalibrationDynastyTestsSeedOne(ITestOutputHelper output) : OvrCalibrationDynastyTests(output)
{
    [Fact] public void TwentyYearFbsRatingsRemainStable() => RunCareer(1);
}

public sealed class OvrCalibrationDynastyTestsSeedSeventeen(ITestOutputHelper output) : OvrCalibrationDynastyTests(output)
{
    [Fact] public void TwentyYearFbsRatingsRemainStable() => RunCareer(17);
}
