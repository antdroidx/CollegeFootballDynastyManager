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
        Assert.Equal(1, next.Players.Single(p => p.Football.PlayerId == qb.Football.PlayerId).Football.DepthChartOrder);
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
        Assert.Equal(plan, Assert.Single(roundTrip.GamePlans));
    }
}
