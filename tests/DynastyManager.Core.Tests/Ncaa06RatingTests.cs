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
    [InlineData(0, 67, 49)]
    [InlineData(0, 92, 99)]
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
        // QB: center=67.5, spread=49; weighted rating=76.3; 50+(76.3-67.5)*98/49=67.6.
        Assert.Equal(68, Ncaa06RatingService.CalculateOverall(player));
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
        Assert.Equal(49, player.OverallRating);
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

    [Fact]
    public void EveryPositionMatchesLatestPociForVariedAttributes()
    {
        var lines = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "TestData", "NCAA06_POCI.csv"));
        var headers = lines[0].Split(',');
        string[] weights = ["CAWT", "KAWT", "TAWT", "PBWT", "RBWT", "ACWT", "AGWT", "TKWT", "INWT", "KPWT", "SPWT", "TPWT", "BTWT", "CTWT", "STWT", "JUWT", "AWWT"];
        foreach (var line in lines.Skip(1))
        {
            var cells = line.Split(',');
            var row = headers.Select((h, i) => (h, v: int.Parse(cells[i]))).ToDictionary(x => x.h, x => x.v);
            foreach (var seed in Enumerable.Range(0, 20))
            {
                var values = Ncaa06RosterRatings.RatingFields.Select((f, i) => (f, v: 40 + (seed * 13 + i * 7) % 60)).ToDictionary(x => x.f, x => x.v);
                values["PPOS"] = row["PPOS"];
                var player = Ncaa06RosterRatings.Apply(Player(row["PPOS"], 70), values);
                var weighted = Ncaa06RosterRatings.RatingFields.Select((f, i) => values[f] * row[weights[i]]).Sum() / 100d;
                var expected = Math.Clamp((int)Math.Round(50 + (weighted - (row["PLDH"] + row["PLDL"]) / 2d) * 98 / (row["PLDH"] - row["PLDL"]), MidpointRounding.ToEven), 40, 99);
                Assert.Equal(expected, player.OverallRating);
            }
        }
    }
}
