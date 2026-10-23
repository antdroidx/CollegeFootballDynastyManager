using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Persistence;
using Xunit;

namespace DynastyManager.Core.Tests;

public class GamePlanTests
{
    [Fact]
    public void CustomBooksReplaceRatherThanAppendToTheCoachBook()
    {
        var plan = Plan("Home", OffensivePlaybook.Custom) with
        {
            Defense = DefensivePlaybook.Custom,
            CustomOffensivePlayIds = ["off-flexbone-triple", "off-singleback-curl"],
            CustomDefensivePlayIds = ["def-34-cross"]
        };
        var saved = GamePlanService.GetPlan(GamePlanService.SetPlan(State(), plan), Profile("Home"));
        Assert.Equal(2, GamePlanService.AvailableOffensivePlays(saved).Count);
        Assert.Single(GamePlanService.AvailableDefensivePlays(saved));
        for (var seed = 0; seed < 20; seed++)
        {
            var call = GamePlanService.ResolvePlayCall(saved, saved, seed, new Random(seed));
            Assert.Contains(call.Offense.Id, plan.CustomOffensivePlayIds);
            Assert.Contains(call.Defense.Id, plan.CustomDefensivePlayIds);
        }
        Assert.Throws<ArgumentException>(() => GamePlanService.SetPlan(State(), plan with
        { CustomOffensivePlayIds = PlaybookCatalog.OffensivePlays.Take(18).Select(p => p.Id).ToArray() }));
    }

    [Fact]
    public void CoachPlaybooksExposeACuratedFormationMix()
    {
        var plan = Plan("Home", OffensivePlaybook.PowerRun) with
        {
            OffensiveFormation = OffensiveFormation.IForm,
            Defense = DefensivePlaybook.ThreeFour,
            DefensiveFormation = DefensiveFormation.ThreeFour
        };

        var offense = GamePlanService.AvailableOffensivePlays(plan);
        var defense = GamePlanService.AvailableDefensivePlays(plan);

        Assert.True(offense.Count < PlaybookCatalog.OffensivePlays.Count);
        Assert.InRange(offense.Select(play => play.Formation).Distinct().Count(), 2, 3);
        Assert.Contains(offense, play => play.Concept is OffensiveConcept.InsideRun or OffensiveConcept.OutsideRun);
        Assert.Contains(offense, play => play.Concept is OffensiveConcept.QuickPass or OffensiveConcept.DeepPass or OffensiveConcept.PlayAction);
        Assert.True(defense.Count < PlaybookCatalog.DefensivePlays.Count);
        Assert.InRange(defense.Select(play => play.Formation).Distinct().Count(), 2, 3);
    }

    [Fact]
    public void SavedPlansOwnTheirCallSheetAndStayWithTheirSchool()
    {
        var calls = new List<OffensiveConcept> { OffensiveConcept.DeepPass, OffensiveConcept.InsideRun };
        var state = GamePlanService.SetPlan(State(), new TeamGamePlan { TeamName = "Home", OpeningScript = calls });
        calls.Clear();
        state = state with { UserTeamName = "Away" };
        state = GamePlanService.SetPlan(state, Plan("Away", OffensivePlaybook.PowerRun));
        Assert.Equal(2, state.TeamGamePlans.Count);
        Assert.Equal(2, GamePlanService.GetPlan(state, Profile("Home")).OpeningScript.Count);
        Assert.Equal(OffensivePlaybook.PowerRun, GamePlanService.GetPlan(state, Profile("Away")).Offense);
        Assert.Throws<ArgumentException>(() => GamePlanService.SetPlan(state, Plan("Home", (OffensivePlaybook)999)));
    }

    [Fact]
    public void FormationAndFavoritePlaysPersistAsThePlaybookEditorFoundation()
    {
        var plan = Plan("Home", OffensivePlaybook.Spread) with
        {
            OffensiveFormation = OffensiveFormation.Pistol,
            DefensiveFormation = DefensiveFormation.Nickel,
            CustomOffensivePlayIds = ["off-pistol-read", "off-pistol-read"], CustomDefensivePlayIds = ["def-nickel-two"]
        };
        var saved = GamePlanService.GetPlan(GamePlanService.SetPlan(State(), plan), Profile("Home"));

        Assert.Equal(OffensiveFormation.Pistol, saved.OffensiveFormation);
        Assert.Equal(DefensiveFormation.Nickel, saved.DefensiveFormation);
        Assert.Equal(new[] { "off-pistol-read" }, saved.CustomOffensivePlayIds);
        Assert.Equal(new[] { "def-nickel-two" }, saved.CustomDefensivePlayIds);
        Assert.Throws<ArgumentException>(() => GamePlanService.SetPlan(State(), plan with { CustomOffensivePlayIds = ["unknown-play"] }));
        Assert.Contains(PlaybookCatalog.OffensivePlays, play => play.Formation == OffensiveFormation.Pistol && play.Personnel == OffensivePersonnel.Eleven);
    }

