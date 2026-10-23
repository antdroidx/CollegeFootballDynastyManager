using DynastyManager.Core.Models;
using System.Text.Json;
using Xunit;

namespace DynastyManager.Core.Tests;

public sealed class PortraitAppearanceTests
{
    [Fact]
    public void IdentitySurvivesSaveReloadAndSchoolChanges()
    {
        var player = new DynastyPlayer { PlayerId = Guid.Parse("7aa386b1-33f4-4bc7-9821-15787e5b9831"),
            FullName = "Test Player", TeamName = "Washington", Position = Position.QB };
        var reloaded = JsonSerializer.Deserialize<DynastyPlayer>(JsonSerializer.Serialize(player))!;
        var transferred = reloaded with { TeamName = "Michigan", OverallRating = 90 };
        Assert.Equal(PortraitAppearance.ForIdentity(player.PlayerId), PortraitAppearance.ForIdentity(transferred.PlayerId));
    }

    [Fact]
    public void AppearanceIsVariedAndWithinRendererPalettes()
    {
        var appearances = Enumerable.Range(0, 1000).Select(i =>
            PortraitAppearance.ForIdentity(new Guid(i, 0, 0, new byte[8]))).ToArray();
        Assert.True(appearances.Distinct().Count() > 900);
        Assert.All(appearances, face =>
        {
            Assert.InRange(face.Skin, 0, 6); Assert.InRange(face.HairColor, 0, 5);
            Assert.InRange(face.HairStyle, 0, 4); Assert.InRange(face.FaceShape, 0, 3);
            Assert.InRange(face.Brow, 0, 2); Assert.InRange(face.FacialHair, 0, 3); Assert.InRange(face.Detail, 0, 3);
        });
    }

    [Fact]
    public void FixedIdentityRetainsThePublishedAppearance()
    {
        Assert.Equal(new PortraitAppearance(1, 4, 3, 1, 1, 3, 0),
            PortraitAppearance.ForIdentity(Guid.Parse("7aa386b1-33f4-4bc7-9821-15787e5b9831")));
    }
}
