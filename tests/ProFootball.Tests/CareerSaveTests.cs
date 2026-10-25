using Xunit;
using ProFootball.Core;
using DynastyManager.Core.Models;
namespace ProFootball.Tests;
public class CareerSaveTests
{
    [Theory]
    [InlineData(StaffRole.HeadCoach, true, false)]
    [InlineData(StaffRole.HeadCoach, false, false)]
    [InlineData(StaffRole.OffensiveCoordinator, true, false)]
    [InlineData(StaffRole.OffensiveCoordinator, false, true)]
    [InlineData(StaffRole.DefensiveCoordinator, true, true)]
    [InlineData(StaffRole.DefensiveCoordinator, false, false)]
    public void OppositeUnitBelongsToCpuStaff(StaffRole role, bool hasBall, bool cpu)
        => Assert.Equal(cpu, ProCareer.CpuHandlesUnit(role, hasBall));
    [Fact] public void NewsRefreshIsIdempotentAndPreservesReadState()
    {
        var s = ProLeagueService.Create("Seattle Seahawks");
        s = ProNews.Refresh(s with { Transactions = ["Seattle Seahawks hired Test Coach."] });
        var story = Assert.Single(s.News);
        var next = ProNews.Refresh(s with { ReadNewsIds = [story.Id] });
        Assert.Single(next.News); Assert.Contains(story.Id, next.ReadNewsIds);
        Assert.Empty(next.News.Where(n => n.Headline.Contains("GM declined")));
    }
    [Fact] public void ThreeFutureDraftClassesPreserveTransferredPickOwnership()
    {
        var s = ProLeagueService.Create("Seattle Seahawks");
        Assert.Equal(672, s.Picks.Count);
        var pick = s.Picks.First(p => p.Year == s.Year + 1 && p.Owner == s.UserTeam);
        var other = s.League.Teams.First(t => t.Name != s.UserTeam).Name;
        s = s with { Picks = s.Picks.Select(p => p.Id == pick.Id ? p with { Owner = other } : p).ToList() };
        var next = ProDraft.Generate(s with { Year = s.Year + 1 });
        Assert.Equal(other, next.Picks.Single(p => p.Year == pick.Year && p.Round == pick.Round && p.OriginalTeam == pick.OriginalTeam).Owner);
        Assert.Equal(224, next.Picks.Count(p => p.Year == next.Year + 3));
        Assert.Equal(next.Picks.Count, next.Picks.Select(p => (p.Year, p.Round, p.OriginalTeam)).Distinct().Count());
    }
    [Fact] public void TradeValuesRewardPremiumPositionsAndDiscountFuturePicks()
    {
        Assert.True(ProTrade.PositionWeight(Position.QB) > ProTrade.PositionWeight(Position.DE));
        Assert.True(ProTrade.PositionWeight(Position.CB) > ProTrade.PositionWeight(Position.RB));
        var s = ProLeagueService.Create("Seattle Seahawks"); var p = s.Picks[0];
        Assert.True(ProTrade.PickValue(s, p with { Year = s.Year + 3 }) < ProTrade.PickValue(s, p with { Year = s.Year + 1 }));
    }
    [Fact] public async Task DeleteRemovesOnlyNamedSlotAndPreventsAutosaveResurrection()
    {
        var root = Path.Combine(Path.GetTempPath(), "pro-delete-" + Guid.NewGuid());
        try
        {
            var store = new ProSaveStore(root);
            var a = ProLeagueService.Create("Seattle Seahawks"); var b = a with { Id = Guid.NewGuid() };
            await store.Save(a); await store.Save(a); await store.Save(a); await store.Save(b);
            await File.WriteAllTextAsync(Path.Combine(root, a.Id + ".json.tmp"), "partial");
            await store.Delete(a.Id); await store.Delete(a.Id);
            Assert.Equal(new[] { b.Id }, store.Slots());
            foreach (var suffix in new[] { ".json", ".auto1.json", ".auto2.json", ".json.tmp" })
                Assert.False(File.Exists(Path.Combine(root, a.Id + suffix)));
            Assert.Equal(b.Id, (await store.Load(b.Id)).Id);
            await Assert.ThrowsAsync<InvalidOperationException>(() => store.Save(a));
            await Assert.ThrowsAsync<FileNotFoundException>(() => store.Load(a.Id));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
    [Fact] public void CoordinatorsCannotManageOppositeUnitAndGmRejectsUnaffordableRequests()
    {
        var s = ProLeagueService.Create("Seattle Seahawks", role: StaffRole.OffensiveCoordinator);
        var qb = s.Players.First(p => p.Football.Position == Position.QB);
        var cb = s.Players.First(p => p.Football.Position == Position.CB);
        ProCareer.RequirePlayer(s, qb.Football.PlayerId);
        Assert.Throws<InvalidOperationException>(() => ProCareer.RequirePlayer(s, cb.Football.PlayerId));
        var called = false;
        var next = ProManagement.Submit(s, "Request", [qb.Football.PlayerId], s.SalaryCap, x => { called = true; return x; });
        Assert.False(called); Assert.False(Assert.Single(next.GmDecisions).Approved);
        Assert.Throws<InvalidOperationException>(() => ProCareer.RequireHeadCoach(s));
    }
    [Fact] public void DismissalRespectsToggleAndJobMovePreservesLeagueAndHistory()
    {
        var s = ProLeagueService.Create("Seattle Seahawks", firingEnabled: true, coachName: "Test Coach");
        s = s with { Phase = ProPhase.Offseason, Career = s.Career! with { Security = 0 } };
        var safe = ProCareer.Review(s with { Career = s.Career! with { FiringEnabled = false } });
        Assert.False(safe.Career!.Unemployed);
        var fired = ProCareer.Review(s);
        Assert.True(fired.Career!.Unemployed); Assert.NotEmpty(fired.JobOffers);
        Assert.Contains(fired.Coaches, c => c.TeamId == "SEA" && c.Role == StaffRole.HeadCoach && c.Vacant);
        Assert.Single(ProCareer.Review(fired).CoachingHistory);
        var offer = fired.JobOffers[0]; var hired = ProCareer.Accept(fired, offer.Id);
        Assert.Equal(offer.Team, hired.UserTeam); Assert.Equal(offer.Role, hired.Career!.Role);
        Assert.False(hired.Career.Unemployed); Assert.Equal("Test Coach", hired.Career.Name);
        Assert.Equal(s.Players, hired.Players); Assert.Equal(fired.CoachingHistory, hired.CoachingHistory);
    }
}
