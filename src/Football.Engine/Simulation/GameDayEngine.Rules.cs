using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static partial class GameDayEngine
{
    public static bool IsScrimmage(GameSnapEvent snap) => snap.Outcome is SnapOutcome.Run or SnapOutcome.Completion or
        SnapOutcome.Incompletion or SnapOutcome.Sack or SnapOutcome.Turnover or SnapOutcome.Touchdown or
        SnapOutcome.Interception or SnapOutcome.Fumble or SnapOutcome.Scramble or SnapOutcome.Kneel or SnapOutcome.Spike;

    public static bool ShouldAttemptTwo(GameDaySession session)
    {
        if (!session.Rules.TimedOvertime && session.OvertimeRound >= 2) return true;
        var margin = session.PossessionTeamName == session.Game.HomeTeamName
            ? session.HomeScore - session.AwayScore : session.AwayScore - session.HomeScore;
        return session.Quarter == 4 && session.ClockSeconds <= 300 && margin is -8 or -5 or -2 or 1 or 5;
    }

    private static IReadOnlyList<DynastyPlayer> PlaceKickTeam(IReadOnlyList<DynastyPlayer> roster, DynastyPlayer? kicker)
    {
        var eligible = roster.Where(p => p.CurrentInjury is null && !p.IsRedshirted)
            .OrderBy(p => p.DepthChartOrder).ThenByDescending(p => p.OverallRating).ToArray();
        var unit = new List<DynastyPlayer>();
        if (kicker is not null) unit.Add(kicker);
        var holder = eligible.FirstOrDefault(p => p.Position == Position.QB && p.PlayerId != kicker?.PlayerId);
        if (holder is not null) unit.Add(holder);
        unit.AddRange(eligible.Where(p => p.Position == Position.OL).Take(5));
        unit.AddRange(eligible.Where(p => p.Position == Position.TE).Take(2));
        return unit.Concat(eligible).DistinctBy(p => p.PlayerId).Take(11).ToArray();
    }

    private static IReadOnlyList<DynastyPlayer> PlaceKickDefense(IReadOnlyList<DynastyPlayer> roster)
    {
        var eligible = roster.Where(p => p.CurrentInjury is null && !p.IsRedshirted)
            .OrderBy(p => p.DepthChartOrder).ThenByDescending(p => p.OverallRating).ToArray();
        return eligible.Where(p => p.Position is Position.DE or Position.DT or Position.MLB or Position.OLB).Take(8)
            .Concat(eligible.Where(p => p.Position is Position.CB or Position.FS or Position.SS))
            .Concat(eligible).DistinctBy(p => p.PlayerId).Take(11).ToArray();
    }

    private static GameSnapEvent ResolveTry(GameDaySession session, string? choice)
    {
        var two = choice == "special-two-point" || choice != "special-extra-point" && ShouldAttemptTwo(session);
        if (!session.Rules.TimedOvertime && session.OvertimeRound >= 2) two = true;
        var home = session.PossessionTeamName == session.Game.HomeTeamName;
        var profile = home ? session.HomeProfile : session.AwayProfile;
        var defense = home ? session.AwayProfile : session.HomeProfile;
        var kicker = GameDayCoachingService.Specialist(session, session.PossessionTeamName, Kicker(home ? session.HomeRoster : session.AwayRoster));
        var probability = two ? Math.Clamp(.44 + (profile.OffenseRating - defense.DefenseRating) * .006, .15, .75)
            : GameDayMatchupService.FieldGoalProbability(session.Rules.ExtraPointDistance, kicker?.Skills?.KickAccuracy ?? profile.SpecialTeamsRating,
                kicker?.Skills?.KickPower ?? profile.SpecialTeamsRating);
        var made = session.Random.NextDouble() < probability;
        var offenseUnit = PlaceKickTeam(home ? session.HomeRoster : session.AwayRoster, kicker);
        var defenseUnit = PlaceKickDefense(home ? session.AwayRoster : session.HomeRoster);
        var snap = new GameSnapEvent
        {
            Sequence = session.Events.Count + 1, PossessionNumber = session.PossessionNumber,
            Quarter = session.Quarter, ClockSeconds = session.ClockSeconds,
            OffenseTeamName = session.PossessionTeamName, DefenseTeamName = home ? session.Game.AwayTeamName : session.Game.HomeTeamName,
            OffensivePlayId = string.Empty, DefensivePlayId = string.Empty, StartYardLine = 97,
            BallEndYardLine = made ? 100 : 97, Down = 0, Distance = 3, KickerId = two ? null : kicker?.PlayerId,
            CallSource = choice is null ? PlayCallSource.Cpu : PlayCallSource.User,
            Outcome = two ? made ? SnapOutcome.TwoPointConversion : SnapOutcome.FailedTwoPointConversion
                : made ? SnapOutcome.ExtraPoint : SnapOutcome.MissedExtraPoint,
            OffensePersonnel = offenseUnit.Select(p => p.PlayerId).ToArray(),
            DefensePersonnel = defenseUnit.Select(p => p.PlayerId).ToArray(),
            Points = made ? two ? 2 : 1 : 0
        };
        session.Events.Add(snap);
        if (home) session.HomeScore += snap.Points; else session.AwayScore += snap.Points;
        session.TryPending = false;
        ChangePossession(session, 25);
        if (!session.IsComplete && (session.OvertimeRound == 0 || session.Rules.TimedOvertime)) QueueKickoff(session, snap.OffenseTeamName);
        AdvanceQuarter(session);
        return snap;
    }

    private static GameSnapEvent ClockPlay(GameDaySession session, bool spike)
    {
        if (session.OvertimeRound > 0 && !session.Rules.TimedOvertime) throw new InvalidOperationException("Clock plays are unavailable in untimed overtime.");
        var home = session.PossessionTeamName == session.Game.HomeTeamName;
        var offense=GameDayCoachingService.ApplySubstitutions(session,PersonnelRotationService.SelectOffense(home?session.HomeDepthChart:session.AwayDepthChart,session.PossessionTeamName,OffensivePersonnel.Twelve,home?session.HomeFatigue:session.AwayFatigue,session.Random));
        var defense=GameDayCoachingService.ApplySubstitutions(session,PersonnelRotationService.SelectDefense(home?session.AwayDepthChart:session.HomeDepthChart,home?session.Game.AwayTeamName:session.Game.HomeTeamName,DefensivePersonnel.BaseFourThree,home?session.AwayFatigue:session.HomeFatigue,session.Random));
        var qb = offense.PlayerIds.Select(id=>session.PlayersById[id]).FirstOrDefault(p=>p.Position==Position.QB);
        var snap = new GameSnapEvent
        {
            Sequence = session.Events.Count + 1, PossessionNumber = session.PossessionNumber, Quarter = session.Quarter,
            ClockSeconds = session.ClockSeconds, OffenseTeamName = session.PossessionTeamName,
            DefenseTeamName = home ? session.Game.AwayTeamName : session.Game.HomeTeamName,
            OffensivePlayId = string.Empty, DefensivePlayId = string.Empty, CallSource = PlayCallSource.User,
            Down = session.Down, Distance = session.Distance, StartYardLine = session.YardLine,
            BallEndYardLine = session.YardLine - (spike ? 0 : 1), Yards = spike ? 0 : -1,
            QuarterbackId = qb?.PlayerId, BallCarrierId = spike ? null : qb?.PlayerId,
            PreSnapSeconds = session.LastPreSnapSeconds, LiveBallSeconds = spike ? 2 : 4,
            OffensePersonnel=offense.PlayerIds,DefensePersonnel=defense.PlayerIds,
            Outcome = spike ? SnapOutcome.Spike : SnapOutcome.Kneel
        };
        session.Events.Add(snap);
        ConsumeLiveClock(session, spike ? 2 : 4);
        AdvanceState(session, home, snap.Yards, false, false, spike);
        return snap;
    }

    private static GameSnapEvent ResolveOnsideKick(GameDaySession session)
    {
        var kicking = session.KickoffTeamName;
        var recovered = session.Random.NextDouble() < .12;
        var receiving = session.PossessionTeamName;
        var snap = new GameSnapEvent
        {
            Sequence = session.Events.Count + 1, PossessionNumber = session.PossessionNumber,
            Quarter = session.Quarter, ClockSeconds = session.ClockSeconds, OffenseTeamName = receiving,
            DefenseTeamName = kicking, OffensivePlayId = "", DefensivePlayId = "", Outcome = SnapOutcome.Kickoff,
            IsOnsideKick = true, KickingTeamRecovered = recovered,
            StartYardLine = 65, KickLandingYardLine = 55, BallEndYardLine = recovered ? 45 : 55
        };
        session.Events.Add(snap);
        session.PossessionTeamName = recovered ? kicking : receiving;
        session.YardLine = snap.BallEndYardLine; session.Down = 1; session.Distance = 10;
        session.PossessionNumber++; session.KickoffPending = false; session.OnsideKickRequested = false;
        return snap;
    }

    private static GameSnapEvent? TryPreSnapPenalty(GameDaySession session)
    {
        if (session.Events.Count == 0) return null;
        var random = new Random(unchecked(session.Game.SimulationSeed * 397 ^ session.Events.Count * 7919));
        if (random.NextDouble() >= .025) return null;
        var offense = random.Next(2) == 0;
        // Field spots use whole yards. Do not turn a half-distance foul into a goal-line crossing.
        var enforced = Math.Min(5, Math.Max(0, (offense ? session.YardLine : 100 - session.YardLine) / 2));
        var yards = offense ? -enforced : enforced;
        var defenseTeam = session.PossessionTeamName == session.Game.HomeTeamName ? session.Game.AwayTeamName : session.Game.HomeTeamName;
        var penalizedTeam = offense ? session.PossessionTeamName : defenseTeam;
        var foul = offense ? "False start" : "Offside";
        var snap = new GameSnapEvent
        {
            Sequence = session.Events.Count + 1, PossessionNumber = session.PossessionNumber,
            Quarter = session.Quarter, ClockSeconds = session.ClockSeconds, OffenseTeamName = session.PossessionTeamName,
            DefenseTeamName = defenseTeam,
            OffensivePlayId = "", DefensivePlayId = "", Down = session.Down, Distance = session.Distance,
            StartYardLine = session.YardLine, BallEndYardLine = session.YardLine + yards,
            PenalizedTeamName = penalizedTeam, PenaltyName = foul, PenaltyYards = enforced,
            Outcome = SnapOutcome.Penalty, Description = $"{foul} on {penalizedTeam}: {enforced} yards."
        };
        session.YardLine += yards; session.Distance -= yards;
        if (session.Distance <= 0) { session.Down = 1; session.Distance = Math.Min(10, 100 - session.YardLine); }
        session.ClockRestartsOnReady = !session.ClockStopped || session.ClockRestartsOnReady;
        session.ClockStopped = true; session.PlayClockSeconds = 25; session.Events.Add(snap);
        return snap;
    }

    private static void StartProfessionalOvertime(GameDaySession session)
    {
        session.OvertimeRound = 1; session.Quarter = 5;
        session.ClockSeconds = session.Rules.Postseason ? session.Rules.PlayoffOvertimeSeconds : session.Rules.RegularOvertimeSeconds;
        session.HomeTimeouts = session.AwayTimeouts = session.Rules.Postseason ? 3 : 2;
        session.OvertimePossessions = 0;
        session.OvertimeFirstTeam = session.Random.Next(2) == 0 ? session.Game.HomeTeamName : session.Game.AwayTeamName;
        session.PossessionTeamName = session.OvertimeFirstTeam;
        session.PossessionNumber++; session.YardLine = 25; session.Down = 1; session.Distance = 10;
        QueueKickoff(session, session.OvertimeFirstTeam == session.Game.HomeTeamName ? session.Game.AwayTeamName : session.Game.HomeTeamName);
    }

    private static void StartOvertimeRound(GameDaySession session)
    {
        session.OvertimeRound++;
        session.Quarter = 4 + session.OvertimeRound;
        session.ClockSeconds = 0;
        session.KickoffPending = false;
        session.OvertimePossessions = 0;
        session.OvertimeFirstTeam = session.OvertimeRound % 2 == 1 ? session.Game.AwayTeamName : session.Game.HomeTeamName;
        session.PossessionTeamName = session.OvertimeFirstTeam;
        session.PossessionNumber++;
        session.HomeTimeouts = session.AwayTimeouts = session.OvertimeRound <= 3 ? 1 : 0;
        SetOvertimeSpot(session);
    }

    private static void SetOvertimeSpot(GameDaySession session)
    {
        session.YardLine = session.OvertimeRound >= 3 ? 97 : 75;
        session.Down = 1; session.Distance = session.OvertimeRound >= 3 ? 3 : 10;
        session.TryPending = session.OvertimeRound >= 3;
        session.ClockStopped = true;
    }

    private static void EndOvertimePossession(GameDaySession session)
    {
        session.OvertimePossessions++;
        session.PossessionNumber++;
        if (session.OvertimePossessions == 1)
        {
            session.PossessionTeamName = session.OvertimeFirstTeam == session.Game.HomeTeamName
                ? session.Game.AwayTeamName : session.Game.HomeTeamName;
            SetOvertimeSpot(session);
        }
        else if (session.HomeScore != session.AwayScore) session.IsComplete = true;
        else
        {
            var homeTimeouts = session.HomeTimeouts; var awayTimeouts = session.AwayTimeouts;
            StartOvertimeRound(session);
            // Since 2025, each team has only one timeout total from the third overtime onward.
            if (session.OvertimeRound > 3) { session.HomeTimeouts = homeTimeouts; session.AwayTimeouts = awayTimeouts; }
        }
    }
}
