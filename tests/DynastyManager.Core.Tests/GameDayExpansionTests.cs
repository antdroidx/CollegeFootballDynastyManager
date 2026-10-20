using System.Text.Json;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Import;
using Xunit;

namespace DynastyManager.Core.Tests;

public class GameDayExpansionTests
{
    [Fact]
    public void CreatedPlayDefaultsKeepFiveDistinctOffensiveLineSlots()
    {
        var slots = Enumerable.Range(0, 5).Select(i => GameDayAnimationPlanner.Alignment(OffensiveFormation.Singleback, Position.OL, i)).ToArray();
        Assert.Equal(5, slots.Select(p => p.Y).Distinct().Count());
        Assert.All(slots, p => Assert.Equal(-.25, p.X));
        var qb = GameDayAnimationPlanner.Alignment(OffensiveFormation.Singleback, Position.QB, 0);
        var back = GameDayAnimationPlanner.Alignment(OffensiveFormation.Singleback, Position.RB, 0);
        Assert.Equal(qb.Y, back.Y); Assert.True(back.X < qb.X);
    }

    [Fact]
    public void CompletedGamesScoresReconcileWithRecordedScoringEvents()
    {
        for (var seed = 1; seed <= 30; seed++)
        {
            var s = GameDayEngineTests.Session(GameDayMode.Simulate, includeOpeningKickoff: true, seed: seed);
            GameDayEngine.SimulateToEnd(s);
            foreach (var team in new[] { "Home", "Away" })
            {
                var events = s.Events.Where(e => e.OffenseTeamName == team).ToArray();
                var points = events.Sum(e => e.Outcome == SnapOutcome.Touchdown ? 6 : e.Outcome == SnapOutcome.FieldGoal ? 3 : e.Points);
                Assert.Equal(team == "Home" ? s.HomeScore : s.AwayScore, points);
                var game = GameDayEngine.Finish(s);
                var stats = team == "Home" ? game.HomeStats! : game.AwayStats!;
                Assert.Equal(events.Count(e => e.Outcome == SnapOutcome.ExtraPoint), stats.ExtraPointsMade);
                Assert.Equal(events.Count(e => e.Outcome is SnapOutcome.ExtraPoint or SnapOutcome.MissedExtraPoint), stats.ExtraPointsAttempted);
            }
        }
    }

    [Fact]
    public void NoPlayPenaltyPreservesDownAndIsNotCountedAsAnAttempt()
    {
        GameDaySession? penalized = null;
        for (var seed = 1; seed <= 100 && penalized is null; seed++)
        {
            var s = GameDayEngineTests.Session(GameDayMode.Play, includeOpeningKickoff: true, seed: seed);
            GameDayEngine.Step(s);
            if (GameDayEngine.Step(s, "off-spread-zone").Outcome == SnapOutcome.Penalty) penalized = s;
        }
        Assert.NotNull(penalized); Assert.Equal(1, penalized.Down);
        Assert.False(GameDayEngine.IsScrimmage(penalized.Events.Last()));
        Assert.All(penalized.Events, e => Assert.False(GameDayEngine.IsScrimmage(e)));
    }

    [Fact]
    public void TiedRegulationStartsUntimedOvertimeWithoutInventedPoints()
    {
        var s = GameDayEngineTests.Session(GameDayMode.Play);
        s.Quarter = 4; s.ClockSeconds = 1; s.Down = 1;
        GameDayEngine.Step(s, "special-kneel");
        Assert.Equal(1, s.OvertimeRound); Assert.Equal(0, s.ClockSeconds);
        Assert.Equal(75, s.YardLine); Assert.False(s.KickoffPending);
        Assert.Equal(0, s.HomeScore + s.AwayScore);
        var firstTeam = s.PossessionTeamName;
        GameDayEngine.Step(s, "special-field-goal");
        Assert.Equal(1, s.OvertimePossessions); Assert.NotEqual(firstTeam, s.PossessionTeamName);
        Assert.Equal(75, s.YardLine); Assert.False(s.IsComplete);
    }

    [Fact]
    public void OvertimeGivesBothTeamsAChanceAndAlternatesTwoPointTries()
    {
        var s = GameDayEngineTests.Session(GameDayMode.Play);
        s.OvertimeRound = 3; s.Quarter = 7; s.ClockSeconds = 0;
        s.TryPending = true; s.OvertimeFirstTeam = s.PossessionTeamName;
        var first = GameDayEngine.Step(s, "special-extra-point");
        Assert.Contains(first.Outcome, new[] { SnapOutcome.TwoPointConversion, SnapOutcome.FailedTwoPointConversion });
        Assert.False(s.IsComplete); Assert.True(s.TryPending);
        Assert.Equal(97, s.YardLine);
        GameDayEngine.Step(s);
        Assert.True(s.IsComplete || s.OvertimeRound == 4);
        Assert.Equal(0, s.ClockSeconds);
    }

