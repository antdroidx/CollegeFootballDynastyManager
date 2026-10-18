using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class GameDayEngine
{
    public static GameDaySession Start(ScheduledGame game, TeamSimulationProfile home,
        TeamSimulationProfile away, IReadOnlyList<DynastyPlayer> roster, GameDayMode mode)
    {
        var homeRoster = roster.Where(player =>
            player.TeamName.Equals(game.HomeTeamName, StringComparison.OrdinalIgnoreCase)).ToArray();
        var awayRoster = roster.Where(player =>
            player.TeamName.Equals(game.AwayTeamName, StringComparison.OrdinalIgnoreCase)).ToArray();
        var gameRoster = homeRoster.Concat(awayRoster).ToArray();
        return new GameDaySession
        {
            Game = game, HomeProfile = home, AwayProfile = away, Roster = gameRoster,
            HomeRoster = homeRoster, AwayRoster = awayRoster,
            PlayersById = gameRoster.ToDictionary(player => player.PlayerId),
            HomeDepthChart = PersonnelRotationService.BuildDepthChart(homeRoster, game.HomeTeamName),
            AwayDepthChart = PersonnelRotationService.BuildDepthChart(awayRoster, game.AwayTeamName),
            Mode = mode, Random = new Random(game.SimulationSeed), PossessionTeamName = game.HomeTeamName
        };
    }

    public static GameSnapEvent Step(GameDaySession session, string? userPlayId = null)
    {
        if (session.IsComplete) throw new InvalidOperationException("The game is complete.");
        var homeOnOffense = session.PossessionTeamName.Equals(session.Game.HomeTeamName, StringComparison.OrdinalIgnoreCase);
        var offense = homeOnOffense ? session.HomeProfile : session.AwayProfile;
        var defense = homeOnOffense ? session.AwayProfile : session.HomeProfile;
        var offensePlan = GamePlanService.Normalize(offense.GamePlan ?? new TeamGamePlan { TeamName = offense.TeamName });
        var defensePlan = GamePlanService.Normalize(defense.GamePlan ?? new TeamGamePlan { TeamName = defense.TeamName });
        if (!string.IsNullOrWhiteSpace(userPlayId))
            offensePlan = offensePlan with { OpeningPlayIds = [userPlayId] };
        var offensivePossessionIndex = session.Events.Count;
        var call = GamePlanService.ResolvePlayCall(offensePlan, defensePlan,
            string.IsNullOrWhiteSpace(userPlayId) ? offensivePossessionIndex : 0, session.Random,
            (offense.HeadCoachGameManagement + offense.OffensiveCoordinatorGameManagement) / 2,
            (defense.HeadCoachGameManagement + defense.DefensiveCoordinatorGameManagement) / 2);

        // A coach can choose a kick on fourth down before a normal offensive snap.
        // CPU decisions are situational rather than always running the same play.
        var special = userPlayId is "special-punt" or "special-field-goal"
            ? userPlayId
            : session.Down == 4 && string.IsNullOrWhiteSpace(userPlayId)
                ? (session.YardLine >= 65 ? "special-field-goal" : "special-punt")
                : null;
        if (special is not null)
        {
            var kickStart = session.YardLine;
            var kickDistance = 100 - kickStart + 17;
            var made = special == "special-field-goal" &&
                session.Random.NextDouble() < Math.Clamp(.92 - Math.Max(0, kickDistance - 30) * .018, .12, .95);
            var kick = new GameSnapEvent
            {
                Sequence = session.Events.Count + 1, PossessionNumber = session.PossessionNumber,
                Quarter = session.Quarter, ClockSeconds = session.ClockSeconds,
                OffenseTeamName = offense.TeamName, DefenseTeamName = defense.TeamName,
                OffensivePlayId = call.Offense.Id, DefensivePlayId = call.Defense.Id, CallSource = call.Source,
                Down = session.Down, Distance = session.Distance, StartYardLine = kickStart, Yards = 0,
                Outcome = special == "special-punt" ? SnapOutcome.Punt :
                    made ? SnapOutcome.FieldGoal : SnapOutcome.MissedFieldGoal
            };
            session.Events.Add(kick);
            session.ClockSeconds -= session.Random.Next(5, 13);
            if (made) { if (homeOnOffense) session.HomeScore += 3; else session.AwayScore += 3; }
            var newSpot = made ? 25 : special == "special-punt"
                ? Math.Clamp(100 - Math.Min(95, kickStart + session.Random.Next(34, 51)), 5, 80)
                : Math.Clamp(100 - Math.Max(20, kickStart - 7), 10, 80);
            ChangePossession(session, newSpot);
            AdvanceQuarter(session);
            return kick;
        }

        var offenseFatigue = homeOnOffense ? session.HomeFatigue : session.AwayFatigue;
        var defenseFatigue = homeOnOffense ? session.AwayFatigue : session.HomeFatigue;
        var offenseRoster = homeOnOffense ? session.HomeRoster : session.AwayRoster;
        var defenseRoster = homeOnOffense ? session.AwayRoster : session.HomeRoster;
        var offenseDepthChart = homeOnOffense ? session.HomeDepthChart : session.AwayDepthChart;
        var defenseDepthChart = homeOnOffense ? session.AwayDepthChart : session.HomeDepthChart;
        var offensePersonnel = PersonnelRotationService.SelectOffense(offenseDepthChart, offense.TeamName,
            call.Offense.Personnel, offenseFatigue, session.Random);
        var defensePersonnel = PersonnelRotationService.SelectDefense(defenseDepthChart, defense.TeamName,
            call.Defense.Personnel, defenseFatigue, session.Random);
        var offensePlayers = Players(session, offensePersonnel);
        var defensePlayers = Players(session, defensePersonnel);

        var isPass = call.Offense.Concept is OffensiveConcept.QuickPass or OffensiveConcept.PlayAction or OffensiveConcept.DeepPass;
        var advantage = isPass ? offense.PassOffenseRating - defense.PassDefenseRating : offense.RushOffenseRating - defense.RushDefenseRating;
        advantage += MatchupShift(call.Offense.Concept, call.Defense.Approach);
        var turnover = session.Random.NextDouble() < Math.Clamp(.025 - advantage * .0008, .008, .065);
        var sack = isPass && !turnover && session.Random.NextDouble() < Math.Clamp(.065 - advantage * .001, .02, .14);
        var complete = isPass && !turnover && !sack && session.Random.NextDouble() < Math.Clamp(.61 + advantage * .004 +
            (call.Offense.Concept == OffensiveConcept.QuickPass ? .07 : call.Offense.Concept == OffensiveConcept.DeepPass ? -.11 : 0), .38, .82);
        var yards = turnover ? 0 : sack ? -session.Random.Next(3, 11) : isPass
            ? complete ? Math.Max(-2, (int)Math.Round(session.Random.Next(3, 16) + advantage * .12 +
                (call.Offense.Concept == OffensiveConcept.DeepPass ? session.Random.Next(5, 24) : 0))) : 0
            : Math.Max(-4, (int)Math.Round(session.Random.Next(-2, 10) + advantage * .09));

        var qb = offensePlayers.FirstOrDefault(player => player.Position == Position.QB);
        var carrier = isPass ? null : Pick(offensePlayers.Where(player => player.Position == Position.RB).ToArray(), session.Random) ?? qb;
        var target = isPass ? Pick(offensePlayers.Where(player => player.Position is Position.WR or Position.TE or Position.RB).ToArray(), session.Random) : null;
        var defender = Pick(defensePlayers.Where(player => player.Position != Position.K).ToArray(), session.Random);
        var start = session.YardLine;
        var touchdown = !turnover && start + yards >= 100;
        var outcome = turnover ? SnapOutcome.Turnover : sack ? SnapOutcome.Sack : touchdown ? SnapOutcome.Touchdown :
            isPass ? complete ? SnapOutcome.Completion : SnapOutcome.Incompletion : SnapOutcome.Run;
        var snap = new GameSnapEvent
        {
            Sequence = session.Events.Count + 1, PossessionNumber = session.PossessionNumber,
            Quarter = session.Quarter, ClockSeconds = session.ClockSeconds,
            OffenseTeamName = offense.TeamName, DefenseTeamName = defense.TeamName,
            OffensivePlayId = call.Offense.Id, DefensivePlayId = call.Defense.Id, CallSource = call.Source,
            Down = session.Down, Distance = session.Distance, StartYardLine = start, Yards = yards, Outcome = outcome,
            QuarterbackId = qb?.PlayerId, BallCarrierId = carrier?.PlayerId, TargetId = target?.PlayerId,
            PrimaryDefenderId = defender?.PlayerId, OffensePersonnel = offensePersonnel.PlayerIds,
            DefensePersonnel = defensePersonnel.PlayerIds
        };
        session.Events.Add(snap);
        PersonnelRotationService.ApplySnap(offenseFatigue, offenseRoster, offensePersonnel, Math.Abs(yards) >= 20);
        PersonnelRotationService.ApplySnap(defenseFatigue, defenseRoster, defensePersonnel, Math.Abs(yards) >= 20);
        AdvanceState(session, homeOnOffense, yards, turnover, touchdown);
        return session.Events[^1];
    }

    public static void SimulateToEnd(GameDaySession session)
    {
        while (!session.IsComplete) Step(session);
    }

    public static ScheduledGame Finish(GameDaySession session)
    {
        if (!session.IsComplete) throw new InvalidOperationException("The game is not complete.");
        var homeStats = BuildStats(session, session.Game.HomeTeamName);
        var awayStats = BuildStats(session, session.Game.AwayTeamName);
        return session.Game with
        {
            HasPlayed = true, HomeScore = session.HomeScore, AwayScore = session.AwayScore,
            HomeStats = homeStats, AwayStats = awayStats,
            HomeGamePlan = session.HomeProfile.GamePlan, AwayGamePlan = session.AwayProfile.GamePlan,
            HomePregameRating = (session.HomeProfile.OffenseRating + session.HomeProfile.DefenseRating) / 2,
            AwayPregameRating = (session.AwayProfile.OffenseRating + session.AwayProfile.DefenseRating) / 2,
            Drives = BuildDrives(session),
            PlayerStats = BuildPlayerStats(session)
        };
    }

    private static IReadOnlyList<GameDriveSummary> BuildDrives(GameDaySession session) => session.Events
        .GroupBy(item => (item.OffenseTeamName, item.PossessionNumber))
        .Select(group =>
        {
            var first = group.First();
            var last = group.Last();
            var concept = PlaybookCatalog.OffensivePlays.First(play => play.Id == first.OffensivePlayId).Concept;
            var turnover = group.Any(item => item.Outcome == SnapOutcome.Turnover);
            var touchdown = group.Any(item => item.Outcome == SnapOutcome.Touchdown);
            var fieldGoal = group.Any(item => item.Outcome == SnapOutcome.FieldGoal);
            var points = touchdown ? 7 : fieldGoal ? 3 : 0;
            return new GameDriveSummary
            {
                TeamName = group.Key.OffenseTeamName, PossessionNumber = group.Key.PossessionNumber,
                Concept = concept, OffensivePlayId = first.OffensivePlayId, DefensivePlayId = first.DefensivePlayId,
                PlayCallSource = first.CallSource,
                PassAttempts = group.Count(item => PlaybookCatalog.OffensivePlays.First(play => play.Id == item.OffensivePlayId).Concept is
                    OffensiveConcept.QuickPass or OffensiveConcept.PlayAction or OffensiveConcept.DeepPass),
                RushAttempts = group.Count(item => PlaybookCatalog.OffensivePlays.First(play => play.Id == item.OffensivePlayId).Concept is
                    OffensiveConcept.InsideRun or OffensiveConcept.OutsideRun),
                PassYards = group.Where(item => PlaybookCatalog.OffensivePlays.First(play => play.Id == item.OffensivePlayId).Concept is
                    OffensiveConcept.QuickPass or OffensiveConcept.PlayAction or OffensiveConcept.DeepPass).Sum(item => Math.Max(0, item.Yards)),
                RushYards = group.Where(item => PlaybookCatalog.OffensivePlays.First(play => play.Id == item.OffensivePlayId).Concept is
                    OffensiveConcept.InsideRun or OffensiveConcept.OutsideRun).Sum(item => item.Yards),
                Outcome = touchdown ? DriveOutcome.Touchdown : turnover ? DriveOutcome.Turnover : fieldGoal ? DriveOutcome.FieldGoal : DriveOutcome.Punt,
                Points = points
            };
        }).ToArray();

    private static IReadOnlyList<PlayerSeasonStatLine> BuildPlayerStats(GameDaySession session)
    {
        var lines = new Dictionary<Guid, PlayerSeasonStatLine>();
        PlayerSeasonStatLine Line(Guid id)
        {
            if (lines.TryGetValue(id, out var existing)) return existing;
            var player = session.Roster.First(item => item.PlayerId == id);
            return new PlayerSeasonStatLine { SeasonYear = session.Game.SeasonYear, PlayerId = id,
                PlayerName = player.FullName, TeamName = player.TeamName, Position = player.Position,
                OriginalRecruitingStars = player.OriginalRecruitingStars, Games = 1 };
        }
        void Save(PlayerSeasonStatLine line) => lines[line.PlayerId] = line;
        foreach (var snap in session.Events)
        {
            if (snap.Outcome is SnapOutcome.FieldGoal or SnapOutcome.Punt or SnapOutcome.MissedFieldGoal) continue;
            var concept = PlaybookCatalog.OffensivePlays.First(play => play.Id == snap.OffensivePlayId).Concept;
            var pass = concept is OffensiveConcept.QuickPass or OffensiveConcept.PlayAction or OffensiveConcept.DeepPass;
            if (snap.QuarterbackId is Guid qbId)
            {
                var qb = Line(qbId);
                if (pass) qb = qb with
                {
                    PassAttempts = qb.PassAttempts + (snap.Outcome == SnapOutcome.Sack ? 0 : 1),
                    PassCompletions = qb.PassCompletions + (snap.Outcome is SnapOutcome.Completion or SnapOutcome.Touchdown ? 1 : 0),
                    PassYards = qb.PassYards + (snap.Outcome is SnapOutcome.Completion or SnapOutcome.Touchdown ? Math.Max(0, snap.Yards) : 0),
                    PassingTouchdowns = qb.PassingTouchdowns + (snap.Outcome == SnapOutcome.Touchdown ? 1 : 0),
                    InterceptionsThrown = qb.InterceptionsThrown + (snap.Outcome == SnapOutcome.Turnover ? 1 : 0),
                    Turnovers = qb.Turnovers + (snap.Outcome == SnapOutcome.Turnover ? 1 : 0)
                };
                Save(qb);
            }
            if (!pass && snap.BallCarrierId is Guid carrierId)
            {
                var carrier = Line(carrierId) with
                {
                    RushAttempts = Line(carrierId).RushAttempts + 1,
                    RushYards = Line(carrierId).RushYards + snap.Yards,
                    RushingTouchdowns = Line(carrierId).RushingTouchdowns + (snap.Outcome == SnapOutcome.Touchdown ? 1 : 0),
                    RushingFumbles = Line(carrierId).RushingFumbles + (snap.Outcome == SnapOutcome.Turnover ? 1 : 0),
                    Rushing20Plus = Line(carrierId).Rushing20Plus + (snap.Yards >= 20 ? 1 : 0),
                    Touchdowns = Line(carrierId).Touchdowns + (snap.Outcome == SnapOutcome.Touchdown ? 1 : 0),
                    Turnovers = Line(carrierId).Turnovers + (snap.Outcome == SnapOutcome.Turnover ? 1 : 0)
                };
                Save(carrier);
            }
            if (pass && snap.TargetId is Guid targetId)
            {
                var caught = snap.Outcome is SnapOutcome.Completion or SnapOutcome.Touchdown;
                var target = Line(targetId) with
                {
                    Receptions = Line(targetId).Receptions + (caught ? 1 : 0),
                    ReceivingYards = Line(targetId).ReceivingYards + (caught ? Math.Max(0, snap.Yards) : 0),
                    ReceivingTouchdowns = Line(targetId).ReceivingTouchdowns + (snap.Outcome == SnapOutcome.Touchdown ? 1 : 0),
                    Receiving20Plus = Line(targetId).Receiving20Plus + (caught && snap.Yards >= 20 ? 1 : 0),
                    Drops = Line(targetId).Drops + (snap.Outcome == SnapOutcome.Incompletion ? 1 : 0),
                    Touchdowns = Line(targetId).Touchdowns + (snap.Outcome == SnapOutcome.Touchdown ? 1 : 0)
                };
                Save(target);
            }
            if (snap.PrimaryDefenderId is Guid defenderId && snap.Outcome is not SnapOutcome.Incompletion)
            {
                var defender = Line(defenderId) with
                {
                    Tackles = Line(defenderId).Tackles + 1,
                    TacklesForLoss = Line(defenderId).TacklesForLoss + (snap.Yards < 0 && snap.Outcome != SnapOutcome.Sack ? 1 : 0),
                    Sacks = Line(defenderId).Sacks + (snap.Outcome == SnapOutcome.Sack ? 1 : 0),
                    Interceptions = Line(defenderId).Interceptions + (pass && snap.Outcome == SnapOutcome.Turnover ? 1 : 0),
                    FumbleRecoveries = Line(defenderId).FumbleRecoveries + (!pass && snap.Outcome == SnapOutcome.Turnover ? 1 : 0)
                };
                Save(defender);
            }
        }
        foreach (var team in new[] { session.Game.HomeTeamName, session.Game.AwayTeamName })
        {
            var kicker = session.Roster.Where(player => player.TeamName.Equals(team, StringComparison.OrdinalIgnoreCase) &&
                    player.Position == Position.K && player.CurrentInjury is null && !player.IsRedshirted)
                .OrderBy(player => player.DepthChartOrder).ThenByDescending(player => player.OverallRating).FirstOrDefault();
            if (kicker is null) continue;
            var teamEvents = session.Events.Where(item => item.OffenseTeamName.Equals(team, StringComparison.OrdinalIgnoreCase)).ToArray();
            var fieldGoals = teamEvents.Count(item => item.Outcome == SnapOutcome.FieldGoal);
            var punts = teamEvents.Count(item => item.Outcome == SnapOutcome.Punt);
            var touchdowns = teamEvents.Count(item => item.Outcome == SnapOutcome.Touchdown);
            if (fieldGoals + punts + touchdowns == 0) continue;
            Save(Line(kicker.PlayerId) with { FieldGoalsMade = fieldGoals, FieldGoalsAttempted = fieldGoals,
                ExtraPointsMade = touchdowns, ExtraPointsAttempted = touchdowns, Punts = punts,
                PuntYards = punts * 40, LongPunt = punts > 0 ? 40 : 0,
                PuntsInside20 = teamEvents.Count(item => item.Outcome == SnapOutcome.Punt && item.StartYardLine >= 50),
                PuntsInside10 = teamEvents.Count(item => item.Outcome == SnapOutcome.Punt && item.StartYardLine >= 60) });
        }
        return lines.Values.ToArray();
    }

    private static GameTeamStats BuildStats(GameDaySession session, string teamName)
    {
        var snaps = session.Events.Where(item => item.OffenseTeamName.Equals(teamName, StringComparison.OrdinalIgnoreCase)).ToArray();
        var scrimmage = snaps.Where(item => item.Outcome is not (SnapOutcome.FieldGoal or SnapOutcome.Punt or SnapOutcome.MissedFieldGoal)).ToArray();
        var passes = scrimmage.Where(item => PlaybookCatalog.OffensivePlays.First(play => play.Id == item.OffensivePlayId).Concept is
            OffensiveConcept.QuickPass or OffensiveConcept.PlayAction or OffensiveConcept.DeepPass).ToArray();
        var runs = scrimmage.Except(passes).ToArray();
        return new GameTeamStats
        {
            Possessions = Math.Max(1, snaps.Count(item => item.Down == 1 && item.Distance == 10)),
            PassAttempts = passes.Count(item => item.Outcome != SnapOutcome.Sack),
            PassCompletions = passes.Count(item => item.Outcome is SnapOutcome.Completion or SnapOutcome.Touchdown),
            PassYards = passes.Where(item => item.Outcome is SnapOutcome.Completion or SnapOutcome.Touchdown).Sum(item => item.Yards),
            RushAttempts = runs.Length, RushYards = runs.Sum(item => item.Yards),
            PassingTouchdowns = passes.Count(item => item.Outcome == SnapOutcome.Touchdown),
            RushingTouchdowns = runs.Count(item => item.Outcome == SnapOutcome.Touchdown),
            InterceptionsThrown = passes.Count(item => item.Outcome == SnapOutcome.Turnover),
            FumblesLost = runs.Count(item => item.Outcome == SnapOutcome.Turnover),
            Turnovers = snaps.Count(item => item.Outcome == SnapOutcome.Turnover),
            ExplosivePasses20 = passes.Count(item => item.Yards >= 20),
            ExplosiveRuns20 = runs.Count(item => item.Yards >= 20),
            SacksAllowed = passes.Count(item => item.Outcome == SnapOutcome.Sack),
            FieldGoalsMade = snaps.Count(item => item.Outcome == SnapOutcome.FieldGoal),
            FieldGoalsAttempted = snaps.Count(item => item.Outcome == SnapOutcome.FieldGoal),
            ExtraPointsMade = snaps.Count(item => item.Outcome == SnapOutcome.Touchdown),
            ExtraPointsAttempted = snaps.Count(item => item.Outcome == SnapOutcome.Touchdown),
            Punts = snaps.Count(item => item.Outcome == SnapOutcome.Punt),
            PuntYards = snaps.Count(item => item.Outcome == SnapOutcome.Punt) * 40,
            LongPunt = snaps.Any(item => item.Outcome == SnapOutcome.Punt) ? 40 : 0
        };
    }

    private static void AdvanceState(GameDaySession session, bool homeOnOffense, int yards, bool turnover, bool touchdown)
    {
        session.ClockSeconds -= session.Random.Next(22, 41);
        if (touchdown)
        {
            if (homeOnOffense) session.HomeScore += 7; else session.AwayScore += 7;
            ChangePossession(session, 25);
        }
        else if (turnover) ChangePossession(session, Math.Clamp(100 - session.YardLine, 10, 90));
        else
        {
            session.YardLine = Math.Clamp(session.YardLine + yards, 1, 99);
            if (yards >= session.Distance) { session.Down = 1; session.Distance = Math.Min(10, 100 - session.YardLine); }
            else { session.Distance -= yards; session.Down++; }
            if (session.Down > 4)
            {
                if (session.YardLine >= 65 && session.Random.NextDouble() < .7)
                {
                    session.Events[^1] = session.Events[^1] with { Outcome = SnapOutcome.FieldGoal, Yards = 0 };
                    if (homeOnOffense) session.HomeScore += 3; else session.AwayScore += 3;
                    ChangePossession(session, 25);
                }
                else
                {
                    session.Events[^1] = session.Events[^1] with { Outcome = SnapOutcome.Punt, Yards = 0 };
                    ChangePossession(session, Math.Clamp(100 - Math.Min(95, session.YardLine + 40), 5, 80));
                }
            }
        }
        AdvanceQuarter(session);
    }

    private static void AdvanceQuarter(GameDaySession session)
    {
        if (session.ClockSeconds <= 0)
        {
            if (session.Quarter >= 4) session.IsComplete = session.HomeScore != session.AwayScore;
            if (!session.IsComplete) { session.Quarter++; session.ClockSeconds = 900; }
        }
        if (session.Events.Count >= 180)
        {
            if (session.HomeScore == session.AwayScore)
            {
                if (session.Random.Next(2) == 0) session.HomeScore += 3; else session.AwayScore += 3;
            }
            session.IsComplete = true;
        }
    }

    private static void ChangePossession(GameDaySession session, int yardLine)
    {
        session.PossessionTeamName = session.PossessionTeamName.Equals(session.Game.HomeTeamName, StringComparison.OrdinalIgnoreCase)
            ? session.Game.AwayTeamName : session.Game.HomeTeamName;
        session.PossessionNumber++; session.Down = 1; session.Distance = 10; session.YardLine = yardLine;
    }

    private static double MatchupShift(OffensiveConcept offense, DefensiveApproach defense) => (offense, defense) switch
    {
        (OffensiveConcept.InsideRun or OffensiveConcept.OutsideRun, DefensiveApproach.StopRun) => -5,
        (OffensiveConcept.QuickPass or OffensiveConcept.DeepPass, DefensiveApproach.ProtectPass) => -5,
        (OffensiveConcept.DeepPass, DefensiveApproach.Pressure) => -3,
        (OffensiveConcept.QuickPass, DefensiveApproach.Pressure) => 4,
        (OffensiveConcept.PlayAction, DefensiveApproach.StopRun) => 5,
        _ => 0
    };

    private static DynastyPlayer[] Players(GameDaySession session, SnapPersonnel personnel) =>
        personnel.PlayerIds.Select(id => session.PlayersById[id]).ToArray();
    private static DynastyPlayer? Pick(DynastyPlayer[] players, Random random) => players.Length == 0 ? null : players[random.Next(players.Length)];
}
