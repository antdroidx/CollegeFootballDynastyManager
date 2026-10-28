using DynastyManager.Core.Models;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public class DraftCapAppearanceTests
{
    static ProFranchise DraftState()
    {
        var s=ProLeagueService.Create("Seattle Seahawks") with{Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.Draft};
        return ProDraft.Generate(s);
    }
    [Fact] public void OverCapCpuCannotStallDraftOrPlayUnsignedRookie()
    {
        var s=DraftState() with{TeamCapAllocations=[],SalaryCap=0};var pick=ProDraft.Current(s)!;var id=s.Prospects.First().Football.PlayerId;
        var next=ProDraft.Pick(s,id);
        Assert.Equal(1,next.DraftIndex);Assert.DoesNotContain(next.Prospects,p=>p.Football.PlayerId==id);
        Assert.Contains(next.DraftHistory,h=>h.Player.Football.PlayerId==id);
        Assert.Contains(next.UnsignedRookies,r=>r.PlayerId==id&&r.Team==pick.Owner);
        Assert.True(next.Players.Single(p=>p.Football.PlayerId==id).Reserve);
        Assert.Equal(ProEconomy.Used(s,pick.Owner),ProEconomy.Used(next,pick.Owner));
        Assert.Contains("unsigned",ProSeasonEligibility.Issue(next,pick.Owner));
    }
    [Fact] public void PendingRookieSignsFourYearsAfterCapRepairWithoutDuplication()
    {
        var s=DraftState() with{TeamCapAllocations=[],SalaryCap=0};var id=s.Prospects.First().Football.PlayerId;
        s=ProDraft.Pick(s,id) with{TeamCapAllocations=[],SalaryCap=1_000_000_000};s=ProDraft.SignRookies(s);
        Assert.Empty(s.UnsignedRookies);var p=Assert.Single(s.Players,p=>p.Football.PlayerId==id);Assert.False(p.Reserve);
        var c=Assert.Single(s.Contracts,c=>c.PlayerId==id&&c.Active);Assert.Equal(4,c.Years.Count);
        Assert.Equal(s,ProDraft.SignRookies(s));
    }
    [Fact] public void EveryCpuPickCanCompleteWithNoCapRoom()
    {
        var s=DraftState() with{TeamCapAllocations=[],SalaryCap=0};s=ProDraft.AutoUntilUser(s,true);
        Assert.Null(ProDraft.Current(s));Assert.Equal(s.Picks.Count(p=>p.Year==s.Year),s.DraftIndex);Assert.Equal(s.DraftIndex,s.UnsignedRookies.Count);
        Assert.Equal(s.DraftIndex,s.DraftHistory.Count);Assert.Equal(s.DraftIndex,s.UnsignedRookies.Select(r=>r.PlayerId).Distinct().Count());
    }
    [Fact] public void ExplicitAppearanceUsesAllSevenConsistentSkinColors()
    {
        var id=Guid.NewGuid();var colors=new HashSet<string>();
        for(var tone=0;tone<7;tone++)
        {
            var face=PortraitAppearance.ForPlayer(id,new NcaaPlayerAppearance{SkinTone=tone});
            Assert.Equal(tone,face.Skin);colors.Add(PortraitAppearance.SkinColor(face.Skin));
        }
        Assert.Equal(7,colors.Count);
    }
}
