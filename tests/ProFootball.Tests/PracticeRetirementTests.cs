using System.Text.Json;
using DynastyManager.Core.Models;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public class PracticeRetirementTests
{
    [Fact] public async Task OldSaveReleasesPracticePlayersPreservesIrAndNegotiatedActiveContractsOnce()
    {
        var s=ProLeagueService.Create("Seattle Seahawks",preserveLegacyPracticeForAudit:true);
        var ps=s.Players.First(p=>p.PracticeSquad&&!p.Reserve);
        s=s with {Contracts=s.Contracts.Where(c=>c.PlayerId!=ps.Football.PlayerId).Append(new(ps.Football.PlayerId,ps.Football.TeamName,s.Year,2,200_000,[new(s.Year,1_000_000,500_000),new(s.Year+1,1_000_000,0)])).ToList()};
        s=s with {Contracts=s.Contracts.Select(c=>s.Players.Any(p=>p.Football.PlayerId==c.PlayerId&&!p.PracticeSquad)?c with {LastChangedYear=s.Year}:c).ToList()};
        var active=s.Contracts.Where(c=>s.Players.Any(p=>p.Football.PlayerId==c.PlayerId&&!p.PracticeSquad)).ToArray();
        var reserves=s.Players.Where(p=>p.Reserve).ToArray();var before=s.DeadCap.ToArray();
        var directory=Path.Combine(Path.GetTempPath(),"no-ps-"+Guid.NewGuid());
        try {
            var store=new ProSaveStore(directory);await store.Save(s);var migrated=await store.Load(s.Id);
            Assert.True(migrated.PracticeSquadRemoved);Assert.Equal(0,migrated.League.PracticeSquadLimit);
            Assert.DoesNotContain(migrated.Players,p=>p.PracticeSquad);
            Assert.All(s.Players.Where(p=>p.PracticeSquad&&!p.Reserve&&!p.Retired),p=>Assert.Equal("Free agent",migrated.Players.Single(x=>x.Football.PlayerId==p.Football.PlayerId).Football.TeamName));
            Assert.Equal("FA",migrated.Players.Single(x=>x.Football.PlayerId==ps.Football.PlayerId).RosterStatus);
            Assert.Equal(reserves,migrated.Players.Where(p=>p.Reserve));
            Assert.All(active,c=>Assert.Equal(JsonSerializer.Serialize(c),JsonSerializer.Serialize(migrated.Contracts.Single(x=>x.PlayerId==c.PlayerId&&x.Team==c.Team&&x.Active==c.Active&&x.SignedYear==c.SignedYear))));Assert.All(before,d=>Assert.Contains(d,migrated.DeadCap));
            Assert.Contains(migrated.DeadCap,d=>d.PlayerId==ps.Football.PlayerId&&d.Amount>0);
            await store.Save(migrated);var twice=await store.Load(s.Id);Assert.Equal(JsonSerializer.Serialize(migrated),JsonSerializer.Serialize(twice));
        } finally {if(Directory.Exists(directory))Directory.Delete(directory,true);}
    }
    [Fact] public void NewWorldsAndCpuCutsCannotCreatePracticeSquads()
    {
        var s=ProLeagueService.Create("Seattle Seahawks",generatedPlayers:true);
        Assert.DoesNotContain(s.Players,p=>p.PracticeSquad);Assert.All(s.League.Teams,t=>Assert.Equal(53,ProRoster.Active(s,t.Name)));
        var p=s.Players.First(p=>p.Football.TeamName==s.UserTeam);
        Assert.Throws<InvalidOperationException>(()=>ProRoster.Practice(s,s.UserTeam,p.Football.PlayerId,true));
        var next=ProCpuRoster.RetainCut(s,s.UserTeam,p.Football.PlayerId);
        Assert.Equal("Free agent",next.Players.Single(x=>x.Football.PlayerId==p.Football.PlayerId).Football.TeamName);
        Assert.DoesNotContain(ProCpuRoster.FillPractice(next,s.UserTeam).Players,p=>p.PracticeSquad);
        Assert.Equal(0,ProCpu.Need(s,s.UserTeam,Position.FB));
    }
}
