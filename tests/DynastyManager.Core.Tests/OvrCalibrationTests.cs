using System.Globalization;
using System.Text.Json;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Import;
using Xunit;
using Xunit.Abstractions;

namespace DynastyManager.Core.Tests;

public class OvrCalibrationTests(ITestOutputHelper output)
{
    internal static string Data(string name) => Path.Combine(AppContext.BaseDirectory, "TestData", name);
    internal static Team[] Teams() => ModernFbsUniverseFactory.Create(LegacyUniverseCsvImporter.Import(File.ReadAllText(Data("universe.csv"))).Teams).ToArray();
    internal static DynastyState StartingState(int seed = 1)
    {
        using var reader = File.OpenText(Data("next26-roster.csv"));
        var pack = Next26RosterCsvImporter.LoadPack(reader, Teams().Select(t => t.Name));
        var state = new DynastyState { DynastyId = new Guid(seed, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1), DynastyName = "Calibration", UserTeamName = "Washington", SeasonYear = 2026, ActiveRoster = pack.Players };
        return RosterManagementService.NormalizeAllDepthCharts(PlayerRatingService.EnsureProfiles(state));
    }

    [Fact]
    public void CurvesAreMonotonicDeterministicAndBoundedForEveryPosition()
    {
        for (var position = 0; position < 21; position++)
        {
            var before = 40;
            for (var step = 0; step <= 9900; step++)
            {
                var score = step / 100d;
                var rating = Next26OverallCalibration.Calculate(position, score);
                Assert.InRange(rating, before, 99);
                Assert.Equal(rating, Next26OverallCalibration.Calculate(position, score));
                before = rating;
            }
            Assert.Equal(40, Next26OverallCalibration.Calculate(position, -10));
            Assert.Equal(99, Next26OverallCalibration.Calculate(position, 100));
            Assert.True(Next26OverallCalibration.Calculate(position, 98) < 99);
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => Next26OverallCalibration.Calculate(21, 80));
        Assert.Throws<ArgumentOutOfRangeException>(() => Next26OverallCalibration.Calculate(0, double.NaN));
    }

    [Fact]
    public void ProposedAnchorsAreRetainedWithDocumentedRefinement()
    {
        foreach (var line in File.ReadAllLines(Data("NEXT26_calibration_curve_v1.csv")).Skip(1))
        {
            var r = line.Split(',');
            var rating = double.Parse(r[4], CultureInfo.InvariantCulture);
            var expected = (int)Math.Round(rating + 2 * Math.Clamp((99 - rating) / 11, 0, 1), MidpointRounding.ToEven);
            Assert.Equal(expected, Next26OverallCalibration.Calculate(int.Parse(r[0]), double.Parse(r[3], CultureInfo.InvariantCulture)));
        }
    }

    [Fact]
    public void StartingRosterMatchesTargetsAndReportsAllPositionsAgainstBuild289()
    {
        var state = StartingState();
        var rows = File.ReadAllLines(Data("NCAA06_POCI.csv")).Skip(1).Select(l => l.Split(',').Select(int.Parse).ToArray()).ToDictionary(r => r[2]);
        int Legacy(DynastyPlayer p)
        {
            var r = rows[Ncaa06RatingService.PositionCode(p)];
            return Math.Clamp((int)Math.Round(50 + (Ncaa06RatingService.CalculateWeightedScore(p) - (r[0] + r[1]) / 2d) * 98 / (r[0] - r[1]), MidpointRounding.ToEven), 40, 99);
        }
        var old = RosterManagementService.NormalizeAllDepthCharts(state with { ActiveRoster = state.ActiveRoster.Select(p => p with { OverallRating = Legacy(p) }).ToArray() });
        var before = OvrMetrics.Measure(old, Teams());
        var after = OvrMetrics.Measure(state, Teams());
        OvrMetrics.Write("starting-roster", new { before, after });
        output.WriteLine(JsonSerializer.Serialize(new { before, after }));
        Assert.InRange(after.Average, 72, 74);
        Assert.InRange(after.StarterAverage, 78, 82);
        Assert.InRange(after.Percent90, 1, 2);
        Assert.InRange(after.Percent95, 0, .5);
        Assert.InRange(after.Percent99, 0, .05);
        Assert.Equal(21, after.Positions.Length);
        Assert.All(state.ActiveRoster, p => Assert.Equal(p.OverallRating, PlayerRatingService.CalculateOverall(p)));
    }

