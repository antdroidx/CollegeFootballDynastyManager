using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class GameDayCommentaryTests
{
    [Theory]
    [InlineData(SnapOutcome.Sack, "sacks Quarter Back")]
    [InlineData(SnapOutcome.Interception, "intercepts Quarter Back's pass")]
    [InlineData(SnapOutcome.Fumble, "Running Back fumbles")]
    public void TurnoverAndPressureCommentaryNamesThePlayers(SnapOutcome outcome, string expected)
    {
        var qb = Player("Quarter Back", Position.QB);
        var runner = Player("Running Back", Position.RB);
        var receiver = Player("Wide Receiver", Position.WR);
        var defender = Player("Middle Linebacker", Position.MLB);
        var players = new[] { qb, runner, receiver, defender }.ToDictionary(player => player.PlayerId);
        var snap = new GameSnapEvent
        {
            OffenseTeamName = "Home", DefenseTeamName = "Away", OffensivePlayId = "off-spread-verts",
            DefensivePlayId = "def-43-cover2", Outcome = outcome, Yards = -5,
            QuarterbackId = qb.PlayerId, BallCarrierId = runner.PlayerId, TargetId = receiver.PlayerId,
            PrimaryDefenderId = defender.PlayerId
        };

        var commentary = GameDayCommentaryService.Describe(snap, players);

        Assert.Contains(expected, commentary);
        Assert.Contains("Middle Linebacker", commentary);
    }

    [Fact]
    public void KickoffCommentaryNamesKickerAndReturner()
    {
        var kicker = Player("Kai Kicker", Position.K);
        var returner = Player("Riley Returner", Position.WR);
        var players = new[] { kicker, returner }.ToDictionary(player => player.PlayerId);
        var snap = new GameSnapEvent
        {
            OffenseTeamName = "Home", DefenseTeamName = "Away", OffensivePlayId = string.Empty,
            DefensivePlayId = string.Empty, Outcome = SnapOutcome.Kickoff, ReturnYards = 27,
            KickerId = kicker.PlayerId, ReturnerId = returner.PlayerId
        };

        Assert.Equal("Kai Kicker kicks to Riley Returner, who returns it 27 yards.",
            GameDayCommentaryService.Describe(snap, players));
    }

    [Fact]
    public void ReturnCommentaryNamesTheCoverageTackler()
    {
        var kicker = Player("Kai Kicker", Position.K);
        var returner = Player("Riley Returner", Position.WR);
        var tackler = Player("Terry Tackler", Position.MLB);
        var players = new[] { kicker, returner, tackler }.ToDictionary(player => player.PlayerId);
        var snap = new GameSnapEvent
        {
            OffenseTeamName = "Home", DefenseTeamName = "Away",
            OffensivePlayId = "off-spread-zone", DefensivePlayId = "def-43-cover3",
            Outcome = SnapOutcome.Punt, ReturnYards = 9, KickerId = kicker.PlayerId,
            ReturnerId = returner.PlayerId, PrimaryDefenderId = tackler.PlayerId
        };

        var commentary = GameDayCommentaryService.Describe(snap, players);

        Assert.Contains("Riley Returner", commentary);
        Assert.Contains("Terry Tackler makes the tackle", commentary);
    }

    [Fact]
    public void ScrambleAndFairCatchUseFootballSpecificCommentary()
    {
        var quarterback = Player("Quinn Quarterback", Position.QB);
        var defender = Player("Drew Defender", Position.MLB);
        var returner = Player("Riley Returner", Position.WR);
        var kicker = Player("Pat Punter", Position.K);
        var players = new[] { quarterback, defender, returner, kicker }.ToDictionary(player => player.PlayerId);
        var scramble = new GameSnapEvent
        {
            OffenseTeamName = "Home", DefenseTeamName = "Away", OffensivePlayId = "off-spread-verts",
            DefensivePlayId = "def-43-cover2", Outcome = SnapOutcome.Scramble, Yards = 8,
            IsQuarterbackScramble = true, QuarterbackId = quarterback.PlayerId,
            BallCarrierId = quarterback.PlayerId, PrimaryDefenderId = defender.PlayerId
        };
        var punt = new GameSnapEvent
        {
            OffenseTeamName = "Home", DefenseTeamName = "Away", OffensivePlayId = "off-spread-verts",
            DefensivePlayId = "def-43-cover2", Outcome = SnapOutcome.Punt, IsFairCatch = true,
            KickerId = kicker.PlayerId, ReturnerId = returner.PlayerId
        };

        Assert.Contains("escapes the pocket", GameDayCommentaryService.Describe(scramble, players));
        Assert.Contains("fair catch", GameDayCommentaryService.Describe(punt, players));
    }

    private static DynastyPlayer Player(string name, Position position) => new()
    {
        PlayerId = Guid.NewGuid(), FullName = name, TeamName = position == Position.MLB ? "Away" : "Home",
        Position = position, ClassYear = 2, TalentLevel = 75, OverallRating = 80, PotentialRating = 85,
        SpeedRating = 75, StrengthRating = 75, AgilityRating = 75, AwarenessRating = 75,
        TechniqueRating = 75, DurabilityRating = 80
    };
}
