using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class DynastyHistoryService
{
    public static DynastyState FinalizeSeason(
        DynastyState state,
        IReadOnlyDictionary<string, Team> teamsByName,
        IReadOnlyDictionary<string, TeamSimulationProfile>? profilesByTeam = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(teamsByName);

        if (state.TeamSeasonHistory.Any(item => item.SeasonYear == state.SeasonYear))
            return state;

        var rankings = NationalRankingService.Build(state, teamsByName, profilesByTeam)
            .ToDictionary(item => item.TeamName, item => item.Rank, StringComparer.OrdinalIgnoreCase);
        var teamHistory = state.TeamSeasonHistory.ToList();

        foreach (var team in teamsByName.Values)
        {
            var games = state.Schedule.Where(game =>
                    game.SeasonYear == state.SeasonYear &&
                    game.HasPlayed &&
                    game.InvolvesTeam(team.Name))
                .ToArray();
            var wins = games.Count(game =>
                game.WinnerTeamName?.Equals(team.Name, StringComparison.OrdinalIgnoreCase) == true);
            var conferenceGames = games.Where(game =>
                game.GameType is ScheduledGameType.Conference or ScheduledGameType.ConferenceChampionship)
                .ToArray();
            var conferenceWins = conferenceGames.Count(game =>
                game.WinnerTeamName?.Equals(team.Name, StringComparison.OrdinalIgnoreCase) == true);
            var bowl = state.BowlHistory
                .Where(item => item.SeasonYear == state.SeasonYear &&
                    (item.WinnerTeamName.Equals(team.Name, StringComparison.OrdinalIgnoreCase) ||
                     item.LoserTeamName.Equals(team.Name, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(item => item.BowlName)
                .FirstOrDefault();
            var recruitingSeason = state.RecruitingCommitments
                .Where(item =>
                    item.SeasonYear < state.SeasonYear &&
                    item.TeamName.Equals(team.Name, StringComparison.OrdinalIgnoreCase))
                .Select(item => item.SeasonYear)
                .DefaultIfEmpty(state.SeasonYear)
                .Max();
            var commitments = state.RecruitingCommitments.Where(item =>
                    item.SeasonYear == recruitingSeason &&
                    item.TeamName.Equals(team.Name, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            teamHistory.Add(new TeamSeasonHistoryRecord
            {
                SeasonYear = state.SeasonYear,
                TeamName = team.Name,
                Wins = wins,
                Losses = games.Length - wins,
                ConferenceWins = conferenceWins,
                ConferenceLosses = conferenceGames.Length - conferenceWins,
                FinalRanking = rankings.TryGetValue(team.Name, out var rank) ? rank : 0,
                ConferenceChampion = state.ConferenceChampionshipHistory.Any(item =>
                    item.SeasonYear == state.SeasonYear &&
                    item.ChampionTeamName.Equals(team.Name, StringComparison.OrdinalIgnoreCase)),
                PlayoffParticipant = state.CollegeFootballPlayoffHistory.Any(item =>
                    item.SeasonYear == state.SeasonYear &&
                    item.TeamName.Equals(team.Name, StringComparison.OrdinalIgnoreCase)),
                NationalChampion = state.NationalChampionshipHistory.Any(item =>
                    item.SeasonYear == state.SeasonYear &&
                    item.ChampionTeamName.Equals(team.Name, StringComparison.OrdinalIgnoreCase)),
                BowlName = bowl?.BowlName,
                WonBowl = bowl is null
                    ? null
                    : bowl.WinnerTeamName.Equals(team.Name, StringComparison.OrdinalIgnoreCase),
                RecruitingCommitments = commitments.Length,
                RecruitingAverageRating = commitments.Length == 0
                    ? 0
                    : commitments.Average(item => item.OverallRating)
            });
        }

        var playerStats = BuildPlayerStats(state);
        var awards = BuildAwards(state, playerStats);

        return state with
        {
            TeamSeasonHistory = teamHistory,
            PlayerSeasonStats = state.PlayerSeasonStats
                .Where(item => item.SeasonYear != state.SeasonYear)
                .Concat(playerStats)
                .ToArray(),
            PlayerAwardHistory = state.PlayerAwardHistory
                .Concat(awards)
                .ToArray()
        };
    }

    private static IReadOnlyList<PlayerSeasonStatLine> BuildPlayerStats(DynastyState state)
    {
        var result = new List<PlayerSeasonStatLine>();

        foreach (var teamGroup in state.ActiveRoster.GroupBy(
                     player => player.TeamName,
                     StringComparer.OrdinalIgnoreCase))
        {
            var games = state.Schedule.Where(game =>
                    game.SeasonYear == state.SeasonYear &&
                    game.HasPlayed &&
                    game.InvolvesTeam(teamGroup.Key))
                .ToArray();
            if (games.Length == 0)
                continue;

            var passAttempts = games.Sum(game =>
                GetTeamStats(game, teamGroup.Key)?.PassAttempts ?? 0);
            var passYards = games.Sum(game =>
                GetTeamStats(game, teamGroup.Key)?.PassYards ?? 0);
            var rushAttempts = games.Sum(game =>
                GetTeamStats(game, teamGroup.Key)?.RushAttempts ?? 0);
            var rushYards = games.Sum(game =>
                GetTeamStats(game, teamGroup.Key)?.RushYards ?? 0);
            var turnovers = games.Sum(game =>
                GetTeamStats(game, teamGroup.Key)?.Turnovers ?? 0);
            var points = games.Sum(game =>
                GetTeamScore(game, teamGroup.Key));
            var touchdowns = Math.Max(0, points / 7);

            var players = teamGroup.Where(player => !player.IsRedshirted).ToArray();
            var qbs = players.Where(player => player.Position == Position.QB)
                .OrderBy(player => player.DepthChartOrder).ThenByDescending(player => player.OverallRating).ToArray();
            var runners = players.Where(player => player.Position is Position.RB or Position.QB)
                .OrderBy(player => player.DepthChartOrder).ThenByDescending(player => player.OverallRating).ToArray();

            if (qbs.Length > 0)
            {
                var qb = qbs[0];
                result.Add(new PlayerSeasonStatLine
                {
                    SeasonYear = state.SeasonYear,
                    PlayerId = qb.PlayerId,
                    PlayerName = qb.FullName,
                    TeamName = qb.TeamName,
                    Position = qb.Position,
                    Games = games.Length,
                    PassAttempts = passAttempts,
                    PassYards = passYards,
                    RushAttempts = rushAttempts / 7,
                    RushYards = rushYards / 10,
                    Touchdowns = Math.Max(0, touchdowns * 3 / 5),
                    Turnovers = Math.Max(0, turnovers * 3 / 4)
                });
            }

            var remainingRushAttempts = Math.Max(0, rushAttempts - rushAttempts / 7);
            var remainingRushYards = Math.Max(0, rushYards - rushYards / 10);
            var backs = runners.Where(player => qbs.Length == 0 || player.PlayerId != qbs[0].PlayerId)
                .Take(3).ToArray();
            var weights = new[] { 60, 27, 13 };
            for (var i = 0; i < backs.Length; i++)
            {
                var player = backs[i];
                result.Add(new PlayerSeasonStatLine
                {
                    SeasonYear = state.SeasonYear,
                    PlayerId = player.PlayerId,
                    PlayerName = player.FullName,
                    TeamName = player.TeamName,
                    Position = player.Position,
                    Games = games.Length,
                    RushAttempts = remainingRushAttempts * weights[i] / 100,
                    RushYards = remainingRushYards * weights[i] / 100,
                    Touchdowns = Math.Max(0, touchdowns * weights[i] / 220),
                    Turnovers = Math.Max(0, turnovers * weights[i] / 300)
                });
            }
        }

        return result;
    }

    private static IReadOnlyList<PlayerAwardRecord> BuildAwards(
        DynastyState state,
        IReadOnlyList<PlayerSeasonStatLine> stats)
    {
        var awards = new List<PlayerAwardRecord>();
        var skill = stats
            .Where(item => item.Position is Position.QB or Position.RB)
            .OrderByDescending(item =>
                item.ScrimmageYards + item.Touchdowns * 120 - item.Turnovers * 80)
            .ToArray();

        if (skill.Length > 0)
        {
            var winner = skill[0];
            awards.Add(new PlayerAwardRecord
            {
                SeasonYear = state.SeasonYear,
                AwardName = "National Player of the Year",
                PlayerId = winner.PlayerId,
                PlayerName = winner.PlayerName,
                TeamName = winner.TeamName,
                Position = winner.Position,
                Summary = $"{winner.ScrimmageYards:N0} yards • {winner.Touchdowns} TD"
            });
        }

        foreach (var position in Enum.GetValues<Position>())
        {
            var candidate = state.ActiveRoster
                .Where(player => !player.IsRedshirted && player.Position == position)
                .OrderByDescending(player => player.OverallRating)
                .ThenByDescending(player => player.PotentialRating)
                .FirstOrDefault();
            if (candidate is null)
                continue;

            awards.Add(new PlayerAwardRecord
            {
                SeasonYear = state.SeasonYear,
                AwardName = "First Team All-American",
                PlayerId = candidate.PlayerId,
                PlayerName = candidate.FullName,
                TeamName = candidate.TeamName,
                Position = candidate.Position,
                IsAllAmerican = true,
                Summary = $"OVR {candidate.OverallRating} • POT {candidate.PotentialRating}"
            });
        }

        return awards;
    }

    private static GameTeamStats? GetTeamStats(ScheduledGame game, string teamName) =>
        game.HomeTeamName.Equals(teamName, StringComparison.OrdinalIgnoreCase)
            ? game.HomeStats
            : game.AwayStats;

    private static int GetTeamScore(ScheduledGame game, string teamName) =>
        game.HomeTeamName.Equals(teamName, StringComparison.OrdinalIgnoreCase)
            ? game.HomeScore ?? 0
            : game.AwayScore ?? 0;
}
