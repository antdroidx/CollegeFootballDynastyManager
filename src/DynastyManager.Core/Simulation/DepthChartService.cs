using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class DepthChartService
{
    public static IReadOnlyList<string> PositionNames { get; } = Array.AsReadOnly(new[]
        { "QB", "HB", "FB", "WR", "TE", "LT", "LG", "C", "RG", "RT", "LE", "RE", "DT", "LOLB", "MLB", "ROLB", "CB", "FS", "SS", "K", "P" });

    public static Position PositionForCode(int code) => code switch
    {
        0 => Position.QB, 1 or 2 => Position.RB, 3 => Position.WR, 4 => Position.TE,
        >= 5 and <= 9 => Position.OL, 10 or 11 => Position.DE, 12 => Position.DT,
        13 or 15 => Position.OLB, 14 => Position.MLB, 16 => Position.CB, 17 => Position.FS,
        18 => Position.SS, 19 or 20 => Position.K, _ => throw new ArgumentOutOfRangeException(nameof(code))
    };

    public static int PositionalRating(DynastyPlayer player, int code)
    {
        _ = PositionForCode(code);
        return Ncaa06RatingService.CalculateOverall(player with { Position = PositionForCode(code), NcaaPositionCode = code });
    }

    public static int AssignedCode(DynastyState state, DynastyPlayer player) => state.DepthChartAssignments
        .LastOrDefault(a => a.PlayerId == player.PlayerId && a.TeamName.Equals(player.TeamName, StringComparison.OrdinalIgnoreCase))?.PositionCode
        ?? Ncaa06RatingService.PositionCode(player);

    public static IReadOnlyList<DynastyPlayer> AtPosition(DynastyState state, string team, int code)
    {
        _ = PositionForCode(code);
        var orders = state.DepthChartAssignments.Where(a => a.TeamName.Equals(team, StringComparison.OrdinalIgnoreCase))
            .GroupBy(a => a.PlayerId).ToDictionary(g => g.Key, g => g.Last());
        return state.ActiveRoster.Where(p => p.TeamName.Equals(team, StringComparison.OrdinalIgnoreCase) &&
                (orders.TryGetValue(p.PlayerId, out var entry) ? entry.PositionCode : Ncaa06RatingService.PositionCode(p)) == code)
            .OrderBy(p => p.IsRedshirted).ThenBy(p => orders.TryGetValue(p.PlayerId, out var entry)
                ? entry.Order : p.DepthChartOrder > 0 ? p.DepthChartOrder : int.MaxValue)
            .ThenByDescending(p => PositionalRating(p, code)).ThenBy(p => p.PlayerId).ToArray();
    }

    public static DynastyState Assign(DynastyState state, string team, Guid playerId, int code, int order)
    {
        _ = PositionForCode(code);
        var player = state.ActiveRoster.SingleOrDefault(p => p.PlayerId == playerId && p.TeamName.Equals(team, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException("Player is not on this team's roster.", nameof(playerId));
        var previousCode = AssignedCode(state, player);
        var target = AtPosition(state, team, code).Where(p => p.PlayerId != playerId).Select(p => p.PlayerId).ToList();
        target.Insert(Math.Clamp(order - 1, 0, target.Count), playerId);
        var assignments = state.DepthChartAssignments.Where(a => !a.TeamName.Equals(team, StringComparison.OrdinalIgnoreCase) ||
            a.PositionCode != code && a.PlayerId != playerId).ToList();
        assignments.AddRange(target.Select((id, index) => new DepthChartAssignment(team, id, code, index + 1)));
        if (previousCode != code)
        {
            assignments.RemoveAll(a => a.TeamName.Equals(team, StringComparison.OrdinalIgnoreCase) && a.PositionCode == previousCode);
            assignments.AddRange(AtPosition(state, team, previousCode).Where(p => p.PlayerId != playerId)
                .Select((p, index) => new DepthChartAssignment(team, p.PlayerId, previousCode, index + 1)));
        }
        return state with { DepthChartAssignments = assignments };
    }

    public static DynastyState AutoSet(DynastyState state, string team, int? selectedCode = null)
    {
        if (selectedCode is int code)
        {
            var result = state;
            // Sorting this group doesn't unexpectedly empty another starting position.
            var ranked = AtPosition(state, team, code).OrderBy(p => p.IsRedshirted || p.CurrentInjury is not null)
                .ThenByDescending(p => FitScore(state, team, p, code)).ThenBy(p => p.PlayerId).ToArray();
            for (var i = 0; i < ranked.Length; i++) result = Assign(result, team, ranked[i].PlayerId, code, i + 1);
            return result;
        }
        var roster = state.ActiveRoster.Where(p => p.TeamName.Equals(team, StringComparison.OrdinalIgnoreCase)).ToArray();
        var remaining = roster.Where(p => !p.IsRedshirted && p.CurrentInjury is null).ToList();
        var draft = new List<DepthChartAssignment>();
        var plan = Plan(state, team);
        // Reserve specialist and line jobs before flexible skill/defensive jobs.
        int[] roles = [0, 19, 20, 7, 5, 9, 6, 8, 16, 17, 18, 10, 11, 12, 14, 13, 15, 3, 4, 1, 2];
        foreach (var role in roles)
        {
            var count = role switch
            {
                3 => plan.Offense is OffensivePlaybook.Spread or OffensivePlaybook.AirRaid ? 4 : plan.Offense == OffensivePlaybook.Flexbone ? 2 : 3,
                12 => plan.Defense == DefensivePlaybook.ThreeFour ? 1 : 2,
                14 => plan.Defense == DefensivePlaybook.ThreeFour ? 2 : 1,
                16 => plan.Defense == DefensivePlaybook.Nickel ? 3 : 2,
                _ => 1
            };
            for (var index = 0; index < count && remaining.Count > 0; index++)
            {
                var choice = remaining.OrderByDescending(p => FitScore(state, team, p, role)
                    - (Ncaa06RatingService.PositionCode(p) == role ? 0 : p.Position == PositionForCode(role) ? 1 : 4))
                    .ThenBy(p => p.PlayerId).First();
                draft.Add(new(team, choice.PlayerId, role, index + 1));
                remaining.Remove(choice);
            }
        }
        var selected = draft.Select(a => a.PlayerId).ToHashSet();
        foreach (var group in roster.Where(p => !selected.Contains(p.PlayerId)).GroupBy(Ncaa06RatingService.PositionCode))
        {
            var offset = draft.Count(a => a.PositionCode == group.Key);
            draft.AddRange(group.OrderBy(p => p.IsRedshirted || p.CurrentInjury is not null)
                .ThenByDescending(p => FitScore(state, team, p, group.Key)).ThenBy(p => p.PlayerId)
                .Select((p, index) => new DepthChartAssignment(team, p.PlayerId, group.Key, offset + index + 1)));
        }
        return state with { DepthChartAssignments = state.DepthChartAssignments
            .Where(a => !a.TeamName.Equals(team, StringComparison.OrdinalIgnoreCase)).Concat(draft).ToArray() };
    }

    private static TeamGamePlan Plan(DynastyState state, string team) => state.TeamGamePlans.LastOrDefault(p => p.TeamName.Equals(team, StringComparison.OrdinalIgnoreCase))
        ?? new TeamGamePlan { TeamName = team };

    public static double FitScore(DynastyState state, string team, DynastyPlayer player, int code)
    {
        var p = PlayerSkillService.EnsureInitialized(player);
        var s = p.Skills!;
        var plan = Plan(state, team);
        var role = code <= 9 ? StaffRole.OffensiveCoordinator : code >= 19 ? StaffRole.SpecialTeamsCoordinator : StaffRole.DefensiveCoordinator;
        var staff = StaffManagementService.GetStaff(state, team, role);
        var expertise = ((staff?.Scheme ?? 60) + (staff?.TalentEvaluation ?? 60)) / 200d;
        double fit = code switch
        {
            0 when plan.Offense is OffensivePlaybook.Option or OffensivePlaybook.Flexbone => (p.SpeedRating + s.BallCarrying) / 2d,
            1 or 2 when plan.Offense is OffensivePlaybook.Spread or OffensivePlaybook.AirRaid => s.Catching,
            >= 5 and <= 9 => plan.Offense is OffensivePlaybook.PowerRun or OffensivePlaybook.Option or OffensivePlaybook.Flexbone ? s.RunBlocking : s.PassBlocking,
            10 or 11 when plan.Approach == DefensiveApproach.Pressure => (p.SpeedRating + s.Acceleration) / 2d,
            >= 13 and <= 18 when plan.Approach == DefensiveApproach.StopRun => s.Tackling,
            >= 13 and <= 18 => (p.AwarenessRating + p.AgilityRating) / 2d,
            19 => s.KickAccuracy, 20 => s.KickPower,
            _ => (p.AwarenessRating + s.Discipline) / 2d
        };
        return PositionalRating(p, code) + (fit - 75) * .12 * expertise;
    }

    public static IReadOnlyList<DynastyPlayer> ProjectRoster(IReadOnlyList<DynastyPlayer> roster, IReadOnlyList<DepthChartAssignment> assignments)
    {
        if (assignments.Count == 0) return roster;
        var available = roster.ToDictionary(p => p.PlayerId);
        var valid = assignments.Where(a => a.PositionCode is >= 0 and <= 20 && a.Order > 0 && available.TryGetValue(a.PlayerId, out var player)
            && player.TeamName.Equals(a.TeamName, StringComparison.OrdinalIgnoreCase)).GroupBy(a => a.PlayerId).ToDictionary(g => g.Key, g => g.Last());
        var teams = valid.Values.Select(a => a.TeamName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var projected = roster.Select(p =>
        {
            if (!teams.Contains(p.TeamName)) return p;
            var code = valid.TryGetValue(p.PlayerId, out var a) ? a.PositionCode : Ncaa06RatingService.PositionCode(p);
            var order = a?.Order ?? Math.Max(1, p.DepthChartOrder);
            var interleaved = code switch { >= 5 and <= 9 => (order - 1) * 5 + code - 4,
                1 or 2 => (order - 1) * 2 + code, 10 or 11 => (order - 1) * 2 + code - 9,
                13 or 15 => (order - 1) * 2 + (code == 13 ? 1 : 2), _ => order };
            return p with { Position = PositionForCode(code), NcaaPositionCode = code, PlayingPositionCode = code, OverallRating = PositionalRating(p, code), DepthChartOrder = interleaved };
        }).ToArray();
        // Injured/redshirted first choices do not displace the opposite-side
        // starter when the next healthy athlete is promoted within a role.
        var result = new List<DynastyPlayer>(projected.Length);
        foreach (var group in projected.GroupBy(p => (p.TeamName, Code: Ncaa06RatingService.PositionCode(p))))
        {
            if (!teams.Contains(group.Key.TeamName)) { result.AddRange(group); continue; }
            var code = group.Key.Code;
            var index = 0;
            foreach (var p in group.OrderBy(p => p.IsRedshirted || p.CurrentInjury is not null).ThenBy(p => p.DepthChartOrder).ThenBy(p => p.PlayerId))
            {
                var rank = index++;
                var order = code switch { >= 5 and <= 9 => rank * 5 + code - 4,
                    1 or 2 => rank * 2 + code, 10 or 11 => rank * 2 + code - 9,
                    13 or 15 => rank * 2 + (code == 13 ? 1 : 2), _ => rank + 1 };
                result.Add(p with { DepthChartOrder = order });
            }
        }
        return result;
    }
}
