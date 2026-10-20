using System.Text.Json;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Import;
using Xunit;

namespace DynastyManager.Core.Tests;

public sealed class PlayerBioImportTests
{
    private static string Csv(string yearColumn = "PYER", string extraHeader = "", string extraValues = "", int year = 0) =>
        $"TGID,PFNA,PLNA,{yearColumn},PPOS," + string.Join(',', Ncaa06RosterRatings.RatingFields) + extraHeader + "\n" +
        $"Washington,Test,Player,{year},0," + string.Join(',', Enumerable.Repeat("80", 17)) + extraValues;

    [Theory]
    [InlineData("PYER", 0, 1)] [InlineData("PYER", 4, 5)] [InlineData("PYEA", 2, 3)]
    public void CanonicalClassYearAndLegacyAliasWork(string column, int year, int expected)
    {
        var preview = Ncaa06RosterCsvImporter.Preview(new StringReader(Csv(column, year: year)), ["Washington"]);
        Assert.True(preview.CanApply, string.Join(';', preview.Errors)); Assert.Equal(expected, preview.Players.Single().ClassYear);
    }

    [Fact]
    public void ExtraColumnsAreIgnoredButDuplicateYearAliasesAreRejected()
    {
        Assert.True(Ncaa06RosterCsvImporter.Preview(new StringReader(Csv(extraHeader: ",UNUSED,NOTES", extraValues: ",123,hello")), ["Washington"]).CanApply);
        Assert.False(Ncaa06RosterCsvImporter.Preview(new StringReader(Csv(extraHeader: ",PYEA", extraValues: ",0")), ["Washington"]).CanApply);
        Assert.Contains(Ncaa06RosterCsvImporter.Preview(new StringReader(Csv("YEAR")), ["Washington"]).Errors, e => e.Contains("PYER"));
    }

    [Fact]
    public void OptionalBioFieldsAreDecodedAndSurviveSchoolChangesAndSaveReload()
    {
        var preview = Ncaa06RosterCsvImporter.Preview(new StringReader(Csv(extraHeader: ",PHGT,PWGT,PSKI,PHCL,PHED,PFMP", extraValues: ",75,55,3,0,14,31")), ["Washington"]);
        Assert.True(preview.CanApply, string.Join(';', preview.Errors)); var player = preview.Players.Single();
        Assert.Equal(75, player.HeightInches); Assert.Equal(215, player.WeightPounds);
        Assert.Equal(new NcaaPlayerAppearance { SkinTone = 3, HairColor = 0, HairStyle = 14, Face = 31 }, player.Appearance);
        var loaded = JsonSerializer.Deserialize<DynastyPlayer>(JsonSerializer.Serialize(player with { TeamName = "Michigan" }))!;
        Assert.Equal(player.Appearance, loaded.Appearance); Assert.Equal(player.HeightInches, loaded.HeightInches); Assert.Equal(player.WeightPounds, loaded.WeightPounds);
        Assert.Equal(PortraitAppearance.ForPlayer(player.PlayerId, player.Appearance), PortraitAppearance.ForPlayer(loaded.PlayerId, loaded.Appearance));
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void OptionalFacialHairColumnWorksWithoutOtherAppearanceFields(int style)
    {
        var preview = Ncaa06RosterCsvImporter.Preview(new StringReader(Csv(extraHeader: ",FacialHair", extraValues: "," + style)), ["Washington"]);
        Assert.True(preview.CanApply, string.Join(';', preview.Errors));
        var player = preview.Players.Single();
        Assert.Equal(style, player.Appearance!.FacialHair);
        Assert.Equal(style, PortraitAppearance.ForPlayer(player.PlayerId, player.Appearance).FacialHair);
    }

    [Theory]
    [InlineData(0, false, false)] [InlineData(1, true, true)] [InlineData(2, false, true)]
    public void MenuImporterUsesNcaaRedshirtStates(int code, bool current, bool previous)
    {
        var preview = Ncaa06RosterCsvImporter.Preview(new StringReader(Csv(extraHeader: ",PRSD", extraValues: "," + code)), ["Washington"]);
        Assert.True(preview.CanApply); var player = preview.Players.Single();
        Assert.Equal(current, player.IsRedshirted); Assert.Equal(previous, player.HasRedshirted);
    }

    [Theory]
    [InlineData("PHGT", "20")] [InlineData("PWGT", "300")] [InlineData("PSKI", "8")]
    [InlineData("PHCL", "6")] [InlineData("PHED", "15")] [InlineData("PFMP", "64")]
    [InlineData("FacialHair", "-1")] [InlineData("FacialHair", "4")]
    public void InvalidKnownOptionalFieldsFailPreview(string header, string value) =>
        Assert.False(Ncaa06RosterCsvImporter.Preview(new StringReader(Csv(extraHeader: "," + header, extraValues: "," + value)), ["Washington"]).CanApply);

    [Fact]
    public void ExplicitPoundsDoNotReceiveTheStoredWeightOffset()
    {
        var preview = Ncaa06RosterCsvImporter.Preview(new StringReader(Csv(extraHeader: ",HeightInches,WeightPounds", extraValues: ",74,205")), ["Washington"]);
        Assert.Equal(205, preview.Players.Single().WeightPounds); Assert.Equal(74, preview.Players.Single().HeightInches);
        Assert.False(Ncaa06RosterCsvImporter.Preview(new StringReader(Csv(extraHeader: ",PWGT,WeightPounds", extraValues: ",40,250")), ["Washington"]).CanApply);
    }

    [Theory]
    [InlineData(0, 4)] [InlineData(1, 5)] [InlineData(2, 3)] [InlineData(3, 6)] [InlineData(4, 0)]
    [InlineData(5, 7)] [InlineData(6, 8)] [InlineData(7, 9)] [InlineData(8, 4)] [InlineData(9, 4)]
    [InlineData(10, 8)] [InlineData(11, 0)] [InlineData(12, 7)] [InlineData(13, 10)] [InlineData(14, 11)]
    public void NcaaHairStylesMapToIllustratedStyles(int code, int expected) =>
        Assert.Equal(expected, PortraitAppearance.ForPlayer(Guid.NewGuid(), new NcaaPlayerAppearance { HairStyle = code }).HairStyle);

    [Fact]
    public void ImportedSkinAndHairOverrideIdentityAndGeneratedSizeIsStable()
    {
        var id = Guid.NewGuid(); var appearance = new NcaaPlayerAppearance { SkinTone = 7, HairColor = 5, HairStyle = 0, Face = 63 };
        var face = PortraitAppearance.ForPlayer(id, appearance);
        Assert.Equal(1, face.Skin); Assert.Equal(6, face.HairColor); Assert.Equal(4, face.HairStyle); Assert.Equal(3, face.FaceShape);
        Assert.Equal(PortraitAppearance.ForIdentity(id), PortraitAppearance.ForPlayer(id, null));
        var player = new DynastyPlayer { PlayerId = id, FullName = "Generated", TeamName = "Home", Position = Position.OL };
        var bio = PlayerPhysicalService.EnsureMeasurements(player);
        Assert.InRange(bio.HeightInches, 74, 80); Assert.InRange(bio.WeightPounds, 275, 335);
        Assert.Equal(bio, PlayerPhysicalService.EnsureMeasurements(bio with { TeamName = "Away" }) with { TeamName = "Home" });
        Assert.Equal("6′3″ · 215 lb", PlayerPhysicalService.Format(75, 215));
    }
}
