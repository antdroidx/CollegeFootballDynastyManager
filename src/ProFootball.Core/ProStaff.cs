using DynastyManager.Core.Models;
namespace ProFootball.Core;

public static class ProStaff
{
    public static ProFranchise DevelopSeason(ProFranchise s)
    {
        var standings=ProStandings.Build(s).ToDictionary(r=>r.Team.Id);
        return s with {Coaches=s.Coaches.Select(c=>{
            if(c.Vacant||c.LastDevelopmentYear>=s.Year||!standings.TryGetValue(c.TeamId,out var row)||row.Wins+row.Losses+row.Ties==0)return c;
            var target=s.Owners.FirstOrDefault(o=>o.Team==row.Team.Name)?.TargetWins??8;
            var change=Math.Clamp((row.Wins-target)/3,-1,2);
            var learners=s.Players.Where(p=>p.Football.TeamName==row.Team.Name&&!p.Retired&&p.Age<=26&&DynastyManager.Core.Simulation.UserCoachControlService.ControlsPosition(c.Role,p.Football.Position)).Where(p=>ProDevelopment.Delta(p)>0).Count();
            return c with {Rating=Math.Clamp(c.Rating+change,40,95),Development=Math.Clamp(c.Development+(learners>=3?1:0),40,95),LastDevelopmentYear=s.Year};
        }).ToList()};
    }
    public static ProFranchise ManageCpu(ProFranchise s)
    {
        foreach (var team in s.League.Teams.Where(t => t.Name != s.UserTeam || s.Career?.Unemployed == true))
        {
            var owner = s.Owners.FirstOrDefault(o => o.Team == team.Name);
            if (owner?.Fired == true)
            {
                var hc = s.Coaches.FirstOrDefault(c => c.TeamId == team.Id && c.Role == StaffRole.HeadCoach);
                if (hc is not null && !hc.Vacant) s = Fire(s, team.Name, StaffRole.HeadCoach);
                s = s with { Owners = s.Owners.Select(o => o.Team == team.Name ? o with { Security = 60, Fired = false } : o).ToList() };
            }
            foreach (var role in new[] { StaffRole.HeadCoach, StaffRole.OffensiveCoordinator, StaffRole.DefensiveCoordinator, StaffRole.SpecialTeamsCoordinator })
            {
                var current = s.Coaches.FirstOrDefault(c => c.TeamId == team.Id && c.Role == role);
                if (current is not null && !current.Vacant) continue;
                // A source-declared DC vacancy can represent the head coach calling defense.
                if (current?.Title.Contains("calls defense") == true) continue;
                var candidate = s.CoachCandidates.OrderBy(c => c.Name).FirstOrDefault(); if (candidate is null) break;
                s = Hire(s, team.Name, role, candidate.Name);
            }
        }
        return s;
    }
    public static ProFranchise Hire(ProFranchise s, string team, StaffRole role, string name)
    {
        var club = s.League.Teams.Single(t => t.Name == team); var candidate = s.CoachCandidates.FirstOrDefault(c => c.Name == name) ?? throw new InvalidOperationException("This coach is unavailable.");
        var coaches = s.Coaches.ToList(); var old = coaches.FirstOrDefault(c => c.TeamId == club.Id && c.Role == role); var pool = s.CoachCandidates.Where(c => c.Name != name).ToList();
        if (old is not null && !old.Vacant) pool.Add(new(old.Name, club.Id, old.Title, old.Source));
        coaches.RemoveAll(c => c.TeamId == club.Id && c.Role == role); coaches.Add(new(club.Id, role, name, role.ToString() + " · franchise hire", candidate.Source) { Rating = 65, Development = 65 });
        return ProEconomy.Log(s with { Coaches = coaches, CoachCandidates = pool }, $"{team} hired {name} as {role}.");
    }
    public static ProFranchise Fire(ProFranchise s, string team, StaffRole role)
    {
        var club = s.League.Teams.Single(t => t.Name == team); var old = s.Coaches.First(c => c.TeamId == club.Id && c.Role == role);
        if (old.Vacant) throw new InvalidOperationException("The role is already vacant.");
        return ProEconomy.Log(s with { Coaches = s.Coaches.Select(c => c == old ? c with { Name = "Vacant", Vacant = true, Title = "Vacant franchise role" } : c).ToList(), CoachCandidates = s.CoachCandidates.Append(new(old.Name, club.Id, old.Title, old.Source)).DistinctBy(c => c.Name).ToList() }, $"{team} dismissed {old.Name}.");
    }
}
