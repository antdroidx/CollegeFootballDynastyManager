using DynastyManager.Core.Models;
namespace ProFootball.Core;

public static class ProEconomy
{
    public static long MarketSalary(ProPlayer p) => ProMarket.BaseSalary(p);
    public static long MarketSalary(ProFranchise s,ProPlayer p)=>(long)(MarketSalary(p)*(s.SalaryCap/(double)ProFinance.Base2026));
    public static long BonusCharge(ProContract c, int year) => c.Years.FirstOrDefault(y => y.Year == year)?.ProratedBonus ?? (year < c.SignedYear || year >= c.SignedYear + c.BonusYears ? 0 : c.SigningBonus / c.BonusYears + (year - c.SignedYear < c.SigningBonus % c.BonusYears ? 1 : 0));
    public static long CapHit(ProContract c, int year) => !c.Active ? 0 : (c.Years.FirstOrDefault(y => y.Year == year)?.Salary ?? 0) + (c.Years.FirstOrDefault(y => y.Year == year)?.OtherCompensation ?? 0) + BonusCharge(c, year);
    public static long DeadOnRelease(ProFranchise s, Guid id) => ProContractInsights.Release(s,id).Sum(y=>y.DeadMoney);
    public static long Used(ProFranchise s,string team)=>ProFinance.Budget(s,team).TotalCommitments;
    public static long Room(ProFranchise s,string team)=>ProFinance.Budget(s,team).Available;
    public static long BidCharge(ProFranchise s,FreeAgentBid bid,int year)
    {
        var offset=year-s.Year;if(bid.Years<1||offset<0||offset>=bid.Years)return 0;
        var player=s.Players.FirstOrDefault(p=>p.Football.PlayerId==bid.PlayerId);
        var salary=player is null?bid.AnnualSalary:Math.Max(bid.AnnualSalary,ProFinance.Minimum(s,player,year,practice:false));
        return checked(salary+bid.Bonus/bid.Years+(offset<bid.Bonus%bid.Years?1:0));
    }
    public static long PendingForYear(ProFranchise s,string team,int year,Guid? except=null)=>s.Bids.Where(b=>b.Team==team&&b.PlayerId!=except).Sum(b=>BidCharge(s,b,year));
    public static long Pending(ProFranchise s,string team,Guid? except=null)=>PendingForYear(s,team,s.Year,except);
    public static ProFranchise Initialize(ProFranchise s)
    {
        if (s.Contracts.Count > 0) return EnsurePickInventory(s);
        var contracts = new List<ProContract>();
        var sourceTerms=s.Year==2026?ProContractData.Load().ToDictionary(c=>c.PlayerId):new Dictionary<Guid,ProContract>();
        foreach (var team in s.League.Teams)
        {
            var players = s.Players.Where(p => p.Football.TeamName == team.Name && !p.Retired).ToArray();
            var desired = players.Sum(p => Math.Max(1_000_000, MarketSalary(p)));
            var scale = Math.Min(1, s.SalaryCap * .78 / Math.Max(1, desired));
            foreach (var p in players)
            {
                var salary = p.PracticeSquad ? Math.Max(300_000,ProFinance.Minimum(s,p)) : (long)Math.Max(ProFinance.Minimum(s,p), MarketSalary(p) * scale);
                var years = p.PracticeSquad ? 1 : sourceTerms.TryGetValue(p.Football.PlayerId,out var term)&&term.Team==team.Name?Math.Max(1,ProContractInsights.PlayingEnd(term)-s.Year+1):p.RookieYear is int rookieYear && s.Year>=rookieYear && s.Year<rookieYear+(p.DraftOverall is >0?4:3) ? rookieYear+(p.DraftOverall is >0?4:3)-s.Year : p.Age>=33?1:2; var bonus = p.PracticeSquad ? 0 : salary / 2;
                contracts.Add(new(p.Football.PlayerId, team.Name, s.Year, years, bonus, Enumerable.Range(0, years).Select(i => new ContractYear(s.Year + i, Math.Max(salary,ProFinance.Minimum(s,p,s.Year+i)), i == 0 ? salary / 3 : 0)).ToList()){OriginalRookieYear=p.RookieYear is int r&&p.DraftOverall is >0&&s.Year<=r+3&&(!sourceTerms.TryGetValue(p.Football.PlayerId,out var original)||original.SignedYear==r)?r:null});
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
        var c = new ProContract(p.Football.PlayerId, bid.Team, s.Year, bid.Years, bid.Bonus, Enumerable.Range(0, bid.Years).Select(i => new ContractYear(s.Year + i, Math.Max(bid.AnnualSalary,ProFinance.Minimum(s,p,s.Year+i,practice:false)), i == 0 ? bid.AnnualSalary / 3 : 0)).ToList()) { Estimated = false, OriginalRookieYear=rookieScale?p.RookieYear??s.Year:null };
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
    public static ProFranchise Release(ProFranchise s, string team, Guid id, bool waive = true, bool postJune1 = false)
    {
        var p = s.Players.Single(p => p.Football.PlayerId == id);
        if (p.Football.TeamName != team || p.Retired) throw new InvalidOperationException("Player is not on this team.");
        s=ProTermination.CloseRetained(s,id);
        if(postJune1&&!ProTermination.CanDesignate(s,team,id))throw new InvalidOperationException("June 1 designation requires an unchanged contract before June 1, with no more than two designations per team/year; unavailable in 2030.");
        var releaseId=Guid.NewGuid();
        var original=s.Contracts.Where(c=>c.Active&&c.PlayerId==id&&c.Team==team).ToList();
        var charges=original.SelectMany(c=>ProTermination.Charges(s,c,releaseId,postJune1)).ToList();var dead=charges.Sum(d=>d.Amount);
        var contracts=s.Contracts.Select(c=>c.Active&&c.PlayerId==id?c with{ReleaseId=releaseId,Active=false,TerminatedYear=s.Year,EarnedCash=ProContractInsights.EarnedCash(s,c),BonusCash=ProBonusAccounting.TerminatedCash(s,c)}:c).ToList();
        var waivers = s.Waivers.ToList(); if (waive && (p.Experience < 4 || s.Phase == ProPhase.Playoffs || s.Phase == ProPhase.RegularSeason && s.Week > 9)) waivers.Add(new(id, s.TransactionTick + 1, []){ReleaseId=releaseId,OriginalContracts=original,ReleasedFraction=ProFinance.EarnedFraction(s),ReleasedYear=s.Year});
        return Log(s with { Contracts = contracts, UnsignedRookies = s.UnsignedRookies.Where(r=>r.PlayerId!=id).ToList(), DeadCap = s.DeadCap.Concat(charges).ToList(), June1Designations=postJune1?s.June1Designations.Append(new(team,s.Year,id,releaseId)).ToList():s.June1Designations, Players = s.Players.Select(x => x.Football.PlayerId == id ? x with { Football = x.Football with { TeamName = "Free agent" }, PracticeSquad = false, Reserve = false } : x).ToList(), Waivers = waivers }, $"{team} released {p.Football.FullName}; dead cap ${dead:N0}.");
    }
    public static ProFranchise Expire(ProFranchise s,string team,Guid id)
    {
        s=ProTermination.CloseRetained(s,id);
        var contracts=s.Contracts.Where(c=>c.Active&&c.PlayerId==id&&c.Team==team).ToArray();
        if(contracts.Length==0||contracts.Any(c=>ProContractInsights.PlayingEnd(c)>s.Year))throw new InvalidOperationException("Contract has not expired.");
        var nextYear=s.Year+1;
        var remainingBonus=contracts.Sum(c=>Enumerable.Range(nextYear,Math.Max(0,c.SignedYear+c.BonusYears-nextYear)).Sum(y=>BonusCharge(c,y)));
        return Log(s with{
            Contracts=s.Contracts.Select(c=>contracts.Contains(c)?c with{Active=false,TerminatedYear=nextYear}:c).ToList(),
            DeadCap=remainingBonus==0?s.DeadCap:s.DeadCap.Append(new DeadCapCharge(team,nextYear,id,remainingBonus)).ToList(),
            Players=s.Players.Select(p=>p.Football.PlayerId==id?p with{Football=p.Football with{TeamName="Free agent"},PracticeSquad=false,Reserve=false}:p).ToList()
        },$"{s.Players.Single(p=>p.Football.PlayerId==id).Football.FullName}'s contract expired."+(remainingBonus>0?$" Remaining bonus cap charge ${remainingBonus:N0} in {nextYear}.":""));
    }
    public static ProFranchise Extend(ProFranchise s, string team, Guid id, int years, long annual, long bonus = 0)
    {
        if (years is < 1 or > 5) throw new InvalidOperationException("Extension must be one to five years.");
        var p = s.Players.Single(p => p.Football.PlayerId == id); var c = s.Contracts.LastOrDefault(c => c.Active && c.PlayerId == id && c.Team == team) ?? throw new InvalidOperationException("No active contract.");
        if (c.LastChangedYear >= s.Year) throw new InvalidOperationException("This player already completed a contract change this season.");
        if (p.Football.TeamName != team || !ProNegotiation.Evaluate(p,years,annual,bonus,Math.Max(ProFinance.Minimum(s,p,practice:false),MarketSalary(s,p))).Accepted) throw new InvalidOperationException("Extension rejected. Increase the annual salary.");
        var first = ProContractInsights.PlayingEnd(c) + 1;
        if(annual<ProFinance.Minimum(s,p,first,practice:false))throw new InvalidOperationException("Offer must meet the player’s league minimum salary.");
        if (first + years - s.Year > 6) throw new InvalidOperationException("Extend only when five or fewer future contract years would remain.");
        var changeYear=ProFinance.ContractChangeYear(s);
        var extended = c with { LastChangedYear = changeYear, Years = c.Years.Where(y=>y.Year<first).Concat(Enumerable.Range(0, years).Select(i => new ContractYear(first + i, Math.Max(annual,ProFinance.Minimum(s,p,first+i,practice:false)), i == 0 ? annual / 4 : 0){ProratedBonus=BonusCharge(c,first+i)})).Concat(c.Years.Where(y=>y.Year>=first+years)).OrderBy(y=>y.Year).ToList(), ReportedAnnualValue=null, Estimated = false };
        if(bonus>0)
        {
            var count=Math.Min(5,extended.Years.Max(y=>y.Year)-changeYear+1);
            extended=extended with {SigningBonus=c.SigningBonus+bonus,BonusCash=AddBonusCash(c,changeYear,bonus),BonusYears=Math.Max(c.BonusYears,changeYear+count-c.SignedYear),
                Years=extended.Years.Select(y=>y with {ProratedBonus=BonusCharge(c,y.Year)+(y.Year>=changeYear&&y.Year<changeYear+count?bonus/count+(y.Year-changeYear<bonus%count?1:0):0)}).ToList()};
        }
        extended=ProContractInsights.NormalizeVoidCharges(extended);
        if(extended.Years.Where(y=>y.Year>=s.Year).Any(y=>{
            var before=ProFinance.Budget(s,team,y.Year).TotalCommitments;
            var after=before-CapHit(c,y.Year)+CapHit(extended,y.Year);
            return after>Math.Max(ProFinance.Limit(s,team,y.Year),before);
        }))throw new InvalidOperationException("Extension exceeds current or future cap space.");
        return ProMorale.ContractAgreement(Log(s with { Contracts = s.Contracts.Select(x => x == c ? extended : x).ToList() }, $"{p.Football.FullName} agreed to a {years}-year extension."), id);
    }
    public static ProFranchise Tag(ProFranchise s, string team, Guid id)
    {
        if (s.Phase != ProPhase.Offseason || s.OffseasonStep != OffseasonStep.Retention || s.TaggedTeams.Contains(team)) throw new InvalidOperationException("One tag per team is available during offseason retention.");
        var p = s.Players.Single(p => p.Football.PlayerId == id); var c = s.Contracts.LastOrDefault(c => c.Active && c.PlayerId == id && c.Team == team) ?? throw new InvalidOperationException("No contract.");
        if (c.LastChangedYear >= s.Year) throw new InvalidOperationException("This player already completed a contract change this season.");
        if (ProContractInsights.PlayingEnd(c) != s.Year) throw new InvalidOperationException("Only an expiring contract may be tagged.");
        var top = s.Players.Where(x => x.Football.Position == p.Football.Position).Select(x => MarketSalary(s,x)).OrderDescending().Take(5).DefaultIfEmpty(1_000_000).Average();
        var salary = (long)Math.Max(top, (c.Years.Last(y=>!y.IsVoid).Salary + BonusCharge(c, s.Year)) * 1.2);
        if (s.Contracts.Where(x => x.Team == team).Sum(x => CapHit(x, s.Year + 1)) + salary +s.DeadCap.Where(d=>d.Team==team&&d.Year==s.Year+1).Sum(d=>d.Amount) > ProFinance.Limit(s,team,s.Year+1)) throw new InvalidOperationException("Projected cap space is insufficient for the tag.");
        return Log(s with { TaggedTeams = s.TaggedTeams.Append(team).ToList(), Contracts = s.Contracts.Select(x => x == c ? c with { LastChangedYear = s.Year, Years = c.Years.Where(y=>y.Year!=s.Year+1).Append(new ContractYear(s.Year + 1, salary, salary){ProratedBonus=BonusCharge(c,s.Year+1)}).OrderBy(y=>y.Year).ToList(), ReportedAnnualValue=null, Tagged = true } : x).ToList() }, $"{team} tagged {p.Football.FullName}.");
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
            var c=s.Contracts.LastOrDefault(c=>c.Active&&c.PlayerId==id);var minimum=c is null?ProFinance.Minimum(s,p,practice:false):ProFinance.ContractMinimum(s,p,c,s.Year);
            if(c is not null){var extra=Math.Max(0,minimum-(c.Years.FirstOrDefault(y=>y.Year==s.Year)?.Salary??0));if(ProEconomy.Room(s,team)<extra)throw new InvalidOperationException("Promotion requires cap space for the active-roster minimum salary.");s=s with{Contracts=s.Contracts.Select(x=>x==c?c with{Years=c.Years.Select(y=>y with{Salary=Math.Max(y.Salary,ProFinance.ContractMinimum(s,p,c,y.Year))}).ToList()}:x).ToList()};}
        }
        return ProEconomy.Log(s with { Players = s.Players.Select(x => x.Football.PlayerId == id ? x with { PracticeSquad = practice } : x).ToList() }, $"{p.Football.FullName} moved to {(practice ? "practice squad" : "active roster")}.");
    }
    public static ProFranchise Claim(ProFranchise s, string team, Guid id)
    {
        if(s.Waivers.FirstOrDefault(w=>w.PlayerId==id) is {OriginalContracts.Count:>0,ReleaseId:not null} original)return ProWaivers.Claim(s,team,original);
        if (!s.League.Teams.Any(t => t.Name == team) || CountForLimit(s, team) >= Limit(s)) throw new InvalidOperationException("No roster space for a waiver claim.");
        var player = s.Players.Single(p => p.Football.PlayerId == id); if (ProEconomy.Room(s, team) < Math.Max(ProFinance.Minimum(s,player,practice:false), ProEconomy.MarketSalary(s,player))) throw new InvalidOperationException("Insufficient cap space for this claim.");
        var entry = s.Waivers.Single(w => w.PlayerId == id); if (entry.Claims.Contains(team)) return s; return s with { Waivers = s.Waivers.Select(w => w == entry ? w with { Claims = w.Claims.Append(team).ToList() } : w).ToList() };
    }
    internal static ProFranchise ClaimStaff(ProFranchise s,string team,Guid id)
    {
        if(s.Waivers.FirstOrDefault(w=>w.PlayerId==id) is {OriginalContracts.Count:>0,ReleaseId:not null} original)return ProWaivers.Claim(s,team,original,staff:true);
        var p=s.Players.Single(p=>p.Football.PlayerId==id);
        var quote=new FreeAgentBid(id,team,1,Math.Max(ProFinance.Minimum(s,p,practice:false),ProEconomy.MarketSalary(s,p)),0,s.Week);
        var reviewed=ProManagement.SubmitStaff(s,"Staff waiver claim · "+p.Football.FullName,[id],x=>x with{Bids=x.Bids.Append(quote).ToList()});
        return reviewed.GmDecisions.Last().Approved?Claim(reviewed with{Bids=s.Bids},team,id):reviewed;
    }
    public static ProFranchise ResolveWaivers(ProFranchise s)
    {
        var order = ProStandings.Build(s).AsEnumerable().Reverse().Select(r => r.Team.Name).ToArray();
        foreach (var entry in s.Waivers.Where(w => w.ExpiresAfterWeek <= s.TransactionTick).ToArray())
        {
            s=s with{Waivers=s.Waivers.Where(w=>w!=entry).ToList()};
            foreach(var team in entry.Claims.OrderBy(t=>Array.IndexOf(order,t)))
            {
                if(entry is {OriginalContracts.Count:>0,ReleaseId:not null})
                {
                    try{s=ProWaivers.Award(s,team,entry);}catch(InvalidOperationException){continue;}
                    if(s.Players.Single(p=>p.Football.PlayerId==entry.PlayerId).Football.TeamName==team)break;
                    continue;
                }
                var p=s.Players.Single(p=>p.Football.PlayerId==entry.PlayerId);
                ProFranchise Sign(ProFranchise state)=>ProEconomy.Sign(state,new(p.Football.PlayerId,team,1,Math.Max(ProFinance.Minimum(state,p,practice:false),ProEconomy.MarketSalary(state,p)),0,0));
                var staff=team==s.UserTeam&&s.Career?.Unemployed!=true&&ProCareer.Role(s)!=StaffRole.HeadCoach&&!ProCareer.Controls(s,p.Football.Position);
                s=staff?ProManagement.SubmitStaff(s,"Staff waiver signing · "+p.Football.FullName,[p.Football.PlayerId],Sign):Sign(s);
                if(s.Players.Single(x=>x.Football.PlayerId==entry.PlayerId).Football.TeamName==team)break;
            }
        }
        return s;
    }
}
