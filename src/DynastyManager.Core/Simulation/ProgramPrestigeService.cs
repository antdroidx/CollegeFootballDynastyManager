using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class ProgramPrestigeService
{
    public static int GetCurrentPrestige(DynastyState state, Team team)
    {
        var latest = state.ProgramPrestigeHistory
            .Where(item => item.TeamName.Equals(team.Name, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.SeasonYear)
            .FirstOrDefault();

        return latest?.EndingPrestige ?? Math.Clamp(team.Prestige, 35, 99);
    }

    public static DynastyState EnsureInitialPrestige(
        DynastyState state,
        IEnumerable<Team> teams)
    {
        if (state.ProgramPrestigeHistory.Count > 0)
            return state;

        var snapshots = teams.Select(team => new ProgramPrestigeSnapshot
        {
            SeasonYear = state.SeasonYear - 1,
            TeamName = team.Name,
            StartingPrestige = Math.Clamp(team.Prestige, 35, 99),
            EndingPrestige = Math.Clamp(team.Prestige, 35, 99),
            Reasons = new[] { "Initial program reputation" }
        }).ToArray();

        return state with { ProgramPrestigeHistory = snapshots };
    }

    public static DynastyState ApplySeasonResults(
        DynastyState state,
        IReadOnlyDictionary<string, Team> teamsByName,
        IReadOnlyDictionary<string, TeamSimulationProfile>? profilesByTeam = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(teamsByName);

        if (state.ProgramPrestigeHistory.Any(item =>
                item.SeasonYear == state.SeasonYear))
        {
            return state;
        }

        var rankings = NationalRankingService.Build(state, teamsByName, profilesByTeam)
            .ToDictionary(item => item.TeamName, item => item.Rank, StringComparer.OrdinalIgnoreCase);
        var history = state.ProgramPrestigeHistory.ToList();

        foreach (var team in teamsByName.Values)
        {
            var games = state.Schedule.Where(game =>
                    game.SeasonYear == state.SeasonYear &&
                    game.HasPlayed &&
                    game.InvolvesTeam(team.Name))
                .ToArray();
            var wins = games.Count(game =>
                game.WinnerTeamName?.Equals(team.Name, StringComparison.OrdinalIgnoreCase) == true);
            var losses = games.Length - wins;
            var finalRank = rankings.TryGetValue(team.Name, out var rank) ? rank : 0;
            var wonConference = state.ConferenceChampionshipHistory.Any(item =>
                item.SeasonYear == state.SeasonYear &&
                item.ChampionTeamName.Equals(team.Name, StringComparison.OrdinalIgnoreCase));
            var madePlayoff = state.CollegeFootballPlayoffHistory.Any(item =>
                item.SeasonYear == state.SeasonYear &&
                item.TeamName.Equals(team.Name, StringComparison.OrdinalIgnoreCase));
            var nationalChampion = state.NationalChampionshipHistory.Any(item =>
                item.SeasonYear == state.SeasonYear &&
                item.ChampionTeamName.Equals(team.Name, StringComparison.OrdinalIgnoreCase));

            var commitments = state.RecruitingCommitments.Where(item =>
                    item.SeasonYear == state.SeasonYear &&
                    item.TeamName.Equals(team.Name, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var recruitingScore = commitments.Length == 0
                ? 0
                : (int)Math.Round(commitments.Average(item => item.OverallRating));

            var starting = GetCurrentPrestige(state, team);
            var target = CalculateTarget(
                starting, wins, losses, finalRank, wonConference,
                madePlayoff, nationalChampion, recruitingScore);
            var rawChange = target - starting;
            var change = Math.Clamp(
                (int)Math.Round(rawChange * 0.38, MidpointRounding.AwayFromZero),
                -6,
                7);
            if (nationalChampion)
                change = Math.Max(change, 3);
            else if (madePlayoff)
                change = Math.Max(change, 1);

            var ending = Math.Clamp(starting + change, 35, 99);
            var reasons = BuildReasons(
                wins, losses, finalRank, wonConference,
                madePlayoff, nationalChampion, recruitingScore);

            history.Add(new ProgramPrestigeSnapshot
            {
                SeasonYear = state.SeasonYear,
                TeamName = team.Name,
                StartingPrestige = starting,
                EndingPrestige = ending,
                Wins = wins,
                Losses = losses,
                FinalRanking = finalRank,
                WonConference = wonConference,
                MadePlayoff = madePlayoff,
                WonNationalChampionship = nationalChampion,
                RecruitingClassScore = recruitingScore,
                Reasons = reasons
            });
        }

        return state with { ProgramPrestigeHistory = history };
    }

    private static int CalculateTarget(
        int starting,
        int wins,
        int losses,
        int finalRank,
        bool wonConference,
        bool madePlayoff,
        bool nationalChampion,
        int recruitingScore)
    {
        var games = Math.Max(1, wins + losses);
        var winPct = wins / (double)games;
        var performance = 42 + (int)Math.Round(winPct * 45);

        if (finalRank is > 0 and <= 25)
            performance += Math.Max(1, (26 - finalRank) / 3);
        if (wonConference) performance += 5;
        if (madePlayoff) performance += 7;
        if (nationalChampion) performance += 10;
        if (recruitingScore > 0)
            performance += (recruitingScore - 72) / 5;

        return Math.Clamp(
            (int)Math.Round(starting * 0.55 + performance * 0.45),
            35,
            99);
    }

    private static IReadOnlyList<string> BuildReasons(
        int wins,
        int losses,
        int finalRank,
        bool wonConference,
        bool madePlayoff,
        bool nationalChampion,
        int recruitingScore)
    {
        var reasons = new List<string> { $"{wins}-{losses} season" };
        if (nationalChampion) reasons.Add("National championship");
        else if (madePlayoff) reasons.Add("College Football Playoff appearance");
        if (wonConference) reasons.Add("Conference championship");
        if (finalRank is > 0 and <= 25) reasons.Add($"Finished #{finalRank}");
        if (recruitingScore >= 82) reasons.Add("Strong recruiting class");
        if (wins <= 4) reasons.Add("Sustained losing season");
        return reasons;
    }
}
