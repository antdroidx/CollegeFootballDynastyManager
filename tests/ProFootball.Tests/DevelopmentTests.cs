using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using ProFootball.Core;
using System.Text.Json;
using Xunit;
namespace ProFootball.Tests;

public sealed class DevelopmentTests
{
    static ProPlayer Player(Position position, int age) => ProPlayerRatingService.Refresh(new(new DynastyPlayer
    {
        PlayerId = Guid.NewGuid(), FullName = "Development test", TeamName = "Test", Position = position,
        OverallRating = 80, PotentialRating = 99, SeasonStartOverall = 80,
        SpeedRating = 80, AgilityRating = 80, StrengthRating = 80, AwarenessRating = 80, InjuryRating = 80,
        Skills = new() { PassingPower = 80, PassingAccuracy = 80, Catching = 80, BreakTackle = 80,
            Tackling = 80, RunBlocking = 80, PassBlocking = 80, BallCarrying = 80,
            Acceleration = 80, Leadership = 80, Discipline = 80, Jumping = 80, KickPower = 80, KickAccuracy = 80 }
    }, age, Math.Max(0, age - 22), false), resetBaseline: true);

    [Theory]
    [InlineData(Position.RB, 28)] [InlineData(Position.CB, 30)] [InlineData(Position.DE, 32)]
    [InlineData(Position.QB, 35)] [InlineData(Position.OL, 34)] [InlineData(Position.K, 37)] [InlineData(Position.P, 37)]
    public void PastPrimeVeteransLoseAbilityAndHeadroomWithoutChangingOverallFormula(Position position, int age)
    {
        var before = Player(position, age);
        var after = ProDevelopment.AgeOneYear(before, new Random(41));
        Assert.True(after.Football.OverallRating < before.Football.OverallRating);
        Assert.Equal(PlayerRatingService.CalculateOverall(after.Football), after.Football.OverallRating);
        Assert.Equal(after.Football.OverallRating, after.Football.PotentialRating);
        Assert.Equal("At ceiling", ProDevelopment.Growth(after));
        Assert.True(ProDevelopment.Delta(after) < 0);
        Assert.Equal(before.Football.OverallRating, after.PreviousSeasonOverall);
        Assert.Equal(after.Football.OverallRating, after.Football.SeasonStartOverall);
    }
    [Fact] public void PhysicalDeclineStartsBeforeTechniqueAndMentalDecline()
    {
        var before = Player(Position.QB, 31); var after = ProDevelopment.AgeOneYear(before, new Random(3));
        Assert.True(after.Football.SpeedRating < before.Football.SpeedRating);
        Assert.Equal(before.Football.Skills!.PassingAccuracy, after.Football.Skills!.PassingAccuracy);
        Assert.Equal(before.Football.AwarenessRating, after.Football.AwarenessRating);
        Assert.Equal(before.Football.Skills.Discipline, after.Football.Skills.Discipline);
    }
    [Fact] public void SameAgeRunningBackHasLessUpsideThanQuarterbackOrKicker()
    {
        var rb = Player(Position.RB, 28); var qb = Player(Position.QB, 28); var k = Player(Position.K, 28);
        Assert.Equal(0, ProDevelopment.Headroom(rb));
        Assert.True(ProDevelopment.Headroom(qb) > 0);
        Assert.True(ProDevelopment.Headroom(k) > ProDevelopment.Headroom(qb));
        Assert.True(ProDevelopment.Ceiling(rb) >= rb.Football.OverallRating);
    }
    [Fact] public void YoungDevelopmentCannotExceedItsCeilingAndAgingDoesNotRegrowVeteranUpside()
    {
        var young = Player(Position.WR, 21); var ceiling = ProDevelopment.Ceiling(young);
        var random = new Random(73);
        for (var i = 0; i < 16; i++)
        {
            young = ProDevelopment.AgeOneYear(young, random);
            Assert.InRange(young.Football.OverallRating, 1, ceiling);
            Assert.InRange(young.Football.PotentialRating, young.Football.OverallRating, ceiling);
            Assert.Equal(19, PlayerAttributeCatalog.Groups(young.Football).Sum(g => g.Attributes.Length));
        }
        Assert.Equal(0, ProDevelopment.Headroom(young));
    }
    [Fact] public void ExistingVeteranViewDoesNotRequireRewritingRatingsAndHistorySurvivesSerialization()
    {
        var old = Player(Position.RB, 33); var rating = old.Football.PotentialRating;
        Assert.Equal("At ceiling", ProDevelopment.Growth(old));
        Assert.Equal(rating, old.Football.PotentialRating);
        Assert.Equal(0, ProDevelopment.Delta(old));
        var changed = ProDevelopment.AgeOneYear(old, new Random(9));
        var loaded = JsonSerializer.Deserialize<ProPlayer>(JsonSerializer.Serialize(changed))!;
        Assert.Equal(ProDevelopment.Delta(changed), ProDevelopment.Delta(loaded));
        Assert.Equal(changed.PreviousSeasonOverall, loaded.PreviousSeasonOverall);
        Assert.Null(ProDevelopment.Delta(old with { Football = old.Football with { SeasonStartOverall = 0 } }));
    }
}