    [Fact]
    public void CpuAndUserCallsUseTheSamePlayResolver()
    {
        var offense = Plan("Home", OffensivePlaybook.Spread) with
        {
            OffensiveFormation = OffensiveFormation.Pistol,
            OpeningPlayIds = ["off-pistol-read"]
        };
        var defense = Plan("Away", OffensivePlaybook.Balanced) with
        {
            DefensiveFormation = DefensiveFormation.Nickel,
            Approach = DefensiveApproach.ProtectPass
        };

        var userCall = GamePlanService.ResolvePlayCall(offense, defense, 0, new Random(7));
        var cpuCall = GamePlanService.ResolvePlayCall(offense with { OpeningPlayIds = [] }, defense, 0, new Random(7));

        Assert.Equal("off-pistol-read", userCall.Offense.Id);
        Assert.Equal(PlayCallSource.User, userCall.Source);
        Assert.Equal(PlayCallSource.Cpu, cpuCall.Source);
        Assert.Equal(OffensiveFormation.Pistol, cpuCall.Offense.Formation);
        Assert.Equal(DefensiveFormation.Nickel, userCall.Defense.Formation);
    }

    [Fact]
    public void GameManagementImprovesCpuAdherenceWithoutChangingUserCalls()
    {
        var offense = Plan("Home", OffensivePlaybook.AirRaid) with { OffensiveFormation = OffensiveFormation.ShotgunSpread };
        var defense = Plan("Away", OffensivePlaybook.Balanced) with
            { DefensiveFormation = DefensiveFormation.Nickel, Approach = DefensiveApproach.ProtectPass };
        var strongMatches = 0;
        var weakMatches = 0;
        for (var seed = 0; seed < 300; seed++)
        {
            var expected = GamePlanService.GetConcept(offense, seed % 12);
            if (GamePlanService.ResolvePlayCall(offense, defense, seed % 12, new Random(seed), 95, 95).Offense.Concept == expected)
                strongMatches++;
            if (GamePlanService.ResolvePlayCall(offense, defense, seed % 12, new Random(seed), 25, 25).Offense.Concept == expected)
                weakMatches++;
        }
        Assert.True(strongMatches > weakMatches + 40, $"Strong {strongMatches}, weak {weakMatches}");

        var explicitCall = offense with { OpeningPlayIds = ["off-spread-verts"] };
        Assert.Equal("off-spread-verts",
            GamePlanService.ResolvePlayCall(explicitCall, defense, 0, new Random(1), 25, 25).Offense.Id);
    }

    [Fact]
    public void InitialPlayCatalogHasUniqueIdsAndEveryFormation()
    {
        var ids = PlaybookCatalog.OffensivePlays.Select(play => play.Id)
            .Concat(PlaybookCatalog.DefensivePlays.Select(play => play.Id)).ToArray();
        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.All(Enum.GetValues<OffensiveFormation>(), formation =>
            Assert.True(PlaybookCatalog.OffensivePlays.Count(play => play.Formation == formation) >= 8,
                $"{formation} needs at least eight offensive calls."));
        Assert.All(Enum.GetValues<DefensiveFormation>(), formation =>
            Assert.True(PlaybookCatalog.DefensivePlays.Count(play => play.Formation == formation) >= 6,
                $"{formation} needs at least six defensive calls."));
    }

