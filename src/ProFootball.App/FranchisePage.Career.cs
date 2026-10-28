using DynastyManager.App;
using DynastyManager.Core.Models;
using ProFootball.Core;
namespace ProFootball.App;
public sealed partial class FranchisePage
{
    void Career()
    {
        var s = state!; var c = s.Career!;
        var team = s.League.Teams.Single(t => t.Name == s.UserTeam);
        var coach = s.Coaches.FirstOrDefault(x => x.TeamId == team.Id && x.Role == c.Role);
        if (coach is not null && !c.Unemployed) body.Add(CoachIdentity(coach, team));
        body.Add(Panel("Your career", Text(c.Name, 24, true), Text(RoleName(c.Role) + " · " + (c.Unemployed ? "Seeking employment" : s.UserTeam)), Text($"Reputation {c.Reputation}/100 · Job security {c.Security}/100")));
        var firing = new Switch { IsToggled = c.FiringEnabled };
        firing.Toggled += async (_, e) => { state = state! with { Career = state!.Career! with { FiringEnabled = e.Value } }; await Save(); };
        body.Add(new HorizontalStackLayout { Spacing = 8, Children = { firing, Text("Coach can be fired") } });
        body.Add(Text("When off, poor results still affect reputation and job security but cannot dismiss you. Roles change through job offers.", 12));
        body.Add(Panel("Role responsibilities", Text(c.Role == StaffRole.HeadCoach ? "You manage the franchise and both sides of the football team." : "Manage your unit's players, game plan, calls and motivation. The GM reviews shared financial and draft-pick decisions; the staff manages the opposite side.")));
        var expectation=s.Owners.FirstOrDefault(o=>o.Team==s.UserTeam);
        if(expectation is not null)body.Add(Panel("Ownership expectations",Text($"Target {expectation.TargetWins} wins · Security {c.Security}/100"),Text("Results against the target influence job security and reputation. Openings at less secure clubs receive priority in the job market.",12)));
        if(s.CoachingHistory.Count>0)body.Add(Panel("Career progress",FootballBarChart.Create(s.CoachingHistory.Select(h=>(h.Year.ToString(),(double)h.Reputation))),Text("Reputation by season",12)));
        body.Add(Text("Job offers", 21, true));
        if (s.JobOffers.Count == 0) body.Add(Text("Offers arrive during the offseason based on your record and reputation."));
        foreach (var offer in s.JobOffers)
        {
            var job = offer; body.Add(Panel(job.Team + " · " + RoleName(job.Role), Text(job.Reason), Action("Accept job", async () => { if (await GameDialogs.AlertAsync(this, "Accept coaching job?", $"Move to {job.Team} as {RoleName(job.Role)}?", "Accept", "Cancel")) await Update(x => ProCareer.Accept(x, job.Id)); })));
        }
        body.Add(Text("GM inbox", 21, true));
        foreach (var decision in s.GmDecisions.TakeLast(15).Reverse()) body.Add(Panel(decision.Action + (decision.Approved ? " · Approved" : " · Declined"), Text(decision.Reason)));
        body.Add(Text("Coaching history", 21, true));
        foreach (var year in s.CoachingHistory.AsEnumerable().Reverse()) body.Add(Text($"{year.Year} · {year.Team} · {RoleName(year.Role)} · {year.Wins}–{year.Losses}" + (year.Fired ? " · Dismissed" : "")));
    }
}
