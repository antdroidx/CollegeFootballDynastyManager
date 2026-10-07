using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class ScoutingDepartmentTests
{
    [Fact]
    public void RegularSeasonScoutingCreatesStaffAndImprovesKnowledge()
    {
        var state = State(100);
        var team = Team();

        var updated = ScoutingDepartmentService.AdvanceRegularSeasonWeek(
            state, team);

        Assert.Equal(3, updated.ScoutingStaff.Count);
        Assert.Equal(3, updated.ScoutAssignments.Count);
        Assert.Contains(updated.RecruitingInteractions,
            interaction => interaction.ScoutingPercent > 0);
    }

    [Fact]
    public void PriorityScoutIsIncludedInWeeklyWork()
    {
        var state = ScoutingDepartmentService.EnsureDepartment(State(150), Team());
        var priority = state.HighSchoolRecruitingPool.Last();
        state = ScoutingDepartmentService.TogglePriority(state,
            RecruitingSource.HighSchool, priority.RecruitId);

        var updated = ScoutingDepartmentService.AdvanceRegularSeasonWeek(
            state, Team());
        var interaction = InteractiveRecruitingService.GetInteraction(updated,
            RecruitingSource.HighSchool, priority.RecruitId);

        Assert.True(interaction.IsPriorityScout);
        Assert.True(interaction.ScoutingPercent >= 10);
    }

    [Fact]
    public void ReportUsesHeavilyScoutedPlayers()
    {
        var state = State(20);
        state = state with
        {
            RecruitingInteractions = state.HighSchoolRecruitingPool.Select(recruit =>
                new RecruitingInteraction
                {
                    ProspectId = recruit.RecruitId,
                    Source = RecruitingSource.HighSchool,
                    SeasonYear = state.SeasonYear,
                    ScoutingPercent = 80,
                    UserInterest = 60
                }).ToArray()
        };

        var updated = ScoutingDepartmentService.GenerateRecommendationReport(
            state, Team());

        Assert.NotNull(updated.ScoutingRecommendationReport);
        Assert.NotEmpty(updated.ScoutingRecommendationReport!.Recommendations);
        Assert.All(updated.ScoutingRecommendationReport.Recommendations, item =>
            Assert.True(InteractiveRecruitingService.GetInteraction(updated,
                item.Source, item.ProspectId).ScoutingPercent >= 75));
    }

    [Fact]
    public void FullRegularSeasonProducesReportReadyEvaluations()
    {
        var state = State(180);
        var team = Team();
        for (var week = 0; week < 14; week++)
        {
            state = ScoutingDepartmentService.AdvanceRegularSeasonWeek(
                state with { Week = week }, team);
        }

        state = ScoutingDepartmentService.GenerateRecommendationReport(
            state, team);

        Assert.Contains(state.RecruitingInteractions,
            interaction => interaction.ScoutingPercent >= 75);
        Assert.NotEmpty(state.ScoutingRecommendationReport!.Recommendations);
    }

    [Fact]
    public void LowPrestigeScoutingReportAvoidsUninterestedFiveStarReach()
    {
        var fiveStar = new HighSchoolRecruit
        {
            RecruitId = Guid.Parse("aaaaaaaa-1111-2222-3333-444444444444"),
            SeasonYear = 2027,
            FullName = "National Five Star",
            Position = Position.QB,
            StarRating = 5,
            TrueOverallRating = 89,
            PotentialRating = 97,
            HomeState = 45,
            HomeRegion = 4
        };
        var realistic = new HighSchoolRecruit
        {
            RecruitId = Guid.Parse("bbbbbbbb-1111-2222-3333-444444444444"),
            SeasonYear = 2027,
            FullName = "Regional Prospect",
            Position = Position.QB,
            StarRating = 1,
            TrueOverallRating = 62,
            PotentialRating = 78,
            HomeState = 1,
            HomeRegion = 1
        };
        var team = new Team
        {
            Name = "User",
            Abbreviation = "USR",
            ConferenceName = "Test",
            Prestige = 25,
            LegacyRegionId = 1
        };
        var state = State(0) with
        {
            HighSchoolRecruitingPool = new[] { fiveStar, realistic },
            RecruitingInteractions = new[]
            {
                new RecruitingInteraction
                {
                    ProspectId = fiveStar.RecruitId,
                    Source = RecruitingSource.HighSchool,
                    SeasonYear = 2027,
                    ScoutingPercent = 80
                },
                new RecruitingInteraction
                {
                    ProspectId = realistic.RecruitId,
                    Source = RecruitingSource.HighSchool,
                    SeasonYear = 2027,
                    ScoutingPercent = 80
                }
            }
        };

        var updated =
            ScoutingDepartmentService.GenerateRecommendationReport(
                state,
                team);

        Assert.DoesNotContain(
            updated.ScoutingRecommendationReport!.Recommendations,
            item => item.ProspectId == fiveStar.RecruitId);
        Assert.Contains(
            updated.ScoutingRecommendationReport.Recommendations,
            item => item.ProspectId == realistic.RecruitId);
    }

    [Fact]
    public void LowPrestigeProgramStillScoutsHigherRatedRegionalProspects()
    {
        var regionalFourStar = new HighSchoolRecruit
        {
            RecruitId = Guid.Parse(
                "cccccccc-1111-2222-3333-444444444444"),
            SeasonYear = 2027,
            FullName = "Regional Four Star",
            Position = Position.WR,
            StarRating = 4,
            TrueOverallRating = 80,
            PotentialRating = 92,
            HomeState = 12,
            HomeRegion = 1
        };

        var filler = Enumerable.Range(1, 80)
            .Select(index => new HighSchoolRecruit
            {
                RecruitId = Guid.NewGuid(),
                SeasonYear = 2027,
                FullName = $"Filler {index}",
                Position = Position.RB,
                StarRating = 1 + index % 2,
                TrueOverallRating = 58 + index % 8,
                PotentialRating = 70 + index % 10,
                HomeState = index % 50,
                HomeRegion = index % 5
            })
            .ToArray();

        var team = new Team
        {
            Name = "User",
            Abbreviation = "USR",
            ConferenceName = "Test",
            Prestige = 25,
            LegacyRegionId = 1
        };

        var state = State(0) with
        {
            HighSchoolRecruitingPool =
                new[] { regionalFourStar }
                    .Concat(filler)
                    .ToArray()
        };

        var updated =
            ScoutingDepartmentService
                .AdvanceRegularSeasonWeek(
                    state,
                    team);

        var interaction =
            InteractiveRecruitingService.GetInteraction(
                updated,
                RecruitingSource.HighSchool,
                regionalFourStar.RecruitId);

        Assert.True(
            interaction.ScoutingPercent > 0);
    }

    [Fact]
    public void FullyScoutedRatingStillHasSmallUncertainty()
    {
        var state = State(1);
        var recruit = Assert.Single(state.HighSchoolRecruitingPool);
        state = state with
        {
            RecruitingInteractions = new[]
            {
                new RecruitingInteraction
                {
                    ProspectId = recruit.RecruitId,
                    Source = RecruitingSource.HighSchool,
                    SeasonYear = state.SeasonYear,
                    ScoutingPercent = 100
                }
            }
        };

        var range = InteractiveRecruitingService.GetScoutedOverallRange(state,
            RecruitingSource.HighSchool, recruit.RecruitId);

        Assert.True(range.Maximum > range.Minimum);
        Assert.InRange(recruit.TrueOverallRating, range.Minimum - 2,
            range.Maximum + 2);
    }

    private static DynastyState State(int recruitCount)
    {
        var recruits = Enumerable.Range(0, recruitCount).Select(index =>
            new HighSchoolRecruit
            {
                RecruitId = Guid.NewGuid(),
                SeasonYear = 2027,
                FullName = $"Recruit {index}",
                Position = (Position)(index % Enum.GetValues<Position>().Length),
                StarRating = 1 + index % 5,
                TrueOverallRating = 60 + index % 30,
                PotentialRating = 70 + index % 25,
                HomeState = index % 50,
                HomeRegion = index % 5
            }).ToArray();

        return new DynastyState
        {
            DynastyId = Guid.Parse("14141414-2525-3636-4747-585858585858"),
            DynastyName = "Scouting Test",
            UserTeamName = "User",
            SeasonYear = 2027,
            Week = 4,
            Phase = SeasonPhase.RegularSeason,
            HighSchoolRecruitingPool = recruits
        };
    }

    private static Team Team() => new()
    {
        Name = "User",
        Abbreviation = "USR",
        ConferenceName = "Test",
        Prestige = 80,
        LegacyRegionId = 3
    };
}
