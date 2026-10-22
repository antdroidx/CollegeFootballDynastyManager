using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static partial class GameDayEngine
{
    public static string BallOn(GameDaySession session, Func<string, string>? label = null)
    {
        if (session.YardLine == 50) return "50";
        var own = session.YardLine < 50;
        var team = own ? session.PossessionTeamName : session.PossessionTeamName == session.Game.HomeTeamName
            ? session.Game.AwayTeamName : session.Game.HomeTeamName;
        var yard = Math.Clamp(own ? session.YardLine : 100 - session.YardLine, 1, 50);
        return $"{(label ?? (name => name))(team)} {yard}";
    }

    public static OffensiveTempo EffectiveTempo(GameDaySession session)
    {
        var home = session.PossessionTeamName == session.Game.HomeTeamName;
        var plan = (home ? session.HomeProfile : session.AwayProfile).GamePlan;
        var margin = home ? session.HomeScore - session.AwayScore : session.AwayScore - session.HomeScore;
        // Late-game necessities take priority over an ordinary pacing preference.
        if (session.Quarter == 4 && margin < 0 && session.ClockSeconds <= Math.Max(180, Math.Ceiling(-margin / 8d) * 180) ||
            session.Quarter == 2 && session.ClockSeconds <= 120 && margin <= 0)
            return OffensiveTempo.UpTempo;
        if (session.Quarter == 4 && margin > 0 && session.ClockSeconds <= 300) return OffensiveTempo.ChewClock;
        if (plan?.Tempo is { } tempo && tempo != OffensiveTempo.Automatic) return tempo;
        // Stable coaching identity, rather than drawing a fresh personality on every snap.
        var identity = session.PossessionTeamName.Aggregate(17, (hash, c) => unchecked(hash * 31 + c));
        return plan?.Offense is OffensivePlaybook.Option or OffensivePlaybook.Flexbone or OffensivePlaybook.PowerRun
            ? OffensiveTempo.ChewClock : (identity & 7) == 0 ? OffensiveTempo.UpTempo : OffensiveTempo.Normal;
    }

    private static GameSnapEvent? PrepareSnap(GameDaySession session, bool spike, bool quickShot = false)
    {
        session.LastPreSnapSeconds = 0;
        if (session.OvertimeRound > 0) return null;
        if (CheckTwoMinuteTimeout(session)) return ClockEvent(session, "Two-minute timeout.");
        if (session.ClockSeconds <= 0) { var end = ClockEvent(session, "The period ends before another snap."); AdvanceQuarter(session); return end; }
        var tempo = EffectiveTempo(session);
        var seconds = spike || quickShot ? 1 : tempo switch
        {
            OffensiveTempo.UpTempo => session.Random.Next(12, 21),
            OffensiveTempo.ChewClock => session.Random.Next(37, 40),
            _ => session.Random.Next(31, 38)
        };
        seconds = Math.Min(seconds, session.PlayClockSeconds - 1);
        // A first down inside two minutes pauses only until the ball is ready, unlike an incompletion.
        var runoff = !session.ClockStopped ? seconds : session.ClockRestartsOnReady ? Math.Max(0, seconds - 4) : 0;
        if (quickShot) runoff = Math.Min(runoff, Math.Max(0, session.ClockSeconds - 1));
        var half = session.Quarter == 2 ? 1 : session.Quarter == 4 ? 2 : 0;
        if (half > 0 && session.TwoMinuteTimeoutHalf < half && session.ClockSeconds > 120 && session.ClockSeconds - runoff <= 120)
        {
            session.LastPreSnapSeconds = session.ClockSeconds - 120;
            session.ClockSeconds = 120; CheckTwoMinuteTimeout(session);
            return ClockEvent(session, "Two-minute timeout.");
        }
        session.LastPreSnapSeconds = Math.Min(session.ClockSeconds, runoff);
        session.ClockSeconds -= session.LastPreSnapSeconds;
        session.PlayClockSeconds = Math.Max(1, session.PlayClockSeconds - seconds);
        if (session.ClockSeconds > 0) return null;
        var ended = ClockEvent(session, "The period ends before another snap.");
        AdvanceQuarter(session);
        return ended;
    }

    private static void ConsumeLiveClock(GameDaySession session, int seconds)
    {
        if (session.OvertimeRound == 0) session.ClockSeconds = Math.Max(0, session.ClockSeconds - seconds);
    }

    private static bool CheckTwoMinuteTimeout(GameDaySession session)
    {
        var half = session.Quarter == 2 ? 1 : session.Quarter == 4 ? 2 : 0;
        if (session.OvertimeRound > 0 || half == 0 || session.ClockSeconds > 120 || session.ClockSeconds <= 0 || session.TwoMinuteTimeoutHalf >= half) return false;
        session.TwoMinuteTimeoutHalf = half;
        session.ClockStopped = true; session.ClockRestartsOnReady = false; session.PlayClockSeconds = 25;
        return true;
    }

    private static GameSnapEvent ClockEvent(GameDaySession session, string text)
    {
        var snap = new GameSnapEvent { Sequence = session.Events.Count + 1, PossessionNumber = session.PossessionNumber,
            Quarter = session.Quarter, ClockSeconds = session.ClockSeconds, OffenseTeamName = session.PossessionTeamName,
            DefenseTeamName = session.PossessionTeamName == session.Game.HomeTeamName ? session.Game.AwayTeamName : session.Game.HomeTeamName,
            OffensivePlayId = "", DefensivePlayId = "", Down = session.Down, Distance = session.Distance,
            StartYardLine = session.YardLine, BallEndYardLine = session.YardLine, Outcome = SnapOutcome.ClockStoppage,
            PreSnapSeconds = session.LastPreSnapSeconds, Description = text };
        session.Events.Add(snap);
        return snap;
    }

    private static bool ShouldKneel(GameDaySession session)
    {
        if (session.OvertimeRound > 0 || session.Quarter != 4 || session.ClockSeconds > 120 ||
            session.Mode == GameDayMode.Play && session.ControlledTeamName == session.PossessionTeamName) return false;
        var home = session.PossessionTeamName == session.Game.HomeTeamName;
        var margin = home ? session.HomeScore - session.AwayScore : session.AwayScore - session.HomeScore;
        var timeouts = home ? session.AwayTimeouts : session.HomeTimeouts;
        return margin > 0 && session.ClockSeconds <= Math.Max(0, 4 - session.Down - timeouts) * 39 + 3;
    }
}
