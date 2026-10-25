using DynastyManager.Core.Models;
namespace ProFootball.Core;

public static class ProStaff
{
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
        if (s.Phase != ProPhase.Offseason) throw new InvalidOperationException("Staff hires open during the offseason.");
        var club = s.League.Teams.Single(t => t.Name == team); var candidate = s.CoachCandidates.SingleOrDefault(c => c.Name == name) ?? throw new InvalidOperationException("This coach is unavailable.");
        var coaches = s.Coaches.ToList(); var old = coaches.FirstOrDefault(c => c.TeamId == club.Id && c.Role == role); var pool = s.CoachCandidates.Where(c => c.Name != name).ToList();
        if (old is not null && !old.Vacant) pool.Add(new(old.Name, club.Id, old.Title, old.Source));
        coaches.RemoveAll(c => c.TeamId == club.Id && c.Role == role); coaches.Add(new(club.Id, role, name, role.ToString() + " · franchise hire", candidate.Source) { Rating = 65, Development = 65 });
        return ProEconomy.Log(s with { Coaches = coaches, CoachCandidates = pool }, $"{team} hired {name} as {role}.");
    }
    public static ProFranchise Fire(ProFranchise s, string team, StaffRole role)
    {
        if (s.Phase != ProPhase.Offseason) throw new InvalidOperationException("Staff changes open during the offseason.");
        var club = s.League.Teams.Single(t => t.Name == team); var old = s.Coaches.First(c => c.TeamId == club.Id && c.Role == role);
        if (old.Vacant) throw new InvalidOperationException("The role is already vacant.");
        return ProEconomy.Log(s with { Coaches = s.Coaches.Select(c => c == old ? c with { Name = "Vacant", Vacant = true, Title = "Vacant franchise role" } : c).ToList(), CoachCandidates = s.CoachCandidates.Append(new(old.Name, club.Id, old.Title, old.Source)).DistinctBy(c => c.Name).ToList() }, $"{team} dismissed {old.Name}.");
    }
}
