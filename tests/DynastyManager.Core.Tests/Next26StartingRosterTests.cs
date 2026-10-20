using System.Text.Json;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Microsoft.VisualBasic.FileIO;
using DynastyManager.Data.Import;
using Xunit;

namespace DynastyManager.Core.Tests;

public sealed class Next26StartingRosterTests
{
    private static string Data(string name) => Path.Combine(AppContext.BaseDirectory, "TestData", name);
    private static Team[] Teams() => ModernFbsUniverseFactory.Create(LegacyUniverseCsvImporter.Import(File.ReadAllText(Data("universe.csv"))).Teams).ToArray();
    private static StartingRosterPack Pack() => Next26RosterCsvImporter.LoadPack(File.OpenText(Data("next26-roster.csv")), Teams().Select(t => t.Name));

    [Fact]
    public void SuppliedRosterLoadsEveryPlayerAndSchoolWithExactAttributeConversion()
    {
        var teams = Teams();
        using var reader = File.OpenText(Data("next26-roster.csv"));
        var pack = Next26RosterCsvImporter.LoadPack(reader, teams.Select(t => t.Name));
        Assert.Equal(11560, pack.Players.Count);
        Assert.Equal(136, pack.Players.Select(p => p.TeamName).Distinct().Count());
        Assert.All(pack.Players.GroupBy(p => p.TeamName), school => Assert.Equal(85, school.Count()));
        Assert.Equal(pack.Players.Count, pack.Players.Select(p => p.PlayerId).Distinct().Count());
        Assert.All(pack.Players, p => Assert.Contains(teams, t => t.Name == p.TeamName));
        using var sourceReader = new TextFieldParser(Data("next26-roster.csv")) { Delimiters = [","], HasFieldsEnclosedInQuotes = true };
        var header = sourceReader.ReadFields()!;
        for (var i = 0; i < pack.Players.Count; i++)
        {
            var values = sourceReader.ReadFields()!;
            var source = header.Select((key, index) => (key, values[index])).ToDictionary(x => x.key, x => x.Item2);
            var p = pack.Players[i]; var fields = Ncaa06RosterRatings.Export(p);
            foreach (var field in Ncaa06RosterRatings.RatingFields) Assert.Equal(int.Parse(source[field]) + 40, fields[field]);
            Assert.Equal(int.Parse(source["PPOS"]), p.NcaaPositionCode);
            Assert.Equal(int.Parse(source["PYER"]) + 1, p.ClassYear);
            Assert.Equal(source["PRSD"] == "1", p.IsRedshirted);
            Assert.Equal(source["PRSD"] != "0", p.HasRedshirted);
            Assert.Equal(60, p.Skills!.Leadership); Assert.Equal(60, p.Skills.Discipline);
            Assert.True(p.RecruitingStarsEstimated); Assert.InRange(p.OriginalRecruitingStars!.Value, 1, 5);
            Assert.Equal(Ncaa06RatingService.CalculateOverall(p), p.OverallRating);
        }
        var qb = Assert.Single(pack.Players, p => p.TeamName == "Washington" && p.FullName == "Demond Williams Jr.");
        Assert.Equal(88, qb.SpeedRating); Assert.Equal(81, qb.Skills!.PassingAccuracy); Assert.Equal(88, qb.Skills.PassingPower);
        Assert.Equal(2, qb.ClassYear);
    }

    [Fact]
    public void ModernLeagueUsesPackFor136SchoolsAndGeneratedPlayersForTwoMissingSchools()
    {
        var teams = Teams(); var pack = Pack();
        var generated = teams.Select(t => new DynastyPlayer { PlayerId = Guid.NewGuid(), TeamName = t.Name,
            FullName = "Generated Player", Position = Position.QB }).ToArray();
        var roster = StartingLibraryService.CreateRoster(new StartingLeagueSetup { Name = "Modern", Teams = teams }, pack, generated);
        Assert.Equal(11562, roster.Count);
        Assert.Equal(new[] { "North Dakota State", "Sacramento State" }, roster.Where(p => p.FullName == "Generated Player").Select(p => p.TeamName).OrderBy(n => n));
        Assert.Equal(85, roster.Count(p => p.TeamName == "Washington"));
        var sourceIds = pack.Players.Select(p => p.PlayerId).ToHashSet();
        Assert.DoesNotContain(roster, p => sourceIds.Contains(p.PlayerId));
    }

