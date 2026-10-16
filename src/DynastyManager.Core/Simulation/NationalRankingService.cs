using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

/// <summary>
/// Deterministic national poll derived from the legacy game's poll ingredients:
/// preseason roster/prestige bias, offensive and defensive performance ranks,
/// strength of wins, strength of losses, and conference-champion bonus.
/// </summary>
public static class NationalRankingService
{
    public static IReadOnlyList<NationalRanking> Build(
        DynastyState state,
        IReadOnlyDictionary<string, Team> teamsByName,
        IReadOnlyDictionary<string, TeamSimulationProfile>? profilesByTeam = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(teamsByName);

        var teams = teamsByName.Values
            .OrderBy(team => team.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (teams.Length == 0)
            return Array.Empty<NationalRanking>();

        var conferencePrestige = teams
            .GroupBy(team => team.ConferenceName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Average(team =>
                    ProgramPrestigeService.GetCurrentPrestige(state, team)),
                StringComparer.OrdinalIgnoreCase);

        var accumulators = teams.ToDictionary(
            team => team.Name,
            team => new Accumulator(
                team,
                GetPreseasonScore(
                    state,
                    team,
                    teamsByName,
                    profilesByTeam,
                    conferencePrestige)),
            StringComparer.OrdinalIgnoreCase);

        foreach (var game in state.Schedule.Where(game =>
                     game.SeasonYear == state.SeasonYear &&
                     game.HasPlayed &&
                     game.HomeScore is not null &&
                     game.AwayScore is not null))
        {
            if (!accumulators.TryGetValue(game.HomeTeamName, out var home) ||
                !accumulators.TryGetValue(game.AwayTeamName, out var away))
            {
                continue;
            }

            var homeScore = game.HomeScore!.Value;
            var awayScore = game.AwayScore!.Value;

            RecordGame(
                home,
                away.Team.Name,
                homeScore,
                awayScore,
                game.HomeStats,
                game.AwayStats);

            RecordGame(
                away,
                home.Team.Name,
                awayScore,
                homeScore,
                game.AwayStats,
                game.HomeStats);
        }

        var preseasonOrder = accumulators.Values
            .OrderByDescending(value => value.PreseasonScore)
            .ThenByDescending(value =>
                ProgramPrestigeService.GetCurrentPrestige(state, value.Team))
            .ThenBy(value => value.Team.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var preseasonRanks = preseasonOrder
            .Select((value, index) => (value.Team.Name, Rank: index + 1))
            .ToDictionary(
                item => item.Name,
                item => item.Rank,
                StringComparer.OrdinalIgnoreCase);

        var pointsRanks = RankByMetric(
            accumulators.Values,
            value => value.GamesPlayed == 0
                ? double.NegativeInfinity
                : value.PointsFor / (double)value.GamesPlayed,
            descending: true);

        var yardsRanks = RankByMetric(
            accumulators.Values,
            value => value.GamesWithStats == 0
                ? double.NegativeInfinity
                : value.YardsFor / (double)value.GamesWithStats,
            descending: true);

        var opponentPointsRanks = RankByMetric(
            accumulators.Values,
            value => value.GamesPlayed == 0
                ? double.PositiveInfinity
                : value.PointsAgainst / (double)value.GamesPlayed,
            descending: false);

        var opponentYardsRanks = RankByMetric(
            accumulators.Values,
            value => value.GamesWithStats == 0
                ? double.PositiveInfinity
                : value.YardsAgainst / (double)value.GamesWithStats,
            descending: false);

        var championTeams = state.ConferenceChampionshipHistory
            .Where(record => record.SeasonYear == state.SeasonYear)
            .Select(record => record.ChampionTeamName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var scored = new List<ScoredTeam>(teams.Length);

        foreach (var value in accumulators.Values)
        {
            double score;

            if (value.GamesPlayed == 0)
            {
                score = value.PreseasonScore;
            }
            else
            {
                var teamCount = teams.Length;

                var offenseRating =
                    (teamCount - pointsRanks[value.Team.Name]) +
                    (teamCount - yardsRanks[value.Team.Name]);

                var defenseRating =
                    (teamCount - opponentPointsRanks[value.Team.Name]) +
                    (teamCount - opponentYardsRanks[value.Team.Name]);

                var strengthOfWins = value.WinsAgainst.Sum(opponent =>
                    5 + (teamCount - preseasonRanks[opponent]));

                var strengthOfLosses = value.LossesAgainst.Sum(opponent =>
                    preseasonRanks[opponent]);

                var preseasonBias =
                    Math.Max(3.0, 15.0 - value.GamesPlayed) / 15.0;

                score =
                    preseasonBias * value.PreseasonScore +
                    offenseRating +
                    defenseRating +
                    strengthOfWins -
                    strengthOfLosses +
                    500.0;
            }

            if (championTeams.Contains(value.Team.Name))
                score += 20.0;

            scored.Add(new ScoredTeam(value, score));
        }

        var rankings = scored
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.Value.Wins)
            .ThenBy(item => item.Value.Losses)
            .ThenByDescending(item =>
                ProgramPrestigeService.GetCurrentPrestige(state, item.Value.Team))
            .ThenBy(item => item.Value.Team.Name, StringComparer.OrdinalIgnoreCase)
            .Select((item, index) => new NationalRanking
            {
                Rank = index + 1,
                TeamName = item.Value.Team.Name,
                ConferenceName = item.Value.Team.ConferenceName,
                Wins = item.Value.Wins,
                Losses = item.Value.Losses,
                Score = item.Score,
                PointsPerGame = item.Value.GamesPlayed == 0
                    ? 0
                    : item.Value.PointsFor / (double)item.Value.GamesPlayed,
                OpponentPointsPerGame = item.Value.GamesPlayed == 0
                    ? 0
                    : item.Value.PointsAgainst / (double)item.Value.GamesPlayed
            })
            .ToArray();

        return ApplyFinalChampionshipPlacement(state, rankings);
    }

    private static IReadOnlyList<NationalRanking> ApplyFinalChampionshipPlacement(
        DynastyState state,
        IReadOnlyList<NationalRanking> rankings)
    {
        var championship = state.NationalChampionshipHistory
            .FirstOrDefault(record =>
                record.SeasonYear == state.SeasonYear);

        if (championship is null)
            return rankings;

        var champion = rankings.FirstOrDefault(ranking =>
            ranking.TeamName.Equals(
                championship.ChampionTeamName,
                StringComparison.OrdinalIgnoreCase));

        var runnerUp = rankings.FirstOrDefault(ranking =>
            ranking.TeamName.Equals(
                championship.RunnerUpTeamName,
                StringComparison.OrdinalIgnoreCase));

        if (champion is null || runnerUp is null)
            return rankings;

        var reordered = new List<NationalRanking>(rankings.Count)
        {
            champion,
            runnerUp
        };

        reordered.AddRange(rankings.Where(ranking =>
            !ranking.TeamName.Equals(
                championship.ChampionTeamName,
                StringComparison.OrdinalIgnoreCase) &&
            !ranking.TeamName.Equals(
                championship.RunnerUpTeamName,
                StringComparison.OrdinalIgnoreCase)));

        return reordered
            .Select((ranking, index) => ranking with
            {
                Rank = index + 1
            })
            .ToArray();
    }

    private static void RecordGame(
        Accumulator team,
        string opponentName,
        int pointsFor,
        int pointsAgainst,
        GameTeamStats? stats,
        GameTeamStats? opponentStats)
    {
        team.PointsFor += pointsFor;
        team.PointsAgainst += pointsAgainst;

        if (stats is not null && opponentStats is not null)
        {
            team.YardsFor += stats.TotalYards;
            team.YardsAgainst += opponentStats.TotalYards;
            team.GamesWithStats++;
        }

        if (pointsFor > pointsAgainst)
        {
            team.Wins++;
            team.WinsAgainst.Add(opponentName);
        }
        else
        {
            team.Losses++;
            team.LossesAgainst.Add(opponentName);
        }
    }

    private static Dictionary<string, int> RankByMetric(
        IEnumerable<Accumulator> values,
        Func<Accumulator, double> selector,
        bool descending)
    {
        var ordered = descending
            ? values
                .OrderByDescending(selector)
                .ThenBy(value => value.Team.Name, StringComparer.OrdinalIgnoreCase)
            : values
                .OrderBy(selector)
                .ThenBy(value => value.Team.Name, StringComparer.OrdinalIgnoreCase);

        return ordered
            .Select((value, index) => (value.Team.Name, Rank: index + 1))
            .ToDictionary(
                item => item.Name,
                item => item.Rank,
                StringComparer.OrdinalIgnoreCase);
    }

    private static double GetPreseasonScore(
        DynastyState state,
        Team team,
        IReadOnlyDictionary<string, Team> teamsByName,
        IReadOnlyDictionary<string, TeamSimulationProfile>? profilesByTeam,
        IReadOnlyDictionary<string, double> conferencePrestige)
    {
        TeamSimulationProfile? profile = null;
        profilesByTeam?.TryGetValue(team.Name, out profile);

        var prestigeValue = ProgramPrestigeService.GetCurrentPrestige(state, team);
        var fallbackRating = Math.Clamp(
            60.0 + (prestigeValue - 50) * 0.5,
            55.0,
            95.0);

        var offenseTalent = profile?.OffenseRating ?? fallbackRating;
        var defenseTalent = profile?.DefenseRating ?? fallbackRating;

        var conferenceTeamCount = teamsByName.Values.Count(candidate =>
            candidate.ConferenceName.Equals(
                team.ConferenceName,
                StringComparison.OrdinalIgnoreCase));

        var conferenceComponent =
            conferenceTeamCount < ConferenceChampionshipService.MinimumConferenceTeams
                ? prestigeValue / 1.2
                : conferencePrestige.TryGetValue(
                    team.ConferenceName,
                    out var prestige)
                    ? prestige
                    : prestigeValue;

        return offenseTalent +
               defenseTalent +
               3.0 * prestigeValue +
               conferenceComponent;
    }

    private sealed class Accumulator
    {
        public Accumulator(Team team, double preseasonScore)
        {
            Team = team;
            PreseasonScore = preseasonScore;
        }

        public Team Team { get; }
        public double PreseasonScore { get; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int PointsFor { get; set; }
        public int PointsAgainst { get; set; }
        public int YardsFor { get; set; }
        public int YardsAgainst { get; set; }
        public int GamesWithStats { get; set; }
        public List<string> WinsAgainst { get; } = new();
        public List<string> LossesAgainst { get; } = new();
        public int GamesPlayed => Wins + Losses;
    }

    private sealed record ScoredTeam(
        Accumulator Value,
        double Score);
}