    [Fact]
    public void DriveReportReconcilesBoxScoreAndCapturesOpeningCalls()
    {
        var script = new[] { OffensiveConcept.DeepPass, OffensiveConcept.InsideRun, OffensiveConcept.QuickPass };
        var profile = Profile("Home") with { GamePlan = Plan("Home", OffensivePlaybook.AirRaid) with { OpeningScript = script } };
        for (var seed = 0; seed < 50; seed++)
        {
            var game = Simulate(seed, profile, Profile("Away"));
            Assert.NotEqual(game.HomeScore, game.AwayScore);
            Assert.Equal(script, game.Drives.Where(drive => drive.TeamName == "Home").Take(3).Select(drive => drive.Concept));
            foreach (var (team, stats, score) in new[] { ("Home", game.HomeStats!, game.HomeScore), ("Away", game.AwayStats!, game.AwayScore) })
            {
                var drives = game.Drives.Where(drive => drive.TeamName == team).ToArray();
                Assert.Equal(score, drives.Sum(drive => drive.Points));
                Assert.Equal(stats.Possessions, drives.Count(drive => !drive.IsOvertime));
                Assert.Equal(stats.PassAttempts, drives.Sum(drive => drive.PassAttempts));
                Assert.Equal(stats.RushAttempts, drives.Sum(drive => drive.RushAttempts));
                Assert.Equal(stats.PassYards, drives.Sum(drive => drive.PassYards));
                Assert.Equal(stats.RushYards, drives.Sum(drive => drive.RushYards));
                Assert.Equal(stats.Turnovers, drives.Count(drive => drive.Outcome == DriveOutcome.Turnover));
            }
            Assert.Same(game, DeterministicGameSimulator.Simulate(game, Team("Home"), Team("Away"), Profile("Home"), Profile("Away")));
        }
    }

    [Fact]
    public void PlaybooksChangeTendenciesAndDeepCallsTradeControlForYards()
    {
        var airPasses = 0; var powerPasses = 0; var deepTurnovers = 0; var quickTurnovers = 0;
        var deepYards = 0; var quickYards = 0;
        for (var seed = 0; seed < 400; seed++)
        {
            airPasses += Simulate(seed, Profile("Home") with { GamePlan = Plan("Home", OffensivePlaybook.AirRaid) }, Profile("Away")).HomeStats!.PassAttempts;
            powerPasses += Simulate(seed, Profile("Home") with { GamePlan = Plan("Home", OffensivePlaybook.PowerRun) }, Profile("Away")).HomeStats!.PassAttempts;
            var deep = Simulate(seed, Script(OffensiveConcept.DeepPass), Profile("Away")).HomeStats!;
            var quick = Simulate(seed, Script(OffensiveConcept.QuickPass), Profile("Away")).HomeStats!;
            deepTurnovers += deep.Turnovers; quickTurnovers += quick.Turnovers;
            deepYards += deep.PassYards; quickYards += quick.PassYards;
        }
        Assert.True(airPasses > powerPasses * 1.5, $"Air Raid {airPasses}, Power Run {powerPasses}");
        Assert.True(deepTurnovers > quickTurnovers, $"Deep {deepTurnovers}, Quick {quickTurnovers}");
        Assert.True(deepYards > quickYards, $"Deep {deepYards}, Quick {quickYards}");
    }

    [Fact]
    public void DefensiveFocusTradesRunStrengthForPassCoverage()
    {
        var runAgainstRunFocus = 0; var runAgainstPassFocus = 0;
        var passAgainstRunFocus = 0; var passAgainstPassFocus = 0;
        for (var seed = 0; seed < 300; seed++)
        {
            var runDefense = Profile("Away") with { GamePlan = Plan("Away", OffensivePlaybook.Balanced) with { Approach = DefensiveApproach.StopRun } };
            var passDefense = Profile("Away") with { GamePlan = Plan("Away", OffensivePlaybook.Balanced) with { Approach = DefensiveApproach.ProtectPass } };
            runAgainstRunFocus += Simulate(seed, Script(OffensiveConcept.InsideRun), runDefense).HomeStats!.RushYards;
            runAgainstPassFocus += Simulate(seed, Script(OffensiveConcept.InsideRun), passDefense).HomeStats!.RushYards;
            passAgainstRunFocus += Simulate(seed, Script(OffensiveConcept.QuickPass), runDefense).HomeStats!.PassYards;
            passAgainstPassFocus += Simulate(seed, Script(OffensiveConcept.QuickPass), passDefense).HomeStats!.PassYards;
        }
        Assert.True(runAgainstRunFocus < runAgainstPassFocus);
        Assert.True(passAgainstPassFocus < passAgainstRunFocus);
    }

