using System.Text.Json;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Branding;
using Xunit;

namespace DynastyManager.Core.Tests;

public class GameDayCoachingTests
{
    [Fact]
    public void ManualQuarterbackActuallyPlaysNextSnapWithoutChangingSeasonRoster()
    {
        var s = GameDayEngineTests.Session(GameDayMode.Play);
        var depth = s.PossessionTeamName == s.Game.HomeTeamName ? s.HomeDepthChart : s.AwayDepthChart;
        var original = depth[Position.QB][0]; var backup = depth[Position.QB][1];
        Assert.True(GameDayCoachingService.Substitute(s, s.PossessionTeamName, backup.PlayerId));
        var snap = GameDayEngine.Step(s, "off-spread-verts");
        Assert.Equal(backup.PlayerId, snap.QuarterbackId);
        Assert.Contains(backup.PlayerId, snap.OffensePersonnel);
        Assert.DoesNotContain(original.PlayerId, snap.OffensePersonnel);
        Assert.Equal(snap.OffensePersonnel.Count, snap.OffensePersonnel.Distinct().Count());
        Assert.Equal(original.PlayerId, depth[Position.QB][0].PlayerId);
        Assert.False(GameDayCoachingService.Substitute(s, "Unknown", backup.PlayerId));
    }

    [Fact]
    public void TalksHaveMixedReactionsAndDoNotConsumePlayRandomness()
    {
        var s = GameDayEngineTests.Session(GameDayMode.Play);
        var comparison = GameDayEngineTests.Session(GameDayMode.Play);
        var talk = GameDayCoachingService.Talk(s, s.Game.HomeTeamName, CoachTalkTone.Challenge)!;
        Assert.Contains(talk.Reactions, r => r.Change > 0);
        Assert.Contains(talk.Reactions, r => r.Change < 0);
        Assert.Contains(talk.Reactions, r => r.Change == 0);
        Assert.Equal(comparison.Random.Next(), s.Random.Next());
        Assert.Null(GameDayCoachingService.Talk(s, s.Game.HomeTeamName, CoachTalkTone.Praise));
    }

    [Fact]
    public void TalkReactionsFadeAndSurviveFinishedGameSerialization()
    {
        var s = GameDayEngineTests.Session(GameDayMode.Play);
        var talk = GameDayCoachingService.Talk(s, s.Game.HomeTeamName, CoachTalkTone.Encourage)!;
        var player = s.PlayersById[talk.Reactions.First(r => r.Change != 0).PlayerId];
        Assert.InRange(GameDayCoachingService.LivePlayerMorale(s, player, 50), 20, 85);
        GameDayEngine.SimulateToEnd(s);
        var game = GameDayEngine.Finish(s);
        var copy = JsonSerializer.Deserialize<ScheduledGame>(JsonSerializer.Serialize(game))!;
        Assert.Single(copy.CoachTalks);
        Assert.Equal(talk.Reactions.Count, copy.CoachTalks[0].Reactions.Count);
        Assert.InRange(GameMoraleService.InGame(s, s.Game.HomeTeamName), 20, 80);
    }

    [Fact]
    public void OldGameWithoutCoachingFieldsStillLoads()
    {
        var game = JsonSerializer.Deserialize<ScheduledGame>("""{"GameId":"old","HomeTeamName":"Home","AwayTeamName":"Away"}""")!;
        Assert.Empty(game.CoachTalks);
    }

    [Fact]
    public void WinningRunRecoversMoraleAndSeasonTalkEffectsExpire()
    {
        var source = GameDayEngineTests.Session(GameDayMode.Play);
        var player = source.Roster.First(p => p.TeamName == "Home");
        var state = new DynastyState { DynastyName = "Morale", UserTeamName = "Home", SeasonYear = 2026, Week = 5,
            ActiveRoster = source.Roster,
            Schedule = Enumerable.Range(1, 4).Select(w => source.Game with { GameId = $"g{w}", Week = w,
                HasPlayed = true, HomeScore = 0, AwayScore = 14 }).ToArray() };
        var low = GameMoraleService.SeasonMorale(state, "Home");
        var recovering = state with { Schedule = state.Schedule.Select(g => g.Week >= 3 ? g with { HomeScore = 21, AwayScore = 7 } : g).ToArray() };
        Assert.True(GameMoraleService.SeasonMorale(recovering, "Home") > low);
        var talk = new CoachTalkRecord("Home", CoachTalkTone.Challenge, 2026, 5, 0, 0,
            [new(player.PlayerId, player.FullName, -5, "Felt pressured")]);
        var criticized = recovering with { CoachTalks = [talk] };
        Assert.True(GameMoraleService.PlayerMorale(criticized, player) < GameMoraleService.PlayerMorale(recovering, player));
        Assert.Equal(GameMoraleService.PlayerMorale(recovering with { Week = 8 }, player),
            GameMoraleService.PlayerMorale(criticized with { Week = 8 }, player));
        var restored = JsonSerializer.Deserialize<DynastyState>(JsonSerializer.Serialize(criticized))!;
        Assert.Single(restored.CoachTalks);
    }

