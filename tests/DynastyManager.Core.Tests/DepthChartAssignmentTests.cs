using System.Text.Json;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class DepthChartAssignmentTests
{
    private static DynastyState State(params DynastyPlayer[] roster) => new()
        { DynastyId = Guid.Empty, DynastyName = "Depth", UserTeamName = "Home", SeasonYear = 2026, ActiveRoster = roster };
    private static DynastyPlayer Player(int code, int value = 75, string team = "Home")
    {
        var fields = Ncaa06RosterRatings.RatingFields.ToDictionary(f => f, _ => value);
        fields["PPOS"] = code;
        return Ncaa06RosterRatings.Apply(new DynastyPlayer { PlayerId = Guid.NewGuid(), FullName = "Player " + code,
            TeamName = team, Position = Position.QB, DepthChartOrder = 1, ClassYear = 2 }, fields);
    }

    [Fact]
    public void PositionNavigationFollowsAll21SourceCodes()
    {
        Assert.Equal(21, DepthChartService.PositionNames.Count);
        Assert.Equal(new[] { "QB", "HB", "FB", "WR", "TE", "LT", "LG", "C", "RG", "RT" }, DepthChartService.PositionNames.Take(10));
        for (var code = 0; code < 21; code++) Assert.Equal(Player(code).Position, DepthChartService.PositionForCode(code));
        Assert.Throws<ArgumentOutOfRangeException>(() => DepthChartService.PositionForCode(21));
    }

    [Fact]
    public void EveryPositionalRatingUsesTheActiveCalibratedPociWeightsWithoutMutatingPlayer()
    {
        var player = Player(3, 80);
        for (var code = 0; code < 21; code++)
        {
            var role = player with { NcaaPositionCode = code, Position = DepthChartService.PositionForCode(code) };
            var expected = PositionFit.Apply(PlayerRatingService.CalculateOverall(role), player.Position, role.Position);
            Assert.Equal(expected, DepthChartService.PositionalRating(player, code));
        }
        Assert.Equal(3, player.NcaaPositionCode);
        Assert.Equal(Position.WR, player.Position);
    }

    [Fact]
    public void MovingAndReorderingPreservesNaturalProfileAndSurvivesSaveRoundTrip()
    {
        var receiver = Player(3); var corner = Player(16); var other = Player(16, team: "Other");
        var state = State(receiver, corner, other);
        state = DepthChartService.Assign(state, "Home", receiver.PlayerId, 16, 1);
        Assert.Equal(receiver, state.ActiveRoster.Single(p => p.PlayerId == receiver.PlayerId));
        Assert.Empty(DepthChartService.AtPosition(state, "Home", 3));
        Assert.Equal(new[] { receiver.PlayerId, corner.PlayerId }, DepthChartService.AtPosition(state, "Home", 16).Select(p => p.PlayerId));
        state = DepthChartService.Assign(state, "Home", corner.PlayerId, 16, 1);
        Assert.Equal(corner.PlayerId, DepthChartService.AtPosition(state, "Home", 16)[0].PlayerId);
        var loaded = JsonSerializer.Deserialize<DynastyState>(JsonSerializer.Serialize(state))!;
        Assert.Equal(state.DepthChartAssignments, loaded.DepthChartAssignments);
        Assert.Equal(16, DepthChartService.AssignedCode(loaded, receiver));
        Assert.Throws<ArgumentException>(() => DepthChartService.Assign(state, "Home", other.PlayerId, 0, 1));
    }

    [Fact]
    public void GameProjectionUsesPlayingPositionAndKeepsUniqueIdentities()
    {
        var receiver = Player(3); var corner = Player(16); var other = Player(3, team: "Other");
        var state = DepthChartService.Assign(State(receiver, corner, other), "Home", receiver.PlayerId, 16, 1);
        var projected = DepthChartService.ProjectRoster(state.ActiveRoster, state.DepthChartAssignments);
        var moved = projected.Single(p => p.PlayerId == receiver.PlayerId);
        Assert.Equal(Position.CB, moved.Position);
        Assert.Equal(16, moved.NcaaPositionCode);
        Assert.Equal(DepthChartService.PositionalRating(receiver, 16), moved.OverallRating);
        Assert.Equal(receiver.Skills, moved.Skills);
        Assert.Equal(receiver.SpeedRating, moved.SpeedRating);
        Assert.Equal(other, projected.Single(p => p.PlayerId == other.PlayerId));
        Assert.Equal(projected.Count, projected.Select(p => p.PlayerId).Distinct().Count());
        Assert.Equal(receiver, state.ActiveRoster[0]);
    }

    [Fact]
    public void HealthyLeftTackleBackupDoesNotStealTheRightTacklesJob()
    {
        var left = Player(5); var backup = Player(5, 70); var right = Player(9, 80);
        var middle = new[] { Player(6), Player(7), Player(8) };
        var state = State(new[] { left, backup, right }.Concat(middle).ToArray());
        state = DepthChartService.Assign(state, "Home", left.PlayerId, 5, 1);
        state = DepthChartService.Assign(state, "Home", backup.PlayerId, 5, 2);
        var projected = DepthChartService.ProjectRoster(state.ActiveRoster.Where(p => p.PlayerId != left.PlayerId).ToArray(), state.DepthChartAssignments);
        var line = OffensiveLineService.Lineup(projected);
        Assert.Equal(backup.PlayerId, line[0].PlayerId);
        Assert.Equal(right.PlayerId, line[4].PlayerId);
        Assert.Equal(5, line.Select(p => p.PlayerId).Distinct().Count());
    }

    [Fact]
    public void AutoSetIsDeterministicUniqueAndExcludesRedshirtsFromStartingJobs()
    {
        var roster = Enumerable.Range(0, 21).SelectMany(code => new[] { Player(code, 75), Player(code, 65) }).ToArray();
        roster = roster.Append(Player(0, 99) with { IsRedshirted = true }).ToArray();
        var state = State(roster);
        var automatic = DepthChartService.AutoSet(state, "Home");
        Assert.Equal(automatic.DepthChartAssignments, DepthChartService.AutoSet(state, "Home").DepthChartAssignments);
        Assert.Equal(roster.Length, automatic.DepthChartAssignments.Select(a => a.PlayerId).Distinct().Count());
        Assert.False(DepthChartService.AtPosition(automatic, "Home", 0)[0].IsRedshirted);
        Assert.Equal(roster, automatic.ActiveRoster);
        var projected = DepthChartService.ProjectRoster(automatic.ActiveRoster, automatic.DepthChartAssignments);
        var personnel = PersonnelRotationService.SelectOffense(projected, "Home", OffensivePersonnel.Eleven, new GameFatigueState(), new Random(1));
        Assert.Equal(11, personnel.PlayerIds.Distinct().Count());
    }

    [Fact]
    public void SchemeAndStaffAffectFitWithoutChangingDisplayedPositionalOvr()
    {
        var qb = Player(0, 80) with { SpeedRating = 99 };
        qb = qb with { Skills = qb.Skills! with { BallCarrying = 99 } };
        var plan = new TeamGamePlan { TeamName = "Home", Offense = OffensivePlaybook.Option };
        var low = new StaffMember { FullName = "Coach", TeamName = "Home", Role = StaffRole.OffensiveCoordinator, Scheme = 10, TalentEvaluation = 10 };
        var high = low with { Scheme = 99, TalentEvaluation = 99 };
        var state = State(qb) with { TeamGamePlans = [plan], Staff = [] };
        // Staff access goes through the same persisted league-staff collection as coaching screens.
        state = state with { Staff = [low] };
        var weakFit = DepthChartService.FitScore(state, "Home", qb, 0);
        var strongFit = DepthChartService.FitScore(state with { Staff = [high] }, "Home", qb, 0);
        Assert.True(strongFit > weakFit);
        Assert.Equal(DepthChartService.PositionalRating(qb, 0), PlayerRatingService.CalculateOverall(qb));
    }
    [Fact]
    public void GameSessionUsesAssignedRoleAndSpecialistStatsFollowActualKicker()
    {
        var source = GameDayEngineTests.Session(GameDayMode.Play);
        var receiver = source.Roster.First(p => p.TeamName == "Home" && p.Position == Position.WR);
        var kicker = Player(19); var punter = Player(20);
        var state = State(source.Roster.Concat(new[] { kicker, punter }).ToArray());
        state = DepthChartService.Assign(state, "Home", receiver.PlayerId, 16, 1);
        var game = GameDayEngine.Start(source.Game, source.HomeProfile, source.AwayProfile, state.ActiveRoster, GameDayMode.Play, state.DepthChartAssignments);
        Assert.Equal(Position.CB, game.PlayersById[receiver.PlayerId].Position);
        Assert.Equal(Position.WR, state.ActiveRoster.Single(p => p.PlayerId == receiver.PlayerId).Position);
        game.KickoffPending = false;
        game.PossessionTeamName = "Home";
        game.YardLine = 35;
        var punt = GameDayEngine.Step(game, "special-punt");
        Assert.Equal(punter.PlayerId, punt.KickerId);
        game.PossessionTeamName = "Home";
        game.KickoffPending = false;
        game.YardLine = 85;
        var kick = GameDayEngine.Step(game, "special-field-goal");
        Assert.Equal(kicker.PlayerId, kick.KickerId);
        game.IsComplete = true; // Inspect exactly the two special-team snaps above.
        var result = GameDayEngine.Finish(game);
        Assert.Equal(1, result.PlayerStats.Single(p => p.PlayerId == punter.PlayerId).Punts);
        Assert.Equal(1, result.PlayerStats.Single(p => p.PlayerId == kicker.PlayerId).FieldGoalsAttempted);
    }

    [Fact]
    public void NormalizingRosterRemovesDepartedAssignments()
    {
        var player = Player(3);
        var state = DepthChartService.Assign(State(player), "Home", player.PlayerId, 16, 1);
        Assert.Empty(RosterManagementService.NormalizeAllDepthCharts(state with { ActiveRoster = [] }).DepthChartAssignments);
    }
}
