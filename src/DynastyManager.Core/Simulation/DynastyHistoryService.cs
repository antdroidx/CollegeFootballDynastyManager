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
                RecruitingStarsEstimated = previous?.OriginalRecruitingStars is not null ? previous.RecruitingStarsEstimated : player.RecruitingStarsEstimated,
                HeightInches = player.HeightInches, WeightPounds = player.WeightPounds, Appearance = player.Appearance, JerseyNumber = player.JerseyNumber,
                FirstRecordedSeason = previous?.FirstRecordedSeason ?? state.SeasonYear,
                LastRecordedSeason = state.SeasonYear
            };
        }
        var playerStats = BuildCurrentPlayerStats(state);
        var awards = SeasonAwardsService.Build(state, false);

        return CoachCareerService.RecordCompletedSeason(state with
        {
            PlayerGameAwardHistory = PlayerOfGameService.All(state),
            TeamSeasonHistory = teamHistory,
            PlayerCareerHistory = careers.Values.OrderBy(item => item.PlayerId).ToArray(),
            PlayerSeasonStats = state.PlayerSeasonStats
                .Where(item => item.SeasonYear != state.SeasonYear)
                .Concat(playerStats)
                .ToArray(),
            PlayerAwardHistory = state.PlayerAwardHistory
                .Where(a => a.SeasonYear != state.SeasonYear || a.IsPreseason)
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
            var passCompletions = games.Sum(game => GetTeamStats(game, teamGroup.Key)?.PassCompletions ?? 0);
            var passYards = games.Sum(game =>
                GetTeamStats(game, teamGroup.Key)?.PassYards ?? 0);
            var rushAttempts = games.Sum(game =>
                GetTeamStats(game, teamGroup.Key)?.RushAttempts ?? 0);
            var rushYards = games.Sum(game =>
                GetTeamStats(game, teamGroup.Key)?.RushYards ?? 0);
            var turnovers = games.Sum(game =>
                GetTeamStats(game, teamGroup.Key)?.Turnovers ?? 0);
            var passingTouchdowns = games.Sum(game => GetTeamStats(game, teamGroup.Key)?.PassingTouchdowns ?? 0);
            var rushingTouchdowns = games.Sum(game => GetTeamStats(game, teamGroup.Key)?.RushingTouchdowns ?? 0);
            var interceptionsThrown = games.Sum(game => GetTeamStats(game, teamGroup.Key)?.InterceptionsThrown ?? 0);
            var fumblesLost = games.Sum(game => GetTeamStats(game, teamGroup.Key)?.FumblesLost ?? 0);
            var explosivePasses = games.Sum(game => GetTeamStats(game, teamGroup.Key)?.ExplosivePasses20 ?? 0);
            var explosiveRuns = games.Sum(game => GetTeamStats(game, teamGroup.Key)?.ExplosiveRuns20 ?? 0);
            var drops = games.Sum(game => GetTeamStats(game, teamGroup.Key)?.Drops ?? 0);
            var yardsAfterCatch = games.Sum(game => GetTeamStats(game, teamGroup.Key)?.YardsAfterCatch ?? 0);
            var opponentPassAttempts = games.Sum(game =>
                GetOpponentStats(game, teamGroup.Key)?.PassAttempts ?? 0);
            var opponentRushAttempts = games.Sum(game =>
                GetOpponentStats(game, teamGroup.Key)?.RushAttempts ?? 0);
            var opponentTurnovers = games.Sum(game =>
                GetOpponentStats(game, teamGroup.Key)?.Turnovers ?? 0);
            var opponentFumbles = games.Sum(game => GetOpponentStats(game, teamGroup.Key)?.FumblesLost ?? 0);
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
                    OriginalRecruitingStars = qb.OriginalRecruitingStars, RecruitingStarsEstimated = qb.RecruitingStarsEstimated, HeightInches = qb.HeightInches, WeightPounds = qb.WeightPounds, Appearance = qb.Appearance, JerseyNumber = qb.JerseyNumber,
                    PlayerName = qb.FullName,
                    TeamName = qb.TeamName,
                    Position = qb.Position,
                    Games = games.Length,
                    PassAttempts = passAttempts,
                    PassCompletions = passCompletions,
                    PassYards = passYards,
                    PassingTouchdowns = passingTouchdowns,
                    InterceptionsThrown = interceptionsThrown,
                    RushAttempts = rushAttempts / 7,
                    RushYards = rushYards / 10,
                    RushingTouchdowns = Math.Max(0, rushingTouchdowns / 5),
                    RushingFumbles = Math.Max(0, fumblesLost / 5),
                    Rushing20Plus = Math.Max(0, explosiveRuns / 6),
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
                    OriginalRecruitingStars = player.OriginalRecruitingStars, RecruitingStarsEstimated = player.RecruitingStarsEstimated, HeightInches = player.HeightInches, WeightPounds = player.WeightPounds, Appearance = player.Appearance, JerseyNumber = player.JerseyNumber,
                    PlayerName = player.FullName,
                    TeamName = player.TeamName,
                    Position = player.Position,
                    Games = games.Length,
                    RushAttempts = remainingRushAttempts * weights[i] / 100,
                    RushYards = remainingRushYards * weights[i] / 100,
                    RushingTouchdowns = rushingTouchdowns * weights[i] / 100,
                    RushingFumbles = fumblesLost * weights[i] / 100,
                    Rushing20Plus = explosiveRuns * weights[i] / 100,
                    Touchdowns = rushingTouchdowns * weights[i] / 100,
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
                    OriginalRecruitingStars = player.OriginalRecruitingStars, RecruitingStarsEstimated = player.RecruitingStarsEstimated, HeightInches = player.HeightInches, WeightPounds = player.WeightPounds, Appearance = player.Appearance, JerseyNumber = player.JerseyNumber,
                    PlayerName = player.FullName, TeamName = player.TeamName, Position = player.Position,
                    Games = games.Length, Receptions = passCompletions * receivingWeights[i] / 100,
                    ReceivingYards = passYards * receivingWeights[i] / 100,
                    ReceivingTouchdowns = passingTouchdowns * receivingWeights[i] / 100,
                    YardsAfterCatch = yardsAfterCatch * receivingWeights[i] / 100,
                    Drops = drops * receivingWeights[i] / 100,
                    ReceivingFumbles = fumblesLost * receivingWeights[i] / 250,
                    Receiving20Plus = explosivePasses * receivingWeights[i] / 100,
                    Touchdowns = passingTouchdowns * receivingWeights[i] / 100
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
                    OriginalRecruitingStars = player.OriginalRecruitingStars, RecruitingStarsEstimated = player.RecruitingStarsEstimated, HeightInches = player.HeightInches, WeightPounds = player.WeightPounds, Appearance = player.Appearance, JerseyNumber = player.JerseyNumber,
                    PlayerName = player.FullName, TeamName = player.TeamName, Position = player.Position,
                    Games = games.Length, Tackles = estimatedTackles * tackleWeight / 100,
                    TacklesForLoss = Math.Max(0, opponentRushAttempts / 12 * tackleWeight / 100),
                    Sacks = (player.Position is Position.DE or Position.DT or Position.OLB or Position.MLB) && i < 5
                        ? Math.Max(0, estimatedSacks * (5 - i) / 10) : 0,
                    PassesDefended = player.Position is Position.CB or Position.FS or Position.SS
                        ? Math.Max(0, opponentPassAttempts * (8 - Math.Min(i, 7)) / 120) : 0,
                    Interceptions = (player.Position is Position.CB or Position.FS or Position.SS) && i < 7
                        ? Math.Max(0, estimatedInterceptions * (8 - i) / 12) : 0,
                    FumbleRecoveries = i < Math.Min(5, defenders.Length) ? Math.Max(0, opponentFumbles * (5 - i) / 15) : 0
                });
            }

            var kicker = players.Where(player => player.Position == Position.K)
                .OrderBy(player => player.DepthChartOrder).ThenByDescending(player => player.OverallRating).FirstOrDefault();
            if (kicker is not null)
            {
                result.Add(new PlayerSeasonStatLine
                {
                    SeasonYear = state.SeasonYear, PlayerId = kicker.PlayerId,
                    OriginalRecruitingStars = kicker.OriginalRecruitingStars, RecruitingStarsEstimated = kicker.RecruitingStarsEstimated, HeightInches = kicker.HeightInches, WeightPounds = kicker.WeightPounds, Appearance = kicker.Appearance, JerseyNumber = kicker.JerseyNumber, PlayerName = kicker.FullName,
                    TeamName = kicker.TeamName, Position = kicker.Position, Games = games.Length,
                    ExtraPointsMade = games.Sum(game => GetTeamStats(game, teamGroup.Key)?.ExtraPointsMade ?? 0),
                    ExtraPointsAttempted = games.Sum(game => GetTeamStats(game, teamGroup.Key)?.ExtraPointsAttempted ?? 0),
                    FieldGoalsMade = games.Sum(game => GetTeamStats(game, teamGroup.Key)?.FieldGoalsMade ?? 0),
                    FieldGoalsAttempted = games.Sum(game => GetTeamStats(game, teamGroup.Key)?.FieldGoalsAttempted ?? 0),
                    Punts = games.Sum(game => GetTeamStats(game, teamGroup.Key)?.Punts ?? 0),
                    PuntYards = games.Sum(game => GetTeamStats(game, teamGroup.Key)?.PuntYards ?? 0),
                    LongPunt = games.Max(game => GetTeamStats(game, teamGroup.Key)?.LongPunt ?? 0),
                    PuntsInside20 = games.Sum(game => GetTeamStats(game, teamGroup.Key)?.PuntsInside20 ?? 0),
                    PuntsInside10 = games.Sum(game => GetTeamStats(game, teamGroup.Key)?.PuntsInside10 ?? 0)
                });
            }

            var returner = players.Where(player => player.Position is Position.RB or Position.WR or Position.CB)
                .OrderBy(player => player.DepthChartOrder).ThenByDescending(player => player.SpeedRating).FirstOrDefault();
            if (returner is not null)
            {
                var index = result.FindIndex(line => line.SeasonYear == state.SeasonYear && line.PlayerId == returner.PlayerId);
                var baseLine = index >= 0 ? result[index] : new PlayerSeasonStatLine
                {
                    SeasonYear = state.SeasonYear, PlayerId = returner.PlayerId,
                    OriginalRecruitingStars = returner.OriginalRecruitingStars, RecruitingStarsEstimated = returner.RecruitingStarsEstimated, HeightInches = returner.HeightInches, WeightPounds = returner.WeightPounds, Appearance = returner.Appearance, JerseyNumber = returner.JerseyNumber, PlayerName = returner.FullName,
                    TeamName = returner.TeamName, Position = returner.Position, Games = games.Length
                };
                var returnLine = baseLine with
                {
                    KickReturns = games.Sum(game => GetTeamStats(game, teamGroup.Key)?.KickReturns ?? 0),
                    KickReturnYards = games.Sum(game => GetTeamStats(game, teamGroup.Key)?.KickReturnYards ?? 0),
                    KickReturnTouchdowns = games.Sum(game => GetTeamStats(game, teamGroup.Key)?.KickReturnTouchdowns ?? 0),
                    PuntReturns = games.Sum(game => GetTeamStats(game, teamGroup.Key)?.PuntReturns ?? 0),
                    PuntReturnYards = games.Sum(game => GetTeamStats(game, teamGroup.Key)?.PuntReturnYards ?? 0),
                    PuntReturnTouchdowns = games.Sum(game => GetTeamStats(game, teamGroup.Key)?.PuntReturnTouchdowns ?? 0)
                };
                if (index >= 0) result[index] = returnLine; else result.Add(returnLine);
            }

            var recorded = result.Where(line => line.SeasonYear == state.SeasonYear &&
                line.TeamName.Equals(teamGroup.Key, StringComparison.OrdinalIgnoreCase)).Select(line => line.PlayerId).ToHashSet();
            foreach (var player in players.Where(player => !recorded.Contains(player.PlayerId)))
                result.Add(new PlayerSeasonStatLine { SeasonYear = state.SeasonYear, PlayerId = player.PlayerId,
                    OriginalRecruitingStars = player.OriginalRecruitingStars, RecruitingStarsEstimated = player.RecruitingStarsEstimated, HeightInches = player.HeightInches, WeightPounds = player.WeightPounds, Appearance = player.Appearance, JerseyNumber = player.JerseyNumber, PlayerName = player.FullName,
                    TeamName = player.TeamName, Position = player.Position, Games = games.Length });
        }

        var actualRecorded = state.Schedule.Where(game => game.SeasonYear == state.SeasonYear && game.HasPlayed)
            .SelectMany(game => game.PlayerStats).GroupBy(line => line.PlayerId).Select(group =>
                group.Aggregate((total, next) => Sum(total, next))).ToArray();
        if (actualRecorded.Length > 0)
        {
            var ids = actualRecorded.Select(line => line.PlayerId).ToHashSet();
            result.RemoveAll(line => ids.Contains(line.PlayerId));
            result.AddRange(actualRecorded);
        }
        return result;
    }

    private static PlayerSeasonStatLine Sum(PlayerSeasonStatLine a, PlayerSeasonStatLine b) => a with
    {
        Games = a.Games + b.Games, PassAttempts = a.PassAttempts + b.PassAttempts,
        PassCompletions = a.PassCompletions + b.PassCompletions, PassYards = a.PassYards + b.PassYards,
        PassingTouchdowns = a.PassingTouchdowns + b.PassingTouchdowns, InterceptionsThrown = a.InterceptionsThrown + b.InterceptionsThrown,
        RushAttempts = a.RushAttempts + b.RushAttempts, RushYards = a.RushYards + b.RushYards,
        RushingTouchdowns = a.RushingTouchdowns + b.RushingTouchdowns, RushingFumbles = a.RushingFumbles + b.RushingFumbles,
        Rushing20Plus = a.Rushing20Plus + b.Rushing20Plus, Receptions = a.Receptions + b.Receptions,
        ReceivingYards = a.ReceivingYards + b.ReceivingYards, ReceivingTouchdowns = a.ReceivingTouchdowns + b.ReceivingTouchdowns,
        YardsAfterCatch = a.YardsAfterCatch + b.YardsAfterCatch, Drops = a.Drops + b.Drops,
        ReceivingFumbles = a.ReceivingFumbles + b.ReceivingFumbles, Receiving20Plus = a.Receiving20Plus + b.Receiving20Plus,
        Tackles = a.Tackles + b.Tackles, TacklesForLoss = a.TacklesForLoss + b.TacklesForLoss,
        Sacks = a.Sacks + b.Sacks, PassesDefended = a.PassesDefended + b.PassesDefended,
        DefensiveReturnYards = a.DefensiveReturnYards + b.DefensiveReturnYards, DefensiveTouchdowns = a.DefensiveTouchdowns + b.DefensiveTouchdowns,
        Interceptions = a.Interceptions + b.Interceptions, FumbleRecoveries = a.FumbleRecoveries + b.FumbleRecoveries,
        ExtraPointsMade = a.ExtraPointsMade + b.ExtraPointsMade, ExtraPointsAttempted = a.ExtraPointsAttempted + b.ExtraPointsAttempted,
        FieldGoalsMade = a.FieldGoalsMade + b.FieldGoalsMade, FieldGoalsAttempted = a.FieldGoalsAttempted + b.FieldGoalsAttempted,
        Punts = a.Punts + b.Punts, PuntYards = a.PuntYards + b.PuntYards, LongPunt = Math.Max(a.LongPunt, b.LongPunt),
        PuntsInside20 = a.PuntsInside20 + b.PuntsInside20, PuntsInside10 = a.PuntsInside10 + b.PuntsInside10,
        KickReturns = a.KickReturns + b.KickReturns, KickReturnYards = a.KickReturnYards + b.KickReturnYards,
        KickReturnTouchdowns = a.KickReturnTouchdowns + b.KickReturnTouchdowns, PuntReturns = a.PuntReturns + b.PuntReturns,
        PuntReturnYards = a.PuntReturnYards + b.PuntReturnYards, PuntReturnTouchdowns = a.PuntReturnTouchdowns + b.PuntReturnTouchdowns,
        Touchdowns = a.Touchdowns + b.Touchdowns, Turnovers = a.Turnovers + b.Turnovers
    };

    private static GameTeamStats? GetOpponentStats(ScheduledGame game, string teamName) =>
        game.HomeTeamName.Equals(teamName, StringComparison.OrdinalIgnoreCase) ? game.AwayStats : game.HomeStats;

    private static GameTeamStats? GetTeamStats(ScheduledGame game, string teamName) =>
        game.HomeTeamName.Equals(teamName, StringComparison.OrdinalIgnoreCase)
            ? game.HomeStats
            : game.AwayStats;

    private static int GetTeamScore(ScheduledGame game, string teamName) =>
        game.HomeTeamName.Equals(teamName, StringComparison.OrdinalIgnoreCase)
            ? game.HomeScore ?? 0
            : game.AwayScore ?? 0;
}
