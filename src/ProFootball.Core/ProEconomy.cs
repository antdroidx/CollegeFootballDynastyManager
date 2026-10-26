using DynastyManager.Core.Models;
namespace ProFootball.Core;

public static class ProEconomy
{
    public static long MarketSalary(ProPlayer p) => (long)(Math.Pow(Math.Max(1, p.Football.OverallRating - 48), 2) * 6500 * (p.Football.Position == Position.QB ? 2.5 : 1));
    public static long MarketSalary(ProFranchise s,ProPlayer p)=>(long)(MarketSalary(p)*(s.SalaryCap/(double)ProFinance.Base2026));
    public static long BonusCharge(ProContract c, int year) => c.Years.FirstOrDefault(y => y.Year == year)?.ProratedBonus ?? (year < c.SignedYear || year >= c.SignedYear + c.BonusYears ? 0 : c.SigningBonus / c.BonusYears + (year - c.SignedYear < c.SigningBonus % c.BonusYears ? 1 : 0));
    public static long CapHit(ProContract c, int year) => !c.Active ? 0 : (c.Years.FirstOrDefault(y => y.Year == year)?.Salary ?? 0) + (c.Years.FirstOrDefault(y => y.Year == year)?.OtherCompensation ?? 0) + BonusCharge(c, year);
    public static long DeadOnRelease(ProFranchise s, Guid id) => s.Contracts.Where(c => c.Active && c.PlayerId == id).Sum(c => c.Years.Where(y => y.Year >= s.Year).Sum(y => y.Guaranteed) + Enumerable.Range(s.Year, Math.Max(0, c.SignedYear + c.BonusYears - s.Year)).Sum(y => BonusCharge(c, y)));
    public static long Used(ProFranchise s, string team) => s.Contracts.Where(c => c.Team == team).Sum(c => CapHit(c, s.Year)) + s.DeadCap.Where(c => c.Team == team && c.Year == s.Year).Sum(c => c.Amount);
    public static long Room(ProFranchise s, string team) => ProFinance.Limit(s,team) - Used(s, team);
    public static long Pending(ProFranchise s, string team, Guid? except = null) => s.Bids.Where(b => b.Team == team && b.PlayerId != except).Sum(b => b.AnnualSalary + b.Bonus / b.Years);
    public static ProFranchise Initialize(ProFranchise s)
    {
        if (s.Contracts.Count > 0) return EnsurePickInventory(s);
        var contracts = new List<ProContract>();
        foreach (var team in s.League.Teams)
        {
            var players = s.Players.Where(p => p.Football.TeamName == team.Name && !p.Retired).ToArray();
            var desired = players.Sum(p => Math.Max(1_000_000, MarketSalary(p)));
            var scale = Math.Min(1, s.SalaryCap * .78 / Math.Max(1, desired));
            foreach (var p in players)
            {
                var salary = p.PracticeSquad ? Math.Max(300_000,ProFinance.Minimum(s,p)) : (long)Math.Max(ProFinance.Minimum(s,p), MarketSalary(p) * scale);
                var years = p.PracticeSquad ? 1 : p.RookieYear is int rookieYear && s.Year>=rookieYear && s.Year<rookieYear+(p.DraftOverall is >0?4:3) ? rookieYear+(p.DraftOverall is >0?4:3)-s.Year : 1 + (p.Football.PlayerId.ToByteArray()[0] % 4); var bonus = p.PracticeSquad ? 0 : salary / 2;
                contracts.Add(new(p.Football.PlayerId, team.Name, s.Year, years, bonus, Enumerable.Range(0, years).Select(i => new ContractYear(s.Year + i, Math.Max(salary,ProFinance.Minimum(s,p,s.Year+i)), i == 0 ? salary / 3 : 0)).ToList()));
            }
        }
        return EnsurePickInventory(s with { SchemaVersion = 2, Contracts = contracts, Picks = s.Picks.Count > 0 ? s.Picks : CreatePicks(s, s.Year + 1), Owners = s.League.Teams.Select(t => new OwnerExpectation(t.Name, 8, 65)).ToList() });
    }
    public static ProFranchise EnsurePickInventory(ProFranchise s)
    {
        var picks = s.Picks.ToList();
        foreach (var year in Enumerable.Range(s.Year + 1, 3))
            foreach (var pick in CreatePicks(s, year))
                if (!picks.Any(p => p.Year == year && p.Round == pick.Round && p.OriginalTeam == pick.OriginalTeam)) picks.Add(pick);
        return s with { Picks = picks };
    }
    public static List<ProDraftPick> CreatePicks(ProFranchise s, int year)
    {
        var order = ProStandings.Build(s).AsEnumerable().Reverse().Select(r => r.Team.Name).ToArray(); var picks = new List<ProDraftPick>();
        for (var round = 1; round <= 7; round++) for (var slot = 0; slot < order.Length; slot++)
        { var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"pick-{year}-{round}-{order[slot]}")); picks.Add(new(new Guid(bytes[..16]), year, round, slot + 1, order[slot], order[slot])); }
        return picks;
    }
    public static ProFranchise Offer(ProFranchise s, string team, Guid playerId, int years, long annual, long bonus)
    {
        if (years is < 1 or > 5 || annual is < 300_000 or > 500_000_000 || bonus < 0 || bonus > annual * years || !s.League.Teams.Any(t => t.Name == team)) throw new InvalidOperationException("Invalid contract offer.");
        var p = s.Players.Single(p => p.Football.PlayerId == playerId);
        if(annual<ProFinance.Minimum(s,p,practice:false))throw new InvalidOperationException($"Minimum annual salary is ${ProFinance.Minimum(s,p,practice:false):N0} for this player.");
        if (p.Retired || s.Waivers.Any(w => w.PlayerId == playerId) || p.Football.TeamName != "Free agent") throw new InvalidOperationException("Player is not an unrestricted free agent.");
        if (ProRoster.CountForLimit(s, team) + s.Bids.Count(b => b.Team == team && b.PlayerId != playerId) >= ProRoster.Limit(s) || Room(s, team) - Pending(s, team, playerId) < annual + (bonus + years - 1) / years) throw new InvalidOperationException("Insufficient cap room or roster space (pending offers reserve cap room).");
        var bids = s.Bids.Where(b => !(b.PlayerId == playerId && b.Team == team)).ToList(); bids.Add(new(playerId, team, years, annual, bonus, s.Week)); return s with { Bids = bids };
    }
    public static ProFranchise Sign(ProFranchise s, FreeAgentBid bid, bool rookieScale = false)
    {
        var p = s.Players.Single(p => p.Football.PlayerId == bid.PlayerId);
        if (!(rookieScale && (s.Phase == ProPhase.Offseason && s.OffseasonStep == OffseasonStep.Draft && s.Prospects.Any(x=>x.Football.PlayerId==bid.PlayerId) || s.UnsignedRookies.Any(x=>x.PlayerId==bid.PlayerId && x.Team==bid.Team)) || ProNegotiation.Evaluate(p,bid.Years,bid.AnnualSalary,bid.Bonus,Math.Max(ProFinance.Minimum(s,p,practice:false),MarketSalary(s,p))).Accepted) || bid.AnnualSalary<ProFinance.Minimum(s,p,practice:false) || p.Football.TeamName != "Free agent" || p.Retired || s.Waivers.Any(w => w.PlayerId == bid.PlayerId) || Room(s, bid.Team) < bid.AnnualSalary + (bid.Bonus + bid.Years - 1) / bid.Years || ProRoster.CountForLimit(s, bid.Team) >= ProRoster.Limit(s)) return s;
        var c = new ProContract(p.Football.PlayerId, bid.Team, s.Year, bid.Years, bid.Bonus, Enumerable.Range(0, bid.Years).Select(i => new ContractYear(s.Year + i, Math.Max(bid.AnnualSalary,ProFinance.Minimum(s,p,s.Year+i,practice:false)), i == 0 ? bid.AnnualSalary / 3 : 0)).ToList()) { Estimated = false };
        return ProMorale.Signing(Log(s with { Players = s.Players.Select(x => x.Football.PlayerId == p.Football.PlayerId ? x with { Football = x.Football with { TeamName = bid.Team, DepthChartOrder = 99 }, PracticeSquad = false, Reserve = false } : x).ToList(), Contracts = s.Contracts.Append(c).ToList() }, $"{bid.Team} signed {p.Football.FullName} for {bid.Years} years."), p.Football.PlayerId, bid.Team);
    }
    public static ProFranchise ResolveBids(ProFranchise s)
    {
        foreach(var group in s.Bids.GroupBy(b=>b.PlayerId).OrderBy(g=>g.Key))
        {
            foreach(var bid in group.OrderByDescending(b=>b.AnnualSalary+b.Bonus/b.Years).ThenBy(b=>b.Team))
            {var next=Sign(s,bid);if(next!=s){s=next;break;}}
            var user=group.FirstOrDefault(b=>b.Team==s.UserTeam);var player=s.Players.Single(p=>p.Football.PlayerId==group.Key);
            if(user is not null && player.Football.TeamName!=s.UserTeam)
                s=Log(s,player.Football.TeamName=="Free agent"?$"{player.Football.FullName} declined the offer or could not be signed within cap/roster limits.":$"{player.Football.FullName} chose a competing offer from {player.Football.TeamName}.");
        }
        return s with {Bids=[]};
    }
    public static ProFranchise Release(ProFranchise s, string team, Guid id, bool waive = true)
    {
        var p = s.Players.Single(p => p.Football.PlayerId == id);
        if (p.Football.TeamName != team || p.Retired) throw new InvalidOperationException("Player is not on this team.");
        var charges=ProContractInsights.Release(s,id);var dead=charges.Sum(y=>y.DeadMoney);
        var contracts=s.Contracts.Select(c=>c.Active&&c.PlayerId==id?c with{Active=false,TerminatedYear=s.Year,EarnedCash=ProContractInsights.EarnedCash(s,c)}:c).ToList();
        var waivers = s.Waivers.ToList(); if (waive && (p.Experience < 4 || s.Phase == ProPhase.Playoffs || s.Phase == ProPhase.RegularSeason && s.Week > 9)) waivers.Add(new(id, s.TransactionTick + 1, []));
        return Log(s with { Contracts = contracts, UnsignedRookies = s.UnsignedRookies.Where(r=>r.PlayerId!=id).ToList(), DeadCap = dead == 0 ? s.DeadCap : s.DeadCap.Concat(charges.Where(y=>y.DeadMoney>0).Select(y=>new DeadCapCharge(team,y.Year,id,y.DeadMoney){CashAmount=y.Year>s.Year?s.Contracts.Where(c=>c.Active&&c.PlayerId==id).Sum(c=>c.Years.Where(row=>row.Year==y.Year).Sum(row=>row.Guaranteed)):0})).ToList(), Players = s.Players.Select(x => x.Football.PlayerId == id ? x with { Football = x.Football with { TeamName = "Free agent" }, PracticeSquad = false, Reserve = false } : x).ToList(), Waivers = waivers }, $"{team} released {p.Football.FullName}; dead cap ${dead:N0}.");
    }
    public static ProFranchise Extend(ProFranchise s, string team, Guid id, int years, long annual, long bonus = 0)
    {
        if (years is < 1 or > 5) throw new InvalidOperationException("Extension must be one to five years.");
        var p = s.Players.Single(p => p.Football.PlayerId == id); var c = s.Contracts.LastOrDefault(c => c.Active && c.PlayerId == id && c.Team == team) ?? throw new InvalidOperationException("No active contract.");
        if (c.LastChangedYear == s.Year) throw new InvalidOperationException("This player already completed a contract change this season.");
        if (p.Football.TeamName != team || !ProNegotiation.Evaluate(p,years,annual,bonus,Math.Max(ProFinance.Minimum(s,p,practice:false),MarketSalary(s,p))).Accepted) throw new InvalidOperationException("Extension rejected. Increase the annual salary.");
        var first = c.Years.Max(y => y.Year) + 1;
        if(annual<ProFinance.Minimum(s,p,first,practice:false))throw new InvalidOperationException("Offer must meet the player’s league minimum salary.");
        if (first + years - s.Year > 6) throw new InvalidOperationException("Extend only when five or fewer future contract years would remain.");
        if (Enumerable.Range(first,years).Any(year => s.Contracts.Where(x => x.Team == team).Sum(x => CapHit(x,year)) + Math.Max(annual,ProFinance.Minimum(s,p,year,practice:false)) > ProFinance.Limit(s,team,year))) throw new InvalidOperationException("Future cap commitments cannot accommodate this extension.");
        var extended = c with { LastChangedYear = s.Year, Years = c.Years.Concat(Enumerable.Range(0, years).Select(i => new ContractYear(first + i, Math.Max(annual,ProFinance.Minimum(s,p,first+i,practice:false)), i == 0 ? annual / 4 : 0))).ToList(), Estimated = false };
        if(bonus>0)
        {
            var count=Math.Min(5,extended.Years.Max(y=>y.Year)-s.Year+1);
            if(Room(s,team)<(bonus+count-1)/count)throw new InvalidOperationException("Insufficient current cap room for the extension bonus.");
            extended=extended with {SigningBonus=c.SigningBonus+bonus,BonusCash=AddBonusCash(c,s.Year,bonus),BonusYears=Math.Max(c.BonusYears,s.Year+count-c.SignedYear),
                Years=extended.Years.Select(y=>y with {ProratedBonus=BonusCharge(c,y.Year)+(y.Year>=s.Year&&y.Year<s.Year+count?bonus/count+(y.Year-s.Year<bonus%count?1:0):0)}).ToList()};
        }
        if(extended.Years.Where(y=>y.Year>=s.Year).Any(y=>s.Contracts.Where(x=>x.Team==team&&x!=c).Sum(x=>CapHit(x,y.Year))+CapHit(extended,y.Year)+s.DeadCap.Where(d=>d.Team==team&&d.Year==y.Year).Sum(d=>d.Amount)>ProFinance.Limit(s,team,y.Year)))throw new InvalidOperationException("Extension exceeds current or future cap space.");
        return ProMorale.ContractAgreement(Log(s with { Contracts = s.Contracts.Select(x => x == c ? extended : x).ToList() }, $"{p.Football.FullName} agreed to a {years}-year extension."), id);
    }
    public static ProFranchise Tag(ProFranchise s, string team, Guid id)
    {
        if (s.Phase != ProPhase.Offseason || s.OffseasonStep != OffseasonStep.Retention || s.TaggedTeams.Contains(team)) throw new InvalidOperationException("One tag per team is available during offseason retention.");
        var p = s.Players.Single(p => p.Football.PlayerId == id); var c = s.Contracts.LastOrDefault(c => c.Active && c.PlayerId == id && c.Team == team) ?? throw new InvalidOperationException("No contract.");
        if (c.LastChangedYear == s.Year) throw new InvalidOperationException("This player already completed a contract change this season.");
        if (c.Years.Max(y => y.Year) != s.Year) throw new InvalidOperationException("Only an expiring contract may be tagged.");
        var top = s.Players.Where(x => x.Football.Position == p.Football.Position).Select(x => MarketSalary(s,x)).OrderDescending().Take(5).DefaultIfEmpty(1_000_000).Average();
        var salary = (long)Math.Max(top, (c.Years.Last().Salary + BonusCharge(c, s.Year)) * 1.2);
        if (s.Contracts.Where(x => x.Team == team).Sum(x => CapHit(x, s.Year + 1)) + salary +s.DeadCap.Where(d=>d.Team==team&&d.Year==s.Year+1).Sum(d=>d.Amount) > ProFinance.Limit(s,team,s.Year+1)) throw new InvalidOperationException("Projected cap space is insufficient for the tag.");
        return Log(s with { TaggedTeams = s.TaggedTeams.Append(team).ToList(), Contracts = s.Contracts.Select(x => x == c ? c with { LastChangedYear = s.Year, Years = c.Years.Append(new(s.Year + 1, salary, salary)).ToList(), Tagged = true } : x).ToList() }, $"{team} tagged {p.Football.FullName}.");
    }
    public static Dictionary<int,long> AddBonusCash(ProContract c,int year,long amount)
    {
        var cash=c.BonusCash is {} existing?new Dictionary<int,long>(existing):new Dictionary<int,long>{{c.SignedYear,c.SigningBonus}};
        cash[year]=cash.GetValueOrDefault(year)+amount;return cash;
    }
    public static ProFranchise Log(ProFranchise s,string text)=>ProTransactions.Add(s,text);
}

