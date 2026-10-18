using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class GameDayEngineTests
{
    [Fact]
    public void UserAndCpuCallsAdvanceTheSameSessionAndRecordPersonnel()
    {
        var session = Session(GameDayMode.Play);
        var user = GameDayEngine.Step(session, "off-spread-verts");
        var cpu = GameDayEngine.Step(session);

        Assert.Equal("off-spread-verts", user.OffensivePlayId);
        Assert.Equal(PlayCallSource.User, user.CallSource);
        Assert.Equal(PlayCallSource.Cpu, cpu.CallSource);
        Assert.NotEmpty(user.OffensePersonnel);
        Assert.NotEmpty(user.DefensePersonnel);
        Assert.NotNull(user.QuarterbackId);
    }

    [Fact]
    public void SkipToEndCompletesARegulationGame()
    {
        var session = Session(GameDayMode.Watch);
        GameDayEngine.SimulateToEnd(session);

        Assert.True(session.IsComplete);
        Assert.NotEmpty(session.Events);
        Assert.InRange(session.Events.Count, 1, 180);
        Assert.NotEqual(session.HomeScore, session.AwayScore);
    }

    [Fact]
    public void UserPuntBypassesNormalPlayLookupAndChangesPossession()
    {
        var session = Session(GameDayMode.Play);
        session.Down = 4;
        session.YardLine = 42;

        var punt = GameDayEngine.Step(session, "special-punt");

        Assert.Equal(SnapOutcome.Punt, punt.Outcome);
        Assert.Equal(PlayCallSource.User, punt.CallSource);
        Assert.InRange(punt.BallEndYardLine, 76, 92);
        Assert.Equal("Away", session.PossessionTeamName);
        Assert.Equal(2, session.PossessionNumber);
    }

    [Fact]
    public void UserFieldGoalBypassesNormalPlayLookup()
    {
        var session = Session(GameDayMode.Play);
        session.Down = 4;
        session.YardLine = 72;

        var kick = GameDayEngine.Step(session, "special-field-goal");

        Assert.True(kick.Outcome is SnapOutcome.FieldGoal or SnapOutcome.MissedFieldGoal);
        Assert.Equal(PlayCallSource.User, kick.CallSource);
        Assert.Equal(100, kick.BallEndYardLine);
        Assert.Equal("Away", session.PossessionTeamName);
    }

    [Fact]
    public void PursuitMeetsCarrierBehindTheLineOnATackleForLoss()
    {
        var snap = new GameSnapEvent
        {
            OffenseTeamName = "Home", DefenseTeamName = "Away",
            OffensivePlayId = "off-spread-zone", DefensivePlayId = "def-43-cover2",
            StartYardLine = 50, BallEndYardLine = 46, Yards = -4, Outcome = SnapOutcome.Run
        };

        var contact = GameDayAnimationPlanner.ContactPoint(snap, 1, .58);
        var tackler = GameDayAnimationPlanner.PursuitTarget(contact, 4, true);
        var support = GameDayAnimationPlanner.PursuitTarget(contact, 5, false);

        Assert.True(contact.X < .50);
        Assert.Equal(contact, tackler);
        Assert.InRange(Math.Abs(support.X - contact.X), 0, .01);
        Assert.InRange(Math.Abs(support.Y - contact.Y), 0, .025);
        Assert.Equal(1, GameDayAnimationPlanner.PursuitProgress(1, true));
    }

    private static GameDaySession Session(GameDayMode mode)
    {
        var homePlan = new TeamGamePlan { TeamName = "Home", Offense = OffensivePlaybook.Spread,
            OffensiveFormation = OffensiveFormation.ShotgunSpread, DefensiveFormation = DefensiveFormation.Nickel };
        var awayPlan = homePlan with { TeamName = "Away" };
        return GameDayEngine.Start(new ScheduledGame { GameId = "game-day", SeasonYear = 2026, Week = 1,
                HomeTeamName = "Home", AwayTeamName = "Away", SimulationSeed = 7123 },
            Profile("Home", homePlan), Profile("Away", awayPlan), Roster(), mode);
    }

    private static TeamSimulationProfile Profile(string team, TeamGamePlan plan) => new()
    {
        TeamName = team, PassOffenseRating = 78, RushOffenseRating = 76,
        PassDefenseRating = 75, RushDefenseRating = 75, SpecialTeamsRating = 75, GamePlan = plan
    };

    private static DynastyPlayer[] Roster()
    {
        var players = new List<DynastyPlayer>();
        foreach (var team in new[] { "Home", "Away" })
        foreach (var (position, count) in new[] { (Position.QB, 2), (Position.RB, 4), (Position.WR, 7),
                     (Position.TE, 4), (Position.OL, 10), (Position.DE, 5), (Position.DT, 5),
                     (Position.OLB, 5), (Position.MLB, 4), (Position.CB, 7), (Position.FS, 4), (Position.SS, 4) })
        for (var i = 1; i <= count; i++) players.Add(new DynastyPlayer
        {
            PlayerId = Guid.NewGuid(), FullName = $"{team} {position} {i}", TeamName = team, Position = position,
            ClassYear = 2, TalentLevel = 75, OverallRating = 85 - i, PotentialRating = 90,
            SpeedRating = 75, StrengthRating = 75, AgilityRating = 75, AwarenessRating = 75,
            TechniqueRating = 75, DurabilityRating = 80, DepthChartOrder = i
        });
        return players.ToArray();
    }
}
