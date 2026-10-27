using System.Security.Cryptography;
using System.Text;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
namespace ProFootball.Core;

public static class ProCareer
{
    public static ProFranchise Initialize(ProFranchise state, StaffRole role = StaffRole.HeadCoach, bool firing = false, string? name = null)
    {
        if (state.Career is not null) return state;
        if (role is not (StaffRole.HeadCoach or StaffRole.OffensiveCoordinator or StaffRole.DefensiveCoordinator)) throw new ArgumentException("Choose head coach, offensive coordinator or defensive coordinator.");
        var team = state.League.Teams.Single(t => t.Name == state.UserTeam);
        var existing = state.Coaches.FirstOrDefault(c => c.TeamId == team.Id && c.Role == role);
        var coachName = string.IsNullOrWhiteSpace(name) ? existing?.Name ?? "Your coach" : name.Trim();
        var coaches = state.Coaches.ToList(); var candidates = state.CoachCandidates.ToList();
        if (existing is not null && existing.Name != coachName && !existing.Vacant)
            candidates.Add(new(existing.Name, team.Id, existing.Title, existing.Source));
        coaches.RemoveAll(c => c.TeamId == team.Id && c.Role == role);
        coaches.Add(existing is null ? new(team.Id, role, coachName, role.ToString(), "User career") : existing with { Name = coachName, Vacant = false });
        return state with { Career = new(coachName, role, firing), Coaches = coaches, CoachCandidates = candidates.DistinctBy(c => c.Name).ToList() };
    }
    /// <summary>Coordinators call their own unit; the opposite unit continues under CPU staff.</summary>
    public static bool CpuHandlesUnit(StaffRole role, bool controlledTeamHasBall) => role switch
    {
        StaffRole.OffensiveCoordinator => !controlledTeamHasBall,
        StaffRole.DefensiveCoordinator => controlledTeamHasBall,
        _ => false
    };
    public static StaffRole Role(ProFranchise s) => s.Career?.Role ?? StaffRole.HeadCoach;
    public static bool Controls(ProFranchise s, Position position) => s.Career?.Unemployed != true && UserCoachControlService.ControlsPosition(Role(s), position);
    public static void RequirePlayer(ProFranchise s, Guid id)
    {
        var player = s.Players.Concat(s.Prospects).FirstOrDefault(p => p.Football.PlayerId == id) ?? throw new InvalidOperationException("Player unavailable.");
        if (!Controls(s, player.Football.Position)) throw new InvalidOperationException("Your coaching role does not manage this side of the team.");
    }
    public static void RequireHeadCoach(ProFranchise s)
    {
        if (Role(s) != StaffRole.HeadCoach || s.Career?.Unemployed == true) throw new InvalidOperationException("The head coach and GM manage this decision.");
    }
    public static TeamGamePlan LimitPlan(ProFranchise s, TeamGamePlan next)
    {
        var old = s.GamePlans.FirstOrDefault(p => p.TeamName == s.UserTeam) ?? new() { TeamName = s.UserTeam };
        if (s.Career?.Unemployed == true) throw new InvalidOperationException("Accept a job before managing a team.");
        return Role(s) switch
        {
            StaffRole.OffensiveCoordinator => next with { Defense = old.Defense, Approach = old.Approach, DefensiveFormation = old.DefensiveFormation, CustomDefensivePlayIds = old.CustomDefensivePlayIds, CreatedDefensivePlays = old.CreatedDefensivePlays },
            StaffRole.DefensiveCoordinator => next with { Offense = old.Offense, Tempo = old.Tempo, OffensiveFormation = old.OffensiveFormation, CustomOffensivePlayIds = old.CustomOffensivePlayIds, CreatedOffensivePlays = old.CreatedOffensivePlays, OpeningPlayIds = old.OpeningPlayIds, OpeningScript = old.OpeningScript },
            _ => next
        };
    }
    public static ProFranchise Review(ProFranchise state)
    {
        var s = Initialize(state); var career = s.Career!;
        if (career.LastReviewedYear >= s.Year) return s;
        var standing = ProStandings.Build(s).Single(r => r.Team.Name == s.UserTeam);
        var expected = s.Owners.FirstOrDefault(o => o.Team == s.UserTeam)?.TargetWins ?? 8;
        var security = Math.Clamp(career.Security + (standing.Wins - expected) * 5, 0, 100);
        var reputation = Math.Clamp(career.Reputation + (standing.Wins - expected) * 2 + (s.Champion == s.UserTeam ? 12 : 0), 10, 99);
        var fired = career.Unemployed || career.FiringEnabled && security < 15;
        var offers = new List<ProJobOffer>();
        foreach (var team in s.League.Teams.Where(t => t.Name != s.UserTeam).OrderBy(t => t.Id))
        {
            var owner = s.Owners.FirstOrDefault(o => o.Team == team.Name);
            foreach (var role in new[] { StaffRole.OffensiveCoordinator, StaffRole.DefensiveCoordinator, StaffRole.HeadCoach })
            {
                if (role == StaffRole.HeadCoach && reputation < 60 || role != StaffRole.HeadCoach && reputation < 30) continue;
                if (!fired && owner is { Security: > 75 }) continue;
                if (offers.Count >= 8) break;
                var id = new Guid(SHA256.HashData(Encoding.UTF8.GetBytes($"{s.Id}-{s.Year}-{team.Id}-{role}"))[..16]);
                offers.Add(new(id, team.Name, role, s.Year, role == StaffRole.HeadCoach ? "Ownership sees you as a head-coach candidate." : "Your coaching record fits this unit."));
            }
        }
        // A dismissed coach can always seek coordinator employment rather than losing the save.
        if (fired && offers.Count == 0)
        {
            var team = s.League.Teams.First(t => t.Name != s.UserTeam);
            offers.Add(new(Guid.NewGuid(), team.Name, StaffRole.OffensiveCoordinator, s.Year, "Interview opportunity to rebuild your coaching career."));
        }
        s = s with { Career = career with { Security = security, Reputation = reputation, Unemployed = fired, LastReviewedYear = s.Year },
            JobOffers = offers, CoachingHistory = s.CoachingHistory.Append(new(s.Year, s.UserTeam, career.Role, standing.Wins, standing.Losses, reputation, security, fired)).ToList() };
        if (fired)
        {
            var teamId = s.League.Teams.Single(t => t.Name == s.UserTeam).Id;
            s = s with { Coaches = s.Coaches.Select(c => c.TeamId == teamId && c.Role == career.Role
                ? c with { Name = "Vacant", Title = "Vacant coaching role", Vacant = true } : c).ToList() };
        }
        return ProEconomy.Log(s, fired ? $"{career.Name} was dismissed by {s.UserTeam}; new job opportunities are available." : $"{career.Name} completed the season with job security {security}/100.");
    }
    public static ProFranchise Accept(ProFranchise s, Guid offerId)
    {
        var offer = s.JobOffers.SingleOrDefault(o => o.Id == offerId) ?? throw new InvalidOperationException("This job offer is no longer available.");
        if (s.Phase != ProPhase.Offseason) throw new InvalidOperationException("Coaching moves take place in the offseason.");
        var old = s.Career ?? throw new InvalidOperationException("No user career.");
        var oldTeam = s.League.Teams.Single(t => t.Name == s.UserTeam);
        var next = s with { UserTeam = offer.Team, Career = null, JobOffers = [], Coaches = s.Coaches.Select(c => c.TeamId == oldTeam.Id && c.Role == old.Role ? c with { Name = "Vacant", Title = "Vacant coaching role", Vacant = true } : c).ToList() };
        next = Initialize(next, offer.Role, old.FiringEnabled, old.Name);
        return ProEconomy.Log(next with { Career = next.Career! with { Reputation = old.Reputation, Security = 65, LastReviewedYear = s.Year } }, $"{old.Name} accepted the {offer.Role} job with {offer.Team}.");
    }
}