    private static string Csv(int rating, int redshirt = 0)
    {
        var fields = Ncaa06RosterRatings.RatingFields.Concat(["FIRSTNAME", "LASTNAME", "COLLEGE", "PYER", "PRSD", "PPOS", "PGID"]).ToArray();
        var row = fields.Select(f => f switch { "FIRSTNAME" => "Test", "LASTNAME" => "Player", "COLLEGE" => "Washington",
            "PYER" => "0", "PRSD" => redshirt.ToString(), "PPOS" => "0", "PGID" => "1", _ => rating.ToString() });
        return string.Join(',', fields) + "\n" + string.Join(',', row);
    }

    [Theory]
    [InlineData(0, 40)] [InlineData(59, 99)]
    public void RawAttributeEndpointsAreConvertedExactlyOnce(int raw, int displayed)
    {
        var preview = Next26RosterCsvImporter.Preview(new StringReader(Csv(raw)), ["Washington"]);
        Assert.True(preview.CanApply, string.Join(';', preview.Errors));
        Assert.Equal(displayed, preview.Players[0].SpeedRating);
        Assert.Equal(displayed, preview.Players[0].Skills!.PassingAccuracy);
    }

    [Theory]
    [InlineData(-1)] [InlineData(60)] [InlineData(99)]
    public void RejectsOutOfRangeRawRatingsInsteadOfClamping(int raw)
        => Assert.False(Next26RosterCsvImporter.Preview(new StringReader(Csv(raw)), ["Washington"]).CanApply);

    [Theory]
    [InlineData(0, false, false)] [InlineData(1, true, true)] [InlineData(2, false, true)]
    public void CurrentAndPreviousRedshirtsAreDistinct(int code, bool current, bool previous)
    {
        var player = Next26RosterCsvImporter.Preview(new StringReader(Csv(40, code)), ["Washington"]).Players.Single();
        Assert.Equal(current, player.IsRedshirted); Assert.Equal(previous, player.HasRedshirted);
    }

    [Theory]
    [InlineData(84, 1, false, 5)] [InlineData(84, 2, false, 4)]
    [InlineData(84, 4, false, 3)] [InlineData(84, 4, true, 3)]
    [InlineData(76, 1, false, 4)] [InlineData(68, 1, false, 3)]
    [InlineData(60, 1, false, 2)] [InlineData(59, 1, false, 1)]
    public void EstimatesEntryStarsUsingClassAndPreviousRedshirt(int overall, int year, bool redshirt, int expected)
    {
        var player = ImportedRecruitingStarsService.EstimateIfUnknown(new DynastyPlayer { FullName = "Import", TeamName = "Washington",
            Position = Position.QB, OverallRating = overall, ClassYear = year, HasRedshirted = redshirt });
        Assert.Equal(expected, player.OriginalRecruitingStars); Assert.True(player.RecruitingStarsEstimated);
        var reloaded = JsonSerializer.Deserialize<DynastyPlayer>(JsonSerializer.Serialize(player))!;
        Assert.Equal(player.OriginalRecruitingStars, ImportedRecruitingStarsService.EstimateIfUnknown(reloaded with { OverallRating = 99 }).OriginalRecruitingStars);
        Assert.True(reloaded.RecruitingStarsEstimated);
        Assert.False(ImportedRecruitingStarsService.EstimateIfUnknown(player with { RecruitingStarsEstimated = false }).RecruitingStarsEstimated);
    }

    [Fact]
    public void CareerArchiveRetainsEstimatedProvenanceAfterDevelopment()
    {
        var player = ImportedRecruitingStarsService.EstimateIfUnknown(new DynastyPlayer { PlayerId = Guid.NewGuid(),
            FullName = "Estimated Recruit", TeamName = "Washington", Position = Position.QB, ClassYear = 2, OverallRating = 82 });
        var state = new DynastyState { DynastyName = "NEXT", UserTeamName = "Washington", SeasonYear = 2026, ActiveRoster = [player] };
        var teams = Teams().Where(t => t.Name == "Washington").ToDictionary(t => t.Name);
        var archived = DynastyHistoryService.FinalizeSeason(state, teams);
        var career = Assert.Single(archived.PlayerCareerHistory);
        Assert.Equal(player.OriginalRecruitingStars, career.OriginalRecruitingStars); Assert.True(career.RecruitingStarsEstimated);
        var developed = archived with { SeasonYear = 2027, ActiveRoster = [player with { OverallRating = 99, ClassYear = 3 }] };
        career = Assert.Single(DynastyHistoryService.FinalizeSeason(developed, teams).PlayerCareerHistory);
        Assert.Equal(player.OriginalRecruitingStars, career.OriginalRecruitingStars); Assert.True(career.RecruitingStarsEstimated);
    }
}