    [Fact]
    public void SecondOvertimeRequiresTwoEvenWhenUserRequestsAnExtraPoint()
    {
        var s = GameDayEngineTests.Session(GameDayMode.Play);
        s.OvertimeRound = 2; s.Quarter = 6; s.TryPending = true; s.OvertimeFirstTeam = s.PossessionTeamName;
        var snap = GameDayEngine.Step(s, "special-extra-point");
        Assert.Contains(snap.Outcome, new[] { SnapOutcome.TwoPointConversion, SnapOutcome.FailedTwoPointConversion });
        Assert.InRange(snap.Points, 0, 2); Assert.False(s.KickoffPending);
    }

    [Fact]
    public void TouchdownAndConversionAreDistinctScoringEvents()
    {
        GameDaySession? scored = null;
        for (var seed = 1; seed < 200 && scored is null; seed++)
        {
            var s = GameDayEngineTests.Session(GameDayMode.Play, seed: seed); s.YardLine = 99; s.Distance = 1;
            if (GameDayEngine.Step(s, "off-spread-zone").Outcome == SnapOutcome.Touchdown) scored = s;
        }
        Assert.NotNull(scored); Assert.Equal(6, scored.HomeScore); Assert.True(scored.TryPending);
        var conversion = GameDayEngine.Step(scored, "special-extra-point");
        Assert.Equal(6 + conversion.Points, scored.HomeScore); Assert.True(scored.KickoffPending);
        Assert.False(scored.TryPending);
    }

    [Fact]
    public void SpikeCostsDownAndAttemptWhileKneelCostsYard()
    {
        var s = GameDayEngineTests.Session(GameDayMode.Play);
        var spike = GameDayEngine.Step(s, "special-spike");
        Assert.Equal(SnapOutcome.Spike, spike.Outcome); Assert.Equal(2, s.Down); Assert.True(s.ClockStopped);
        var prior = s.ClockSeconds;
        var kneel = GameDayEngine.Step(s, "special-kneel");
        Assert.Equal(-1, kneel.Yards); Assert.Equal(24, s.YardLine); Assert.Equal(3, s.Down);
        Assert.InRange(prior - s.ClockSeconds, 4, 8);
    }

    [Fact]
    public void OnsideRecoveryDeterminesPossessionAndDoesNotCreateReturnStats()
    {
        var recovered = 0;
        for (var seed = 1; seed <= 100; seed++)
        {
            var s = GameDayEngineTests.Session(GameDayMode.Play, includeOpeningKickoff: true, seed: seed);
            s.OnsideKickRequested = true;
            var snap = GameDayEngine.Step(s);
            Assert.True(snap.IsOnsideKick); Assert.Null(snap.ReturnerId);
            Assert.Equal(snap.KickingTeamRecovered ? "Away" : "Home", s.PossessionTeamName);
            Assert.False(s.OnsideKickRequested); Assert.False(s.KickoffPending);
            if (snap.KickingTeamRecovered) recovered++;
        }
        Assert.InRange(recovered, 1, 30);
    }

    [Fact]
    public void CreatedPlaySurvivesSerializationAndRunsInEngineAndStats()
    {
        var original = GameDayEngineTests.Session(GameDayMode.Play);
        var custom = PlaybookCatalog.OffensivePlays.First(p => p.Concept == OffensiveConcept.QuickPass) with
        { Id = "custom-curls", Name = "My Curls", Assignments = [new() { Position = Position.WR, RoleIndex = 0, Route = "Curls", Lane = .2 }] };
        var plan = original.HomeProfile.GamePlan! with { Offense = OffensivePlaybook.Custom, CreatedOffensivePlays = [custom], CustomOffensivePlayIds = [custom.Id] };
        var state = GamePlanService.SetPlan(State(), plan);
        var restored = JsonSerializer.Deserialize<DynastyState>(JsonSerializer.Serialize(state))!;
        var loaded = restored.TeamGamePlans.Single();
        var s = GameDayEngine.Start(original.Game, original.HomeProfile with { GamePlan = loaded }, original.AwayProfile, original.Roster, GameDayMode.Play);
        s.KickoffPending = false;
        var snap = GameDayEngine.Step(s, custom.Id);
        Assert.Equal(custom.Id, snap.OffensivePlayId);
        Assert.Equal(GameDayRouteFamily.Curls, GameDayAnimationPlanner.AssignedRoute(loaded.CreatedOffensivePlays.Single(), Position.WR, 0));
        GameDayEngine.SimulateToEnd(s);
        Assert.NotEmpty(GameDayEngine.Finish(s).PlayerStats);
    }