    [Fact]
    public void GeneratedRecruitsKeepTheirAttributesAcrossScoutingSigningAndSerialization()
    {
        var state = StartingState();
        var pool = HighSchoolRecruitingPoolFactory.Create(state.DynastyId, 2027, 138, []);
        Assert.Equal(21, pool.Select(p => p.AttributeProfile!.NcaaPositionCode).Distinct().Count());
        Assert.All(pool, r =>
        {
            var profile = Assert.IsType<DynastyPlayer>(r.AttributeProfile);
            Assert.Equal(r.TrueOverallRating, PlayerRatingService.CalculateOverall(profile));
            Assert.Equal(r.PotentialRating, profile.PotentialRating);
            var moved = PlayerRatingService.EnsureInitialized(state with { SeasonYear = 2036 }, profile with { TeamName = "Alabama" });
            Assert.Equal(profile.Skills, moved.Skills);
            Assert.Equal(profile.SpeedRating, moved.SpeedRating);
            Assert.Equal(profile.OverallRating, moved.OverallRating);
            var reloaded = JsonSerializer.Deserialize<HighSchoolRecruit>(JsonSerializer.Serialize(r))!;
            Assert.Equal(profile, reloaded.AttributeProfile);
        });
        foreach (var stars in Enumerable.Range(1, 5))
            Assert.True(pool.Where(p => p.StarRating == stars).Select(p => p.TrueOverallRating).Distinct().Count() > 5);
    }

    [Fact]
    public void SaveScaleMigrationPreservesAttributesAndPotentialHeadroomOnce()
    {
        var state = StartingState();
        var player = state.ActiveRoster.First(p => p.NcaaPositionCode == 5) with { OverallRating = 60, PotentialRating = 70, SeasonStartOverall = 60 };
        var portalPlayer = player with { PlayerId = Guid.NewGuid(), TeamName = "Other" };
        state = state with { OverallCalibrationVersion = 0, ActiveRoster = [player],
            TransferPortalEntries = [new TransferPortalEntry { SeasonYear = 2026, OriginTeamName = "Other", Player = portalPlayer }],
            PendingTransferCommitments = [new PendingTransferCommitment { TeamName = "Washington", OriginTeamName = "Other", Player = portalPlayer }] };
        var migrated = PlayerRatingService.EnsureProfiles(JsonSerializer.Deserialize<DynastyState>(JsonSerializer.Serialize(state))!);
        var updated = Assert.Single(migrated.ActiveRoster);
        Assert.Equal(player.Skills, updated.Skills);
        Assert.Equal(player.SpeedRating, updated.SpeedRating);
        Assert.Equal(player.NcaaPositionCode, updated.NcaaPositionCode);
        Assert.Equal(Math.Min(99, updated.OverallRating + 10), updated.PotentialRating);
        Assert.Equal(updated.OverallRating, updated.SeasonStartOverall);
        Assert.Equal(updated.OverallRating, Assert.Single(migrated.TransferPortalEntries).Player.OverallRating);
        Assert.Equal(updated.OverallRating, Assert.Single(migrated.PendingTransferCommitments).Player.OverallRating);
        var twice = PlayerRatingService.EnsureProfiles(migrated);
        Assert.Equal(updated, Assert.Single(twice.ActiveRoster));
    }

    [Fact]
    public void AttributeFittingWorksAcrossAllPositionCurvesAndTalentLevels()
    {
        var state = new DynastyState { DynastyId = Guid.Empty, DynastyName = "Fitting", UserTeamName = "Home", SeasonYear = 2026 };
        for (var code = 0; code < 21; code++)
        foreach (var target in new[] { 55, 70, 84, 90, 95 })
        for (var seed = 0; seed < 8; seed++)
        {
            var fields = Ncaa06RosterRatings.RatingFields.ToDictionary(f => f, _ => 75);
            fields["PPOS"] = code;
            var position = Ncaa06RosterRatings.Apply(new DynastyPlayer { FullName = "Role", TeamName = "Home", Position = Position.QB }, fields).Position;
            var input = new DynastyPlayer { PlayerId = new Guid(code + seed * 100, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1),
                FullName = "Generated", TeamName = "Home", Position = position, NcaaPositionCode = code, ClassYear = 1, OverallRating = target };
            var player = PlayerRatingService.EnsureInitialized(state, input);
            Assert.InRange(player.OverallRating, target - 1, target + 1);
            Assert.Equal(player.OverallRating, PlayerRatingService.CalculateOverall(player));
            Assert.Equal(player, PlayerRatingService.EnsureInitialized(state, input));
        }
    }

    [Fact]
    public void OffseasonCpuDepthReassessmentKeepsUserOrdering()
    {
        var state = StartingState();
        DynastyPlayer Make(string team, int target, int order) => PlayerRatingService.EnsureInitialized(state,
            new DynastyPlayer { PlayerId = Guid.NewGuid(), FullName = team + order, TeamName = team, Position = Position.QB, ClassYear = 3,
                OverallRating = target, PotentialRating = target, DepthChartOrder = order });
        state = state with { Phase = SeasonPhase.Offseason, ActiveRoster = [Make("Washington", 60, 1), Make("Washington", 85, 2), Make("Alabama", 60, 1), Make("Alabama", 85, 2)] };
        var result = PlayerDevelopmentService.ApplyOffseasonDevelopment(state);
        Assert.True(result.ActiveRoster.Single(p => p.TeamName == "Washington" && p.DepthChartOrder == 1).OverallRating < 70);
        Assert.True(result.ActiveRoster.Single(p => p.TeamName == "Alabama" && p.DepthChartOrder == 1).OverallRating > 80);
    }
}

