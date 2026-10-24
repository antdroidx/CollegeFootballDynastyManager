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

    [Theory]
    [InlineData("UCLA", "UCLA")]
    [InlineData("USC", "USC")]
    [InlineData("Washington State", "WSU")]
    [InlineData("Oregon State", "OSU")]
    [InlineData("Oklahoma State", "OSU")]
    [InlineData("Ohio State", "O")]
    public void SchoolMonogramsFollowExplicitSchoolIdentity(string name, string expected) => Assert.Equal(expected, TeamBrandCatalog.Monogram(name));
}