public static class ProRoster
{
    public static bool IsCamp(ProFranchise s) => s.Phase is ProPhase.Offseason or ProPhase.Preseason;
    public static int Limit(ProFranchise s) => IsCamp(s) ? 90 : 53;
    public static int CountForLimit(ProFranchise s,string team) => IsCamp(s) ? s.Players.Count(p=>p.Football.TeamName==team&&!p.Reserve&&!p.Retired) : Active(s,team);
    public static int Active(ProFranchise s, string team) => s.Players.Count(p => p.Football.TeamName == team && !p.PracticeSquad && !p.Reserve && !p.Retired);
    public static ProFranchise Practice(ProFranchise s, string team, Guid id, bool practice)
    {
        var p = s.Players.Single(p => p.Football.PlayerId == id);
        if (p.Football.TeamName != team || p.Reserve || p.Retired) throw new InvalidOperationException("Unavailable player.");
        if (practice && s.Players.Count(x => x.Football.TeamName == team && x.PracticeSquad) >= 16) throw new InvalidOperationException("Practice squad is full.");
        if (!practice && !IsCamp(s) && Active(s, team) >= Limit(s)) throw new InvalidOperationException("Active roster is full.");
        if (practice && p.Experience > 2 && s.Players.Count(x => x.Football.TeamName == team && x.PracticeSquad && x.Experience > 2) >= 6) throw new InvalidOperationException("Six veteran practice-squad slots are already occupied.");
        if(!practice)
        {
            var minimum=ProFinance.Minimum(s,p,practice:false);var c=s.Contracts.LastOrDefault(c=>c.Active&&c.PlayerId==id);
            if(c is not null){var extra=Math.Max(0,minimum-(c.Years.FirstOrDefault(y=>y.Year==s.Year)?.Salary??0));if(ProEconomy.Room(s,team)<extra)throw new InvalidOperationException("Promotion requires cap space for the active-roster minimum salary.");s=s with{Contracts=s.Contracts.Select(x=>x==c?c with{Years=c.Years.Select(y=>y with{Salary=Math.Max(y.Salary,ProFinance.Minimum(s,p,y.Year,practice:false))}).ToList()}:x).ToList()};}
        }
        return ProEconomy.Log(s with { Players = s.Players.Select(x => x.Football.PlayerId == id ? x with { PracticeSquad = practice } : x).ToList() }, $"{p.Football.FullName} moved to {(practice ? "practice squad" : "active roster")}.");
    }
    public static ProFranchise Claim(ProFranchise s, string team, Guid id)
    {
        if (!s.League.Teams.Any(t => t.Name == team) || CountForLimit(s, team) >= Limit(s)) throw new InvalidOperationException("No roster space for a waiver claim.");
        var player = s.Players.Single(p => p.Football.PlayerId == id); if (ProEconomy.Room(s, team) < Math.Max(ProFinance.Minimum(s,player,practice:false), ProEconomy.MarketSalary(s,player))) throw new InvalidOperationException("Insufficient cap space for this claim.");
        var entry = s.Waivers.Single(w => w.PlayerId == id); if (entry.Claims.Contains(team)) return s; return s with { Waivers = s.Waivers.Select(w => w == entry ? w with { Claims = w.Claims.Append(team).ToList() } : w).ToList() };
    }
    public static ProFranchise ResolveWaivers(ProFranchise s)
    {
        var order = ProStandings.Build(s).AsEnumerable().Reverse().Select(r => r.Team.Name).ToArray();
        foreach (var entry in s.Waivers.Where(w => w.ExpiresAfterWeek <= s.TransactionTick).ToArray())
        { s = s with { Waivers = s.Waivers.Where(w => w != entry).ToList() }; foreach (var team in entry.Claims.OrderBy(t => Array.IndexOf(order, t))) { var p = s.Players.Single(p => p.Football.PlayerId == entry.PlayerId); var next = ProEconomy.Sign(s, new(p.Football.PlayerId, team, 1, Math.Max(ProFinance.Minimum(s,p,practice:false), ProEconomy.MarketSalary(s,p)), 0, 0)); if (next != s) { s = next; break; } } }
        return s;
    }
}
