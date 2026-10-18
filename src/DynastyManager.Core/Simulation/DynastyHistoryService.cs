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

        var careers = state.PlayerCareerHistory.ToDictionary(item => item.PlayerId);
        foreach (var player in state.ActiveRoster)
        {
            careers.TryGetValue(player.PlayerId, out var previous);
            careers[player.PlayerId] = new PlayerCareerRecord
            {
                PlayerId = player.PlayerId, PlayerName = player.FullName, Position = player.Position,
                OriginalRecruitingStars = previous?.OriginalRecruitingStars ?? player.OriginalRecruitingStars,
                FirstRecordedSeason = previous?.FirstRecordedSeason ?? state.SeasonYear,
                LastRecordedSeason = state.SeasonYear
            };
        }
        var playerStats = BuildCurrentPlayerStats(state);
        var awards = BuildAwards(state, playerStats);

        return CoachCareerService.RecordCompletedSeason(state with
        {
            TeamSeasonHistory = teamHistory,
            PlayerCareerHistory = careers.Values.OrderBy(item => item.PlayerId).ToArray(),
            PlayerSeasonStats = state.PlayerSeasonStats
                .Where(item => item.SeasonYear != state.SeasonYear)
                .Concat(playerStats)
                .ToArray(),
            PlayerAwardHistory = state.PlayerAwardHistory
                .Concat(awards)
                .ToArray()
        });
    }

    public static IReadOnlyList<PlayerSeasonStatLine> BuildCurrentPlayerStats(DynastyState state)
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
            var opponentPassAttempts = games.Sum(game =>
                GetOpponentStats(game, teamGroup.Key)?.PassAttempts ?? 0);
            var opponentRushAttempts = games.Sum(game =>
                GetOpponentStats(game, teamGroup.Key)?.RushAttempts ?? 0);
            var opponentTurnovers = games.Sum(game =>
                GetOpponentStats(game, teamGroup.Key)?.Turnovers ?? 0);
            var points = games.Sum(game =>
                GetTeamScore(game, teamGroup.Key));
            var touchdowns = Math.Max(0, points / 7);

            var players = teamGroup.Where(player => !player.IsRedshirted).ToArray();
            var qbs = players.Where(player => player.Position == Position.QB)
                .OrderBy(player => player.DepthChartOrder).ThenByDescending(player => player.OverallRating).ToArray();
            var runners = players.Where(player => player.Position is Position.RB or Position.QB)
                .OrderBy(player => player.DepthChartOrder).ThenByDescending(player => player.OverallRating).ToArray();
            var receivers = players.Where(player => player.Position is Position.WR or Position.TE)
                .OrderBy(player => player.DepthChartOrder).ThenByDescending(player => player.OverallRating).Take(5).ToArray();

            if (qbs.Length > 0)
            {
                var qb = qbs[0];
                result.Add(new PlayerSeasonStatLine
                {
                    SeasonYear = state.SeasonYear,
                    PlayerId = qb.PlayerId,
                    OriginalRecruitingStars = qb.OriginalRecruitingStars,
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
                    OriginalRecruitingStars = player.OriginalRecruitingStars,
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

            var receivingWeights = new[] { 35, 25, 18, 13, 9 };
            for (var i = 0; i < receivers.Length; i++)
            {
                var player = receivers[i];
                result.Add(new PlayerSeasonStatLine
                {
                    SeasonYear = state.SeasonYear, PlayerId = player.PlayerId,
                    OriginalRecruitingStars = player.OriginalRecruitingStars,
                    PlayerName = player.FullName, TeamName = player.TeamName, Position = player.Position,
                    Games = games.Length, Receptions = passAttempts * receivingWeights[i] / 100,
                    ReceivingYards = passYards * receivingWeights[i] / 100,
                    Touchdowns = Math.Max(0, touchdowns * 3 * receivingWeights[i] / 500)
                });
            }

            var defenders = players.Where(player => player.Position is Position.DE or Position.DT or Position.OLB or
                    Position.MLB or Position.CB or Position.FS or Position.SS)
                .OrderBy(player => player.DepthChartOrder).ThenByDescending(player => player.OverallRating).Take(11).ToArray();
            var estimatedTackles = Math.Max(0, opponentRushAttempts + opponentPassAttempts * 2 / 3);
            var estimatedSacks = Math.Max(0, opponentPassAttempts / 14);
            var estimatedInterceptions = Math.Max(0, opponentTurnovers / 2);
            for (var i = 0; i < defenders.Length; i++)
            {
                var player = defenders[i];
                var tackleWeight = Math.Max(4, 16 - i);
                result.Add(new PlayerSeasonStatLine
                {
                    SeasonYear = state.SeasonYear, PlayerId = player.PlayerId,
                    OriginalRecruitingStars = player.OriginalRecruitingStars,
                    PlayerName = player.FullName, TeamName = player.TeamName, Position = player.Position,
                    Games = games.Length, Tackles = estimatedTackles * tackleWeight / 100,
                    Sacks = player.Position is Position.DE or Position.DT or Position.OLB or Position.MLB && i < 5
                        ? Math.Max(0, estimatedSacks * (5 - i) / 10) : 0,
                    Interceptions = player.Position is Position.CB or Position.FS or Position.SS && i < 7
                        ? Math.Max(0, estimatedInterceptions * (8 - i) / 12) : 0
                });
            }

            var recorded = result.Where(line => line.SeasonYear == state.SeasonYear &&
                line.TeamName.Equals(teamGroup.Key, StringComparison.OrdinalIgnoreCase)).Select(line => line.PlayerId).ToHashSet();
            foreach (var player in players.Where(player => !recorded.Contains(player.PlayerId)))
                result.Add(new PlayerSeasonStatLine { SeasonYear = state.SeasonYear, PlayerId = player.PlayerId,
                    OriginalRecruitingStars = player.OriginalRecruitingStars, PlayerName = player.FullName,
                    TeamName = player.TeamName, Position = player.Position, Games = games.Length });
        }

        return result;
    }

    private static GameTeamStats? GetOpponentStats(ScheduledGame game, string teamName) =>
        game.HomeTeamName.Equals(teamName, StringComparison.OrdinalIgnoreCase) ? game.AwayStats : game.HomeStats;

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
