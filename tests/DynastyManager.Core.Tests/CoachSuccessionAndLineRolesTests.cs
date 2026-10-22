using System.Text.Json;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class CoachSuccessionAndLineRolesTests
{
    private static DynastyState State() => new() { DynastyId = Guid.Parse("4e46d350-cd6b-4426-bdb8-41b42bf38888"),
        DynastyName = "Test", UserTeamName = "User", SeasonYear = 2035 };
    private static DynastyPlayer Player(int code, int depth) => new() { PlayerId = Guid.NewGuid(), FullName = "Lineman",
        TeamName = "CPU", Position = Position.OL, NcaaPositionCode = code, DepthChartOrder = depth, OverallRating = 75 };

    [Fact]
    public void ImportedLineCodesArePreservedAndUnknownRolesAreBalanced()
    {
        var known = Player(9, 1);
        var players = OffensiveLineService.AssignMissingRoles(new[] { known }.Concat(Enumerable.Range(0, 9).Select(_ => Player(0, 0))));
        Assert.Equal(9, players.Single(p => p.PlayerId == known.PlayerId).NcaaPositionCode);
        Assert.All(players.GroupBy(p => p.NcaaPositionCode), g => Assert.Equal(2, g.Count()));
        Assert.Equal(new[] { "OT", "OG", "OC", "OG", "OT" }, Enumerable.Range(5, 5).Select(c => OffensiveLineService.Label(Player(c, 0))));
    }

    [Fact]
    public void LineupHasFiveDistinctRoleBalancedStartersAndDepthMovePromotesBackup()
    {
        var roster = Enumerable.Range(5, 5).Select((code, index) => Player(code, index + 1)).ToList();
        var backup = Player(5, 6); roster.Add(backup);
        var state = RosterManagementService.NormalizeAllDepthCharts(State() with { ActiveRoster = roster });
        Assert.Equal(5, state.ActiveRoster.Count(RosterManagementService.IsStarter));
        state = RosterManagementService.MovePlayer(state, "CPU", backup.PlayerId, -1);
        var line = OffensiveLineService.Lineup(state.ActiveRoster.OrderBy(p => p.DepthChartOrder).ToArray());
        Assert.Contains(line, p => p.PlayerId == backup.PlayerId);
        Assert.Equal(5, line.Select(p => p.PlayerId).Distinct().Count());
        Assert.Equal(2, line.Count(p => OffensiveLineService.Label(p) == "OT"));
        Assert.Equal(2, line.Count(p => OffensiveLineService.Label(p) == "OG"));
        Assert.Single(line.Where(p => OffensiveLineService.Label(p) == "OC"));
    }

    [Fact]
    public void RetirementIsAnnualIdentityPreservingAndNeverAgesUserAvatarOut()
    {
        var old = new StaffMember { FullName = "Veteran", TeamName = "CPU", Age = 80, Role = StaffRole.HeadCoach };
        var user = old with { StaffId = Guid.NewGuid(), TeamName = "User" };
        var state = State() with { Staff = [old], UserHeadCoach = user };
        var result = StaffManagementService.AdvanceSeason(state);
        Assert.DoesNotContain(result.Staff, s => s.StaffId == old.StaffId);
        Assert.Equal(old.StaffId, Assert.Single(result.StaffRetirements).Coach.StaffId);
        Assert.Equal(user, result.UserHeadCoach);
        Assert.Same(result, StaffManagementService.AdvanceSeason(result));
        var roundTrip = JsonSerializer.Deserialize<DynastyState>(JsonSerializer.Serialize(result))!;
        Assert.Equal(old.StaffId, Assert.Single(roundTrip.StaffRetirements).Coach.StaffId);
    }

    [Fact]
    public void TalentedGraduatesNeedApprenticeshipAndOnlyFillTheirFootballRole()
    {
        var state = State();
        PlayerDepartureRecord graduate;
        do { graduate = new() { PlayerId = Guid.NewGuid(), SeasonYear = 2029, FullName = "Alumnus", TeamName = "CPU",
            Position = Position.QB, Reason = PlayerDepartureReason.Graduation, Leadership = 90, Awareness = 90, Discipline = 85 }; }
        while (!CoachSuccessionService.EligibleGraduate(state, graduate, StaffRole.OffensiveCoordinator));
        Assert.False(CoachSuccessionService.EligibleGraduate(state, graduate with { SeasonYear = 2034 }, StaffRole.OffensiveCoordinator));
        Assert.False(CoachSuccessionService.EligibleGraduate(state, graduate with { Reason = PlayerDepartureReason.EarlyProDeclaration }, StaffRole.OffensiveCoordinator));
        Assert.False(CoachSuccessionService.EligibleGraduate(state, graduate, StaffRole.HeadCoach));
        Assert.False(CoachSuccessionService.EligibleGraduate(state, graduate, StaffRole.MedicalTrainingDirector));
        state = state with { PlayerDepartureHistory = [graduate], Staff = [new StaffMember { FullName = "Old", TeamName = "CPU", Role = StaffRole.OffensiveCoordinator, Age = 80 }] };
        var result = StaffManagementService.AdvanceSeason(state);
        var coach = Assert.Single(result.Staff);
        Assert.Equal(graduate.PlayerId, coach.FormerPlayerId);
        Assert.InRange(coach.OverallRating, 50, 75);
        Assert.Equal(graduate.FullName, coach.FullName);
        Assert.False(CoachSuccessionService.EligibleGraduate(result, graduate, StaffRole.OffensiveCoordinator));
    }
}

