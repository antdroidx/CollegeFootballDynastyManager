using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

/// <summary>Simulated media/coaches ballots; these are dynasty polls, not licensed real-world polls.</summary>
public static class CollegePollService
{
    /// <summary>Read the last released poll. Finishing an individual game never publishes a poll.</summary>
    public static IReadOnlyList<NationalRanking> GetPublished(DynastyState state, IReadOnlyDictionary<string, Team> teams,
        PollKind kind = PollKind.Media, IReadOnlyDictionary<string, TeamSimulationProfile>? profiles = null)
    {
        var snapshot = state.PollHistory.LastOrDefault(p => p.SeasonYear == state.SeasonYear && p.Kind == kind);
        if (snapshot is not null) return snapshot.Rankings;
        // Saves without snapshots must not include any partially completed current week.
        return Build(state with { Schedule = state.Schedule.Where(g => g.Week < state.Week).ToArray() }, teams, kind, profiles);
    }

    /// <summary>Anchor missing polls on load, excluding this week's games, and retain them in saves.</summary>
    public static DynastyState EnsurePublished(DynastyState state, IReadOnlyDictionary<string, Team> teams,
        IReadOnlyDictionary<string, TeamSimulationProfile>? profiles = null)
    {
        var missing = Enum.GetValues<PollKind>().Where(kind => !state.PollHistory.Any(p =>
            p.SeasonYear == state.SeasonYear && p.Kind == kind)).ToArray();
        if (missing.Length == 0) return state;
        return state with { PollHistory = state.PollHistory.Concat(missing.Select(kind => new PollSnapshot
        {
            SeasonYear = state.SeasonYear, Week = Math.Max(0, state.Week - 1),
            Phase = state.Week == 0 ? SeasonPhase.Preseason : state.Phase,
            Kind = kind, Rankings = GetPublished(state, teams, kind, profiles)
        })).ToArray() };
    }

    /// <summary>Compute a candidate poll after all games for a week have been resolved; UI uses GetPublished.</summary>
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
        var prestige = teams.Values.ToDictionary(t => t.Name, t => (double)ProgramPrestigeService.GetCurrentPrestige(state, t));
        double History(Team t)
        {
            var seasons = state.TeamSeasonHistory.Where(h => h.TeamName == t.Name && h.SeasonYear < state.SeasonYear)
                .OrderByDescending(h => h.SeasonYear).Take(3).ToArray();
            if (seasons.Length == 0) return prestige[t.Name];
            var weights = seasons.Select(h => 1.0 / (state.SeasonYear - h.SeasonYear)).ToArray();
            return seasons.Select((h, i) => (45 + 40.0 * h.Wins / Math.Max(1, h.Wins + h.Losses) +
                (h.FinalRanking is > 0 and <= 25 ? (26 - h.FinalRanking) * .6 : 0)) * weights[i]).Sum() / weights.Sum();
        }
        var expectations = teams.Values.ToDictionary(t => t.Name, t =>
            Talent(t) * .45 + prestige[t.Name] * .25 + History(t) * .15 +
            teams.Values.Where(x => x.ConferenceName == t.ConferenceName).Average(x => prestige[x.Name]) * .15);
        var scores = teams.Values.ToDictionary(t => t.Name, t =>
        {
            var count = records[t.Name].Length;
            if (kind == PollKind.Computer) return Resume(t);
            // A one-game resume is evidence, not a full season. By six games results carry full weight;
            // expectations gradually recede over eight games, so byes do not count as losses.
            var evidence = Math.Min(1, count / 6.0);
            var preseasonWeight = Math.Max(.08, 1 - count / 8.0);
            var expectation = (expectations[t.Name] - 70) * 1.6 * preseasonWeight;
            var inertia = previous is not null && previous.TryGetValue(t.Name, out var old)
                ? Math.Max(0, 26 - old.Rank) * (kind == PollKind.Coaches ? .40 : .25) : 0;
            // Distinguish a win over a strong program from an early win over an FCS opponent,
            // before opponents have accumulated meaningful win/loss samples of their own.
            var quality = count == 0 ? 0 : records[t.Name].Average(g =>
            {
                var opponent = g.HomeTeamName == t.Name ? g.AwayTeamName : g.HomeTeamName;
                var strength = expectations.GetValueOrDefault(opponent, 40);
                return (strength - 70) * .45;
            }) * Math.Min(1, count / 3.0) * (1 - evidence);
            return Resume(t) * evidence + expectation + inertia + quality;
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
