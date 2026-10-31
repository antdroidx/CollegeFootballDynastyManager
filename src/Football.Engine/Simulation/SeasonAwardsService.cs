using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class SeasonAwardsService
{
    // A full starting eleven on each side, plus a kicker and punter. OL is a shared five-man group.
    private static readonly (Position Position, int Count)[] Slots =
    [(Position.QB,1),(Position.RB,2),(Position.WR,2),(Position.TE,1),(Position.OL,5),
     (Position.DE,2),(Position.DT,2),(Position.OLB,2),(Position.MLB,1),(Position.CB,2),(Position.FS,1),(Position.SS,1),(Position.K,1),(Position.P,1)];

    public static IReadOnlyList<PlayerAwardRecord> Build(DynastyState state, bool preseason)
    {
        var stats = (preseason ? state.PlayerSeasonStats.Where(s => s.SeasonYear == state.SeasonYear - 1) : DynastyHistoryService.BuildCurrentPlayerStats(state))
            .GroupBy(s => s.PlayerId).ToDictionary(g => g.Key, g => g.First());
        var actualIds = state.Schedule.Where(g => g.SeasonYear == state.SeasonYear && g.HasPlayed).SelectMany(g => g.PlayerStats).Select(s => s.PlayerId).ToHashSet();
        var roster = state.ActiveRoster.Where(p => !p.IsRedshirted && (preseason || stats.TryGetValue(p.PlayerId, out var s) && s.Games > 0 && (actualIds.Count == 0 || actualIds.Contains(p.PlayerId)))).ToArray();
        var teams = state.LeagueTeams.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
        double Score(DynastyPlayer p) => preseason
            ? p.OverallRating * 10 + (stats.TryGetValue(p.PlayerId, out var previous) ? Production(previous) * .08 : 0)
            : stats.TryGetValue(p.PlayerId, out var s) ? Production(s) + p.OverallRating * (p.Position == Position.OL ? 5 : .15) : 0;
        var awards = new List<PlayerAwardRecord>();
        PlayerAwardRecord Make(DynastyPlayer p, string name, string? conference = null, int selection = 1, bool all = false)
        {
            stats.TryGetValue(p.PlayerId, out var line);
            return new() { SeasonYear = state.SeasonYear, PlayerId = p.PlayerId, PlayerName = p.FullName,
                TeamName = p.TeamName, Position = p.Position, AwardName = name, IsAllAmerican = all,
                ConferenceName = conference, SelectionTeam = selection, IsPreseason = preseason,
                Summary = preseason ? $"OVR {p.OverallRating} · returning talent" : Describe(line, p) };
        }
        void SelectTeam(IEnumerable<DynastyPlayer> pool, string? conference)
        {
            foreach (var (position, count) in Slots)
            {
                var players = pool.Where(p => p.Position == position).OrderByDescending(Score).ThenBy(p => p.PlayerId).Take(count * 2).ToArray();
                for (var i = 0; i < players.Length; i++)
                {
                    var tier = i < count ? 1 : 2;
                    var label = $"{(preseason ? "Preseason " : "")}{(tier == 1 ? "First" : "Second")} Team {(conference is null ? "All-American" : "All-Conference")}";
                    awards.Add(Make(players[i], label, conference, tier, conference is null));
                }
            }
        }
        SelectTeam(roster, null);
        foreach (var group in roster.Where(p => teams.ContainsKey(p.TeamName)).GroupBy(p => teams[p.TeamName].ConferenceName)) SelectTeam(group, group.Key);
        if (!preseason)
        {
            void Trophy(string name, Func<DynastyPlayer, bool> eligible, Func<DynastyPlayer, double>? metric = null)
            {
                var winner = roster.Where(eligible).OrderByDescending(metric ?? Score).ThenBy(p => p.PlayerId).FirstOrDefault();
                if (winner is not null) awards.Add(Make(winner, name));
            }
            // Position-normalized excellence allows exceptional defenders and receivers into the Heisman race.
            var maximum = roster.GroupBy(p => p.Position).ToDictionary(g => g.Key, g => Math.Max(1, g.Max(Score)));
            Trophy("Heisman Trophy", p => p.Position is not (Position.OL or Position.K or Position.P), p =>
                Score(p) / maximum[p.Position] * 100 + Math.Log(1 + Math.Max(0, Score(p))) * 7 + TeamWins(state, p.TeamName) * 1.5);
            Trophy("Maxwell Award", p => p.Position is Position.QB or Position.RB or Position.FB or Position.WR or Position.TE);
            Trophy("Davey O’Brien Award", p => p.Position == Position.QB);
            Trophy("Doak Walker Award", p => p.Position == Position.RB);
            Trophy("Biletnikoff Award", p => p.Position is Position.WR or Position.TE);
            Trophy("John Mackey Award", p => p.Position == Position.TE);
            Trophy("Outland Trophy", p => p.Position is Position.OL or Position.DT);
            Trophy("Bednarik Award", p => p.Position is Position.DE or Position.DT or Position.OLB or Position.MLB or Position.CB or Position.FS or Position.SS);
            Trophy("Jim Thorpe Award", p => p.Position is Position.CB or Position.FS or Position.SS);
            Trophy("Lou Groza Award", p => p.Position == Position.K, p => stats.TryGetValue(p.PlayerId, out var s) ? s.FieldGoalsMade * 12 - (s.FieldGoalsAttempted - s.FieldGoalsMade) * 8 + s.ExtraPointsMade : 0);
            Trophy("Ray Guy Award", p => p.Position == Position.P && stats.TryGetValue(p.PlayerId, out var s) && s.Punts >= 10, p => stats.TryGetValue(p.PlayerId, out var s) ? s.PuntYards / (double)Math.Max(1,s.Punts) * 10 + s.PuntsInside20 * 2 : 0);
        }
        return awards;
    }

    public static DynastyState EnsurePreseason(DynastyState state) => state.PlayerAwardHistory.Any(a => a.SeasonYear == state.SeasonYear && a.IsPreseason)
        ? state : state with { PlayerAwardHistory = state.PlayerAwardHistory.Concat(Build(state, true)).ToArray() };

    public static double Production(PlayerSeasonStatLine s) => s.Position switch
    {
        Position.QB => s.PassYards * .12 + s.PassingTouchdowns * 12 - s.InterceptionsThrown * 10 + s.RushYards * .15 + s.RushingTouchdowns * 12,
        Position.RB or Position.FB or Position.WR or Position.TE => (s.RushYards + s.ReceivingYards) * .3 + (s.RushingTouchdowns + s.ReceivingTouchdowns) * 16 + s.Receptions - (s.RushingFumbles + s.ReceivingFumbles) * 10,
        Position.K => s.FieldGoalsMade * 12 - (s.FieldGoalsAttempted - s.FieldGoalsMade) * 8 + s.ExtraPointsMade,
        Position.P => s.PuntYards / (double)Math.Max(1, s.Punts) * 10 + s.PuntsInside20 * 2 + s.PuntsInside10 * 3,
        Position.OL => s.Games * 12, // Individual blocking stats are not recorded; combine appearances/availability with talent.
        _ => s.Tackles * 2 + s.TacklesForLoss * 7 + s.Sacks * 12 + s.Interceptions * 20 + s.PassesDefended * 5 + s.FumbleRecoveries * 12
    };
    private static int TeamWins(DynastyState state, string team) => state.Schedule.Count(g => g.SeasonYear == state.SeasonYear && g.HasPlayed && g.WinnerTeamName == team);
    private static string Describe(PlayerSeasonStatLine? s, DynastyPlayer p) => s is null ? $"OVR {p.OverallRating}" : p.Position switch
    {
        Position.QB => $"{s.PassYards:N0} pass yds · {s.PassingTouchdowns} TD · {s.InterceptionsThrown} INT",
        Position.RB or Position.FB or Position.WR or Position.TE => $"{s.RushYards + s.ReceivingYards:N0} yards · {s.RushingTouchdowns + s.ReceivingTouchdowns} TD",
        Position.OL => $"{s.Games} appearances · OVR {p.OverallRating}",
        Position.K => $"{s.FieldGoalsMade}/{s.FieldGoalsAttempted} FG · {s.ExtraPointsMade} XP",
        Position.P => $"{s.Punts} punts · {s.PuntYards / (double)Math.Max(1, s.Punts):F1} avg · {s.PuntsInside20} inside 20",
        _ => $"{s.Tackles} tackles · {s.Sacks} sacks · {s.Interceptions} INT"
    };
}
