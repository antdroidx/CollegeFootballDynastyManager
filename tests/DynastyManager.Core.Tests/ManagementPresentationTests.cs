using DynastyManager.Data.Branding;
using DynastyManager.Data.Presentation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class ManagementPresentationTests
{
    [Theory]
    [InlineData("Georgia Tech")]
    [InlineData("Washington")]
    [InlineData("Air Force")]
    [InlineData("Michigan")]
    [InlineData("Tennessee")]
    [InlineData("LSU")]
    public void SchoolAccentsRemainReadableOnBothManagementThemes(string school)
    {
        var brand=TeamBrandCatalog.Get(school).Primary;
        foreach(var (selected, surface) in new[]{("#E7EEF6","#FFFFFF"),("#192E43","#122233")})
        {
            var accent=ManagementColors.ReadableAccent(brand,selected);
            Assert.True(ManagementColors.ContrastRatio(accent,selected)>=4.5);
            Assert.True(ManagementColors.ContrastRatio(accent,surface)>=4.5);
            Assert.Equal(accent,ManagementColors.ReadableAccent(brand,selected));
        }
    }

    [Fact]
    public void WhiteAndBlackHaveStandardContrastAndReadableBrandIsPreserved()
    {
        Assert.Equal(21,ManagementColors.ContrastRatio("#FFFFFF","#000000"),6);
        Assert.Equal("#250047",ManagementColors.ReadableAccent("#250047","#FFFFFF"));
        Assert.True(ManagementColors.ContrastRatio(ManagementColors.ReadableAccent("#FFD700","#FFFFFF"),"#FFFFFF")>=4.5);
    }
}
