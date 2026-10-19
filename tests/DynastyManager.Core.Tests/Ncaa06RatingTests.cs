using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class Ncaa06RatingTests
{
    [Fact]
    public void GeneratedTalentTargetsAreCalibratedOnTheSamePositionWeightedScale()
    {
        var state = new DynastyState { DynastyName = "Talent", UserTeamName = "Home" };
        foreach (var position in Enum.GetValues<Position>())
        {
            var player = PlayerRatingService.EnsureInitialized(state, new DynastyPlayer
            {
                PlayerId = Guid.NewGuid(), FullName = "Generated Recruit", TeamName = "Home",
                Position = position, OverallRating = 84, TalentLevel = 7, ClassYear = 1
            });
            Assert.InRange(player.OverallRating, 83, 85);
            Assert.Equal(player.OverallRating, Ncaa06RatingService.CalculateOverall(player));
        }
    }
    private static DynastyPlayer Player(int position, int rating) => Ncaa06RosterRatings.Apply(
        new DynastyPlayer { FullName = "NEXT Player", TeamName = "Home", Position = Position.QB },
        Ncaa06RosterRatings.RatingFields.ToDictionary(f => f, _ => rating)
            .Append(new KeyValuePair<string, int>("PPOS", position)).ToDictionary(p => p.Key, p => p.Value));

    [Theory]
    [InlineData(0, 67, 50)]
    [InlineData(0, 91, 99)]
    [InlineData(0, 43, 40)]
    [InlineData(7, 68, 50)]
    [InlineData(19, 96, 99)]
    [InlineData(20, 50, 40)]
    public void PociEndpointsAndMidpointsMatchTheWorkbook(int position, int value, int expected)
        => Assert.Equal(expected, Player(position, value).OverallRating);

    [Fact]
    public void QuarterbackWeightsMatchAnIndependentWorkedExample()
    {
        var player = Player(0, 67);
        player = player with { Skills = player.Skills! with { PassingAccuracy = 87, PassingPower = 77 } };
        // QB: THA 33%, THP 27%; center=67; spread=48; 50+(20*.33+10*.27)*98/48=68.9875.
        Assert.Equal(69, Ncaa06RatingService.CalculateOverall(player));
    }

    [Fact]
    public void RatingFieldsRoundTripWithoutLosingDetailedPositionOrCharacter()
    {
        for (var position = 0; position <= 20; position++)
        {
            var original = Player(position, 78);
            original = original with { Skills = original.Skills! with { Leadership = 83, Discipline = 71 } };
            var loaded = Ncaa06RosterRatings.Apply(original, Ncaa06RosterRatings.Export(original));
            Assert.Equal(original, loaded);
            Assert.Equal(position, loaded.NcaaPositionCode);
            Assert.Equal(loaded, PlayerRatingService.EnsureInitialized(new DynastyState
                { DynastyName = "NEXT", UserTeamName = "Home" }, loaded));
        }
    }

    [Fact]
    public void CompressedRatingsUseTheProvidedLookupInsteadOfAssumingDisplayValues()
    {
        var fields = Ncaa06RosterRatings.RatingFields.ToDictionary(f => f, _ => 0);
        fields["PPOS"] = 0;
        var player = Ncaa06RosterRatings.Apply(Player(0, 80), fields, new[] { 67 });
        Assert.Equal(50, player.OverallRating);
        Assert.Equal(67, player.Skills!.PassingAccuracy);
        fields.Remove("PSPD");
        Assert.Throws<ArgumentException>(() => Ncaa06RosterRatings.Apply(player, fields));
    }

    [Fact]
    public void OnlyTheRequestedNineteenRatingsRemain()
    {
        Assert.Equal(14, typeof(PlayerSkillRatings).GetProperties().Length);
        var names = typeof(DynastyPlayer).GetProperties().Select(p => p.Name);
        Assert.Contains("InjuryRating", names);
        Assert.DoesNotContain("TechniqueRating", names);
        Assert.DoesNotContain("DurabilityRating", names);
    }
}
