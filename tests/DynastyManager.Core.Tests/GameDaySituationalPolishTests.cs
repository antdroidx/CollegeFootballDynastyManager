using System.Text.Json;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class GameDaySituationalPolishTests
{
    private static bool Run(OffensivePlayDefinition p) => p.Concept is OffensiveConcept.InsideRun or OffensiveConcept.OutsideRun;

    [Theory]
    [InlineData(3)] [InlineData(4)] [InlineData(5)]
    public void ThirdAndMediumUsuallyPassesEvenInRunHeavyBooks(int distance)
    {
        foreach (var book in new[] { OffensivePlaybook.Balanced, OffensivePlaybook.PowerRun, OffensivePlaybook.Flexbone })
        {
            var plan = new TeamGamePlan { TeamName = "Home", Offense = book };
            var runs = Enumerable.Range(0, 1000).Count(i => Run(GamePlanService.RecommendOffensivePlay(plan, 3, distance, 40, i)));
            Assert.InRange(runs, 1, 190); // Draws remain possible; no hard ban on rushing.
        }
    }

    [Fact]
    public void TrailingTeamsThrowMoreBeforeTheFinalThreeMinutesWithoutAbandoningEarlyRuns()
    {
        var s = GameDayEngineTests.Session(GameDayMode.Simulate);
        var plan = new TeamGamePlan { TeamName = "Home", Offense = OffensivePlaybook.PowerRun };
        int Runs(int deficit, int quarter, int clock)
        {
            s.AwayScore = deficit; s.Quarter = quarter; s.ClockSeconds = clock;
            var bias = GameDayRecommendationService.PassingUrgency(s);
            return Enumerable.Range(0, 1000).Count(i => Run(GamePlanService.RecommendOffensivePlay(plan, 1, 10, 40, i, passBias: bias)));
        }
        var tied = Runs(0, 3, 600); var behind = Runs(21, 3, 600); var late = Runs(21, 4, 420);
        Assert.True(behind < tied - 150); Assert.True(late < behind);
        Assert.True(Runs(7, 1, 600) > 400);
        s.Quarter = 4; s.ClockSeconds = 420; s.AwayScore = 21; s.Down = 4;
        Assert.Equal(RecommendedCallType.Scrimmage, GameDayRecommendationService.Recommend(s, plan).Type);
    }

    [Fact]
    public void CpuEngineUsesSituationalAdviceAndExplicitUserCallsStillWin()
    {
        var s = GameDayEngineTests.Session(GameDayMode.Simulate, seed: 123);
        s.Quarter = 3; s.ClockSeconds = 600; s.AwayScore = 21; s.Down = 3; s.Distance = 4;
        var expected = GameDayRecommendationService.Recommend(s, s.HomeProfile.GamePlan!);
        Assert.Equal(expected.PlayId, GameDayEngine.Step(s).OffensivePlayId);
        s = GameDayEngineTests.Session(GameDayMode.Play, seed: 123); s.Down = 3; s.Distance = 4;
        Assert.Equal("off-spread-zone", GameDayEngine.Step(s, "off-spread-zone").OffensivePlayId);
    }

    [Fact]
    public void FairCatchRequiresCoverageToArriveNearTheReceiver()
    {
        var gunner = new DynastyPlayer { PlayerId = Guid.NewGuid(), FullName = "Gunner", TeamName = "Home", Position = Position.CB, SpeedRating = 90 };
        var near = PuntCoverageService.Evaluate(38, 4.8, [gunner], null, 80);
        var far = PuntCoverageService.Evaluate(50, 3.6, [gunner], null, 80);
        Assert.True(near.FairCatch); Assert.InRange(near.DistanceAtCatch, 1.5, 8); Assert.Equal(gunner.PlayerId, near.DefenderId);
        Assert.False(far.FairCatch); Assert.True(far.DistanceAtCatch > 15);
        Assert.False(PuntCoverageService.Evaluate(38, 4.8, [], null, 80).FairCatch);
        Assert.False(PuntCoverageService.Evaluate(38, 4.8, [gunner], gunner.PlayerId, 80).FairCatch);
    }

    [Fact]
    public void PuntEventsRecordCoverageUsedByTheirPlayback()
    {
        var fair = 0; var returns = 0;
        for (var seed = 1; seed <= 100; seed++)
        {
            var s = GameDayEngineTests.Session(GameDayMode.Play, seed: seed); s.YardLine = 30;
            var snap = GameDayEngine.Step(s, "special-punt");
            if (snap.IsFairCatch)
            {
                fair++; Assert.Equal(0, snap.ReturnYards); Assert.True(snap.NearestCoverageYardsAtCatch <= 8);
                Assert.Contains(snap.PrimaryDefenderId!.Value, snap.OffensePersonnel);
            }
            else returns++;
            Assert.InRange(snap.KickHangTimeSeconds, 3.6, 5.4);
        }
        Assert.True(fair > 0 && returns > 0);
    }

    [Fact]
    public void PatHasKickingPersonnelAndIsRecognizedByTheSharedAnimator()
    {
        var s = GameDayEngineTests.Session(GameDayMode.Play); s.TryPending = true;
        var snap = GameDayEngine.Step(s, "special-extra-point");
        Assert.True(GameDayAnimationPlanner.IsPlaceKick(snap)); Assert.Equal(97, snap.StartYardLine);
        Assert.Equal(11, snap.OffensePersonnel.Count); Assert.Equal(11, snap.DefensePersonnel.Count);
        Assert.Contains(snap.KickerId!.Value, snap.OffensePersonnel);
        Assert.Equal(5, snap.OffensePersonnel.Count(id => s.PlayersById[id].Position == Position.OL));
        Assert.Equal(snap.KickerId, GameDayAnimationPlanner.BallOwner(snap, .1, .5));
        Assert.Null(GameDayAnimationPlanner.BallOwner(snap, .5, .5));
        Assert.True(GameDayAnimationPlanner.IsPlaceKick(snap with { Outcome = SnapOutcome.MissedExtraPoint }));
    }

    [Theory]
    [InlineData(1)] [InlineData(-1)]
    public void IncompleteThrowsReachTheIntendedRouteWithoutAddingStatisticalYards(int direction)
    {
        var start = GameDayAnimationPlanner.ProjectYardLine(30, direction);
        var receiver = new GameFieldPoint(GameDayAnimationPlanner.ProjectYardLine(52, direction), .25);
        var miss = GameDayAnimationPlanner.IncompletePassDestination(receiver, direction, false, 2);
        Assert.True((miss.X - start) * direction > 20 / 120d); Assert.InRange(miss.X, 0, 1);
        Assert.Equal(receiver, GameDayAnimationPlanner.IncompletePassDestination(receiver, direction, true, 2));
    }

    [Fact]
    public void JerseyAssignmentPreservesImportedZeroAndDuplicatesAndIsStableAcrossReloadAndOrder()
    {
        var source = GameDayEngineTests.Session(GameDayMode.Play).HomeRoster.Select((p, i) => p with { JerseyNumber = i < 2 ? 0 : null }).ToArray();
        var first = PlayerJerseyService.Assign(source); var reverse = PlayerJerseyService.Assign(source.Reverse().ToArray());
        Assert.Equal(0, first[0].JerseyNumber); Assert.Equal(0, first[1].JerseyNumber);
        Assert.All(first, p => Assert.InRange(p.JerseyNumber!.Value, 0, 99));
        Assert.Equal(first.Count - 1, first.Select(p => p.JerseyNumber).Distinct().Count());
        Assert.All(first, p => Assert.Equal(p.JerseyNumber, reverse.Single(q => q.PlayerId == p.PlayerId).JerseyNumber));
        var loaded = JsonSerializer.Deserialize<DynastyPlayer[]>(JsonSerializer.Serialize(first))!;
        Assert.Equal(first, PlayerJerseyService.Assign(loaded));
    }

    [Fact]
    public void OpponentReportUsesOnlyPlayedCurrentSeasonGamesAndCorrectHomeAwayStats()
    {
        var game = new ScheduledGame { GameId = "one", SeasonYear = 2026, Week = 1, HomeTeamName = "Home", AwayTeamName = "Away",
            HasPlayed = true, HomeScore = 17, AwayScore = 24, AwayStats = new() { PassAttempts = 30, RushAttempts = 20, Turnovers = 2 } };
        var state = new DynastyState { DynastyName = "Test", UserTeamName = "Home", SeasonYear = 2026,
            Schedule = [game, game with { GameId = "future", HasPlayed = false }, game with { GameId = "old", SeasonYear = 2025 }],
            ActiveRoster = GameDayEngineTests.Session(GameDayMode.Play).Roster };
        var report = OpponentScoutingService.Build(state, "Away");
        Assert.Equal(1, report.Games); Assert.Equal(1, report.Wins); Assert.Equal(0, report.Losses);
        Assert.Equal(24, report.PointsFor); Assert.Equal(17, report.PointsAgainst); Assert.Equal(.6, report.PassShare); Assert.Equal(2, report.Turnovers);
        Assert.Single(report.RecentGames); Assert.All(report.KeyPlayers, p => Assert.Equal("Away", p.TeamName));
        Assert.Null(OpponentScoutingService.Build(state, "Unknown").PassShare);
    }
}
