using DynastyManager.Core.Models;
using Xunit;
namespace ProFootball.Tests;
public class CoachPortraitTests
{
    [Fact]public void EveryNamedSnapshotCoachHasAReviewedOfflinePortrait()
    {
        var assembly=typeof(ProFootball.Core.ProCoachPortraits).Assembly;
        using var stream=assembly.GetManifestResourceStream(assembly.GetManifestResourceNames().Single(n=>n.EndsWith("nfl-snapshot.json")))!;
        using var snapshot=System.Text.Json.JsonDocument.Parse(stream);
        foreach(var coach in snapshot.RootElement.GetProperty("Coaches").EnumerateArray()){var name=coach.GetProperty("Name").GetString()!;if(name!="Vacant")Assert.NotNull(ProFootball.Core.ProCoachPortraits.Find(name));}
        Assert.Equal(127,ProFootball.Core.ProCoachPortraits.All.Count);
        foreach(var portrait in ProFootball.Core.ProCoachPortraits.All){Assert.StartsWith("https://",portrait.Source);Assert.StartsWith("https://static.clubs.nfl.com/",portrait.ImageUrl);Assert.InRange(portrait.Appearance.SkinTone!.Value,0,6);Assert.InRange(portrait.Appearance.HairColor!.Value,0,5);Assert.InRange(portrait.Appearance.HairStyle!.Value,0,14);Assert.InRange(portrait.Appearance.FacialHair!.Value,0,3);}
        Assert.NotNull(ProFootball.Core.ProCoachPortraits.Find("Mike Macdonald"));Assert.NotNull(ProFootball.Core.ProCoachPortraits.Find("Chris O’Leary"));Assert.Equal(ProFootball.Core.ProCoachPortraits.Find("Chris O’Leary"),ProFootball.Core.ProCoachPortraits.Find("Chris O�Leary"));
    }
    [Fact]public void FamiliarFeaturesAreExplicitAndUnknownFacesAreNotInventedAsReal()
    {
        var andy=ProFootball.Core.ProCoachPortraits.Find("Andy Reid")!;Assert.True(andy.Glasses);Assert.True(andy.Moustache);
        Assert.True(ProFootball.Core.ProCoachPortraits.Find("Mike McDaniel")!.Glasses);Assert.False(ProFootball.Core.ProCoachPortraits.Find("Mike Macdonald")!.Glasses);
        Assert.Null(ProFootball.Core.ProCoachPortraits.Find("Vacant"));Assert.Null(ProFootball.Core.ProCoachPortraits.Find("Fictional New Coach"));
    }
}