    [Fact]
    public void TalksRequireBreakAndShareOneUseAcrossAudiences()
    {
        var s = GameDayEngineTests.Session(GameDayMode.Play);
        var first = GameDayCoachingService.Talk(s, "Home", CoachTalkTone.Encourage, CoachTalkAudience.Offense)!;
        Assert.All(first.Reactions, r => Assert.True(GameDayCoachingService.IsOffense(s.PlayersById[r.PlayerId].Position)));
        Assert.Null(GameDayCoachingService.Talk(s, "Home", CoachTalkTone.Praise, CoachTalkAudience.Defense));
        GameDayEngine.Step(s, "off-spread-verts");
        Assert.NotEqual("Available", GameDayCoachingService.TalkAvailability(s, "Home"));
        Assert.Null(GameDayCoachingService.Talk(s, "Home", CoachTalkTone.Challenge));
        s.Quarter = s.Events[^1].Quarter + 1;
        Assert.Equal("Available", GameDayCoachingService.TalkAvailability(s, "Home"));
        var defense = GameDayCoachingService.Talk(s, "Home", CoachTalkTone.StayComposed, CoachTalkAudience.Defense)!;
        Assert.All(defense.Reactions, r => Assert.False(GameDayCoachingService.IsOffense(s.PlayersById[r.PlayerId].Position)));
        Assert.Null(GameDayCoachingService.Talk(s, "Home", CoachTalkTone.Praise));
        GameDayEngine.SimulateToEnd(s);
        Assert.NotNull(GameDayCoachingService.Talk(s, "Home", CoachTalkTone.Praise));
        Assert.Null(GameDayCoachingService.Talk(s, "Home", CoachTalkTone.Praise));
    }

    [Fact]
    public void DepthChartRecognizesAutomaticAndManuallySelectedQuarterbacks()
    {
        var s = GameDayEngineTests.Session(GameDayMode.Play);
        var qb = s.HomeDepthChart[Position.QB];
        Assert.Contains(qb[0].PlayerId, GameDayCoachingService.Starters(s, "Home"));
        Assert.DoesNotContain(qb[1].PlayerId, GameDayCoachingService.Starters(s, "Home"));
        GameDayCoachingService.Substitute(s, "Home", qb[1].PlayerId);
        var starters = GameDayCoachingService.Starters(s, "Home");
        Assert.Contains(qb[1].PlayerId, starters);
        Assert.DoesNotContain(qb[0].PlayerId, starters);
    }

    [Theory]
    [InlineData("UCLA", "UCLA")]
    [InlineData("USC", "USC")]
    [InlineData("Washington State", "WSU")]
    [InlineData("Oregon State", "OSU")]
    [InlineData("Oklahoma State", "OSU")]
    [InlineData("Ohio State", "O")]
    public void SchoolMonogramsFollowExplicitSchoolIdentity(string name, string expected) => Assert.Equal(expected, TeamBrandCatalog.Monogram(name));

    [Fact]
    public void SecondUnitChangesMultiplePositionsAndCanBeReversedWithoutChangingDepthChart()
    {
        var s = GameDayEngineTests.Session(GameDayMode.Play);
        var originalQb = s.HomeDepthChart[Position.QB][0].PlayerId;
        var backupQb = s.HomeDepthChart[Position.QB][1].PlayerId;
        GameDayCoachingService.Substitute(s, "Home", originalQb);
        Assert.True(GameDayCoachingService.UseSecondUnit(s, "Home", true));
        Assert.Empty(s.ManualStarters);
        var snap = GameDayEngine.Step(s, "off-spread-verts");
        Assert.Equal(backupQb, snap.QuarterbackId);
        Assert.Equal(11, snap.OffensePersonnel.Distinct().Count());
        Assert.DoesNotContain(originalQb, snap.OffensePersonnel);
        Assert.Equal(originalQb, s.HomeDepthChart[Position.QB][0].PlayerId);
        Assert.True(GameDayCoachingService.UseSecondUnit(s, "Home", false));
        Assert.Empty(s.SecondUnits);
        Assert.False(GameDayCoachingService.UseSecondUnit(s, "Unknown", true));
    }

    [Fact]
    public void SecondUnitWithThinDepthKeepsDistinctPlayersAndFillsGapsWithStarters()
    {
        var source = GameDayEngineTests.Session(GameDayMode.Play);
        // Explicitly trim to three receivers; imported depth orders can be zero.
        var receivers = source.HomeDepthChart[Position.WR].Take(3).Select(p => p.PlayerId).ToHashSet();
        var roster = source.Roster.Where(p => p.TeamName != "Home" || p.Position != Position.WR || receivers.Contains(p.PlayerId)).ToArray();
        var s = GameDayEngine.Start(source.Game, source.HomeProfile, source.AwayProfile, roster, GameDayMode.Play);
        GameDayCoachingService.UseSecondUnit(s, "Home", true);
        var pool = s.HomeDepthChart[Position.WR];
        var unit = new SnapPersonnel { TeamName = "Home", PlayerIds = [pool[0].PlayerId, pool[2].PlayerId] };
        var selected = GameDayCoachingService.ApplySubstitutions(s, unit);
        Assert.Equal(2, selected.PlayerIds.Distinct().Count());
        Assert.Contains(pool[2].PlayerId, selected.PlayerIds);
    }
}