    [Fact]
    public async Task ReloadPreservesPlanAndCompletedReportsWithoutRerolling()
    {
        var state = GamePlanService.SetPlan(State() with { Schedule = [Game(42)] },
            Plan("Home", OffensivePlaybook.PowerRun) with { OpeningScript = [OffensiveConcept.DeepPass] });
        // No profiles supplied: public weekly simulation must still apply saved plans.
        state = WeekSimulation.SimulateCurrentRegularSeasonWeek(state, Teams());
        Assert.Equal(OffensiveConcept.DeepPass, state.Schedule[0].Drives[0].Concept);
        var path = Path.Combine(Path.GetTempPath(), $"game-plan-{Guid.NewGuid():N}.db");
        try
        {
            await using var repository = new SqliteDynastySaveRepository(path);
            var save = await repository.SaveAsync(state, SaveKind.Manual);
            var loaded = await repository.LoadAsync(save);
            Assert.NotNull(loaded);
            Assert.Equivalent(state.TeamGamePlans, loaded.TeamGamePlans, strict: true);
            Assert.Equivalent(state.Schedule, loaded.Schedule, strict: true);
            var changed = GamePlanService.SetPlan(loaded, Plan("Home", OffensivePlaybook.AirRaid));
            var repeated = WeekSimulation.SimulateCurrentRegularSeasonWeek(changed, Teams());
            Assert.Same(changed, repeated);
            Assert.Equal(OffensivePlaybook.PowerRun, repeated.Schedule[0].HomeGamePlan!.Offense);
        }
        finally { foreach (var suffix in new[] { "", "-wal", "-shm" }) if (File.Exists(path + suffix)) File.Delete(path + suffix); }
    }

    [Theory]
    [InlineData(ScheduledGameType.ConferenceChampionship, SeasonPhase.ConferenceChampionship)]
    [InlineData(ScheduledGameType.Bowl, SeasonPhase.Postseason)]
    [InlineData(ScheduledGameType.CollegeFootballPlayoff, SeasonPhase.Postseason)]
    public void SavedPlansApplyInEveryPostseasonPath(ScheduledGameType type, SeasonPhase phase)
    {
        var state = GamePlanService.SetPlan(State() with { Phase = phase, Schedule = [Game(25) with { GameType = type }] },
            Plan("Home", OffensivePlaybook.AirRaid) with { OpeningScript = [OffensiveConcept.OutsideRun] });
        var result = type switch
        {
            ScheduledGameType.ConferenceChampionship => ConferenceChampionshipService.SimulateChampionships(state, Teams()),
            ScheduledGameType.Bowl => BowlService.SimulateCurrentWeek(state, Teams()),
            _ => CollegeFootballPlayoffService.SimulateCurrentRound(state, Teams())
        };
        Assert.True(result.Schedule[0].HasPlayed);
        Assert.Equal(OffensiveConcept.OutsideRun, result.Schedule[0].Drives.First(d => d.TeamName == "Home").Concept);
    }

    private static TeamGamePlan Plan(string name, OffensivePlaybook book) => new() { TeamName = name, Offense = book };
    private static TeamSimulationProfile Script(OffensiveConcept concept) => Profile("Home") with
    { GamePlan = Plan("Home", OffensivePlaybook.Balanced) with { OpeningScript = Enumerable.Repeat(concept, 12).ToArray() } };
    private static DynastyState State() => new() { DynastyName = "Coaching", UserTeamName = "Home", SeasonYear = 2026, Week = 1, Phase = SeasonPhase.RegularSeason };
    private static Dictionary<string, Team> Teams() => new[] { Team("Home"), Team("Away") }.ToDictionary(team => team.Name);
    private static Team Team(string name) => new() { Name = name, Abbreviation = name, ConferenceName = "Test", Prestige = 70 };
    private static TeamSimulationProfile Profile(string name) => new() { TeamName = name, PassOffenseRating = 75, RushOffenseRating = 75,
        PassDefenseRating = 75, RushDefenseRating = 75, SpecialTeamsRating = 75 };
    private static ScheduledGame Game(int seed) => new() { GameId = $"game-{seed}", SeasonYear = 2026, Week = 1, HomeTeamName = "Home", AwayTeamName = "Away", SimulationSeed = seed };
    private static ScheduledGame Simulate(int seed, TeamSimulationProfile home, TeamSimulationProfile away) =>
        DeterministicGameSimulator.Simulate(Game(seed), Team("Home"), Team("Away"), home, away);
}
