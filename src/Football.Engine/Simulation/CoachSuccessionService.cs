using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

/// <summary>Annual AI/support coach succession. The human head coach is excluded from automatic retirement.</summary>
public static class CoachSuccessionService
{
    public static bool ShouldRetire(DynastyState state, StaffMember coach)
    {
        if (coach.IsHeadCoachRoleAssignment || coach.Age < 62) return false;
        var chance = Math.Clamp((coach.Age - 61) * 3, 0, 75);
        return coach.Age >= 80 || SimulationSeed.Create(state.DynastyId, state.SeasonYear,
            (int)coach.Role, coach.StaffId.ToString("N"), "retirement") % 100 < chance;
    }

    public static bool EligibleGraduate(DynastyState state, PlayerDepartureRecord player, StaffRole role) =>
        player.Reason == PlayerDepartureReason.Graduation && player.Leadership >= 75 &&
        player.Awareness >= 75 && player.Discipline >= 60 &&
        state.SeasonYear - player.SeasonYear is >= 6 and <= 20 &&
        role == GraduateRole(player.Position) &&
        !state.Staff.Any(s => s.FormerPlayerId == player.PlayerId) &&
        !state.StaffRetirements.Any(s => s.Coach.FormerPlayerId == player.PlayerId) &&
        SimulationSeed.Create(state.DynastyId, player.SeasonYear, 0, player.PlayerId.ToString("N"), "coaching-path") % 100 < 25;

    public static StaffRole GraduateRole(Position position) => position switch
    {
        Position.K => StaffRole.SpecialTeamsCoordinator,
        Position.QB or Position.RB or Position.WR or Position.TE or Position.OL => StaffRole.OffensiveCoordinator,
        _ => StaffRole.DefensiveCoordinator
    };

    public static DynastyState ResolveRetirements(DynastyState state)
    {
        var retired = state.Staff.Where(s => ShouldRetire(state, s)).OrderBy(s => s.StaffId).ToArray();
        if (retired.Length == 0) return state;
        var staff = state.Staff.Except(retired).ToList();
        var history = state.StaffRetirements.ToList();
        var moves = state.StaffMovementHistory.ToList();
        foreach (var old in retired)
        {
            history.Add(new(state.SeasonYear, old));
            var team = state.LeagueTeams.FirstOrDefault(t => t.Name == old.TeamName) ?? new Team
                { Name = old.TeamName, Abbreviation = "", ConferenceName = "", Prestige = 60 };
            var working = state with { Staff = staff, StaffRetirements = history };
            var replacement = StaffManagementService.GenerateCandidate(working, team, old.Role);
            var graduate = state.PlayerDepartureHistory.Where(p => EligibleGraduate(working, p, old.Role))
                .OrderByDescending(p => p.TeamName == old.TeamName).ThenByDescending(p => p.Leadership + p.Awareness)
                .ThenBy(p => p.PlayerId).FirstOrDefault();
            if (graduate is not null)
            {
                var apprenticeship = state.SeasonYear - graduate.SeasonYear;
                var baseRating = Math.Clamp(40 + (graduate.Awareness + graduate.Leadership) / 10 + apprenticeship / 2, 55, 72);
                replacement = replacement with
                {
                    StaffId = graduate.PlayerId, FormerPlayerId = graduate.PlayerId, FullName = graduate.FullName,
                    AlmaMater = graduate.TeamName, GraduationYear = graduate.SeasonYear, Appearance = graduate.Appearance,
                    Age = 22 + apprenticeship, CareerYears = apprenticeship, TenureYears = 0, GrowthPotential = 4,
                    Leadership = graduate.Leadership, Scheme = baseRating, PlayerDevelopment = baseRating,
                    Recruiting = baseRating - 5, GameManagement = baseRating - 3, SpecialTeams = baseRating,
                    Reputation = baseRating - 8, ContractYearsRemaining = 3
                };
            }
            staff.Add(replacement);
            moves.Add(new StaffMovementRecord { SeasonYear = state.SeasonYear, StaffId = old.StaffId,
                FullName = old.FullName, Role = old.Role, FromTeamName = old.TeamName, MovementType = StaffMovementType.Retired,
                OverallRating = old.OverallRating, Reason = $"Retired at age {old.Age}" });
            moves.Add(new StaffMovementRecord { SeasonYear = state.SeasonYear, StaffId = replacement.StaffId,
                FullName = replacement.FullName, Role = replacement.Role, ToTeamName = replacement.TeamName, MovementType = StaffMovementType.Hired,
                OverallRating = replacement.OverallRating, Reason = graduate is null ? "Replaced retiring coach" : $"Former {graduate.TeamName} player; coaching apprenticeship completed" });
        }
        var retiredIds = retired.Select(s => s.StaffId).ToHashSet();
        return state with { Staff = staff, StaffRetirements = history, StaffMovementHistory = moves,
            StaffMarketCandidates = state.StaffMarketCandidates.Where(c => !retiredIds.Contains(c.Profile.StaffId)).ToArray(),
            StaffMarketSeasonYear = 0 };
    }
}
