using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class TransferRosterPlanningService
{
    public static int GetValue(DynastyState state, Team team, DynastyPlayer candidate,
        IEnumerable<DynastyPlayer>? roster = null) =>
        CreatePlan(state, team, roster).GetValue(candidate);

    public static RosterPlan CreatePlan(DynastyState state, Team team,
        IEnumerable<DynastyPlayer>? roster = null) => new(state, team, roster);

    // One immutable evaluation snapshot per team/roster change. Candidate
    // evaluation must not rescan the entire league or look up staff again.
    public sealed class RosterPlan
    {
        private readonly DynastyPlayer[] _players;
        private readonly Dictionary<Position, DynastyPlayer[]> _positions;
        private readonly int _preference;
        private readonly int _young;
        private readonly int _graduating;

        internal RosterPlan(DynastyState state, Team team, IEnumerable<DynastyPlayer>? roster)
        {
            _players = (roster ?? state.ActiveRoster)
                .Where(player => player.TeamName.Equals(team.Name, StringComparison.OrdinalIgnoreCase))
                .Concat(state.PendingTransferCommitments.Where(item =>
                    item.TeamName.Equals(team.Name, StringComparison.OrdinalIgnoreCase))
                    .Select(item => item.Player))
                .DistinctBy(player => player.PlayerId).ToArray();
            _positions = _players.GroupBy(player => player.Position)
                .ToDictionary(group => group.Key, group => group
                    .OrderByDescending(player => player.OverallRating).ToArray());
            var head = StaffManagementService.GetStaff(state, team.Name, StaffRole.HeadCoach);
            _preference = head is null ? 0 : Math.Clamp(
                (head.Recruiting - head.PlayerDevelopment) / 4, -12, 12);
            _young = _players.Count(player => player.ClassYear <= 2);
            _graduating = _players.Count(player => player.ClassYear >= 4);
        }

        // Positive value means this addition is worth a scholarship. No annual quota.
        public int GetValue(DynastyPlayer candidate)
        {
            // Preserve self-exclusion when evaluating an already planned arrival.
            var self = _positions.GetValueOrDefault(candidate.Position, [])
                .FirstOrDefault(player => player.PlayerId == candidate.PlayerId);
            var count = _players.Length - (self is null ? 0 : 1);
            if (count >= DynastyRosterRules.MaximumRosterSize) return int.MinValue;
            var position = _positions.GetValueOrDefault(candidate.Position, []);
            if (self is not null)
                position = position.Where(player => player.PlayerId != candidate.PlayerId).ToArray();
            var target = DynastyRosterRules.TargetPositionCounts.GetValueOrDefault(candidate.Position, 3);
            var starters = DynastyRosterRules.StarterPositionCounts.GetValueOrDefault(candidate.Position, 1);
            var starterGrade = position.ElementAtOrDefault(starters - 1)?.OverallRating ?? 55;
            var upgrade = candidate.OverallRating - starterGrade;
            var young = _young - (self?.ClassYear <= 2 ? 1 : 0);
            var graduating = _graduating - (self?.ClassYear >= 4 ? 1 : 0);
            var shortTerm = candidate.ClassYear >= 4;
            var futureRisk = shortTerm ? Math.Max(0, count / 2 - young) + graduating / 4 : 0;
            var youngCompetition = position.Count(player => player.ClassYear <= 2 &&
                Math.Max(player.PotentialRating, player.OverallRating) >= candidate.OverallRating);
            var need = Math.Max(0, target - position.Length);
            return _preference + need * 5 + upgrade * 2 +
                (candidate.ClassYear <= 2 ? 8 : 0) -
                youngCompetition * (shortTerm ? 8 : 3) - futureRisk -
                Math.Max(0, position.Length - starters) * 4 - 12;
        }
    }
}