    [Fact]
    public void EditorRejectsCatalogCollisionsDuplicateSlotsAndInvalidCoordinates()
    {
        var play = PlaybookCatalog.OffensivePlays.First() with { Id = "custom-test", Name = "Test" };
        void Check(OffensivePlayDefinition p) => PlayEditorService.Validate(new TeamGamePlan { TeamName = "Home", CreatedOffensivePlays = [p] });
        Assert.Throws<ArgumentException>(() => Check(play with { Id = PlaybookCatalog.OffensivePlays.First().Id }));
        Assert.Throws<ArgumentException>(() => Check(play with { Assignments = [new() { Lane = double.NaN }] }));
        Assert.Throws<ArgumentException>(() => Check(play with { Assignments = [new(), new()] }));
    }

    [Fact]
    public void ProtectionAndPassRatingsChangeReadsUsingTheSameRandomSequence()
    {
        var s = GameDayEngineTests.Session(GameDayMode.Play);
        var play = PlaybookCatalog.OffensivePlays.First(p => p.Concept == OffensiveConcept.QuickPass);
        var defense = PlaybookCatalog.DefensivePlays.First();
        var strong = s.HomeRoster.Select(p => p with { SpeedRating = 95, AgilityRating = 95, AwarenessRating = 95,
            Skills = new PlayerSkillRatings { PassingAccuracy = 95, Catching = 95, PassBlocking = 95 } }).ToArray();
        var weak = strong.Select(p => p with { SpeedRating = 30, AgilityRating = 30, AwarenessRating = 30,
            Skills = p.Skills! with { PassingAccuracy = 30, Catching = 30, PassBlocking = 30 } }).ToArray();
        var good = PassingReadService.Read(play, defense, strong, s.AwayRoster, strong.First(p => p.Position == Position.QB), 85, new Random(123));
        var poor = PassingReadService.Read(play, defense, weak, s.AwayRoster, weak.First(p => p.Position == Position.QB), 30, new Random(123));
        Assert.True(good.Separation > poor.Separation); Assert.True(good.Protection > poor.Protection);
        Assert.True(good.CatchChance > poor.CatchChance); Assert.True(good.InterceptionChance < poor.InterceptionChance);
    }

    [Fact]
    public void PlayersAssignedToBlockAreNotPassingTargets()
    {
        var s = GameDayEngineTests.Session(GameDayMode.Play);
        var play = PlaybookCatalog.OffensivePlays.First(p => p.Concept == OffensiveConcept.QuickPass) with
        {
            Assignments = s.HomeRoster.Where(p => p.Position is Position.WR or Position.TE or Position.RB)
                .GroupBy(p => p.Position).SelectMany(g => g.Select((p, index) => new PlayAssignment
                { Position = p.Position, RoleIndex = index, Task = AssignmentTask.Block })).ToArray()
        };
        var read = PassingReadService.Read(play, PlaybookCatalog.DefensivePlays.First(), s.HomeRoster,
            s.AwayRoster, s.HomeRoster.First(p => p.Position == Position.QB), 70, new Random(1));
        Assert.Null(read.Target);
    }

    [Fact]
    public void CsvPreviewValidatesMappingsRatingsAndDuplicateIdsBeforeApply()
    {
        var csv = Csv("12", "90");
        Assert.False(Ncaa06RosterCsvImporter.Preview(new StringReader(csv), ["Home"]).CanApply);
        var mapping = new Dictionary<string, string> { ["12"] = "Home" };
        var preview = Ncaa06RosterCsvImporter.Preview(new StringReader(csv), ["Home"], mapping);
        Assert.True(preview.CanApply); Assert.Equal("First Last", preview.Players.Single().FullName);
        Assert.Equal(1, preview.Players.Single().ClassYear);
        Assert.Equal(Ncaa06RatingService.CalculateOverall(preview.Players[0]), preview.Players[0].OverallRating);
        Assert.Single(Ncaa06RosterCsvImporter.Apply(State(), preview).ActiveRoster);
        Assert.Throws<InvalidOperationException>(() => Ncaa06RosterCsvImporter.Apply(State() with { Week = 1 }, preview));
        Assert.False(Ncaa06RosterCsvImporter.Preview(new StringReader(Csv("Home", "100")), ["Home"]).CanApply);
        var duplicate = csv + csv.Split('\n')[1] + "\n";
        Assert.Contains(Ncaa06RosterCsvImporter.Preview(new StringReader(duplicate), ["Home"], mapping).Errors, e => e.Contains("Duplicate PGID"));
    }

    private static string Csv(string team, string rating) =>
        "TGID,PFNA,PLNA,PYER,PPOS,PGID," + string.Join(',', Ncaa06RosterRatings.RatingFields) + "\n" +
        $"{team},First,Last,0,0,1," + string.Join(',', Ncaa06RosterRatings.RatingFields.Select(_ => rating)) + "\n";
    private static DynastyState State() => new() { DynastyName = "Test", UserTeamName = "Home", SeasonYear = 2026 };
}
