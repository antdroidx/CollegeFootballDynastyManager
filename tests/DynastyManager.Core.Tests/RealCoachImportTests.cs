using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Import;
using Xunit;

namespace DynastyManager.Core.Tests;

public class RealCoachImportTests
{
    private static IReadOnlyList<RealCoachSeed> Seeds()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "src", "DynastyManager.App", "Resources", "Raw", "coaches-2025.csv"))) directory = directory.Parent;
        using var reader = File.OpenText(Path.Combine(directory!.FullName, "src", "DynastyManager.App", "Resources", "Raw", "coaches-2025.csv"));
        return RealCoachCsvImporter.Read(reader);
    }

    [Fact]
    public void BundledPackCoversEachTeamAndRoleWith2025IdentitySources()
    {
        var seeds = Seeds();
        Assert.Equal(138, seeds.Select(s => s.Team).Distinct().Count());
        Assert.Contains(seeds, s => s.Team == "UConn" && s.Role == StaffRole.HeadCoach);
        Assert.DoesNotContain(seeds, s => s.Team == "Connecticut");
        foreach (var team in seeds.GroupBy(s => s.Team))
            Assert.Equal(4, team.Select(s => s.Role).Distinct().Count());
        Assert.All(seeds, s => { Assert.Equal(2025, s.Season); Assert.StartsWith("https://", s.SourceUrl); });
        Assert.Contains(seeds, s => s.Team == "Arkansas" && s.Role == StaffRole.OffensiveCoordinator && s.FullName == "Bobby Petrino");
        Assert.Contains(seeds, s => s.Team == "Penn State" && s.Role == StaffRole.HeadCoach && s.FullName == "James Franklin");
        Assert.Contains(seeds, s => s.Team == "Northern Illinois" && s.Role == StaffRole.HeadCoach && s.FullName == "Thomas Hammock");
        Assert.Contains(seeds, s => s.Team == "Southern Miss" && s.Role == StaffRole.HeadCoach && s.FullName == "Charles Huff");
        Assert.Contains(seeds, s => s.Team == "Coastal Carolina" && s.Role == StaffRole.HeadCoach && s.FullName == "Tim Beck");
        Assert.DoesNotContain(seeds, s => s.Team == "North Carolina" && s.FullName == "Bobby Petrino");
    }

    [Fact]
    public void ImportPreservesGameRatingsScoutsAndHumanCoachAndListsCoordinators()
    {
        var teams = new[] { "Alabama", "Akron", "User" }.Select(n => new Team { Name = n, Abbreviation = n, ConferenceName = "Test", Prestige = 70 }).ToArray();
        var state = StaffManagementService.EnsureLeagueStaff(new DynastyState { DynastyName = "Test", UserTeamName = "User", SeasonYear = 2026 }, teams);
        var original = state.Staff.Single(s => s.TeamName == "Alabama" && s.Role == StaffRole.OffensiveCoordinator);
        var scout = state.Staff.Single(s => s.TeamName == "Alabama" && s.Role == StaffRole.ChiefScout);
        var imported = RealCoachCsvImporter.ApplyToNewDynasty(state, Seeds());
        var coach = StaffManagementService.GetStaff(imported, "Alabama", StaffRole.OffensiveCoordinator)!;
        Assert.Equal("Ryan Grubb", coach.FullName);
        Assert.Equal(original.OverallRating, coach.OverallRating);
        Assert.Equal(2, coach.SharedRoleCoaches.Count);
        Assert.Contains(coach.SharedRoleCoaches, c => c.FullName == "Nick Sheridan");
        Assert.Contains(imported.Staff, s => s == scout);
        Assert.Equal(state.UserHeadCoach, imported.UserHeadCoach);
        var head = StaffManagementService.GetStaff(imported, "Akron", StaffRole.HeadCoach)!;
        var offense = StaffManagementService.GetStaff(imported, "Akron", StaffRole.OffensiveCoordinator)!;
        Assert.Equal(head.StaffId, offense.StaffId);
        Assert.True(offense.IsHeadCoachRoleAssignment);
        Assert.False(CoachSuccessionService.ShouldRetire(imported, offense with { Age = 90 }));
        Assert.Equal(coach.FullName, StaffManagementService.GetStaff(StaffManagementService.EnsureLeagueStaff(imported, teams), "Alabama", StaffRole.OffensiveCoordinator)!.FullName);
    }

    [Theory]
    [InlineData("2025,Test,Scout,Fake,Title,https://example.org")]
    [InlineData("oops,Test,HC,Name,Title,https://example.org")]
    [InlineData("2025,Test,HC,,Title,https://example.org")]
    [InlineData("2025,Test,HC,Name,Title,file:///coach")]
    public void InvalidDataIsRejected(string row) => Assert.Throws<FormatException>(() =>
        RealCoachCsvImporter.Read(new StringReader("Season,Team,Role,Name,Title,SourceUrl,Ignored\n" + row)));
}
