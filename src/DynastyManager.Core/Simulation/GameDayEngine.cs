using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static partial class GameDayEngine
{
    public static bool IsAvailable(ScheduledGame game, int year, int week, SeasonPhase phase = SeasonPhase.RegularSeason) =>
        phase is (SeasonPhase.RegularSeason or SeasonPhase.ConferenceChampionship or SeasonPhase.Postseason) &&
        !game.HasPlayed && game.SeasonYear == year && game.Week == week;
    public const double HomeFieldRatingBonus = 2.5;
    public static double FieldGoalProbability(GameDaySession session)
    {
        var home = session.PossessionTeamName.Equals(session.Game.HomeTeamName, StringComparison.OrdinalIgnoreCase);
        var profile = home ? session.HomeProfile : session.AwayProfile;
        var kicker = Kicker(home ? session.HomeRoster : session.AwayRoster);
        return GameDayMatchupService.FieldGoalProbability(117 - session.YardLine,
            kicker?.Skills?.KickAccuracy ?? profile.SpecialTeamsRating, kicker?.Skills?.KickPower ?? profile.SpecialTeamsRating);
    }
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
            Mode = mode, Random = new Random(game.SimulationSeed), PossessionTeamName = game.HomeTeamName,
            KickoffTeamName = game.AwayTeamName, KickoffPending = true
        };
    }

    public static GameSnapEvent Step(GameDaySession session, string? userPlayId = null,
        string? userDefensivePlayId = null)
    {
        if (session.IsComplete) throw new InvalidOperationException("The game is complete.");
        if (session.TryPending) return ResolveTry(session, userPlayId);
        if (session.KickoffPending) return ResolveKickoff(session);
        if (userPlayId is "special-kneel" or "special-spike") return ClockPlay(session, userPlayId == "special-spike");
        if (session.Quarter is 2 or 4 && session.ClockSeconds <= 90 && !session.ClockStopped)
        {
            var trailing = session.HomeScore < session.AwayScore ? session.Game.HomeTeamName :
                session.AwayScore < session.HomeScore ? session.Game.AwayTeamName : null;
            if (trailing is not null && trailing != session.PossessionTeamName &&
                (session.Mode != GameDayMode.Play || trailing != session.ControlledTeamName)) TryCallTimeout(session, trailing);
        }
        var homeOnOffense = session.PossessionTeamName.Equals(session.Game.HomeTeamName, StringComparison.OrdinalIgnoreCase);
        var offense = homeOnOffense ? session.HomeProfile : session.AwayProfile;
        var defense = homeOnOffense ? session.AwayProfile : session.HomeProfile;
        var offenseFatigue = homeOnOffense ? session.HomeFatigue : session.AwayFatigue;
        var defenseFatigue = homeOnOffense ? session.AwayFatigue : session.HomeFatigue;
        var offenseRoster = homeOnOffense ? session.HomeRoster : session.AwayRoster;
        var defenseRoster = homeOnOffense ? session.AwayRoster : session.HomeRoster;
        var offenseDepthChart = homeOnOffense ? session.HomeDepthChart : session.AwayDepthChart;
        var defenseDepthChart = homeOnOffense ? session.AwayDepthChart : session.HomeDepthChart;
        var offensePlan = GamePlanService.Normalize(offense.GamePlan ?? new TeamGamePlan { TeamName = offense.TeamName });
        var defensePlan = GamePlanService.Normalize(defense.GamePlan ?? new TeamGamePlan { TeamName = defense.TeamName });
        var requestedSpecial = userPlayId is "special-punt" or "special-field-goal" ? userPlayId : null;
        if (!string.IsNullOrWhiteSpace(userPlayId) && requestedSpecial is null && offensePlan.Offense == OffensivePlaybook.Custom &&
            !offensePlan.CustomOffensivePlayIds.Contains(userPlayId))
            throw new ArgumentException("Offensive play is not in this custom playbook.", nameof(userPlayId));
        if (!string.IsNullOrWhiteSpace(userPlayId) && requestedSpecial is null)
            offensePlan = offensePlan with { OpeningPlayIds = [userPlayId] };
        var penalty = TryPreSnapPenalty(session);
        if (penalty is not null) return penalty;
        var offensivePossessionIndex = session.Events.Count(item => item.Outcome != SnapOutcome.Kickoff);
        var call = GamePlanService.ResolvePlayCall(offensePlan, defensePlan,
            string.IsNullOrWhiteSpace(userPlayId) ? offensivePossessionIndex : 0, session.Random,
            (offense.HeadCoachGameManagement + offense.OffensiveCoordinatorGameManagement) / 2,
            (defense.HeadCoachGameManagement + defense.DefensiveCoordinatorGameManagement) / 2);
        var advice = GameDayRecommendationService.Recommend(session, offensePlan);
        if (string.IsNullOrWhiteSpace(userPlayId) && offensivePossessionIndex >= offensePlan.OpeningPlayIds.Count &&
            offensivePossessionIndex >= offensePlan.OpeningScript.Count && advice.Play is not null)
            call = call with { Offense = advice.Play, Source = PlayCallSource.Cpu };
        if (!string.IsNullOrWhiteSpace(userDefensivePlayId))
        {
            var calledDefense = GamePlanService.AvailableDefensivePlays(defensePlan)
                .SingleOrDefault(play => play.Id == userDefensivePlayId)
                ?? throw new ArgumentException("Defensive play is not in this team's playbook.", nameof(userDefensivePlayId));
            call = call with { Defense = calledDefense };
        }

        // A user can choose a kick on any down; CPU fourth-down decisions remain situational.
        var special = requestedSpecial ?? (string.IsNullOrWhiteSpace(userPlayId) && advice.Type != RecommendedCallType.Scrimmage
            ? advice.PlayId : null);
        if (special is not null)
        {
            var kickStart = session.YardLine;
            var kicker = Kicker(offenseRoster);
            var made = special == "special-field-goal" &&
                session.Random.NextDouble() < FieldGoalProbability(session);
            var puntLanding = special == "special-punt"
                ? Math.Min(95, kickStart + session.Random.Next(34, 51)) : 100;
            var touchback = special == "special-punt" && puntLanding >= 95;
            var fairCatch = special == "special-punt" && !touchback && session.Random.NextDouble() < .28;
            var returnYards = special == "special-punt" && !touchback && !fairCatch ? session.Random.Next(0, 13) : 0;
            var newSpot = made ? 25 : special == "special-punt"
                ? touchback ? 20 : Math.Clamp(100 - puntLanding + returnYards, 5, 80)
                : Math.Clamp(100 - Math.Max(20, kickStart - 7), 10, 80);
            var returner = Returner(defenseRoster);
            var kickingUnit = SpecialTeam(offenseRoster, kicker);
            var returnUnit = SpecialTeam(defenseRoster, returner);
            var coverageTackler = special == "special-punt" && returnYards > 0
                ? CoverageTackler(kickingUnit, kicker, session.Random)
                : null;
            var kick = new GameSnapEvent
            {
                Sequence = session.Events.Count + 1, PossessionNumber = session.PossessionNumber,
                Quarter = session.Quarter, ClockSeconds = session.ClockSeconds,
                OffenseTeamName = offense.TeamName, DefenseTeamName = defense.TeamName,
                OffensivePlayId = call.Offense.Id, DefensivePlayId = call.Defense.Id,
                CallSource = requestedSpecial is null ? call.Source : PlayCallSource.User,
                DefensiveCallSource = string.IsNullOrWhiteSpace(userDefensivePlayId) ? PlayCallSource.Cpu : PlayCallSource.User,
                Down = session.Down, Distance = session.Distance, StartYardLine = kickStart,
                BallEndYardLine = special == "special-punt" ? 100 - newSpot : 100,
                KickLandingYardLine = special == "special-punt" ? puntLanding : 100,
                Yards = 0, ReturnYards = returnYards,
                IsTouchback = touchback, IsFairCatch = fairCatch,
                KickerId = kicker?.PlayerId, ReturnerId = special == "special-punt" && !touchback ? returner?.PlayerId : null,
                PrimaryDefenderId = coverageTackler?.PlayerId,
                OffensePersonnel = kickingUnit.Select(player => player.PlayerId).ToArray(),
                DefensePersonnel = returnUnit.Select(player => player.PlayerId).ToArray(),
                Outcome = special == "special-punt" ? SnapOutcome.Punt :
                    made ? SnapOutcome.FieldGoal : SnapOutcome.MissedFieldGoal
            };
            session.Events.Add(kick);
            session.ClockSeconds -= session.Random.Next(5, 13);
            if (made) { if (homeOnOffense) session.HomeScore += 3; else session.AwayScore += 3; }
            ChangePossession(session, newSpot);
            if (made && session.OvertimeRound == 0) QueueKickoff(session, offense.TeamName);
            AdvanceQuarter(session);
            return kick;
        }

        var offensePersonnel = PersonnelRotationService.SelectOffense(offenseDepthChart, offense.TeamName,
            call.Offense.Personnel, offenseFatigue, session.Random);
        var defensePersonnel = PersonnelRotationService.SelectDefense(defenseDepthChart, defense.TeamName,
            call.Defense.Personnel, defenseFatigue, session.Random);
        var offensePlayers = Players(session, offensePersonnel);
        var defensePlayers = Players(session, defensePersonnel);

        var keepEligible = call.Offense.Name.Contains("Option", StringComparison.OrdinalIgnoreCase) ||
            call.Offense.Name.Contains("RPO", StringComparison.OrdinalIgnoreCase) ||
            call.Offense.Name.Contains("Read", StringComparison.OrdinalIgnoreCase);
        var designedKeep = keepEligible && session.Random.NextDouble() < .32 ||
            call.Offense.Name.Contains("QB Draw", StringComparison.OrdinalIgnoreCase);
        var calledPass = call.Offense.Concept is OffensiveConcept.QuickPass or OffensiveConcept.PlayAction or OffensiveConcept.DeepPass;
        var qb = offensePlayers.FirstOrDefault(player => player.Position == Position.QB);
        var mobility = qb is null ? 50 : (qb.SpeedRating + qb.AgilityRating + (qb.Skills?.Acceleration ?? qb.SpeedRating)) / 3d;
        var scrambleChance = Math.Clamp(.025 + Math.Max(0, mobility - 62) * .0032 +
            (call.Defense.Approach == DefensiveApproach.Pressure ? .035 : 0) +
            (session.Down >= 3 && session.Distance >= 5 ? .018 : 0), .02, .19);
        var scramble = calledPass && !designedKeep && qb is not null && session.Random.NextDouble() < scrambleChance;
        var isPass = calledPass && !scramble && !designedKeep;
        var advantage = isPass ? offense.PassOffenseRating - defense.PassDefenseRating : offense.RushOffenseRating - defense.RushDefenseRating;
        advantage += MatchupShift(call.Offense.Concept, call.Defense.Approach);
        advantage += GameDayMatchupService.FormationAdvantage(call.Offense, call.Defense, isPass);
        advantage += GameDayMatchupService.PersonnelAdvantage(offensePlayers, defensePlayers, isPass);
        if (!session.Game.IsNeutralSite)
            advantage += homeOnOffense ? HomeFieldRatingBonus : -HomeFieldRatingBonus;
        var read = isPass ? PassingReadService.Read(call.Offense, call.Defense, offensePlayers, defensePlayers, qb,
            offense.OffensiveCoordinatorGameManagement, session.Random) : new PassingRead(null, null, 0, 0, 2, 0, 0);
        var turnover = session.Random.NextDouble() < (isPass ? read.InterceptionChance : Math.Clamp(.025 - advantage * .0008, .008, .065));
        var sack = isPass && !turnover && session.Random.NextDouble() < Math.Clamp(.065 - read.Protection * .0018 + (read.ReleaseSeconds - 2) * .015, .015, .18);
        var hailMary = call.Offense.Name == "Hail Mary";
        var complete = isPass && !turnover && !sack && session.Random.NextDouble() <
            Math.Clamp(read.CatchChance + advantage * .0015 - (hailMary ? .25 : 0), .06, .9);
        int RoundYards(double expected)
        {
            var floor = Math.Floor(expected);
            return (int)floor + (session.Random.NextDouble() < expected - floor ? 1 : 0);
        }
        var yards = turnover ? 0 : sack ? -session.Random.Next(3, 11) : scramble
            ? Math.Max(-3, RoundYards(session.Random.Next(-1, 10) + (mobility - 65) * .10)) : isPass
            ? complete ? Math.Max(-2, RoundYards(session.Random.Next(3, 16) + advantage * .12 +
                (call.Offense.Concept == OffensiveConcept.DeepPass ? session.Random.Next(5, 24) : 0))) : 0
            : Math.Max(-4, RoundYards(session.Random.Next(-2, 10) + advantage * .09));

        if (hailMary && complete) yards = Math.Max(1, 100 - session.YardLine + session.Random.Next(-5, 2));
        var carrier = isPass ? null : scramble || designedKeep ? qb : Pick(offensePlayers.Where(player => player.Position == Position.RB).ToArray(), session.Random) ?? qb;
        var target = isPass ? read.Target : null;
        var defenders = defensePlayers.Where(player => !isPass || !turnover
            ? player.Position != Position.K : player.Position is Position.CB or Position.FS or Position.SS or Position.OLB or Position.MLB).ToArray();
        var defender = isPass && !sack ? read.Defender : Pick(defenders.Length == 0 ? defensePlayers : defenders, session.Random);
        var start = session.YardLine;
        var touchdown = !turnover && start + yards >= 100;
        var outcome = turnover ? isPass ? SnapOutcome.Interception : SnapOutcome.Fumble :
            sack ? SnapOutcome.Sack : touchdown ? SnapOutcome.Touchdown :
            scramble ? SnapOutcome.Scramble : isPass ? complete ? SnapOutcome.Completion : SnapOutcome.Incompletion : SnapOutcome.Run;
        var snap = new GameSnapEvent
        {
            Sequence = session.Events.Count + 1, PossessionNumber = session.PossessionNumber,
            Quarter = session.Quarter, ClockSeconds = session.ClockSeconds,
            OffenseTeamName = offense.TeamName, DefenseTeamName = defense.TeamName,
            OffensivePlayId = call.Offense.Id, DefensivePlayId = call.Defense.Id, CallSource = call.Source,
            DefensiveCallSource = string.IsNullOrWhiteSpace(userDefensivePlayId) ? PlayCallSource.Cpu : PlayCallSource.User,
            Down = session.Down, Distance = session.Distance, StartYardLine = start,
            BallEndYardLine = Math.Clamp(start + yards, 0, 100), Yards = yards, Outcome = outcome,
            PassReleaseProgress = Math.Clamp(read.ReleaseSeconds / 5, .3, .75), TargetSeparation = read.Separation,
            IsDrop = isPass && !complete && !turnover && !sack && read.Separation >= 2 && session.Random.NextDouble() < .18,
            IsQuarterbackScramble = scramble, IsQuarterbackKeep = designedKeep,
            IsBootleg = call.Offense.Name.Contains("Boot", StringComparison.OrdinalIgnoreCase),
            QuarterbackId = qb?.PlayerId, BallCarrierId = carrier?.PlayerId, TargetId = target?.PlayerId,
            PrimaryDefenderId = defender?.PlayerId, OffensePersonnel = offensePersonnel.PlayerIds,
            DefensePersonnel = defensePersonnel.PlayerIds
        };
        session.Events.Add(snap);
        PersonnelRotationService.ApplySnap(offenseFatigue, offenseRoster, offensePersonnel, Math.Abs(yards) >= 20);
        PersonnelRotationService.ApplySnap(defenseFatigue, defenseRoster, defensePersonnel, Math.Abs(yards) >= 20);
        AdvanceState(session, homeOnOffense, yards, turnover, touchdown, outcome == SnapOutcome.Incompletion);
        return session.Events[^1];
    }

    public static bool TryCallTimeout(GameDaySession session, string teamName)
    {
        if (session.IsComplete || session.KickoffPending || session.ClockStopped && session.OvertimeRound == 0) return false;
        if (teamName == session.Game.HomeTeamName && session.HomeTimeouts > 0) session.HomeTimeouts--;
        else if (teamName == session.Game.AwayTeamName && session.AwayTimeouts > 0) session.AwayTimeouts--;
        else return false;
        session.ClockStopped = true;
        return true;
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
        .Where(item => item.Outcome != SnapOutcome.Kickoff)
        .GroupBy(item => (item.OffenseTeamName, item.PossessionNumber))
        .Select(group =>
        {
            var first = group.First();
            var last = group.Last();
            var concept = ConceptOf(session, first);
            var turnover = group.Any(item => IsTurnover(item.Outcome));
            var touchdown = group.Any(item => item.Outcome == SnapOutcome.Touchdown);
            var fieldGoal = group.Any(item => item.Outcome == SnapOutcome.FieldGoal);
            var scrimmage = group.Where(IsScrimmage).ToArray();
            bool IsPass(GameSnapEvent item) => !item.IsQuarterbackKeep && !item.IsQuarterbackScramble && item.Outcome != SnapOutcome.Scramble &&
                ConceptOf(session, item) is
                    OffensiveConcept.QuickPass or OffensiveConcept.PlayAction or OffensiveConcept.DeepPass;
            var points = group.Sum(item => item.Outcome == SnapOutcome.Touchdown ? 6 : item.Outcome == SnapOutcome.FieldGoal ? 3 : item.Points);
            return new GameDriveSummary
            {
                TeamName = group.Key.OffenseTeamName, PossessionNumber = group.Key.PossessionNumber,
                IsOvertime = first.Quarter > 4, Concept = concept, OffensivePlayId = first.OffensivePlayId, DefensivePlayId = first.DefensivePlayId,
                PlayCallSource = first.CallSource,
                PassAttempts = scrimmage.Count(IsPass),
                RushAttempts = scrimmage.Count(item => !IsPass(item)),
                PassYards = scrimmage.Where(IsPass).Sum(item => Math.Max(0, item.Yards)),
                RushYards = scrimmage.Where(item => !IsPass(item)).Sum(item => item.Yards),
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
            if (snap.Outcome == SnapOutcome.Kickoff)
            {
                if (snap.ReturnerId is Guid kickoffReturnerId)
                {
                    var returner = Line(kickoffReturnerId);
                    Save(returner with { KickReturns = returner.KickReturns + 1,
                        KickReturnYards = returner.KickReturnYards + snap.ReturnYards,
                        KickReturnTouchdowns = returner.KickReturnTouchdowns +
                            (snap.BallEndYardLine >= 100 ? 1 : 0) });
                }
                CreditSpecialTeamsTackle(snap);
                continue;
            }
            if (snap.Outcome == SnapOutcome.Punt)
            {
                if (snap.ReturnerId is Guid puntReturnerId && snap.ReturnYards > 0)
                {
                    var returner = Line(puntReturnerId);
                    Save(returner with { PuntReturns = returner.PuntReturns + 1,
                        PuntReturnYards = returner.PuntReturnYards + snap.ReturnYards });
                }
                CreditSpecialTeamsTackle(snap);
                continue;
            }
            if (!IsScrimmage(snap)) continue;
            var concept = ConceptOf(session, snap);
            var pass = !snap.IsQuarterbackKeep && !snap.IsQuarterbackScramble && snap.Outcome != SnapOutcome.Scramble &&
                concept is OffensiveConcept.QuickPass or OffensiveConcept.PlayAction or OffensiveConcept.DeepPass;
            if (snap.QuarterbackId is Guid qbId)
            {
                var qb = Line(qbId);
                if (pass) qb = qb with
                {
                    PassAttempts = qb.PassAttempts + (snap.Outcome == SnapOutcome.Sack ? 0 : 1),
                    PassCompletions = qb.PassCompletions + (snap.Outcome is SnapOutcome.Completion or SnapOutcome.Touchdown ? 1 : 0),
                    PassYards = qb.PassYards + (snap.Outcome is SnapOutcome.Completion or SnapOutcome.Touchdown ? Math.Max(0, snap.Yards) : 0),
                    PassingTouchdowns = qb.PassingTouchdowns + (snap.Outcome == SnapOutcome.Touchdown ? 1 : 0),
                    InterceptionsThrown = qb.InterceptionsThrown + (snap.Outcome is SnapOutcome.Turnover or SnapOutcome.Interception ? 1 : 0),
                    Turnovers = qb.Turnovers + (snap.Outcome is SnapOutcome.Turnover or SnapOutcome.Interception ? 1 : 0)
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
                    RushingFumbles = Line(carrierId).RushingFumbles + (snap.Outcome is SnapOutcome.Turnover or SnapOutcome.Fumble ? 1 : 0),
                    Rushing20Plus = Line(carrierId).Rushing20Plus + (snap.Yards >= 20 ? 1 : 0),
                    Touchdowns = Line(carrierId).Touchdowns + (snap.Outcome == SnapOutcome.Touchdown ? 1 : 0),
                    Turnovers = Line(carrierId).Turnovers + (snap.Outcome is SnapOutcome.Turnover or SnapOutcome.Fumble ? 1 : 0)
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
                    Drops = Line(targetId).Drops + (snap.IsDrop ? 1 : 0),
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
                    Interceptions = Line(defenderId).Interceptions + (snap.Outcome == SnapOutcome.Interception || pass && snap.Outcome == SnapOutcome.Turnover ? 1 : 0),
                    FumbleRecoveries = Line(defenderId).FumbleRecoveries + (snap.Outcome == SnapOutcome.Fumble || !pass && snap.Outcome == SnapOutcome.Turnover ? 1 : 0)
                };
                Save(defender);
            }
        }
        void CreditSpecialTeamsTackle(GameSnapEvent snap)
        {
            if (snap.ReturnYards <= 0 || snap.PrimaryDefenderId is not Guid defenderId) return;
            var defender = Line(defenderId);
            Save(defender with { Tackles = defender.Tackles + 1 });
        }
        foreach (var team in new[] { session.Game.HomeTeamName, session.Game.AwayTeamName })
        {
            var kicker = session.Roster.Where(player => player.TeamName.Equals(team, StringComparison.OrdinalIgnoreCase) &&
                    player.Position == Position.K && player.CurrentInjury is null && !player.IsRedshirted)
                .OrderBy(player => player.DepthChartOrder).ThenByDescending(player => player.OverallRating).FirstOrDefault();
            if (kicker is null) continue;
            var teamEvents = session.Events.Where(item => item.OffenseTeamName.Equals(team, StringComparison.OrdinalIgnoreCase)).ToArray();
            var fieldGoals = teamEvents.Count(item => item.Outcome == SnapOutcome.FieldGoal);
            var fieldGoalAttempts = teamEvents.Count(item => item.Outcome is SnapOutcome.FieldGoal or SnapOutcome.MissedFieldGoal);
            var puntEvents = teamEvents.Where(item => item.Outcome == SnapOutcome.Punt).ToArray();
            var punts = puntEvents.Length;
            var touchdowns = teamEvents.Count(item => item.Outcome == SnapOutcome.Touchdown);
            if (fieldGoalAttempts + punts + touchdowns == 0) continue;
            Save(Line(kicker.PlayerId) with { FieldGoalsMade = fieldGoals, FieldGoalsAttempted = fieldGoalAttempts,
                ExtraPointsMade = teamEvents.Count(e => e.Outcome == SnapOutcome.ExtraPoint), ExtraPointsAttempted = teamEvents.Count(e => e.Outcome is SnapOutcome.ExtraPoint or SnapOutcome.MissedExtraPoint), Punts = punts,
                PuntYards = puntEvents.Sum(item => Math.Max(0, item.KickLandingYardLine - item.StartYardLine)),
                LongPunt = puntEvents.Select(item => Math.Max(0, item.KickLandingYardLine - item.StartYardLine)).DefaultIfEmpty().Max(),
                PuntsInside20 = puntEvents.Count(item => !item.IsTouchback && item.KickLandingYardLine >= 80),
                PuntsInside10 = puntEvents.Count(item => !item.IsTouchback && item.KickLandingYardLine >= 90) });
        }
        return lines.Values.ToArray();
    }

    private static GameTeamStats BuildStats(GameDaySession session, string teamName)
    {
        var snaps = session.Events.Where(item => item.OffenseTeamName.Equals(teamName, StringComparison.OrdinalIgnoreCase) &&
            item.Outcome != SnapOutcome.Kickoff).ToArray();
        var kickReturns = session.Events.Where(item => item.Outcome == SnapOutcome.Kickoff && !item.IsOnsideKick &&
            item.OffenseTeamName.Equals(teamName, StringComparison.OrdinalIgnoreCase)).ToArray();
        var puntReturns = session.Events.Where(item => item.Outcome == SnapOutcome.Punt &&
            item.DefenseTeamName.Equals(teamName, StringComparison.OrdinalIgnoreCase) && item.ReturnYards > 0).ToArray();
        var scrimmage = snaps.Where(IsScrimmage).ToArray();
        var passes = scrimmage.Where(item => !item.IsQuarterbackKeep && !item.IsQuarterbackScramble && item.Outcome != SnapOutcome.Scramble &&
            ConceptOf(session, item) is
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
            InterceptionsThrown = passes.Count(item => item.Outcome is SnapOutcome.Turnover or SnapOutcome.Interception),
            FumblesLost = runs.Count(item => item.Outcome is SnapOutcome.Turnover or SnapOutcome.Fumble),
            Turnovers = snaps.Count(item => IsTurnover(item.Outcome)),
            ExplosivePasses20 = passes.Count(item => item.Yards >= 20),
            ExplosiveRuns20 = runs.Count(item => item.Yards >= 20),
            SacksAllowed = passes.Count(item => item.Outcome == SnapOutcome.Sack),
            FieldGoalsMade = snaps.Count(item => item.Outcome == SnapOutcome.FieldGoal),
            FieldGoalsAttempted = snaps.Count(item => item.Outcome is SnapOutcome.FieldGoal or SnapOutcome.MissedFieldGoal),
            ExtraPointsMade = snaps.Count(item => item.Outcome == SnapOutcome.ExtraPoint),
            ExtraPointsAttempted = snaps.Count(item => item.Outcome is SnapOutcome.ExtraPoint or SnapOutcome.MissedExtraPoint),
            Punts = snaps.Count(item => item.Outcome == SnapOutcome.Punt),
            PuntYards = snaps.Where(item => item.Outcome == SnapOutcome.Punt)
                .Sum(item => Math.Max(0, item.KickLandingYardLine - item.StartYardLine)),
            LongPunt = snaps.Where(item => item.Outcome == SnapOutcome.Punt)
                .Select(item => Math.Max(0, item.KickLandingYardLine - item.StartYardLine)).DefaultIfEmpty().Max(),
            KickReturns = kickReturns.Length,
            KickReturnYards = kickReturns.Sum(item => item.ReturnYards),
            PuntReturns = puntReturns.Length,
            PuntReturnYards = puntReturns.Sum(item => item.ReturnYards)
        };
    }

    private static void AdvanceState(GameDaySession session, bool homeOnOffense, int yards, bool turnover, bool touchdown, bool incomplete)
    {
        if (session.OvertimeRound == 0)
        {
            var late = session.Quarter is 2 or 4 && session.ClockSeconds <= 120;
            var trailing = homeOnOffense ? session.HomeScore < session.AwayScore : session.AwayScore < session.HomeScore;
            var hurry = late && trailing;
            session.ClockSeconds -= session.ClockStopped || incomplete || hurry ? session.Random.Next(4, 9) : session.Random.Next(22, 41);
        }
        session.ClockStopped = incomplete || turnover || touchdown;
        if (touchdown)
        {
            if (homeOnOffense) session.HomeScore += 6; else session.AwayScore += 6;
            session.TryPending = true;
            session.YardLine = 97;
        }
        else if (turnover) ChangePossession(session, Math.Clamp(100 - session.YardLine, 10, 90));
        else
        {
            session.YardLine = Math.Clamp(session.YardLine + yards, 1, 99);
            if (yards >= session.Distance) { session.Down = 1; session.Distance = Math.Min(10, 100 - session.YardLine);
                if (session.Quarter is 2 or 4 && session.ClockSeconds <= 120) session.ClockStopped = true; }
            else { session.Distance -= yards; session.Down++; }
            if (session.Down > 4)
                ChangePossession(session, Math.Clamp(100 - session.YardLine, 1, 99));
        }
        AdvanceQuarter(session);
    }

    private static void AdvanceQuarter(GameDaySession session)
    {
        if (session.OvertimeRound > 0 || session.TryPending || session.ClockSeconds > 0) return;
        session.ClockSeconds = 0;
        if (session.Quarter == 4)
        {
            session.KickoffPending = false;
            if (session.HomeScore != session.AwayScore) session.IsComplete = true;
            else StartOvertimeRound(session);
            return;
        }
        session.Quarter++; session.ClockSeconds = 900;
        if (session.Quarter == 3)
        {
            session.HomeTimeouts = session.AwayTimeouts = 3;
            session.PossessionTeamName = session.Game.AwayTeamName;
            session.PossessionNumber++;
            QueueKickoff(session, session.Game.HomeTeamName);
        }
    }

    private static void ChangePossession(GameDaySession session, int yardLine)
    {
        if (session.OvertimeRound > 0) { EndOvertimePossession(session); return; }
        session.PossessionTeamName = session.PossessionTeamName.Equals(session.Game.HomeTeamName, StringComparison.OrdinalIgnoreCase)
            ? session.Game.AwayTeamName : session.Game.HomeTeamName;
        session.PossessionNumber++; session.Down = 1; session.Distance = 10; session.YardLine = yardLine;
    }

    private static GameSnapEvent ResolveKickoff(GameDaySession session)
    {
        var kickingHome = session.KickoffTeamName == session.Game.HomeTeamName;
        var deficit = kickingHome ? session.AwayScore - session.HomeScore : session.HomeScore - session.AwayScore;
        var cpuKicker = session.Mode != GameDayMode.Play || session.ControlledTeamName != session.KickoffTeamName;
        if (session.OnsideKickRequested && (session.ControlledTeamName is null || session.ControlledTeamName == session.KickoffTeamName) || cpuKicker && session.Quarter == 4 && session.ClockSeconds <= 120 && deficit is > 0 and <= 16)
            return ResolveOnsideKick(session);
        var receivingHome = session.PossessionTeamName.Equals(session.Game.HomeTeamName, StringComparison.OrdinalIgnoreCase);
        var receivingRoster = receivingHome ? session.HomeRoster : session.AwayRoster;
        var kickingRoster = receivingHome ? session.AwayRoster : session.HomeRoster;
        var kicker = Kicker(kickingRoster);
        var returner = Returner(receivingRoster);
        var kickoffRandom = new Random(HashCode.Combine(session.Game.SimulationSeed, session.PossessionNumber,
            session.Quarter, 0x4B1C));
        var returnYards = kickoffRandom.Next(16, 34);
        var start = 65;
        var landing = kickoffRandom.Next(1, 8);
        var end = Math.Clamp(landing + returnYards, 15, 40);
        var receivingUnit = SpecialTeam(receivingRoster, returner);
        var coverageUnit = SpecialTeam(kickingRoster, kicker);
        var coverageTackler = CoverageTackler(coverageUnit, kicker, kickoffRandom);
        var kickoff = new GameSnapEvent
        {
            Sequence = session.Events.Count + 1, PossessionNumber = session.PossessionNumber,
            Quarter = session.Quarter, ClockSeconds = session.ClockSeconds,
            OffenseTeamName = session.PossessionTeamName, DefenseTeamName = session.KickoffTeamName,
            OffensivePlayId = string.Empty, DefensivePlayId = string.Empty,
            Down = 0, Distance = 0, StartYardLine = start, BallEndYardLine = end,
            KickLandingYardLine = landing,
            Yards = 0, ReturnYards = returnYards, Outcome = SnapOutcome.Kickoff,
            KickerId = kicker?.PlayerId, ReturnerId = returner?.PlayerId, BallCarrierId = returner?.PlayerId,
            PrimaryDefenderId = coverageTackler?.PlayerId,
            OffensePersonnel = receivingUnit.Select(player => player.PlayerId).ToArray(),
            DefensePersonnel = coverageUnit.Select(player => player.PlayerId).ToArray()
        };
        session.Events.Add(kickoff);
        session.YardLine = end; session.Down = 1; session.Distance = Math.Min(10, 100 - end);
        session.KickoffPending = false;
        return kickoff;
    }

    private static void QueueKickoff(GameDaySession session, string kickingTeamName)
    {
        session.KickoffPending = true;
        session.KickoffTeamName = kickingTeamName;
        session.YardLine = 25; session.Down = 1; session.Distance = 10;
    }

    private static DynastyPlayer? Kicker(IReadOnlyList<DynastyPlayer> roster) => roster
        .Where(player => player.CurrentInjury is null && !player.IsRedshirted)
        .OrderBy(player => player.Position == Position.K ? 0 : 1)
        .ThenByDescending(player => player.OverallRating).FirstOrDefault();

    private static DynastyPlayer? Returner(IReadOnlyList<DynastyPlayer> roster) => roster
        .Where(player => player.CurrentInjury is null && !player.IsRedshirted)
        .OrderBy(player => player.Position is Position.WR or Position.RB ? 0 : 1)
        .ThenByDescending(player => player.SpeedRating).ThenByDescending(player => player.AgilityRating).FirstOrDefault();

    private static DynastyPlayer[] PassTargets(OffensivePlayDefinition play, IReadOnlyList<DynastyPlayer> personnel)
    {
        var eligible = personnel.Where(player => player.Position is Position.WR or Position.TE or Position.RB).ToArray();
        if (eligible.Length == 0) return [];
        var family = GameDayAnimationPlanner.RouteFamily(play);
        var preferred = family switch
        {
            GameDayRouteFamily.Screen when play.Name.Contains("Bubble", StringComparison.OrdinalIgnoreCase) ||
                play.Name.Contains("WR", StringComparison.OrdinalIgnoreCase) =>
                eligible.Where(player => player.Position == Position.WR).ToArray(),
            GameDayRouteFamily.Screen => eligible.Where(player => player.Position == Position.RB).ToArray(),
            GameDayRouteFamily.Verticals => eligible.Where(player => player.Position == Position.WR).ToArray(),
            GameDayRouteFamily.Mesh => eligible.Where(player => player.Position is Position.WR or Position.TE).ToArray(),
            GameDayRouteFamily.Flood or GameDayRouteFamily.Smash => eligible.Where(player => player.Position is Position.WR or Position.TE).ToArray(),
            _ => eligible
        };
        return preferred.Length == 0 ? eligible : preferred;
    }

    private static IReadOnlyList<DynastyPlayer> SpecialTeam(IReadOnlyList<DynastyPlayer> roster, DynastyPlayer? featured) =>
        roster.Where(player => player.CurrentInjury is null && !player.IsRedshirted)
            .OrderBy(player => featured is not null && player.PlayerId == featured.PlayerId ? 0 : 1)
            .ThenByDescending(player => player.SpeedRating + player.AwarenessRating)
            .Take(11).ToArray();

    private static DynastyPlayer? CoverageTackler(IReadOnlyList<DynastyPlayer> coverageUnit,
        DynastyPlayer? kicker, Random random)
    {
        var candidates = coverageUnit.Where(player => player.PlayerId != kicker?.PlayerId)
            .OrderByDescending(player => player.SpeedRating + (player.Skills?.Tackling ?? player.AwarenessRating) + player.AwarenessRating)
            .Take(6).ToArray();
        return candidates.Length == 0 ? null : candidates[random.Next(candidates.Length)];
    }

    private static OffensiveConcept ConceptOf(GameDaySession session, GameSnapEvent snap) => snap.Outcome == SnapOutcome.Spike
        ? OffensiveConcept.QuickPass : snap.Outcome == SnapOutcome.Kneel ? OffensiveConcept.InsideRun
        : PlayEditorService.OffensiveLibrary((snap.OffenseTeamName == session.Game.HomeTeamName ? session.HomeProfile : session.AwayProfile).GamePlan ?? new TeamGamePlan { TeamName = snap.OffenseTeamName }).FirstOrDefault(p => p.Id == snap.OffensivePlayId)?.Concept ?? OffensiveConcept.InsideRun;

    private static bool IsTurnover(SnapOutcome outcome) =>
        outcome is SnapOutcome.Turnover or SnapOutcome.Interception or SnapOutcome.Fumble;

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
