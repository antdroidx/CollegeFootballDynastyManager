using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public sealed class ReviewedRookieContractTests
{
    [Theory]
    [InlineData("Kenyon Sadiq",22_344_304L,4_062_601L)]
    [InlineData("Omar Cooper Jr.",17_474_196L,3_177_126L)]
    [InlineData("Rueben Bain Jr.",23_760_242L,4_320_044L)]
    public void FullyGuaranteedRookiesHaveFourSeasonsAndConservedPublishedCash(string name,long value,long openingCap)
    {
        var s=ProLeagueService.Create("Seattle Seahawks",sourcedContracts:true);var p=s.Players.Single(p=>p.Football.FullName==name);var c=s.Contracts.Single(c=>c.Active&&c.PlayerId==p.Football.PlayerId);
        Assert.False(c.Estimated);Assert.Equal(2026,c.OriginalRookieYear);Assert.Equal(2029,ProContractInsights.PlayingEnd(c));Assert.Equal(4,c.Years.Count);Assert.DoesNotContain(c.Years,y=>y.Year==2030);
        Assert.Equal(openingCap,ProEconomy.CapHit(c,2026));Assert.Equal(value,c.Years.Sum(y=>ProContractInsights.AnnualCash(c,y)));
        Assert.Equal(value,c.Years.Sum(y=>ProBonusAccounting.TotalGuarantee(s,c,y)));
        foreach(var y in c.Years){Assert.True(y.Salary>=ProFinance.Minimum(s,p,y.Year,practice:false));Assert.Equal(y.Salary,y.Guaranteed);Assert.Equal(y.OtherCash,y.GuaranteedOtherCash);Assert.Equal(ProEconomy.CapHit(c,y.Year),y.Salary+y.OtherCash+y.ProratedBonus);}
        Assert.Equal(value,c.BonusCash![2026]+c.Years.Sum(y=>y.Salary+(y.OtherCash??0)));Assert.All(c.BonusCash.Where(b=>b.Key>2026),b=>Assert.Equal(0,b.Value));
        Assert.Empty(s.RookieOptionDecisions.Where(d=>d.PlayerId==p.Football.PlayerId));
    }
}
