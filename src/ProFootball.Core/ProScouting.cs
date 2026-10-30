using DynastyManager.Core.Models;
namespace ProFootball.Core;
/// <summary>Automatic staff evaluations; combine reports narrow uncertainty without revealing exact ability.</summary>
public static class ProScouting
{
    public static ScoutReport Evaluate(ProFranchise s,string team,ProPlayer player)
    {
        var club=s.League.Teams.Single(t=>t.Name==team);
        var role=player.Football.Position is Position.QB or Position.RB or Position.FB or Position.WR or Position.TE or Position.OL?StaffRole.OffensiveCoordinator:player.Football.Position==Position.K?StaffRole.SpecialTeamsCoordinator:StaffRole.DefensiveCoordinator;
        var staff=s.Coaches.Where(c=>c.TeamId==club.Id&&!c.Vacant&&(c.Role==role||c.Role==StaffRole.HeadCoach)).Select(c=>c.Development).DefaultIfEmpty(65).Average();
        var combine=s.Phase==ProPhase.Offseason&&s.OffseasonStep==OffseasonStep.Draft;
        var confidence=Math.Clamp(65+(int)((staff-50)/2)+(combine?10:0),60,95);
        var error=Math.Max(2,(100-confidence)/5);
        var random=new Random(BitConverter.ToInt32(player.Football.PlayerId.ToByteArray(),0)^s.Year);
        var grade=Math.Clamp(player.Football.OverallRating+random.Next(-error,error+1),40,99);
        return new(player.Football.PlayerId,team,s.Year,grade,confidence);
    }
    public static ProFranchise UpdateReports(ProFranchise s)
    {
        var prospects=s.Prospects.ToDictionary(p=>p.Football.PlayerId);
        return s with {Scouting=s.Scouting.Select(r=>{
            if(r.Year!=s.Year||!prospects.TryGetValue(r.PlayerId,out var p))return r;
            var report=Evaluate(s,r.Team,p);return report.Confidence>r.Confidence?report:r;
        }).ToList()};
    }
}
