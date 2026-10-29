using ProFootball.Core;
using DynastyManager.Core.Models;
using Xunit;
namespace ProFootball.Tests;
public sealed class DepthChartTests
{
    [Fact] public void StarterChangeReordersOnlySelectedPositionAndPreservesContracts()
    {
        var s = ProLeagueService.Create("Seattle Seahawks");
        var qb = s.Players.Where(p => p.Football.TeamName == s.UserTeam && p.Football.Position == Position.QB && !p.Reserve && !p.PracticeSquad).OrderByDescending(p => p.Football.DepthChartOrder).First();
        var next = ProDepthChart.MakeStarter(s, s.UserTeam, qb.Football.PlayerId);
        Assert.Equal(qb.Football.PlayerId, ProDepthChart.AtPosition(next, s.UserTeam, 0)[0].Football.PlayerId);
        Assert.Equal(s.Players, next.Players);
        Assert.Equal(s.Contracts, next.Contracts);
        Assert.Equal(s.Players.Where(p => p.Football.TeamName != s.UserTeam || p.Football.Position != Position.QB), next.Players.Where(p => p.Football.TeamName != s.UserTeam || p.Football.Position != Position.QB));
        var unavailable = s.Players.First(p => p.PracticeSquad);
        Assert.Throws<InvalidOperationException>(() => ProDepthChart.MakeStarter(s, s.UserTeam, unavailable.Football.PlayerId));
    }
    [Fact] public void PersistedGamePlanFeedsSharedSimulationProfile()
    {
        var s = ProLeagueService.Create("Seattle Seahawks");
        var plan = new TeamGamePlan { TeamName = s.UserTeam, Offense = OffensivePlaybook.PowerRun, Defense = DefensivePlaybook.ThreeFour, Tempo = OffensiveTempo.ChewClock };
        s = s with { GamePlans = [plan] };
        Assert.Equal(plan, ProLeagueService.Profile(s, s.UserTeam).GamePlan);
        var roundTrip = System.Text.Json.JsonSerializer.Deserialize<ProFranchise>(System.Text.Json.JsonSerializer.Serialize(s))!;
        var restored = Assert.Single(roundTrip.GamePlans);
        Assert.Equal(plan.TeamName, restored.TeamName); Assert.Equal(plan.Offense, restored.Offense); Assert.Equal(plan.Defense, restored.Defense); Assert.Equal(plan.Tempo, restored.Tempo);
    }
    [Fact] public void ReorderBothDirectionsIsContiguousAndPreservesNaturalPlayers()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");var qbs=ProDepthChart.AtPosition(s,s.UserTeam,0);var id=qbs[0].Football.PlayerId;
        var next=ProDepthChart.Assign(s,s.UserTeam,id,0,qbs.Count);
        Assert.Equal(id,ProDepthChart.AtPosition(next,s.UserTeam,0)[^1].Football.PlayerId);
        next=ProDepthChart.Assign(next,s.UserTeam,id,0,1);
        Assert.Equal(id,ProDepthChart.AtPosition(next,s.UserTeam,0)[0].Football.PlayerId);
        Assert.Equal(Enumerable.Range(1,qbs.Count),next.DepthChartAssignments.Where(a=>a.PositionCode==0).Select(a=>a.Order).Order());
        Assert.Equal(s.Players,next.Players);Assert.Equal(s.Contracts,next.Contracts);
    }
    [Fact] public void OutOfPositionPersistsAndFeedsGameRosterWithoutChangingNaturalPlayer()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");var wr=ProDepthChart.AtPosition(s,s.UserTeam,3)[0];
        var next=ProDepthChart.Assign(s,s.UserTeam,wr.Football.PlayerId,4,1);
        Assert.DoesNotContain(ProDepthChart.AtPosition(next,s.UserTeam,3),p=>p.Football.PlayerId==wr.Football.PlayerId);
        Assert.Equal(wr,ProDepthChart.AtPosition(next,s.UserTeam,4)[0]);
        var restored=System.Text.Json.JsonSerializer.Deserialize<ProFranchise>(System.Text.Json.JsonSerializer.Serialize(next))!;
        Assert.Equal(4,ProDepthChart.AssignedCode(restored,wr));
        var game=restored.Games.First(g=>g.Football.InvolvesTeam(s.UserTeam));
        var session=ProLeagueService.Start(restored,game,GameDayMode.Play);
        var projected=Assert.Single(session.Roster,p=>p.PlayerId==wr.Football.PlayerId);
        Assert.Equal(Position.TE,projected.Position);Assert.Equal(4,projected.PlayingPositionCode);
        Assert.Equal(DynastyManager.Core.Simulation.DepthChartService.PositionalRating(wr.Football,4),projected.OverallRating);
        Assert.Equal(s.Players,next.Players);Assert.Equal(s.Contracts,next.Contracts);
    }
    [Fact] public void AssignRejectsUnavailableOwnershipAndCoordinatorCrossUnit()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");var wr=ProDepthChart.AtPosition(s,s.UserTeam,3)[0];
        var oc=s with{Career=s.Career! with{Role=StaffRole.OffensiveCoordinator}};
        Assert.Throws<InvalidOperationException>(()=>ProDepthChart.Assign(oc,s.UserTeam,wr.Football.PlayerId,16,1));
        Assert.Equal(4,ProDepthChart.AssignedCode(ProDepthChart.Assign(oc,s.UserTeam,wr.Football.PlayerId,4,1),wr));
        foreach(var p in s.Players.Where(p=>p.Football.TeamName!=s.UserTeam||p.PracticeSquad||p.Reserve).Take(10))
            Assert.Throws<InvalidOperationException>(()=>ProDepthChart.Assign(s,s.UserTeam,p.Football.PlayerId,4,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>ProDepthChart.Assign(s,s.UserTeam,wr.Football.PlayerId,21,1));
    }
    [Fact] public void OldSaveDefaultsToNaturalDepthAndInactiveAssignmentsDoNotEnterGame()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");var wr=ProDepthChart.AtPosition(s,s.UserTeam,3)[0];
        var json=System.Text.Json.Nodes.JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(s))!;json.AsObject().Remove("DepthChartAssignments");
        Assert.Empty(System.Text.Json.JsonSerializer.Deserialize<ProFranchise>(json.ToJsonString())!.DepthChartAssignments);
        s=ProDepthChart.Assign(s,s.UserTeam,wr.Football.PlayerId,4,1);
        s=s with{Players=s.Players.Select(p=>p==wr?p with{PracticeSquad=true}:p).ToList()};
        var game=s.Games.First(g=>g.Football.InvolvesTeam(s.UserTeam));
        Assert.DoesNotContain(ProLeagueService.Start(s,game,GameDayMode.Play).Roster,p=>p.PlayerId==wr.Football.PlayerId);
    }
    [Fact] public async Task ExportDepthFixtureThroughProductionSaveLoader()
    {
        var s=ProLeagueService.Create("Seattle Seahawks") with{Name="Synthetic depth editor QA",Id=Guid.Parse("de980000-0000-0000-0000-000000000001")};
        var dir=Path.Combine(Path.GetTempPath(),"depth-editor-"+Guid.NewGuid());
        try{var store=new ProSaveStore(dir);await store.Save(s);var restored=await store.Load(s.Id);Assert.Equal(32,restored.League.Teams.Count);Assert.True(ProDepthChart.AtPosition(restored,s.UserTeam,0).Count>=3);}
        finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
        if(Environment.GetEnvironmentVariable("PRO_COORDINATOR_UI_FIXTURES") is string output)
        {
            using var file=File.Create(Path.Combine(output,"pro-staff-depth-ui-fixture.json.gz"));using var gzip=new System.IO.Compression.GZipStream(file,System.IO.Compression.CompressionLevel.SmallestSize);
            System.Text.Json.JsonSerializer.Serialize(gzip,s,new System.Text.Json.JsonSerializerOptions{PropertyNamingPolicy=System.Text.Json.JsonNamingPolicy.CamelCase});
        }
    }

}
