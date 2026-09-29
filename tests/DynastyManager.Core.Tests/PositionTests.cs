using DynastyManager.Core.Models;

namespace DynastyManager.Core.Tests;

public class PositionTests
{
    [Theory]
    [InlineData(Position.DE)]
    [InlineData(Position.DT)]
    [InlineData(Position.OLB)]
    [InlineData(Position.MLB)]
    [InlineData(Position.FS)]
    [InlineData(Position.SS)]
    public void DetailedDefensivePositionsArePartOfTheNewModel(Position position)
    {
        Assert.True(Enum.IsDefined(position));
    }

    [Theory]
    [InlineData("DL")]
    [InlineData("LB")]
    [InlineData("S")]
    public void LegacyGenericDefensivePositionsAreNotNewRosterPositions(string legacyPosition)
    {
        Assert.False(Enum.TryParse<Position>(legacyPosition, out _));
    }

    [Fact]
    public void PlayerCanPlayPrimaryOrSecondaryPosition()
    {
        var player = new Player
        {
            FirstName = "Test",
            LastName = "Edge",
            PrimaryPosition = Position.DE,
            SecondaryPositions = new[] { Position.OLB }
        };

        Assert.True(player.CanPlay(Position.DE));
        Assert.True(player.CanPlay(Position.OLB));
        Assert.False(player.CanPlay(Position.DT));
    }
}
