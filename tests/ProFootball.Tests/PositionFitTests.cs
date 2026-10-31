using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace ProFootball.Tests;

public class PositionFitTests
{
    static DynastyPlayer Player(Position position, int overall = 85)
    {
        var raw = new DynastyPlayer
        {
            PlayerId = Guid.NewGuid(), FullName = "Test Player", TeamName = "Seattle Seahawks",
            Position = position, OverallRating = overall, SeasonStartOverall = overall, PotentialRating = overall,
            SpeedRating = overall, AgilityRating = overall, StrengthRating = overall, AwarenessRating = overall,
            InjuryRating = 85, ClassYear = 3
        };
        return PlayerSkillService.EnsureInitialized(raw);
    }

    [Fact]
    public void CloseAndDistantPositionsReceiveExpectedKnockdowns()
    {
        Assert.Equal(PositionFit.Fit.Same, PositionFit.Classify(Position.SS, Position.SS));
        Assert.Equal(PositionFit.Fit.Close, PositionFit.Classify(Position.SS, Position.FS));
        Assert.Equal(PositionFit.Fit.Medium, PositionFit.Classify(Position.CB, Position.FS));
        Assert.Equal(PositionFit.Fit.Far, PositionFit.Classify(Position.MLB, Position.CB));
        Assert.Equal(PositionFit.Fit.Extreme, PositionFit.Classify(Position.DT, Position.WR));
        Assert.True(PositionFit.Multiplier(Position.SS, Position.FS) > PositionFit.Multiplier(Position.MLB, Position.CB));
    }

    [Fact]
    public void OutOfPositionLinebackerNoLongerOutranksNaturalSafety()
    {
        var linebacker = Player(Position.MLB, 88);
        var safety = Player(Position.SS, 84);
        Assert.True(DepthChartService.PositionalRating(safety, 18) > DepthChartService.PositionalRating(linebacker, 18));
    }

    [Fact]
    public void ConversionDropsAwarenessAndOverall()
    {
        var receiver = Player(Position.WR, 85);
        var converted = PlayerPositionConversion.Convert(receiver, Position.CB);
        Assert.Equal(Position.CB, converted.Position);
        Assert.True(converted.AwarenessRating < receiver.AwarenessRating);
        Assert.True(converted.OverallRating < receiver.OverallRating);
    }
}