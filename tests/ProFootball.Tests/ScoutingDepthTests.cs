using DynastyManager.Core.Models;
using ProFootball.Core;
using Xunit;

namespace ProFootball.Tests;

public class ScoutingDepthTests
{
    static ProFranchise New() => ProLeagueService.Create("Seattle Seahawks");
    static ProFranchise Draft() => ProDraft.Generate(New() with{Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.FreeAgency});

    static ScoutReport Report(ProFranchise s,ProPlayer p,int grade,int confidence)=>new(p.Football.PlayerId,s.UserTeam,s.Year,grade,confidence);

    static ScoutTarget Target(ProFranchise s,ProPlayer p,ScoutDepth depth,ScoutSource source)=>new(s.UserTeam,p.Football.PlayerId,s.Year,depth,source);

    [Fact]
    public void EveryProspectReceivesABasicReportBelowFiftyPercent()
    {
        var s=Draft();
        var reports=s.Scouting.Where(r=>r.Team==s.UserTeam&&r.Year==s.Year).ToList();
        Assert.All(s.Prospects,p=>Assert.Contains(p.Football.PlayerId,reports.Select(r=>r.PlayerId)));
        var basic=reports.Where(r=>r.Depth==ScoutDepth.Basic).ToList();
        Assert.NotEmpty(basic);
        Assert.All(basic,r=>{
            Assert.InRange(r.Confidence,1,49);
            var actual=s.Prospects.Single(p=>p.Football.PlayerId==r.PlayerId).Football.OverallRating;
            var uncertainty=Math.Max(2,(100-r.Confidence)/4);
            Assert.InRange(r.Grade,Math.Max(40,actual-uncertainty),Math.Min(99,actual+uncertainty));
        });
    }

    [Fact]
    public void PriorityAndVisitSlotsStayWithinLimitsAndVisitReportsAreNestedInsidePriority()
    {
        var s=Draft();
        var targets=s.ScoutTargets.Where(t=>t.Team==s.UserTeam&&t.Year==s.Year).ToList();
        var deep=targets.Where(t=>t.Depth!=ScoutDepth.Basic).Select(t=>t.PlayerId).Distinct().ToList();
        var visit=targets.Where(t=>t.Depth==ScoutDepth.Visit).Select(t=>t.PlayerId).Distinct().ToList();
        Assert.NotEmpty(deep);
        Assert.NotEmpty(visit);
        Assert.True(deep.Count<=ProScouting.PriorityLimit,$"deep {deep.Count}");
        Assert.True(visit.Count<=ProScouting.VisitLimit,$"visit {visit.Count}");
        Assert.All(visit,v=>Assert.Equal(1,deep.Count(p=>p==v)));
        var reports=s.Scouting.Where(r=>r.Team==s.UserTeam&&r.Year==s.Year).ToList();
        Assert.All(deep,t=>{
            var report=reports.Single(r=>r.PlayerId==t);
            if(report.Depth==ScoutDepth.Visit)Assert.InRange(report.Confidence,75,95);
            else Assert.InRange(report.Confidence,50,75);
        });
        Assert.All(visit,t=>{
            var report=reports.Single(r=>r.PlayerId==t);
            Assert.InRange(report.Confidence,75,95);
        });
    }

    [Fact]
    public void StaffQualityInfluencesConfidenceInsideEachTier()
    {
        var s=Draft();
        var player=s.Prospects[0];
        var weak=s with{Coaches=s.Coaches.Select(c=>c with{Development=40}).ToList()};
        var strong=s with{Coaches=s.Coaches.Select(c=>c with{Development=95}).ToList()};
        Assert.InRange(ProScouting.Evaluate(weak,s.UserTeam,player,ScoutDepth.Basic).Confidence,1,49);
        Assert.InRange(ProScouting.Evaluate(strong,s.UserTeam,player,ScoutDepth.Basic).Confidence,1,49);
        Assert.InRange(ProScouting.Evaluate(weak,s.UserTeam,player,ScoutDepth.Priority).Confidence,50,75);
        Assert.InRange(ProScouting.Evaluate(strong,s.UserTeam,player,ScoutDepth.Priority).Confidence,50,75);
        Assert.InRange(ProScouting.Evaluate(weak,s.UserTeam,player,ScoutDepth.Visit).Confidence,75,95);
        Assert.InRange(ProScouting.Evaluate(strong,s.UserTeam,player,ScoutDepth.Visit).Confidence,75,95);
        Assert.True(ProScouting.Evaluate(strong,s.UserTeam,player,ScoutDepth.Priority).Confidence>ProScouting.Evaluate(weak,s.UserTeam,player,ScoutDepth.Priority).Confidence);
        Assert.True(ProScouting.Evaluate(strong,s.UserTeam,player,ScoutDepth.Basic).Confidence>ProScouting.Evaluate(weak,s.UserTeam,player,ScoutDepth.Basic).Confidence);
    }

