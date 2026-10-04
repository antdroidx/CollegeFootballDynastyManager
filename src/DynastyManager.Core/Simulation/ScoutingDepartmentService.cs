using System.Security.Cryptography;
using System.Text;
using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class ScoutingDepartmentService
{
    public static DynastyState EnsureDepartment(DynastyState state, Team userTeam)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(userTeam);

        if (state.ScoutingStaff.Count > 0)
            return EnsureAssignments(state, userTeam);

        var names = new[] { "Morgan Reed", "Darius Cole", "Jamie Navarro" };
        var scouts = names.Select((name, index) => new ScoutStaff
        {
            ScoutId = StableId(state.DynastyId, $"scout-{index}"),
            FullName = name,
            TalentEvaluation = 62 + Seed(state, index, "talent") % 27,
            PotentialEvaluation = 62 + Seed(state, index, "potential") % 27,
            RegionalKnowledge = 62 + Seed(state, index, "region") % 27,
            WorkRate = 62 + Seed(state, index, "work") % 27
        }).ToArray();

        return EnsureAssignments(state with { ScoutingStaff = scouts }, userTeam);
    }

    public static DynastyState SetAssignment(
        DynastyState state,
        Guid scoutId,
        ScoutAssignmentScope scope,
        string? target = null)
    {
        var assignments = state.ScoutAssignments
            .Where(item => item.ScoutId != scoutId)
            .Append(new ScoutAssignment
            {
                ScoutId = scoutId,
                Scope = scope,
                Target = target
            })
            .ToArray();

        return state with { ScoutAssignments = assignments };
    }

    public static DynastyState TogglePriority(
        DynastyState state,
        RecruitingSource source,
        Guid prospectId)
    {
        var interaction = InteractiveRecruitingService.GetInteraction(
            state, source, prospectId);
        var interactions = state.RecruitingInteractions.ToList();
        var index = interactions.FindIndex(item =>
            item.SeasonYear == state.SeasonYear &&
            item.Source == source &&
            item.ProspectId == prospectId);
        var updated = interaction with
        {
            IsOnTargetBoard = true,
            IsPriorityScout = !interaction.IsPriorityScout
        };

        if (index >= 0)
            interactions[index] = updated;
        else
            interactions.Add(updated);

        return state with { RecruitingInteractions = interactions };
    }

    public static DynastyState AdvanceRegularSeasonWeek(
        DynastyState state,
        Team userTeam)
    {
        if (state.Phase != SeasonPhase.RegularSeason ||
            state.HighSchoolRecruitingPool.Count == 0)
        {
            return state;
        }

        state = EnsureDepartment(state, userTeam);
        var interactions = state.RecruitingInteractions.ToList();

        foreach (var scout in state.ScoutingStaff)
        {
            var assignment = state.ScoutAssignments.First(item =>
                item.ScoutId == scout.ScoutId);
            var capacity = 8 + scout.WorkRate / 7;
            var candidates = GetAssignedCandidates(state, assignment)
                .OrderByDescending(recruit => GetPriorityScore(state, recruit))
                .ThenBy(recruit => recruit.RecruitId)
                .Take(capacity);

            foreach (var recruit in candidates)
            {
                var existing = interactions.FirstOrDefault(item =>
                    item.SeasonYear == state.SeasonYear &&
                    item.Source == RecruitingSource.HighSchool &&
                    item.ProspectId == recruit.RecruitId) ??
                    InteractiveRecruitingService.GetInteraction(
                        state, RecruitingSource.HighSchool, recruit.RecruitId);

                var gain = 5 + scout.WorkRate / 18 +
                           scout.TalentEvaluation / 25 +
                           (MatchesGeography(assignment, recruit)
                               ? scout.RegionalKnowledge / 25
                               : 0) +
                           (existing.IsPriorityScout ? 10 : 0);
                var updated = existing with
                {
                    ScoutingPercent = Math.Min(100, existing.ScoutingPercent + gain)
                };
                var index = interactions.FindIndex(item =>
                    item.SeasonYear == updated.SeasonYear &&
                    item.Source == updated.Source &&
                    item.ProspectId == updated.ProspectId);
                if (index >= 0)
                    interactions[index] = updated;
                else
                    interactions.Add(updated);
            }
        }

        return state with { RecruitingInteractions = interactions };
    }

    public static DynastyState GenerateRecommendationReport(
        DynastyState state,
        Team userTeam)
    {
        if (state.ScoutingRecommendationReport?.SeasonYear == state.SeasonYear)
            return state;

        var evaluated = state.HighSchoolRecruitingPool
            .Select(recruit => new Evaluation(
                recruit,
                InteractiveRecruitingService.GetInteraction(
                    state, RecruitingSource.HighSchool, recruit.RecruitId),
                GetPositionNeed(state, recruit.Position)))
            .Where(item => item.Interaction.ScoutingPercent >= 75 &&
                           item.Interaction.CommittedTeamName is null)
            .ToArray();

        var recommendations = new List<ScoutingRecommendation>();
        AddSection(recommendations, ScoutingRecommendationSection.TopTargets,
            evaluated.OrderByDescending(item => item.Recruit.TrueOverallRating + item.Recruit.PotentialRating / 2),
            "Most talented fully evaluated prospect");
        AddSection(recommendations, ScoutingRecommendationSection.BestFits,
            evaluated.OrderByDescending(item =>
                RecruitPreferenceService.GetProgramGrade(state, userTeam,
                    RecruitingSource.HighSchool, item.Recruit.RecruitId,
                    RecruitPitchType.SchemeFit) + item.Recruit.TrueOverallRating),
            "Strong scheme and program fit");
        AddSection(recommendations, ScoutingRecommendationSection.HiddenGems,
            evaluated.Where(item => IsSleeper(item.Recruit))
                .OrderByDescending(item => item.Recruit.TrueOverallRating - item.Recruit.StarRating * 10),
            "Grades above his public star profile");
        AddSection(recommendations, ScoutingRecommendationSection.TeamNeeds,
            evaluated.OrderByDescending(item => item.Need * 20 + item.Recruit.TrueOverallRating),
            "Fills a current roster need");
        AddSection(recommendations, ScoutingRecommendationSection.HighInterest,
            evaluated.OrderByDescending(item => item.Interaction.UserInterest),
            "High current interest in your program");

        return state with
        {
            ScoutingRecommendationReport = new ScoutingRecommendationReport
            {
                SeasonYear = state.SeasonYear,
                GeneratedWeek = state.Week,
                Recommendations = recommendations
            }
        };
    }

    public static bool IsSleeper(HighSchoolRecruit recruit) =>
        recruit.TrueOverallRating >= ExpectedRating(recruit.StarRating) + 5 ||
        recruit.PotentialRating >= 92 && recruit.StarRating <= 3;

    public static bool HasBustRisk(HighSchoolRecruit recruit) =>
        recruit.TrueOverallRating <= ExpectedRating(recruit.StarRating) - 5 ||
        recruit.PotentialRating <= recruit.TrueOverallRating + 2;

    private static DynastyState EnsureAssignments(DynastyState state, Team team)
    {
        var assignments = state.ScoutAssignments.ToList();
        for (var index = 0; index < state.ScoutingStaff.Count; index++)
        {
            var scout = state.ScoutingStaff[index];
            if (assignments.Any(item => item.ScoutId == scout.ScoutId))
                continue;

            assignments.Add(index switch
            {
                0 => new ScoutAssignment { ScoutId = scout.ScoutId, Scope = ScoutAssignmentScope.Region, Target = Math.Clamp(team.LegacyRegionId, 0, 4).ToString() },
                1 => new ScoutAssignment { ScoutId = scout.ScoutId, Scope = ScoutAssignmentScope.TeamNeeds },
                _ => new ScoutAssignment { ScoutId = scout.ScoutId, Scope = ScoutAssignmentScope.National }
            });
        }
        return state with { ScoutAssignments = assignments };
    }

    private static IEnumerable<HighSchoolRecruit> GetAssignedCandidates(
        DynastyState state, ScoutAssignment assignment)
    {
        var candidates = state.HighSchoolRecruitingPool.AsEnumerable();
        return assignment.Scope switch
        {
            ScoutAssignmentScope.State when int.TryParse(assignment.Target, out var stateIndex) =>
                candidates.Where(item => RecruitGeography.GetStateIndex(item) == stateIndex),
            ScoutAssignmentScope.Region when int.TryParse(assignment.Target, out var regionIndex) =>
                candidates.Where(item => RecruitGeography.GetRegion(item) == regionIndex),
            ScoutAssignmentScope.Position when Enum.TryParse<Position>(assignment.Target, out var position) =>
                candidates.Where(item => item.Position == position),
            ScoutAssignmentScope.Offense => candidates.Where(item => IsOffense(item.Position)),
            ScoutAssignmentScope.Defense => candidates.Where(item => IsDefense(item.Position)),
            ScoutAssignmentScope.SpecialTeams => candidates.Where(item => item.Position == Position.K),
            ScoutAssignmentScope.TeamNeeds => candidates.Where(item => GetPositionNeed(state, item.Position) > 0),
            _ => candidates
        };
    }

    private static int GetPriorityScore(DynastyState state, HighSchoolRecruit recruit)
    {
        var interaction = InteractiveRecruitingService.GetInteraction(
            state, RecruitingSource.HighSchool, recruit.RecruitId);
        if (interaction.ScoutingPercent >= 100)
            return -10000;

        var progressBonus = interaction.ScoutingPercent > 0 ? 2500 : 0;
        return (interaction.IsPriorityScout ? 10000 : 0) +
               (interaction.IsOnTargetBoard ? 3000 : 0) +
               progressBonus +
               GetPositionNeed(state, recruit.Position) * 150 +
               recruit.StarRating * 100 + recruit.TrueOverallRating -
               interaction.ScoutingPercent * 3;
    }

    private static bool MatchesGeography(ScoutAssignment assignment, HighSchoolRecruit recruit) =>
        assignment.Scope switch
        {
            ScoutAssignmentScope.State => assignment.Target == RecruitGeography.GetStateIndex(recruit).ToString(),
            ScoutAssignmentScope.Region => assignment.Target == RecruitGeography.GetRegion(recruit).ToString(),
            _ => false
        };

    private static int GetPositionNeed(DynastyState state, Position position)
    {
        var current = state.ActiveRoster.Count(player =>
            player.TeamName.Equals(state.UserTeamName, StringComparison.OrdinalIgnoreCase) &&
            player.Position == position);
        return Math.Max(0, DynastyRosterRules.TargetPositionCounts[position] - current);
    }

    private static bool IsOffense(Position position) =>
        position is Position.QB or Position.RB or Position.WR or Position.TE or Position.OL;

    private static bool IsDefense(Position position) =>
        position is Position.DE or Position.DT or Position.OLB or Position.MLB or Position.CB or Position.FS or Position.SS;

    private static int ExpectedRating(int stars) => stars switch
    {
        5 => 86, 4 => 80, 3 => 73, 2 => 66, _ => 58
    };

    private static void AddSection(
        ICollection<ScoutingRecommendation> destination,
        ScoutingRecommendationSection section,
        IEnumerable<Evaluation> candidates,
        string summary)
    {
        foreach (var item in candidates.Take(5))
        {
            var recruit = item.Recruit;
            destination.Add(new ScoutingRecommendation
            {
                Section = section,
                ProspectId = recruit.RecruitId,
                Source = RecruitingSource.HighSchool,
                PlayerName = recruit.FullName,
                Position = recruit.Position,
                Summary = summary
            });
        }
    }

    private static int Seed(DynastyState state, int index, string purpose) =>
        SimulationSeed.Create(state.DynastyId, state.SeasonYear, index,
            state.UserTeamName, purpose);

    private static Guid StableId(Guid dynastyId, string purpose) =>
        new(MD5.HashData(Encoding.UTF8.GetBytes($"{dynastyId:N}|{purpose}")));

    private sealed record Evaluation(
        HighSchoolRecruit Recruit,
        RecruitingInteraction Interaction,
        int Need);
}
