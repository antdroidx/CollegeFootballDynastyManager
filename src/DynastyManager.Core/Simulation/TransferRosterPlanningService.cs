using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class TransferRosterPlanningService
{
    // Positive value means this addition is worth a scholarship. There is no
    // annual transfer count; every player is evaluated against the current plan.
    public static int GetValue(DynastyState state, Team team, DynastyPlayer candidate,
        IEnumerable<DynastyPlayer>? roster = null)
    {
        var players = (roster ?? state.ActiveRoster)
            .Where(player => player.TeamName.Equals(team.Name, StringComparison.OrdinalIgnoreCase))
            .Concat(state.PendingTransferCommitments.Where(item =>
                item.TeamName.Equals(team.Name, StringComparison.OrdinalIgnoreCase))
                .Select(item => item.Player))
            .Where(player => player.PlayerId != candidate.PlayerId)
            .DistinctBy(player => player.PlayerId).ToArray();
        if (players.Length >= DynastyRosterRules.MaximumRosterSize) return int.MinValue;
        var position = players.Where(player => player.Position == candidate.Position).ToArray();
        var target = DynastyRosterRules.TargetPositionCounts.GetValueOrDefault(candidate.Position, 3);
        var starters = DynastyRosterRules.StarterPositionCounts.GetValueOrDefault(candidate.Position, 1);
        var starterGrade = position.OrderByDescending(player => player.OverallRating)
            .Skip(starters - 1).FirstOrDefault()?.OverallRating ?? 55;
        var upgrade = candidate.OverallRating - starterGrade;
        var head = StaffManagementService.GetStaff(state, team.Name, StaffRole.HeadCoach);
        var preference = head is null ? 0 : Math.Clamp(
            (head.Recruiting - head.PlayerDevelopment) / 4, -12, 12);
        var young = players.Count(player => player.ClassYear <= 2);
        var graduating = players.Count(player => player.ClassYear >= 4);
        var shortTerm = candidate.ClassYear >= 4;
        var futureRisk = shortTerm
            ? Math.Max(0, players.Length / 2 - young) + graduating / 4
            : 0;
        var youngCompetition = position.Count(player => player.ClassYear <= 2 &&
            Math.Max(player.PotentialRating, player.OverallRating) >= candidate.OverallRating);
        var need = Math.Max(0, target - position.Length);
        return preference + need * 5 + upgrade * 2 +
            (candidate.ClassYear <= 2 ? 8 : 0) -
            youngCompetition * (shortTerm ? 8 : 3) - futureRisk -
            Math.Max(0, position.Length - starters) * 4 - 12;
    }
}