    [Fact]
    public void CpuFillsUnusedSlotsWithoutReplacingUserChoices()
    {
        var s=Draft();
        var userProspect=s.Prospects.OrderByDescending(ProDraft.PublicGrade).ElementAt(200);
        s=ProScouting.SetTarget(s,s.UserTeam,userProspect.Football.PlayerId,ScoutDepth.Priority);
        var filled=ProDraft.AutoScout(s);
        var target=filled.ScoutTargets.Single(t=>t.Team==s.UserTeam&&t.Year==s.Year&&t.PlayerId==userProspect.Football.PlayerId);
        Assert.Equal(ScoutSource.User,target.Source);
        Assert.Equal(ScoutDepth.Priority,target.Depth);
        var same=filled.ScoutTargets.Where(t=>t.Team==s.UserTeam&&t.Year==s.Year&&t.Depth!=ScoutDepth.Basic).Select(t=>t.PlayerId).Distinct().ToList();
        Assert.Equal(ProScouting.PriorityLimit,same.Count);
        var userCount=filled.ScoutTargets.Count(t=>t.Team==s.UserTeam&&t.Year==s.Year&&t.Source==ScoutSource.User);
        Assert.Equal(1,userCount);
    }

    [Fact]
    public void ScoutingIsDeterministicAndIdempotent()
    {
        var s=Draft();
        var once=ProDraft.AutoScout(s);
        var twice=ProDraft.AutoScout(once);
        Assert.Equal(once.Scouting,twice.Scouting);
        Assert.Equal(once.ScoutTargets,twice.ScoutTargets);
        Assert.Equal(once.Scouting,s.Scouting);
    }

    [Fact]
    public void CpuPrioritySelectionIncludesProspectsOutsideTheTopPublicGrades()
    {
        var s=Draft();
        var targets=s.ScoutTargets.Where(t=>t.Team==s.UserTeam&&t.Year==s.Year&&t.Depth!=ScoutDepth.Basic).ToList();
        var threshold=s.Prospects.Select(ProDraft.PublicGrade).OrderByDescending(x=>x).ElementAt(60);
        var outside=targets.Count(t=>ProDraft.PublicGrade(s.Prospects.Single(p=>p.Football.PlayerId==t.PlayerId))<threshold);
        Assert.True(outside>0,$"outside={outside}");
        var needs=targets.Count(t=>ProCpu.Need(s,s.UserTeam,s.Prospects.Single(p=>p.Football.PlayerId==t.PlayerId).Football.Position)>0);
        Assert.True(needs>0,$"needs={needs}");
    }

    [Fact]
    public void LegacyScoutingMigratesWithoutDiscardingReportsOrUserBoardChoices()
    {
        var s=Draft() with{Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.FreeAgency};
        var p=s.Prospects[0];
        var legacy=new ScoutReport(p.Football.PlayerId,s.UserTeam,s.Year,80,90){Depth=ScoutDepth.Basic,Source=ScoutSource.Cpu};
        s=s with{Scouting=[legacy],DraftBoard=[p.Football.PlayerId],ScoutTargets=[]};
        var migrated=ProScouting.Migrate(s);
        Assert.Single(migrated.ScoutTargets);
        var target=migrated.ScoutTargets[0];
        Assert.Equal(p.Football.PlayerId,target.PlayerId);
        Assert.Equal(ScoutDepth.Visit,target.Depth);
        Assert.Equal(ScoutSource.User,target.Source);
        Assert.Equal(legacy,migrated.Scouting[0]);
    }

    [Fact]
    public async Task SaveAndReloadKeepTierSelections()
    {
        var s=Draft();
        var target=ProScouting.SetTarget(s,s.UserTeam,s.Prospects[2].Football.PlayerId,ScoutDepth.Visit);
        var dir=Path.Combine(Path.GetTempPath(),"scout-save-"+Guid.NewGuid());
        try
        {
            var store=new ProSaveStore(dir);
            await store.Save(target);
            var loaded=await store.Load(s.Id);
            Assert.Equal(target.ScoutTargets,loaded.ScoutTargets);
            Assert.Equal(target.Scouting.Count(r=>r.Team==s.UserTeam&&r.Year==s.Year),loaded.Scouting.Count(r=>r.Team==s.UserTeam&&r.Year==s.Year));
            Assert.DoesNotContain(loaded.Scouting,r=>r.Team==s.UserTeam&&r.Year==s.Year&&r.Depth==ScoutDepth.Basic&&r.Confidence>=50);
        }
        finally
        {
            if(Directory.Exists(dir))Directory.Delete(dir,true);
        }
    }

    [Fact]
    public void OldReportsArePreservedAndUpgradedOnlyWhenTargetsChange()
    {
        var s=Draft() with{Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.FreeAgency};
        var p=s.Prospects[0];
        var legacy=new ScoutReport(p.Football.PlayerId,s.UserTeam,s.Year,82,95){Depth=ScoutDepth.Basic,Source=ScoutSource.Cpu};
        s=s with{Scouting=[legacy],ScoutTargets=[Target(s,p,ScoutDepth.Visit,ScoutSource.User)]};
        var next=ProDraft.AutoScout(s);
        var report=next.Scouting.Single(r=>r.Team==s.UserTeam&&r.PlayerId==p.Football.PlayerId);
        Assert.Equal(legacy.Grade,report.Grade);
        Assert.Equal(ScoutDepth.Visit,report.Depth);
        Assert.Equal(ScoutSource.User,report.Source);
    }
}
