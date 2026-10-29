using System.Text.Json;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Import;
using Xunit;
namespace DynastyManager.Core.Tests;
public class FictionalRosterTests
{
    [Fact]public void NewWorldsDifferAndProgramStrengthsRemainWhileSavingPreservesPlayers()
    {
        var teams=new[]{new Team{Name="Strong",Abbreviation="S",ConferenceName="T",Prestige=90},new Team{Name="Weak",Abbreviation="W",ConferenceName="T",Prestige=35}};
        var template=LegacyDynastyRosterFactory.Create([],teams,Guid.NewGuid());var a=FictionalRosterService.College(template,teams);var b=FictionalRosterService.College(template,teams);
        Assert.Equal(template.Count,a.Count);Assert.Empty(a.Select(p=>p.PlayerId).Intersect(b.Select(p=>p.PlayerId)));Assert.NotEqual(a.Select(p=>p.FullName),b.Select(p=>p.FullName));
        double Gap(IEnumerable<DynastyPlayer> players)=>players.Where(p=>p.TeamName=="Strong").Average(p=>p.OverallRating)-players.Where(p=>p.TeamName=="Weak").Average(p=>p.OverallRating);
        Assert.True(Gap(a)>0);Assert.InRange(Math.Abs(Gap(a)-Gap(template)),0,2);
        Assert.All(a,p=>{Assert.Equal(PlayerRatingService.CalculateOverall(p),p.OverallRating);Assert.Null(p.CurrentInjury);});
        var s=new DynastyState{DynastyName="Generated",UserTeamName="Strong",ActiveRoster=a};var restored=JsonSerializer.Deserialize<DynastyState>(JsonSerializer.Serialize(s))!;
        Assert.Equal(JsonSerializer.Serialize(s.ActiveRoster),JsonSerializer.Serialize(restored.ActiveRoster));
    }
}
