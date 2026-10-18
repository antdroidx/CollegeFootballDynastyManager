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
