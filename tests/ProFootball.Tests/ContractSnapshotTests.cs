using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public sealed class ContractSnapshotTests
{
    [Fact] public void SourcedSalaryAndCapAreDistinctAndInDollars()
    {
        var s = ProLeagueService.Create("Cincinnati Bengals", sourcedContracts: true);
        var p = s.Players.Single(p => p.Football.FullName == "Joe Burrow");
        var c = s.Contracts.Single(c => c.PlayerId == p.Football.PlayerId);
        Assert.False(c.Estimated); Assert.Contains("OverTheCap", c.Source);
        Assert.Equal(12_250_000, c.Years.Single(y => y.Year == 2026).Salary);
        Assert.Equal(37_599_784, ProEconomy.CapHit(c, 2026));
        Assert.True(ProContractData.Load().Count > 1900);
    }
    [Fact] public void ImportIsOptInAndNeverOverwritesSavedFranchiseAutomatically()
    {
        var s = ProLeagueService.Create("Seattle Seahawks");
        Assert.All(s.Contracts, c => Assert.True(c.Estimated));
        var actual = ProContractData.Apply(s);
        Assert.NotEmpty(actual.Contracts.Where(c => c.Source != null));
        Assert.Equal(s.Id, actual.Id); Assert.Equal(s.Players, actual.Players);
        Assert.All(actual.Contracts, c => Assert.All(c.Years, y => Assert.True(ProEconomy.CapHit(c, y.Year) >= 0)));
    }
}
