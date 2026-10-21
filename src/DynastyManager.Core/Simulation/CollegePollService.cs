using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

/// <summary>Simulated media/coaches ballots; these are dynasty polls, not licensed real-world polls.</summary>
public static class CollegePollService
{
    public static IReadOnlyList<NationalRanking> Build(DynastyState state, IReadOnlyDictionary<string, Team> teams,
        PollKind kind = PollKind.Media, IReadOnlyDictionary<string, TeamSimulationProfile>? profiles = null)
    {
        if (teams.Count == 0) return [];
        var games = state.Schedule.Where(g => g.SeasonYear == state.SeasonYear && g.HasPlayed && g.HomeScore.HasValue && g.AwayScore.HasValue).ToArray();
        var records = teams.Keys.ToDictionary(n => n, n => games.Where(g => g.InvolvesTeam(n)).ToArray(), StringComparer.OrdinalIgnoreCase);
        double WinPct(string n) => records.TryGetValue(n, out var slate) && slate.Length > 0 ? slate.Count(g => g.WinnerTeamName == n) / (double)slate.Length : FcsLeagueService.Find(n) is not null ? .15 : .5;
        var previous = state.PollHistory.LastOrDefault(p => p.SeasonYear == state.SeasonYear && p.Kind == kind &&
            (p.Week < state.Week || p.Phase == SeasonPhase.Preseason && state.Phase != SeasonPhase.Preseason))?.Rankings.ToDictionary(r => r.TeamName);
        var baseRanks = NationalRankingService.Build(state, teams, profiles).ToDictionary(r => r.TeamName);
        double Talent(Team t) => profiles is not null && profiles.TryGetValue(t.Name, out var p) ? (p.OffenseRating + p.DefenseRating) / 2 : 60 + t.Prestige * .3;
        double Resume(Team t)
        {
            var slate = records[t.Name];
            if (slate.Length == 0) return 0;
            var wins = slate.Count(g => g.WinnerTeamName == t.Name);
            var opponentStrength = slate.Average(g => WinPct(g.HomeTeamName == t.Name ? g.AwayTeamName : g.HomeTeamName));
            var margin = slate.Average(g => Math.Clamp((g.HomeTeamName == t.Name ? g.HomeScore!.Value - g.AwayScore!.Value : g.AwayScore!.Value - g.HomeScore!.Value), -21, 21));
            // Results and opponents' results dominate; margin has diminishing returns, no reward for running up scores.
            return (wins / (double)slate.Length - .5) * 110 + (opponentStrength - .5) * 35 + wins * 3 - (slate.Length - wins) * 4 + margin * .35;
        }
        var scores = teams.Values.ToDictionary(t => t.Name, t =>
        {
            var count = records[t.Name].Length;
            if (kind == PollKind.Computer) return Resume(t);
            var preseasonWeight = Math.Max(.05, 1 - count / 8.0);
            var conference = teams.Values.Where(x => x.ConferenceName == t.ConferenceName).Average(x => (double)x.Prestige);
            var expectation = Talent(t) * .65 + ProgramPrestigeService.GetCurrentPrestige(state, t) * .25 + conference * .1;
            var inertia = previous is not null && previous.TryGetValue(t.Name, out var old) ? Math.Max(0, 26 - old.Rank) * (kind == PollKind.Coaches ? .22 : .12) : 0;
            return Resume(t) + expectation * preseasonWeight + inertia;
        }, StringComparer.OrdinalIgnoreCase);
        var points = teams.Keys.ToDictionary(n => n, _ => 0d, StringComparer.OrdinalIgnoreCase);
        if (kind != PollKind.Computer)
        {
            var voters = kind == PollKind.Media ? 65 : 60;
            for (var voter = 0; voter < voters; voter++)
            {
                var ballot = teams.Values.OrderByDescending(t => scores[t.Name] +
                    (SimulationSeed.Create(state.DynastyId, state.SeasonYear, voter, t.Name, kind.ToString()) % 1001) / 100.0 - 5)
                    .ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase).Take(25).ToArray();
                for (var i = 0; i < ballot.Length; i++) points[ballot[i].Name] += 25 - i;
            }
        }
        else points = scores;
        var champion = state.NationalChampionshipHistory.FirstOrDefault(c => c.SeasonYear == state.SeasonYear)?.ChampionTeamName;
        return teams.Values.OrderByDescending(t => t.Name == champion).ThenByDescending(t => points[t.Name]).ThenByDescending(t => scores[t.Name])
            .ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase).Select((t, i) => baseRanks[t.Name] with { Rank = i + 1, Score = points[t.Name], Wins = records[t.Name].Count(g => g.WinnerTeamName == t.Name), Losses = records[t.Name].Count(g => g.WinnerTeamName != t.Name) }).ToArray();
    }

    public static DynastyState Record(DynastyState state, IReadOnlyDictionary<string, Team> teams, IReadOnlyDictionary<string, TeamSimulationProfile>? profiles = null)
    {
        var history = state.PollHistory.ToList();
        foreach (var kind in Enum.GetValues<PollKind>())
        {
            if (history.Any(p => p.SeasonYear == state.SeasonYear && p.Week == state.Week && p.Phase == state.Phase && p.Kind == kind)) continue;
            history.Add(new() { SeasonYear = state.SeasonYear, Week = state.Week, Phase = state.Phase, Kind = kind, Rankings = Build(state, teams, kind, profiles) });
        }
        return state with { PollHistory = history.ToArray() };
    }

    public static string RankedName(string name, IReadOnlyList<NationalRanking> rankings)
    {
        var rank = rankings.FirstOrDefault(r => r.TeamName.Equals(name, StringComparison.OrdinalIgnoreCase))?.Rank;
        return rank is > 0 and <= 25 ? $"#{rank} {name}" : name;
    }
}