/// <summary>User commands enforce career authority; CPU teams use the same underlying league transaction rules.</summary>
public static class ProManagement
{
    public static ProFranchise Submit(ProFranchise s, string action, IReadOnlyList<Guid> players, long extraCommitment, Func<ProFranchise, ProFranchise> execute, bool sharedPicks = false)
        =>Review(s,action,players,extraCommitment,execute,sharedPicks,false);
    internal static ProFranchise SubmitStaff(ProFranchise s,string action,IReadOnlyList<Guid> players,Func<ProFranchise,ProFranchise> execute)
        =>Review(s,action,players,0,execute,false,true);
    static ProFranchise Review(ProFranchise s,string action,IReadOnlyList<Guid> players,long extraCommitment,Func<ProFranchise,ProFranchise> execute,bool sharedPicks,bool staffUnit)
    {
        bool InUnit(ProFranchise state,Position position)=>staffUnit?!ProCareer.Controls(state,position):ProCareer.Controls(state,position);
        if(staffUnit)
        {
            if(ProCareer.Role(s)==StaffRole.HeadCoach||s.Career?.Unemployed==true)throw new InvalidOperationException("There is no delegated coordinator unit.");
            foreach(var id in players)
            {
                var p=s.Players.Concat(s.Prospects).FirstOrDefault(p=>p.Football.PlayerId==id);
                if(p is null||(p.Football.TeamName!=s.UserTeam&&p.Football.TeamName!="Free agent"&&!s.Prospects.Any(x=>x.Football.PlayerId==id))||!InUnit(s,p.Football.Position))throw new InvalidOperationException("Staff can manage only the other unit's players.");
            }
        }
        else foreach (var id in players) ProCareer.RequirePlayer(s, id);
        if (s.Career?.Unemployed == true) throw new InvalidOperationException("Accept a coaching job first.");
        if (ProCareer.Role(s) == StaffRole.HeadCoach) return execute(s);
        var unit = s.Players.Where(p => p.Football.TeamName == s.UserTeam && InUnit(s, p.Football.Position)).Select(p => p.Football.PlayerId).ToHashSet();
        var planningYear=ProFinance.ContractChangeYear(s);
        var spending = s.Contracts.Where(c => c.Team == s.UserTeam && unit.Contains(c.PlayerId)).Sum(c => ProEconomy.CapHit(c, planningYear));
        var room = ProFinance.Budget(s,s.UserTeam,planningYear).Available - ProEconomy.Pending(s, s.UserTeam);
        var approved = extraCommitment == 0 || extraCommitment <= room && spending + Math.Max(0, extraCommitment) <= ProFinance.Limit(s,s.UserTeam,planningYear) * .55;
        if (sharedPicks && players.Count == 0) approved = false;
        var reason = approved ? "GM approved: within the team cap and your unit's spending allocation." : sharedPicks && players.Count == 0 ? "The GM retains control of pick-only trades; recommend a player acquisition for your unit." : "GM declined: protect shared cap room and the other unit's budget.";
        var proposal = s;
        if (approved)
        {
            proposal = execute(s);
            // A current-year discount must not bypass review of back-loaded contracts or reserved bids.
            long UnitCost(ProFranchise state, int year)
            {
                var own = state.Players.Where(p => p.Football.TeamName == state.UserTeam && InUnit(state, p.Football.Position)).Select(p => p.Football.PlayerId).ToHashSet();
                var contracts = state.Contracts.Where(c => c.Team == state.UserTeam && own.Contains(c.PlayerId)).Sum(c => ProEconomy.CapHit(c, year));
                var bids = state.Bids.Where(b => b.Team == state.UserTeam && year >= state.Year && year < state.Year + b.Years && state.Players.Any(p => p.Football.PlayerId == b.PlayerId && InUnit(state, p.Football.Position)))
                    .Sum(b => ProEconomy.BidCharge(state,b,year));
                return contracts + bids;
            }
            long TeamCost(ProFranchise state,int year) => state.Contracts.Where(c=>c.Team==state.UserTeam).Sum(c=>ProEconomy.CapHit(c,year))
                + state.DeadCap.Where(c=>c.Team==state.UserTeam&&c.Year==year).Sum(c=>c.Amount)+ProFinance.Budget(state,state.UserTeam,year).RolloverReserved
                + ProEconomy.PendingForYear(state,state.UserTeam,year);
            var years = proposal.Contracts.Where(c => c.Active && c.Team == proposal.UserTeam).SelectMany(c => c.Years.Select(y => y.Year))
                .Concat(proposal.Contracts.Where(c => c.Active && c.Team == proposal.UserTeam).SelectMany(c => Enumerable.Range(s.Year, Math.Max(0, c.SignedYear + c.BonusYears - s.Year))))
                .Concat(proposal.DeadCap.Where(c=>c.Team==proposal.UserTeam).Select(c=>c.Year))
                .Concat(proposal.Bids.Where(b => b.Team == proposal.UserTeam).SelectMany(b => Enumerable.Range(proposal.Year, b.Years)))
                .Where(y => y >= s.Year).Append(s.Year).Distinct().Order();
            foreach (var year in years)
            {
                // Permit a non-worsening repair of an inherited over-budget season.
                var ceiling = Math.Max(ProFinance.Limit(s,s.UserTeam,year) * .55, UnitCost(s, year));
                var projected = UnitCost(proposal, year);
                if (projected > ceiling)
                {
                    approved = false;
                    reason = $"GM declined: {year} unit commitments of ${projected:N0} exceed the ${ceiling:N0} planning allocation, including pending offers. Protect future shared cap space.";
                    break;
                }
                var teamCeiling = Math.Max(ProFinance.Limit(s,s.UserTeam,year), TeamCost(s,year));
                var teamProjected = TeamCost(proposal,year);
                if(teamProjected <= teamCeiling) continue;
                approved = false;
                reason = $"GM declined: {year} shared team commitments of ${teamProjected:N0} exceed the ${teamCeiling:N0} planning allocation, including both units and pending offers.";
                break;
            }
        }
        if (approved && sharedPicks)
        {
            var beforePicks = s.Picks.ToDictionary(p => p.Id);
            var afterPicks = proposal.Picks.ToDictionary(p => p.Id);
            var sent = s.Picks.Where(p => p.Owner == s.UserTeam && afterPicks.TryGetValue(p.Id, out var after) && after.Owner != s.UserTeam).ToArray();
            if (sent.Length > 0)
            {
                var beforePlayers = s.Players.ToDictionary(p => p.Football.PlayerId);
                var afterPlayers = proposal.Players.ToDictionary(p => p.Football.PlayerId);
                var paid = sent.Sum(p => ProTrade.PickValue(s, p)) + s.Players
                    .Where(p => p.Football.TeamName == s.UserTeam && afterPlayers.TryGetValue(p.Football.PlayerId, out var after) && after.Football.TeamName != s.UserTeam)
                    .Sum(p => ProTrade.PlayerValue(s, p, s.UserTeam));
                var received = proposal.Picks.Where(p => p.Owner == s.UserTeam && beforePicks.TryGetValue(p.Id, out var before) && before.Owner != s.UserTeam)
                    .Sum(p => ProTrade.PickValue(s, p)) + proposal.Players
                    .Where(p => p.Football.TeamName == s.UserTeam && beforePlayers.TryGetValue(p.Football.PlayerId, out var before) && before.Football.TeamName != s.UserTeam)
                    .Sum(p => ProTrade.PlayerValue(s, beforePlayers[p.Football.PlayerId], s.UserTeam));
                if (paid > received * 1.10)
                {
                    approved = false;
                    reason = "GM declined: the incoming package does not justify the players and shared future draft capital being traded away.";
                }
            }
        }
        if(staffUnit)reason=reason.Replace("your unit","the other unit");
        var decision = new ProGmDecision(Guid.NewGuid(), s.Year, s.Week, action, approved, reason);
        var result = approved ? proposal : s;
        return result with { GmDecisions = s.GmDecisions.Append(decision).TakeLast(100).ToList() };
    }
}
