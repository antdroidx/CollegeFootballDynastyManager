using System.Text.Json;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public sealed class PortraitAndScoringPolishTests
{
    private static GameSnapEvent Snap(SnapOutcome outcome, int start = 97, int yards = 3) => new()
    { OffenseTeamName = "Home", DefenseTeamName = "Away", OffensivePlayId = "run", DefensivePlayId = "base",
        StartYardLine = start, BallEndYardLine = Math.Clamp(start + yards, 0, 100), Yards = yards, Outcome = outcome };

    [Theory]
    [InlineData(1)] [InlineData(-1)]
    public void TouchdownCarrierAndBallFinishInsideTheEndZoneWithoutExtraStatYards(int direction)
    {
        var snap = Snap(SnapOutcome.Touchdown); var before = snap with { };
        var finish = GameDayAnimationPlanner.CarrierDestinationYardLine(snap);
        var projected = GameDayAnimationPlanner.ProjectFieldYardLine(finish, direction);
        var goal = GameDayAnimationPlanner.ProjectYardLine(100, direction);
        Assert.Equal(104, finish); Assert.True(direction * (projected - goal) > 0);
        Assert.InRange(projected, 0, 1); Assert.Equal(before, snap); Assert.Equal(3, snap.Yards); Assert.Equal(100, snap.BallEndYardLine);
        var route = GameDayAnimationPlanner.RoutePoint(new(.8, .3), new(projected, .4), GameDayRouteFamily.Slants, 1);
        Assert.Equal(projected, route.X, 10);
    }

    [Theory]
    [InlineData(SnapOutcome.Run, 50, -4)] [InlineData(SnapOutcome.Completion, 97, 2)]
    [InlineData(SnapOutcome.Sack, 5, -5)] [InlineData(SnapOutcome.Punt, 35, 40)]
    public void NonScoringPlaysKeepTheRecordedSpot(SnapOutcome outcome, int start, int yards)
        => Assert.Equal(Math.Clamp(start + yards, 0, 100), GameDayAnimationPlanner.CarrierDestinationYardLine(Snap(outcome, start, yards)));

    [Fact]
    public void BackfieldCanLineUpInsideOwnEndZoneAndCoordinatesStayInBounds()
    {
        var back = GameDayAnimationPlanner.Alignment(OffensiveFormation.Singleback, Position.RB, 0);
        var x = GameDayAnimationPlanner.ProjectFieldYardLine(1 + back.X, 1);
        Assert.True(x < GameDayAnimationPlanner.ProjectYardLine(0, 1)); Assert.True(x > 0);
        Assert.InRange(GameDayAnimationPlanner.ProjectFieldYardLine(-100, 1), 0, 1);
        Assert.InRange(GameDayAnimationPlanner.ProjectFieldYardLine(200, -1), 0, 1);
        Assert.Equal(x, 1 - GameDayAnimationPlanner.ProjectFieldYardLine(1 + back.X, -1), 10);
    }

    [Fact]
    public void LongScoringCurlRoutesCannotOvershootBeyondTheRenderedEndZone()
    {
        var start = new GameFieldPoint(GameDayAnimationPlanner.ProjectFieldYardLine(3, 1), .2);
        var end = new GameFieldPoint(GameDayAnimationPlanner.ProjectFieldYardLine(104, 1), .2);
        foreach (var i in Enumerable.Range(0, 101))
            Assert.InRange(GameDayAnimationPlanner.RoutePoint(start, end, GameDayRouteFamily.Curls, i / 100d).X, 0, 1);
        Assert.Equal(end.X, GameDayAnimationPlanner.RoutePoint(start, end, GameDayRouteFamily.Curls, 1).X, 10);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void ImportedPlayersCanUseEveryFacialHairStyleWithoutLosingTheirNcaaFeatures(int style)
    {
        var appearance = new NcaaPlayerAppearance { Face = 37, SkinTone = 4, HairColor = 0, HairStyle = 3, FacialHair = style };
        var face = PortraitAppearance.ForPlayer(Guid.NewGuid(), appearance);
        Assert.Equal(style, face.FacialHair); Assert.Equal(4, face.Skin); Assert.Equal(0, face.HairColor); Assert.Equal(6, face.HairStyle);
        var loaded = JsonSerializer.Deserialize<NcaaPlayerAppearance>(JsonSerializer.Serialize(appearance));
        Assert.Equal(appearance, loaded);
    }

    [Fact]
    public void UnspecifiedImportedFacialHairRetainsStableProceduralVariety()
    {
        var styles = Enumerable.Range(0, 200).Select(i => PortraitAppearance.ForPlayer(new Guid(i, 0, 0, new byte[8]), new() { Face = 37 }).FacialHair).Distinct();
        Assert.Equal(new[] { 0, 1, 2, 3 }, styles.OrderBy(x => x));
    }

    [Theory]
    [InlineData(55, 6)] [InlineData(64, 6)] [InlineData(65, 7)] [InlineData(80, 7)]
    public void StaffAgeIntoGrayAndSilverUnlessHairColorIsExplicit(int age, int expected)
    {
        var id = Guid.NewGuid(); Assert.Equal(expected, PortraitAppearance.ForStaff(id, age).HairColor);
        Assert.Equal(0, PortraitAppearance.ForStaff(id, age, new() { HairColor = 0 }).HairColor);
        Assert.Equal(6, PortraitAppearance.ForStaff(id, age, new() { HairColor = 5 }).HairColor);
    }

    [Fact]
    public void PortraitEditsFollowPlayerThroughTransfersStatsArchivesAndReload()
    {
        var player = new DynastyPlayer { PlayerId = Guid.NewGuid(), FullName = "Player", TeamName = "Home", Position = Position.QB, OverallRating = 80 };
        var other = player with { PlayerId = Guid.NewGuid(), FullName = "Other" };
        var state = new DynastyState { DynastyName = "Test", UserTeamName = "Home", ActiveRoster = [player, other],
            TransferPortalEntries = [new() { OriginTeamName = "Home", Player = player }],
            PendingTransferCommitments = [new() { TeamName = "Away", OriginTeamName = "Home", Player = player }],
            PlayerSeasonStats = [new() { PlayerId = player.PlayerId, PlayerName = player.FullName, TeamName = "Home", PassYards = 250 }],
            PlayerCareerHistory = [new() { PlayerId = player.PlayerId, PlayerName = player.FullName }] };
        var appearance = new NcaaPlayerAppearance { FacialHair = 3, HairColor = 2 };
        var edited = PortraitCustomizationService.Apply(state, player.PlayerId, appearance, false);
        var loaded = JsonSerializer.Deserialize<DynastyState>(JsonSerializer.Serialize(edited))!;
        Assert.Equal(appearance, loaded.ActiveRoster[0].Appearance); Assert.Null(loaded.ActiveRoster[1].Appearance);
        Assert.Equal(appearance, loaded.TransferPortalEntries.Single().Player.Appearance);
        Assert.Equal(appearance, loaded.PendingTransferCommitments.Single().Player.Appearance);
        Assert.Equal(appearance, loaded.PlayerCareerHistory.Single().Appearance);
        Assert.Equal(appearance, loaded.PlayerSeasonStats.Single().Appearance);
        Assert.Equal(250, loaded.PlayerSeasonStats.Single().PassYards); Assert.Equal(80, loaded.ActiveRoster[0].OverallRating);
        Assert.Null(state.ActiveRoster[0].Appearance);
    }

    [Fact]
    public void StaffPortraitEditsUpdateBothUserAndLeagueRecords()
    {
        var coach = new StaffMember { FullName = "Coach", TeamName = "Home", Age = 60 };
        var state = new DynastyState { DynastyName = "Test", UserTeamName = "Home", UserHeadCoach = coach, Staff = [coach] };
        var appearance = new NcaaPlayerAppearance { FacialHair = 2, HairColor = 5 };
        var edited = PortraitCustomizationService.Apply(state, coach.StaffId, appearance, true);
        Assert.Equal(appearance, edited.UserHeadCoach!.Appearance); Assert.Equal(appearance, edited.Staff.Single().Appearance);
        Assert.Equal(60, edited.UserHeadCoach.Age); Assert.Null(state.UserHeadCoach!.Appearance);
        Assert.Throws<ArgumentException>(() => PortraitCustomizationService.Apply(state, coach.StaffId, appearance with { FacialHair = 99 }, true));
    }
}
