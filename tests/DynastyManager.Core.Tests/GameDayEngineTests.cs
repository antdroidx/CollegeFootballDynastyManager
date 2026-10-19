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
        var session = Session(GameDayMode.Watch, includeOpeningKickoff: true);
        GameDayEngine.SimulateToEnd(session);

        Assert.True(session.IsComplete);
        Assert.NotEmpty(session.Events);
        Assert.InRange(session.Events.Count, 1, 180);
        Assert.NotEqual(session.HomeScore, session.AwayScore);
    }

    [Fact]
    public void OpeningKickoffNamesTheKickerAndReturnerAndStartsTheDrive()
    {
        var session = Session(GameDayMode.Watch, includeOpeningKickoff: true);

        var kickoff = GameDayEngine.Step(session);

        Assert.Equal(SnapOutcome.Kickoff, kickoff.Outcome);
        Assert.NotNull(kickoff.KickerId);
        Assert.NotNull(kickoff.ReturnerId);
        Assert.Equal(kickoff.BallEndYardLine, session.YardLine);
        Assert.Equal(11, kickoff.OffensePersonnel.Count);
        Assert.Equal(11, kickoff.DefensePersonnel.Count);
        Assert.NotNull(kickoff.PrimaryDefenderId);
        Assert.Contains(kickoff.PrimaryDefenderId!.Value, kickoff.DefensePersonnel);
    }

    [Fact]
    public void UserCanCallDefenseWhileCpuCallsOffense()
    {
        var session = Session(GameDayMode.Play);
        var defensiveCall = GamePlanService.AvailableDefensivePlays(session.AwayProfile.GamePlan!).First();

        var snap = GameDayEngine.Step(session, userDefensivePlayId: defensiveCall.Id);

        Assert.Equal(defensiveCall.Id, snap.DefensivePlayId);
        Assert.Equal(PlayCallSource.User, snap.DefensiveCallSource);
        Assert.Equal(PlayCallSource.Cpu, snap.CallSource);
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
        Assert.InRange(punt.BallEndYardLine, 64, 92);
        Assert.Equal("Away", session.PossessionTeamName);
        Assert.Equal(2, session.PossessionNumber);
        Assert.NotNull(punt.KickerId);
        Assert.NotNull(punt.ReturnerId);
        Assert.Equal(11, punt.OffensePersonnel.Count);
        Assert.Equal(11, punt.DefensePersonnel.Count);
    }

    [Fact]
    public void FinishedBoxScoreRetainsKickAndPuntReturns()
    {
        var session = Session(GameDayMode.Watch, includeOpeningKickoff: true);
        GameDayEngine.SimulateToEnd(session);

        var game = GameDayEngine.Finish(session);
        var homeKickoffs = session.Events.Where(play => play.Outcome == SnapOutcome.Kickoff && play.OffenseTeamName == "Home").ToArray();
        var awayPuntReturns = session.Events.Where(play => play.Outcome == SnapOutcome.Punt &&
            play.DefenseTeamName == "Away" && play.ReturnYards > 0).ToArray();

        Assert.Equal(homeKickoffs.Length, game.HomeStats!.KickReturns);
        Assert.Equal(homeKickoffs.Sum(play => play.ReturnYards), game.HomeStats.KickReturnYards);
        Assert.Equal(awayPuntReturns.Length, game.AwayStats!.PuntReturns);
        Assert.Equal(awayPuntReturns.Sum(play => play.ReturnYards), game.AwayStats.PuntReturnYards);
        Assert.Contains(session.Events, play => play.Quarter == 3 && play.Outcome == SnapOutcome.Kickoff);
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
    public void MobileQuarterbacksScrambleAndReceiveRushingStats()
    {
        GameDaySession? gameWithScramble = null;
        for (var seed = 1; seed <= 40 && gameWithScramble is null; seed++)
        {
            var candidate = Session(GameDayMode.Watch, seed: seed);
            GameDayEngine.SimulateToEnd(candidate);
            if (candidate.Events.Any(play => play.IsQuarterbackScramble)) gameWithScramble = candidate;
        }

        Assert.NotNull(gameWithScramble);
        var scrambleSession = gameWithScramble!;
        var scrambles = scrambleSession.Events.Where(play => play.IsQuarterbackScramble).ToArray();
        var completed = GameDayEngine.Finish(scrambleSession);
        Assert.All(scrambles, play =>
        {
            Assert.Equal(play.QuarterbackId, play.BallCarrierId);
            Assert.Null(play.TargetId);
        });
        foreach (var quarterback in scrambles.GroupBy(play => play.QuarterbackId!.Value))
            Assert.True(completed.PlayerStats.Single(stat => stat.PlayerId == quarterback.Key).RushAttempts >= quarterback.Count());
    }

    [Fact]
    public void PuntsTrackTheirLandingPointFairCatchesAndTouchbacks()
    {
        var sawFairCatch = false;
        var sawTouchback = false;
        for (var seed = 1; seed <= 80 && (!sawFairCatch || !sawTouchback); seed++)
        {
            var session = Session(GameDayMode.Play, seed: seed);
            session.Down = 4;
            session.YardLine = seed % 2 == 0 ? 42 : 62;
            var punt = GameDayEngine.Step(session, "special-punt");

            Assert.True(punt.KickLandingYardLine > punt.StartYardLine);
            if (punt.IsFairCatch)
            {
                sawFairCatch = true;
                Assert.Equal(0, punt.ReturnYards);
            }
            if (punt.IsTouchback)
            {
                sawTouchback = true;
                Assert.Equal(20, session.YardLine);
                Assert.Null(punt.ReturnerId);
            }
        }
        Assert.True(sawFairCatch);
        Assert.True(sawTouchback);
    }

    [Fact]
    public void ReturnedPuntsAssignExactlyOneCoverageTackler()
    {
        GameSnapEvent? returnedPunt = null;
        for (var seed = 1; seed <= 60 && returnedPunt is null; seed++)
        {
            var session = Session(GameDayMode.Play, seed: seed);
            session.Down = 4;
            session.YardLine = 42;
            var punt = GameDayEngine.Step(session, "special-punt");
            if (punt.ReturnYards > 0) returnedPunt = punt;
        }

        Assert.NotNull(returnedPunt);
        Assert.NotNull(returnedPunt!.PrimaryDefenderId);
        Assert.Contains(returnedPunt.PrimaryDefenderId!.Value, returnedPunt.OffensePersonnel);
        Assert.DoesNotContain(returnedPunt.PrimaryDefenderId.Value, returnedPunt.DefensePersonnel);
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

    [Fact]
    public void HomeFieldImprovesHomeOffenseAndNeutralSitesRemoveTheBonus()
    {
        var homeYards = 0;
        var neutralYards = 0;
        for (var seed = 1; seed <= 500; seed++)
        {
            var home = Session(GameDayMode.Play, seed: seed);
            var neutral = Session(GameDayMode.Play, seed: seed, neutral: true);
            homeYards += GameDayEngine.Step(home, "off-spread-zone").Yards;
            neutralYards += GameDayEngine.Step(neutral, "off-spread-zone").Yards;
        }
        Assert.True(homeYards > neutralYards, $"Home {homeYards}, neutral {neutralYards}");
    }

    [Fact]
    public void HomeCrowdAlsoHelpsTheHomeDefense()
    {
        var visitingYards = 0;
        var neutralYards = 0;
        for (var seed = 1; seed <= 500; seed++)
        {
            var home = Session(GameDayMode.Play, seed: seed);
            var neutral = Session(GameDayMode.Play, seed: seed, neutral: true);
            home.PossessionTeamName = neutral.PossessionTeamName = "Away";
            visitingYards += GameDayEngine.Step(home, "off-spread-zone").Yards;
            neutralYards += GameDayEngine.Step(neutral, "off-spread-zone").Yards;
        }
        Assert.True(visitingYards < neutralYards, $"Visiting {visitingYards}, neutral {neutralYards}");
    }

    [Theory]
    [InlineData("off-spread-bubble", Position.WR)]
    [InlineData("off-pistol-screen", Position.RB)]
    [InlineData("off-empty-screen", Position.WR)]
    public void ScreensTargetThePlayerRoleNamedInThePlay(string playId, Position expectedRole)
    {
        for (var seed = 1; seed <= 30; seed++)
        {
            var session = Session(GameDayMode.Play, seed: seed);
            var snap = GameDayEngine.Step(session, playId);
            if (snap.TargetId is Guid targetId)
                Assert.Equal(expectedRole, session.PlayersById[targetId].Position);
        }
    }

    private static GameDaySession Session(GameDayMode mode, bool includeOpeningKickoff = false, int seed = 7123, bool neutral = false)
    {
        var homePlan = new TeamGamePlan { TeamName = "Home", Offense = OffensivePlaybook.Spread,
            OffensiveFormation = OffensiveFormation.ShotgunSpread, DefensiveFormation = DefensiveFormation.Nickel };
        var awayPlan = homePlan with { TeamName = "Away" };
        var session = GameDayEngine.Start(new ScheduledGame { GameId = "game-day", SeasonYear = 2026, Week = 1,
                HomeTeamName = "Home", AwayTeamName = "Away", SimulationSeed = seed, IsNeutralSite = neutral },
            Profile("Home", homePlan), Profile("Away", awayPlan), Roster(), mode);
        session.KickoffPending = includeOpeningKickoff;
        return session;
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