internal sealed record PopulationOvr(int Count, double Average, double Median, double Percent80, double Percent85, double Percent90, double Percent95, double Percent99);
internal sealed record PositionOvr(int Code, int Count, double Average, double StarterAverage, double Percent90, PopulationOvr Distribution);
internal sealed record OvrSnapshot(double Average, double Median, double StarterAverage, double Percent80, double Percent85, double Percent90, double Percent95, double Percent99,
    double FreshmanAverage, double SeniorAverage, double EliteProgramAverage, double WeakProgramAverage, PositionOvr[] Positions, object[] Stars, PopulationOvr Freshmen, PopulationOvr Seniors);

internal static class OvrMetrics
{
    public static OvrSnapshot Measure(DynastyState state, IEnumerable<Team> teams)
    {
        var roster = state.ActiveRoster.ToArray();
        double Pct(IEnumerable<DynastyPlayer> players, int threshold) { var a = players.ToArray(); return a.Length == 0 ? 0 : 100d * a.Count(p => p.OverallRating >= threshold) / a.Length; }
        double Avg(IEnumerable<DynastyPlayer> players) { var a = players.ToArray(); return a.Length == 0 ? 0 : a.Average(p => p.OverallRating); }
        PopulationOvr Distribution(IEnumerable<DynastyPlayer> players)
        {
            var a = players.OrderBy(p => p.OverallRating).ToArray();
            var median = a.Length == 0 ? 0 : (a[(a.Length - 1) / 2].OverallRating + a[a.Length / 2].OverallRating) / 2d;
            return new(a.Length, Avg(a), median, Pct(a, 80), Pct(a, 85), Pct(a, 90), Pct(a, 95), Pct(a, 99));
        }
        var sorted = roster.Select(p => p.OverallRating).Order().ToArray();
        var rankedTeams = teams.OrderByDescending(t => t.Prestige).ToArray();
        var quarter = Math.Max(1, rankedTeams.Length / 4);
        var elite = rankedTeams.Take(quarter).Select(t => t.Name).ToHashSet();
        var weak = rankedTeams.TakeLast(quarter).Select(t => t.Name).ToHashSet();
        var positions = Enumerable.Range(0, 21).Select(code =>
        {
            var players = roster.Where(p => Ncaa06RatingService.PositionCode(p) == code).ToArray();
            // Position-specific depth leaders, including FB/P specialists; same slots before/after.
            var slots = code switch { 3 or 16 => 3, 12 => 2, _ => 1 };
            var starters = players.GroupBy(p => p.TeamName).SelectMany(g => g.OrderByDescending(p => p.OverallRating).Take(slots));
            return new PositionOvr(code, players.Length, Avg(players), Avg(starters), Pct(players, 90), Distribution(players));
        }).ToArray();
        var stars = Enumerable.Range(1, 5).Select(star =>
        {
            var players = roster.Where(p => p.OriginalRecruitingStars == star).ToArray();
            var ids = players.Select(p => p.PlayerId).ToHashSet();
            var development = state.PlayerDevelopmentHistory.Where(r => r.SeasonYear >= state.SeasonYear - 1 && ids.Contains(r.PlayerId)).ToArray();
            return (object)new { star, count = players.Length, average = Avg(players), freshmanAverage = Avg(players.Where(p => p.ClassYear == 1)), seniorAverage = Avg(players.Where(p => p.ClassYear >= 4)), percent90 = Pct(players, 90), distribution = Distribution(players),
                developmentRecords = development.Length, meanDevelopmentPerStage = development.Length == 0 ? 0 : development.Average(r => r.AfterOverall - r.BeforeOverall) };
        }).ToArray();
        return new(Avg(roster), (sorted[(sorted.Length - 1) / 2] + sorted[sorted.Length / 2]) / 2d,
            Avg(roster.Where(RosterManagementService.IsStarter)), Pct(roster, 80), Pct(roster, 85), Pct(roster, 90), Pct(roster, 95), Pct(roster, 99),
            Avg(roster.Where(p => p.ClassYear == 1)), Avg(roster.Where(p => p.ClassYear >= 4)), Avg(roster.Where(p => elite.Contains(p.TeamName))), Avg(roster.Where(p => weak.Contains(p.TeamName))), positions, stars,
            Distribution(roster.Where(p => p.ClassYear == 1)), Distribution(roster.Where(p => p.ClassYear >= 4)));
    }
    public static void Write(string name, object report)
    {
        var path = Environment.GetEnvironmentVariable("CFDM_CALIBRATION_REPORT_DIR");
        if (string.IsNullOrWhiteSpace(path)) return;
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, name + ".json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    }
}
