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
        var kicker = GameDayCoachingService.Specialist(session, session.PossessionTeamName, Kicker(home ? session.HomeRoster : session.AwayRoster));
        return GameWeatherService.KickProbability(session, GameDayMatchupService.FieldGoalProbability(117 - session.YardLine,
            kicker?.Skills?.KickAccuracy ?? profile.SpecialTeamsRating, kicker?.Skills?.KickPower ?? profile.SpecialTeamsRating));
    }
    public static GameDaySession Start(ScheduledGame game, TeamSimulationProfile home,
        TeamSimulationProfile away, IReadOnlyList<DynastyPlayer> roster, GameDayMode mode,
        IReadOnlyList<DepthChartAssignment>? assignments = null, FootballRules? rules = null)
    {
        roster = DepthChartService.ProjectRoster(roster, assignments ?? []);
        roster = PlayerJerseyService.Assign(roster.Where(p => p.TeamName.Equals(game.HomeTeamName, StringComparison.OrdinalIgnoreCase) ||
            p.TeamName.Equals(game.AwayTeamName, StringComparison.OrdinalIgnoreCase)).ToArray());
        var homeRoster = roster.Where(player =>
            player.TeamName.Equals(game.HomeTeamName, StringComparison.OrdinalIgnoreCase)).ToArray();
        var awayRoster = roster.Where(player =>
            player.TeamName.Equals(game.AwayTeamName, StringComparison.OrdinalIgnoreCase)).ToArray();
        var gameRoster = homeRoster.Concat(awayRoster).ToArray();
        var session = new GameDaySession
        {
            Rules = rules ?? FootballRules.College, ClockSeconds = (rules ?? FootballRules.College).QuarterSeconds,
            Game = game, HomeProfile = home, AwayProfile = away, Roster = gameRoster,
            HomeRoster = homeRoster, AwayRoster = awayRoster,
            PlayersById = gameRoster.ToDictionary(player => player.PlayerId),
            HomeDepthChart = PersonnelRotationService.BuildDepthChart(homeRoster, game.HomeTeamName),
            AwayDepthChart = PersonnelRotationService.BuildDepthChart(awayRoster, game.AwayTeamName),
            Weather = GameWeatherService.ForGame(game),
            Mode = mode, Random = new Random(game.SimulationSeed), PossessionTeamName = game.HomeTeamName,
            KickoffTeamName = game.AwayTeamName, KickoffPending = true
        };
        CoinTossService.Apply(session, CoinTossService.Toss(game));
        return session;
    }

    public static GameSnapEvent Step(GameDaySession session, string? userPlayId = null,
        string? userDefensivePlayId = null)
    {
        if (session.IsComplete) throw new InvalidOperationException("The game is complete.");
        if (session.UserCoachRole != StaffRole.HeadCoach)
        {
            if (session.UserCoachRole == StaffRole.DefensiveCoordinator || session.PossessionTeamName != session.ControlledTeamName) userPlayId = null;
            if (session.UserCoachRole == StaffRole.OffensiveCoordinator || session.PossessionTeamName == session.ControlledTeamName) userDefensivePlayId = null;
        }
        if (session.TryPending) return ResolveTry(session, userPlayId);
        if (session.KickoffPending) return ResolveKickoff(session);
        if (session.Quarter is 2 or 4 && session.ClockSeconds <= 90 && (!session.ClockStopped || session.ClockRestartsOnReady))
        {
            var trailing = session.HomeScore < session.AwayScore ? session.Game.HomeTeamName :
                session.AwayScore < session.HomeScore ? session.Game.AwayTeamName : null;
            if (trailing is not null && trailing != session.PossessionTeamName &&
                (session.Mode != GameDayMode.Play || trailing != session.ControlledTeamName) && TryCallTimeout(session, trailing)) return session.Events[^1];
        }
        var lastSeconds = session.OvertimeRound == 0 && session.Quarter is 2 or 4 && session.ClockSeconds is > 0 and <= 8;
        var finalAdvice = lastSeconds && userPlayId is null ? GameDayRecommendationService.Recommend(session,
            (session.PossessionTeamName == session.Game.HomeTeamName ? session.HomeProfile : session.AwayProfile).GamePlan ?? new TeamGamePlan { TeamName=session.PossessionTeamName }) : null;
        var quickShot = lastSeconds && (userPlayId == "special-field-goal" ||
            PlaybookCatalog.OffensivePlays.Any(p => p.Id == userPlayId && p.Name == "Hail Mary") ||
            finalAdvice?.Play?.Name == "Hail Mary" || finalAdvice?.Type == RecommendedCallType.FieldGoal);
        var clockEvent = PrepareSnap(session, userPlayId == "special-spike", quickShot);
        if (clockEvent is not null) return clockEvent;
        if (userPlayId is "special-kneel" or "special-spike") return ClockPlay(session, userPlayId == "special-spike");
        if (userPlayId is null && ShouldKneel(session)) return ClockPlay(session, false);
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
        if (string.IsNullOrWhiteSpace(userPlayId) && advice.Play is not null &&
            (offensivePossessionIndex >= offensePlan.OpeningPlayIds.Count && offensivePossessionIndex >= offensePlan.OpeningScript.Count ||
             session.Down >= 3 && session.Distance >= 3 || GameDayRecommendationService.PassingUrgency(session) >= 2 || advice.Play.Name == "Hail Mary"))
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
            var kicker = GameDayCoachingService.Specialist(session, offense.TeamName, Kicker(offenseRoster, special == "special-punt"));
            var made = special == "special-field-goal" &&
                session.Random.NextDouble() < FieldGoalProbability(session);
            var puntLanding = special == "special-punt"
                ? Math.Clamp(kickStart + session.Random.Next(34, 51) + (int)Math.Round(GameWeatherService.Tailwind(session, offense.TeamName) * .3), kickStart + 1, 99) : 100;
            var touchback = special == "special-punt" && puntLanding >= 95;
            var returner = Returner(defenseRoster);
            var kickingUnit = special == "special-field-goal" ? PlaceKickTeam(offenseRoster, kicker) : SpecialTeam(offenseRoster, kicker);
            var returnUnit = special == "special-field-goal" ? PlaceKickDefense(defenseRoster) : SpecialTeam(defenseRoster, returner);
            var hangTime = special == "special-punt" ? 3.6 + session.Random.NextDouble() * 1.8 : 0;
            var coverage = PuntCoverageService.Evaluate(puntLanding - kickStart, hangTime, kickingUnit, kicker?.PlayerId, returner?.AwarenessRating ?? 60);
            var fairCatch = special == "special-punt" && !touchback && coverage.FairCatch;
            var returnYards = special == "special-punt" && !touchback && !fairCatch ? session.Random.Next(0, 13) : 0;
            var newSpot = made ? 25 : special == "special-punt"
                ? touchback ? 20 : Math.Clamp(100 - puntLanding + returnYards, 5, 80)
                : Math.Clamp(100 - Math.Max(20, kickStart - 7), 10, 80);
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
                PrimaryDefenderId = fairCatch ? coverage.DefenderId : coverageTackler?.PlayerId,
                KickHangTimeSeconds = hangTime, NearestCoverageYardsAtCatch = coverage.DistanceAtCatch,
                OffensePersonnel = kickingUnit.Select(player => player.PlayerId).ToArray(),
                DefensePersonnel = returnUnit.Select(player => player.PlayerId).ToArray(),
                Outcome = special == "special-punt" ? SnapOutcome.Punt :
                    made ? SnapOutcome.FieldGoal : SnapOutcome.MissedFieldGoal
            };
            session.Events.Add(kick);
            ConsumeLiveClock(session, session.Random.Next(5, 13));
            if (made) { if (homeOnOffense) session.HomeScore += 3; else session.AwayScore += 3; }
            ChangePossession(session, newSpot);
            if (made && !session.IsComplete && (session.OvertimeRound == 0 || session.Rules.TimedOvertime)) QueueKickoff(session, offense.TeamName);
            AdvanceQuarter(session);
            return kick;
        }

        var offensePersonnel = PersonnelRotationService.SelectOffense(offenseDepthChart, offense.TeamName,
            call.Offense.Personnel, offenseFatigue, session.Random);
        var defensePersonnel = PersonnelRotationService.SelectDefense(defenseDepthChart, defense.TeamName,
            call.Defense.Personnel, defenseFatigue, session.Random);
        offensePersonnel = GameDayCoachingService.ApplySubstitutions(session, offensePersonnel);
        defensePersonnel = GameDayCoachingService.ApplySubstitutions(session, defensePersonnel);
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
        advantage += GameMoraleService.MatchupShift(session, offense.TeamName, defense.TeamName);
        advantage += GameDayCoachingService.IndividualTalkShift(session, offensePlayers) - GameDayCoachingService.IndividualTalkShift(session, defensePlayers);
        advantage += GameDayMatchupService.FormationAdvantage(call.Offense, call.Defense, isPass);
        advantage += GameDayMatchupService.PersonnelAdvantage(offensePlayers, defensePlayers, isPass);
        if (!session.Game.IsNeutralSite)
            advantage += homeOnOffense ? HomeFieldRatingBonus : -HomeFieldRatingBonus;
        var read = isPass ? PassingReadService.Read(call.Offense, call.Defense, offensePlayers, defensePlayers, qb,
            offense.OffensiveCoordinatorGameManagement, session.Random) : new PassingRead(null, null, 0, 0, 2, 0, 0);
        var turnover = session.Random.NextDouble() < (isPass ? read.InterceptionChance : Math.Clamp(.025 - advantage * .0008 + GameWeatherService.FumblePenalty(session.Weather), .008, .075));
        var sack = isPass && read.RusherCount > 0 && !turnover && session.Random.NextDouble() < Math.Clamp(.065 - read.Protection * .0018 + (read.ReleaseSeconds - 2) * .015, .015, .18);
        var hailMary = call.Offense.Name == "Hail Mary";
        var complete = isPass && read.Target is not null && !turnover && !sack && session.Random.NextDouble() <
            Math.Clamp(read.CatchChance - .055 + advantage * .0015 - (hailMary ? read.Separation < 2 ? .18 : .05 : 0) -
                GameWeatherService.CatchPenalty(session.Weather, call.Offense.Concept == OffensiveConcept.DeepPass), .06, .9);
        int RoundYards(double expected)
        {
            var floor = Math.Floor(expected);
            return (int)floor + (session.Random.NextDouble() < expected - floor ? 1 : 0);
        }
        var yards = turnover ? 0 : sack ? -session.Random.Next(3, 11) : scramble
            ? Math.Max(-3, RoundYards(session.Random.Next(-1, 10) + (mobility - 65) * .10))
            : isPass ? 0 : Math.Max(-4, RoundYards(session.Random.Next(-2, 11) + advantage * .09));

        var carrier = isPass ? null : scramble || designedKeep ? qb : Pick(offensePlayers.Where(player => player.Position == Position.RB).ToArray(), session.Random) ?? qb;
        var target = isPass ? read.Target : null;
        var gain = complete ? ReceivingGainService.Resolve(call.Offense, target!, read.Defender,
            read.Separation, advantage, session.YardLine, session.Random) : new ReceivingGain(0, 0);
        if (complete) yards = gain.TotalYards;
        if (hailMary && complete) { yards = Math.Min(100 - session.YardLine, Math.Max(1, yards)); gain = new(yards, 0); }
        var brokeTackle = false;
        if (!isPass && !turnover && !sack && carrier is not null && yards > 0)
        {
            // A broken tackle and open-field speed can extend a run; no fixed maximum gain.
            var tackling = defensePlayers.Select(p => (double)(p.Skills?.Tackling ?? p.OverallRating)).DefaultIfEmpty(65).Average();
            var escape = (carrier.SpeedRating + carrier.AgilityRating + (carrier.Skills?.BreakTackle ?? carrier.OverallRating)) / 3d;
            if (session.Random.NextDouble() < Math.Clamp(.065 + (escape - tackling) * .002, .015, .16))
                { brokeTackle = true; yards += session.Random.Next(12, 29) + (session.Random.NextDouble() < .18 ? session.Random.Next(10, 41) : 0); }
        }
        yards = Math.Clamp(yards, 1 - session.YardLine, 100 - session.YardLine);
        var liveSeconds = Math.Clamp((int)Math.Round(isPass ? read.ReleaseSeconds + 1.2 + Math.Max(0, yards) / 8d : 3 + Math.Max(0, yards) / 6d), 4, 17);

        var carrierLane = .50;
        if (isPass && target is not null)
        {
            var role = offensePlayers.TakeWhile(p => p.PlayerId != target.PlayerId).Count(p => p.Position == target.Position);
            var custom = call.Offense.Assignments.FirstOrDefault(a => a.Position == target.Position && a.RoleIndex == role);
            var alignment = GameDayAnimationPlanner.OffensiveAlignment(call.Offense, target.Position, role);
            var route = GameDayAnimationPlanner.AssignedRoute(call.Offense, target.Position, role);
            var eligibleIndex = offensePlayers.TakeWhile(p => p.PlayerId != target.PlayerId).Count(p => p.Position is Position.WR or Position.TE or Position.RB);
            carrierLane = GameDayAnimationPlanner.RouteEndLane(route, target.Position, eligibleIndex, alignment.Y, true);
        }
        else if (sack) carrierLane = call.Offense.Name.Contains("Boot", StringComparison.OrdinalIgnoreCase) ? .25 : .50;
        else if (call.Offense.Concept == OffensiveConcept.OutsideRun) carrierLane = session.Events.Count % 2 == 0 ? .22 : .78;
        else if (designedKeep && call.Offense.Name.Contains("Boot", StringComparison.OrdinalIgnoreCase)) carrierLane = .25;
        var pursuit = DefensivePursuitService.Select(defensePlayers, call.Defense, yards, carrierLane,
            isPass, sack, gain.AirYards, read.ReleaseSeconds, read.Defender?.PlayerId, read.Separation, session.Random);
        var defender = isPass && (turnover || !complete && !sack) ? read.Defender : pursuit.Tackler;
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
            CarrierLane = carrierLane, TackleAttemptIds = pursuit.AttemptIds,
            AirYards = complete ? gain.AirYards : 0, YardsAfterCatch = complete ? gain.YardsAfterCatch : 0,
            PreSnapSeconds = session.LastPreSnapSeconds, LiveBallSeconds = liveSeconds,
            PassReleaseProgress = Math.Clamp(read.ReleaseSeconds / 5, .3, .75), TargetSeparation = read.Separation,
            IsDrop = isPass && !complete && !turnover && !sack && read.Separation >= 2 && session.Random.NextDouble() < .18,
            BrokeTackle = brokeTackle || complete && gain.BrokeTackle, IsQuarterbackScramble = scramble, IsQuarterbackKeep = designedKeep,
            IsBootleg = call.Offense.Name.Contains("Boot", StringComparison.OrdinalIgnoreCase),
            QuarterbackId = qb?.PlayerId, BallCarrierId = carrier?.PlayerId, TargetId = target?.PlayerId,
            PrimaryDefenderId = defender?.PlayerId, OffensePersonnel = offensePersonnel.PlayerIds,
            DefensePersonnel = defensePersonnel.PlayerIds
        };
        if (turnover && defender is not null)
            snap = TurnoverReturnService.Resolve(snap, defender, offensePlayers, call.Offense.Concept, session.Random);
        snap = snap with { IsPassBreakup = snap.Outcome == SnapOutcome.Incompletion && !snap.IsDrop &&
            defender is not null && read.Separation <= 1.5 };
        session.Events.Add(snap);
        PersonnelRotationService.ApplySnap(offenseFatigue, offenseRoster, offensePersonnel, Math.Abs(yards) >= 20);
        PersonnelRotationService.ApplySnap(defenseFatigue, defenseRoster, defensePersonnel, Math.Abs(yards) >= 20);
        ConsumeLiveClock(session, snap.LiveBallSeconds);
        if (turnover && snap.TurnoverYardLine.HasValue) ApplyTurnoverReturn(session, snap);
        else AdvanceState(session, homeOnOffense, yards, turnover, touchdown, outcome == SnapOutcome.Incompletion);
        return session.Events[^1];
    }

    public static bool TryCallTimeout(GameDaySession session, string teamName)
    {
        if (session.IsComplete || session.KickoffPending || session.ClockStopped && !session.ClockRestartsOnReady && session.OvertimeRound == 0) return false;
        if (teamName == session.Game.HomeTeamName && session.HomeTimeouts > 0) session.HomeTimeouts--;
        else if (teamName == session.Game.AwayTeamName && session.AwayTimeouts > 0) session.AwayTimeouts--;
        else return false;
        session.ClockStopped = true; session.ClockRestartsOnReady = false; session.PlayClockSeconds = 25;
        var timeout = ClockEvent(session, $"{teamName} calls timeout.");
        session.Events[^1] = timeout with { TimeoutTeamName = teamName,
            HomeTimeoutsRemaining = session.HomeTimeouts, AwayTimeoutsRemaining = session.AwayTimeouts };
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
        var finished = session.Game with
        {
            CoachTalks = session.CoachTalks.ToArray(),
            Weather = session.Weather, HomeEndingMorale = GameMoraleService.InGame(session, session.Game.HomeTeamName),
            AwayEndingMorale = GameMoraleService.InGame(session, session.Game.AwayTeamName),
            HasPlayed = true, HomeScore = session.HomeScore, AwayScore = session.AwayScore,
            HomeStats = homeStats, AwayStats = awayStats,
            HomeGamePlan = session.HomeProfile.GamePlan, AwayGamePlan = session.AwayProfile.GamePlan,
            HomePregameRating = (session.HomeProfile.OffenseRating + session.HomeProfile.DefenseRating) / 2,
            AwayPregameRating = (session.AwayProfile.OffenseRating + session.AwayProfile.DefenseRating) / 2,
            Drives = BuildDrives(session),
            Highlights = GameHighlightService.Select(session.Events),
            HighlightPlayers = GameHighlightService.Participants(session),
            PeriodScores = BuildPeriodScores(session),
            PlayerStats = BuildPlayerStats(session)
        };
        return finished with { PlayerOfGame = PlayerOfGameService.Select(finished, finished.PlayerStats) };
    }

    private static IReadOnlyList<GameDriveSummary> BuildDrives(GameDaySession session) => session.Events
        .Where(item => item.Outcome is not (SnapOutcome.Kickoff or SnapOutcome.ClockStoppage))
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
            var points = group.Where(item => !item.DefensiveScore).Sum(item => item.Outcome == SnapOutcome.Touchdown ? 6 : item.Outcome == SnapOutcome.FieldGoal ? 3 : item.Points);
            return new GameDriveSummary
            {
                TeamName = group.Key.OffenseTeamName, PossessionNumber = group.Key.PossessionNumber,
                IsOvertime = first.Quarter > 4, Concept = concept, OffensivePlayId = first.OffensivePlayId, DefensivePlayId = first.DefensivePlayId,
                PlayCallSource = first.CallSource,
                PassAttempts = scrimmage.Count(item => IsPass(item) && item.Outcome != SnapOutcome.Sack),
                RushAttempts = scrimmage.Count(item => !IsPass(item) || item.Outcome == SnapOutcome.Sack),
                PassYards = scrimmage.Where(item => IsPass(item) && item.Outcome is SnapOutcome.Completion or SnapOutcome.Touchdown).Sum(item => item.Yards),
                RushYards = scrimmage.Where(item => !IsPass(item) || item.Outcome == SnapOutcome.Sack).Sum(item => item.Yards),
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
                OriginalRecruitingStars = player.OriginalRecruitingStars, RecruitingStarsEstimated = player.RecruitingStarsEstimated, HeightInches = player.HeightInches, WeightPounds = player.WeightPounds, Appearance = player.Appearance, JerseyNumber = player.JerseyNumber, Games = 1 };
        }
        void Save(PlayerSeasonStatLine line) => lines[line.PlayerId] = line;
        foreach (var snap in session.Events)
        {
            foreach (var id in snap.OffensePersonnel.Concat(snap.DefensePersonnel).Distinct())
                if (session.PlayersById.ContainsKey(id)) Save(Line(id));
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
                    PassYards = qb.PassYards + (snap.Outcome is SnapOutcome.Completion or SnapOutcome.Touchdown ? snap.Yards : 0),
                    PassingTouchdowns = qb.PassingTouchdowns + (snap.Outcome == SnapOutcome.Touchdown ? 1 : 0),
                    InterceptionsThrown = qb.InterceptionsThrown + (snap.Outcome is SnapOutcome.Turnover or SnapOutcome.Interception ? 1 : 0),
                    Turnovers = qb.Turnovers + (snap.Outcome is SnapOutcome.Turnover or SnapOutcome.Interception ? 1 : 0)
                };
                Save(qb);
            }
            if ((!pass || snap.Outcome == SnapOutcome.Sack) && (snap.Outcome == SnapOutcome.Sack ? snap.QuarterbackId : snap.BallCarrierId) is Guid carrierId)
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
                    ReceivingYards = Line(targetId).ReceivingYards + (caught ? snap.Yards : 0),
                    ReceivingTouchdowns = Line(targetId).ReceivingTouchdowns + (snap.Outcome == SnapOutcome.Touchdown ? 1 : 0),
                    Receiving20Plus = Line(targetId).Receiving20Plus + (caught && snap.Yards >= 20 ? 1 : 0),
                    YardsAfterCatch = Line(targetId).YardsAfterCatch + (caught ? snap.YardsAfterCatch : 0),
                    Drops = Line(targetId).Drops + (snap.IsDrop ? 1 : 0),
                    Touchdowns = Line(targetId).Touchdowns + (snap.Outcome == SnapOutcome.Touchdown ? 1 : 0)
                };
                Save(target);
            }
            if (snap.IsPassBreakup && snap.PrimaryDefenderId is Guid breakupId)
                Save(Line(breakupId) with { PassesDefended = Line(breakupId).PassesDefended + 1 });
            if (snap.PrimaryDefenderId is Guid defenderId && snap.Outcome is SnapOutcome.Run or SnapOutcome.Scramble or SnapOutcome.Completion or SnapOutcome.Sack or SnapOutcome.Fumble or SnapOutcome.Interception or SnapOutcome.Turnover)
            {
                var defender = Line(defenderId) with
                {
                    Tackles = Line(defenderId).Tackles + (snap.Outcome is SnapOutcome.Interception or SnapOutcome.Turnover ? 0 : 1),
                    TacklesForLoss = Line(defenderId).TacklesForLoss + (snap.Yards < 0 ? 1 : 0),
                    Sacks = Line(defenderId).Sacks + (snap.Outcome == SnapOutcome.Sack ? 1 : 0),
                    Interceptions = Line(defenderId).Interceptions + (snap.Outcome == SnapOutcome.Interception || pass && snap.Outcome == SnapOutcome.Turnover ? 1 : 0),
                    DefensiveReturnYards = Line(defenderId).DefensiveReturnYards + (snap.TurnoverYardLine.HasValue ? snap.ReturnYards : 0),
                    DefensiveTouchdowns = Line(defenderId).DefensiveTouchdowns + (snap.DefensiveScore ? 1 : 0),
                    Touchdowns = Line(defenderId).Touchdowns + (snap.DefensiveScore ? 1 : 0),
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
            var roster = session.Roster.Where(p => p.TeamName.Equals(team, StringComparison.OrdinalIgnoreCase)).ToArray();
            var specialEvents = session.Events.Where(e => e.OffenseTeamName.Equals(team, StringComparison.OrdinalIgnoreCase) &&
                e.Outcome is SnapOutcome.FieldGoal or SnapOutcome.MissedFieldGoal or SnapOutcome.ExtraPoint or SnapOutcome.MissedExtraPoint or SnapOutcome.Punt);
            foreach (var group in specialEvents.GroupBy(e => e.KickerId ?? Kicker(roster, e.Outcome == SnapOutcome.Punt)?.PlayerId))
            {
                if (group.Key is not Guid id) continue;
                var events = group.ToArray();
                var punts = events.Where(e => e.Outcome == SnapOutcome.Punt).ToArray();
                Save(Line(id) with {
                    FieldGoalsMade = events.Count(e => e.Outcome == SnapOutcome.FieldGoal),
                    FieldGoalsAttempted = events.Count(e => e.Outcome is SnapOutcome.FieldGoal or SnapOutcome.MissedFieldGoal),
                    ExtraPointsMade = events.Count(e => e.Outcome == SnapOutcome.ExtraPoint),
                    ExtraPointsAttempted = events.Count(e => e.Outcome is SnapOutcome.ExtraPoint or SnapOutcome.MissedExtraPoint),
                    Punts = punts.Length, PuntYards = punts.Sum(e => Math.Max(0, e.KickLandingYardLine - e.StartYardLine)),
                    LongPunt = punts.Select(e => Math.Max(0, e.KickLandingYardLine - e.StartYardLine)).DefaultIfEmpty().Max(),
                    PuntsInside20 = punts.Count(e => !e.IsTouchback && e.KickLandingYardLine >= 80),
                    PuntsInside10 = punts.Count(e => !e.IsTouchback && e.KickLandingYardLine >= 90) });
            }
        }
        foreach (var snap in session.Events.Where(e => e.TurnoverYardLine.HasValue && !e.DefensiveScore && e.ReturnTacklerId.HasValue))
        { var tackle = Line(snap.ReturnTacklerId!.Value); Save(tackle with { Tackles = tackle.Tackles + 1 }); }
        return lines.Values.ToArray();
    }

    private static GameTeamStats BuildStats(GameDaySession session, string teamName)
    {
        var snaps = session.Events.Where(item => item.OffenseTeamName.Equals(teamName, StringComparison.OrdinalIgnoreCase) &&
            item.Outcome != SnapOutcome.Kickoff).ToArray();
        var kickReturns = session.Events.Where(item => item.Outcome == SnapOutcome.Kickoff && !item.IsOnsideKick && !item.IsTouchback &&
            item.OffenseTeamName.Equals(teamName, StringComparison.OrdinalIgnoreCase)).ToArray();
        var puntReturns = session.Events.Where(item => item.Outcome == SnapOutcome.Punt &&
            item.DefenseTeamName.Equals(teamName, StringComparison.OrdinalIgnoreCase) && item.ReturnYards > 0).ToArray();
        var scrimmage = snaps.Where(IsScrimmage).ToArray();
        var passes = scrimmage.Where(item => !item.IsQuarterbackKeep && !item.IsQuarterbackScramble && item.Outcome != SnapOutcome.Scramble &&
            ConceptOf(session, item) is
            OffensiveConcept.QuickPass or OffensiveConcept.PlayAction or OffensiveConcept.DeepPass).ToArray();
        var runs = scrimmage.Except(passes).Concat(passes.Where(item => item.Outcome == SnapOutcome.Sack)).ToArray();
        return new GameTeamStats
        {
            Efficiency = FootballEfficiency.Capture(scrimmage),
            Possessions = scrimmage.Select(item => item.PossessionNumber).Distinct().Count(),
            YardsAfterCatch = passes.Sum(item => item.YardsAfterCatch),
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
        session.ClockStopped = incomplete || turnover || touchdown;
        session.ClockRestartsOnReady = false;
        session.PlayClockSeconds = 40;
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
                if (session.Rules.StopClockOnFirstDown && session.Quarter is 2 or 4 && session.ClockSeconds <= 120) { session.ClockStopped = true; session.ClockRestartsOnReady = true; } }
            else { session.Distance -= yards; session.Down++; }
            if (session.Down > 4)
                ChangePossession(session, Math.Clamp(100 - session.YardLine, 1, 99));
        }
        CheckTwoMinuteTimeout(session);
        AdvanceQuarter(session);
    }

    private static void AdvanceQuarter(GameDaySession session)
    {
        if (session.Rules.TimedOvertime && session.OvertimeRound > 0)
        {
            if (session.TryPending || session.ClockSeconds > 0) return;
            if (!session.Rules.Postseason || session.HomeScore != session.AwayScore && session.OvertimePossessions >= 2)
                session.IsComplete = true;
            else { session.Quarter++; session.ClockSeconds = session.Rules.PlayoffOvertimeSeconds; }
            return;
        }
        if (session.OvertimeRound > 0 || session.TryPending || session.ClockSeconds > 0) return;
        session.ClockSeconds = 0;
        if (session.Quarter == 4)
        {
            session.KickoffPending = false;
            if (session.HomeScore != session.AwayScore) session.IsComplete = true;
            else if (session.Rules.TimedOvertime) StartProfessionalOvertime(session);
            else StartOvertimeRound(session);
            return;
        }
        session.Quarter++; session.ClockSeconds = session.Rules.QuarterSeconds;
        session.PlayClockSeconds = 25;
        if (session.Quarter == 3)
        {
            session.HomeTimeouts = session.AwayTimeouts = 3;
            var firstReceiver = string.IsNullOrEmpty(session.OpeningReceivingTeam) ? session.Game.HomeTeamName : session.OpeningReceivingTeam;
            session.PossessionTeamName = firstReceiver == session.Game.HomeTeamName ? session.Game.AwayTeamName : session.Game.HomeTeamName;
            session.PossessionNumber++;
            QueueKickoff(session, firstReceiver);
        }
    }

    public static void ApplyTurnoverReturn(GameDaySession session, GameSnapEvent snap)
    {
        if (snap.TurnoverYardLine is null) throw new ArgumentException("Turnover spot is missing.");
        if (snap.DefensiveScore)
        {
            if (snap.DefenseTeamName == session.Game.HomeTeamName) session.HomeScore += 6; else session.AwayScore += 6;
            session.PossessionTeamName = snap.DefenseTeamName; session.PossessionNumber++;
            session.YardLine = 97; session.Down = 1; session.Distance = 3;
            session.ClockStopped = true; session.ClockRestartsOnReady = false;
            if (session.OvertimeRound > 0) { session.IsComplete = true; session.KickoffPending = false; }
            else session.TryPending = true;
        }
        else ChangePossession(session, snap.IsTouchback ? 20 : Math.Clamp(100 - snap.BallEndYardLine, 1, 99));
        CheckTwoMinuteTimeout(session); AdvanceQuarter(session);
    }

    private static void ChangePossession(GameDaySession session, int yardLine)
    {
        if (session.Rules.TimedOvertime && session.OvertimeRound > 0)
        {
            session.OvertimePossessions++;
            if (session.OvertimePossessions >= 2 && session.HomeScore != session.AwayScore) { session.IsComplete = true; session.KickoffPending = false; return; }
        }
        else if (session.OvertimeRound > 0) { EndOvertimePossession(session); return; }
        session.PossessionTeamName = session.PossessionTeamName.Equals(session.Game.HomeTeamName, StringComparison.OrdinalIgnoreCase)
            ? session.Game.AwayTeamName : session.Game.HomeTeamName;
        session.PossessionNumber++; session.Down = 1; session.Distance = Math.Min(10, 100 - yardLine); session.YardLine = yardLine;
        session.ClockStopped = true; session.ClockRestartsOnReady = false; session.PlayClockSeconds = 25;
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
        var kicker = GameDayCoachingService.Specialist(session, kickingRoster.FirstOrDefault()?.TeamName ?? "", Kicker(kickingRoster));
        var returner = Returner(receivingRoster);
        var kickoffRandom = new Random(unchecked(session.Game.SimulationSeed * 397 ^ session.PossessionNumber * 7919 ^
            session.Quarter * 31 ^ 0x4B1C));
        var returnYards = kickoffRandom.Next(16, 34);
        var start = 65;
        var landing = kickoffRandom.Next(1, 8);
        landing = Math.Clamp(landing - (int)Math.Round(GameWeatherService.Tailwind(session, session.KickoffTeamName) * .22), 1, 15);
        var coverageSpeed = kickingRoster.Where(p => p.Position is Position.CB or Position.SS or Position.OLB).Select(p => (double)p.SpeedRating).DefaultIfEmpty(70).Average();
        var returnSpeed = returner?.SpeedRating ?? 70;
        if (kickoffRandom.NextDouble() < Math.Clamp(.025 + (returnSpeed - coverageSpeed) * .0004, .01, .05))
            returnYards += kickoffRandom.Next(18, 85);
        var touchback = kickoffRandom.NextDouble() < Math.Clamp(.40 + ((kicker?.Skills?.KickPower ?? 70) - 60) * .008 + GameWeatherService.Tailwind(session, session.KickoffTeamName) * .004, .20, .80);
        var end = touchback ? session.Rules.KickoffTouchbackYardLine : Math.Clamp(landing + returnYards, 1, 100);
        returnYards = touchback ? 0 : end - landing;
        if (touchback) landing = -3;
        var touchdown = end == 100;
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
            KickerId = kicker?.PlayerId, ReturnerId = touchback ? null : returner?.PlayerId, BallCarrierId = touchback ? null : returner?.PlayerId,
            IsTouchback = touchback,
            PrimaryDefenderId = touchdown || touchback ? null : coverageTackler?.PlayerId, Points = touchdown ? 6 : 0,
            OffensePersonnel = receivingUnit.Select(player => player.PlayerId).ToArray(),
            DefensePersonnel = coverageUnit.Select(player => player.PlayerId).ToArray()
        };
        session.Events.Add(kickoff);
        session.YardLine = end; session.Down = 1; session.Distance = Math.Min(10, 100 - end);
        session.KickoffPending = false;
        if (touchdown)
        {
            if (receivingHome) session.HomeScore += 6; else session.AwayScore += 6;
            session.TryPending = true; session.YardLine = 97;
        }
        session.ClockStopped = true; session.ClockRestartsOnReady = false; session.PlayClockSeconds = 25;
        if (!touchback) ConsumeLiveClock(session, Math.Clamp(2 + returnYards / 6, 3, 9));
        CheckTwoMinuteTimeout(session); AdvanceQuarter(session);
        return kickoff;
    }

    private static void QueueKickoff(GameDaySession session, string kickingTeamName)
    {
        session.KickoffPending = true;
        session.KickoffTeamName = kickingTeamName;
        session.YardLine = 25; session.Down = 1; session.Distance = 10;
    }

    private static DynastyPlayer? Kicker(IReadOnlyList<DynastyPlayer> roster, bool punting = false) => roster
        .Where(player => player.CurrentInjury is null && !player.IsRedshirted)
        .OrderBy(player => player.Position == Position.K ? 0 : 1)
        .ThenBy(player => player.NcaaPositionCode == (punting ? 20 : 19) ? 0 : 1)
        .ThenBy(player => player.DepthChartOrder > 0 ? player.DepthChartOrder : int.MaxValue)
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
