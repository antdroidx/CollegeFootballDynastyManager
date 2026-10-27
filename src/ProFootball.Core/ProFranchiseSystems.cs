using DynastyManager.Core.Models;
namespace ProFootball.Core;

public static class ProTrade
{
    public static bool Available(ProFranchise s, ProDraftPick p) => p.Year > s.Year || p.Year == s.Year && !s.DraftHistory.Any(h => h.Year == p.Year && h.Round == p.Round && h.Slot == p.Slot);
    public static double PositionWeight(Position p) => p switch { Position.QB => 1.8, Position.DE => 1.45, Position.CB => 1.35, Position.OL => 1.2, Position.WR or Position.DT => 1.1, Position.RB => .85, Position.K => .35, _ => 1 };
    public static double PlayerValue(ProPlayer p) => Math.Pow(Math.Max(0, p.Football.OverallRating - 55), 2) * .65
        * Math.Clamp((36 - p.Age) / 10d, .25, 1.5) * PositionWeight(p.Football.Position)
        * (1 + ProDevelopment.Headroom(p) / 100d);
    public static double PlayerValue(ProFranchise s, ProPlayer p, string receivingTeam)
    {
        var contract = s.Contracts.LastOrDefault(c => c.Active && c.PlayerId == p.Football.PlayerId);
        var cap = contract is null ? ProEconomy.MarketSalary(s,p) : ProEconomy.CapHit(contract, s.Year);
        var surplus = Math.Clamp((ProEconomy.MarketSalary(s,p) - cap) / (double)Math.Max(ProFinance.Minimum(s,p,practice:false), ProEconomy.MarketSalary(s,p)), -.5, .5);
        return PlayerValue(p) * (1 + surplus * .35) * (1 + Math.Min(3, ProCpu.Need(s, receivingTeam, p.Football.Position)) * .06)
            * (p.Football.CurrentInjury is null ? 1 : .8);
    }
    public static double PickValue(ProFranchise s, ProDraftPick p) => 1800 / Math.Pow((p.Round-1)*32+ProPickProjection.Slot(s,p),.58) * Math.Pow(.9, Math.Max(0, p.Year - s.Year - 1));
    public static double PickValue(ProDraftPick p) => 1800 / Math.Pow((p.Round - 1) * 32 + p.Slot, .58);
    public static ProFranchise Propose(ProFranchise s, string from, string to, IReadOnlyList<Guid> givePlayers, IReadOnlyList<Guid> getPlayers, IReadOnlyList<Guid> givePicks, IReadOnlyList<Guid> getPicks)
    {
        if (from == to || !s.League.Teams.Any(t => t.Name == from) || !s.League.Teams.Any(t => t.Name == to) || s.Phase == ProPhase.Playoffs || s.Phase == ProPhase.RegularSeason && s.Week > 9) throw new InvalidOperationException("Trades are unavailable after the deadline or between the same team.");
        if (givePlayers.Concat(getPlayers).Distinct().Count() != givePlayers.Count + getPlayers.Count || givePicks.Concat(getPicks).Distinct().Count() != givePicks.Count + getPicks.Count) throw new InvalidOperationException("Duplicate trade assets.");
        var a = givePlayers.Select(id => s.Players.Single(p => p.Football.PlayerId == id && p.Football.TeamName == from && !p.Retired)).ToArray();
        var b = getPlayers.Select(id => s.Players.Single(p => p.Football.PlayerId == id && p.Football.TeamName == to && !p.Retired)).ToArray();
        var ap = givePicks.Select(id => s.Picks.Single(p => p.Id == id && p.Owner == from && Available(s, p))).ToArray(); var bp = getPicks.Select(id => s.Picks.Single(p => p.Id == id && p.Owner == to && Available(s, p))).ToArray();
        if(s.UnsignedRookies.Any(r=>givePlayers.Concat(getPlayers).Contains(r.PlayerId))) throw new InvalidOperationException("Sign rookie contracts before trading these players.");
        if (a.Length + b.Length + ap.Length + bp.Length == 0) throw new InvalidOperationException("Select trade assets.");
        if (a.Sum(p => PlayerValue(s, p, to)) + ap.Sum(p => PickValue(s, p)) < (b.Sum(p => PlayerValue(s, p, from)) + bp.Sum(p => PickValue(s, p))) * 1.05) throw new InvalidOperationException("The CPU rejected this package. Offer more value.");
        var next = s;
        foreach (var p in a) next = Move(next, p, to); foreach (var p in b) next = Move(next, p, from);
        if (ProEconomy.Room(next, from) < 0 || ProEconomy.Room(next, to) < 0 || ProRoster.CountForLimit(next, from) > ProRoster.Limit(next) || ProRoster.CountForLimit(next, to) > ProRoster.Limit(next)) throw new InvalidOperationException("Trade violates the salary cap or active-roster limit.");
        return ProEconomy.Log(next with { TradeHistory = next.TradeHistory.Append(new(Guid.NewGuid(), s.Year, s.Week, from, to, a.Select(p => p.Football.PlayerId).ToList(), b.Select(p => p.Football.PlayerId).ToList(), ap.ToList(), bp.ToList())).ToList(), Picks = next.Picks.Select(p => givePicks.Contains(p.Id) ? p with { Owner = to } : getPicks.Contains(p.Id) ? p with { Owner = from } : p).ToList() }, $"Trade completed: {from} / {to}.");
    }
    static ProFranchise Move(ProFranchise s, ProPlayer p, string to)
    {
        var c = s.Contracts.LastOrDefault(c => c.Active && c.PlayerId == p.Football.PlayerId); var contracts = s.Contracts.ToList(); var dead = s.DeadCap.ToList();
        if(c is not null)
        {
            var fraction=ProFinance.EarnedFraction(s);var current=c.Years.FirstOrDefault(y=>y.Year==s.Year);var earned=(long)((current?.Salary??0)*fraction)+(current?.OtherCash??Math.Max(0,current?.OtherCompensation??0));
            var bonus=Enumerable.Range(s.Year,Math.Max(0,c.SignedYear+c.BonusYears-s.Year)).Sum(y=>ProEconomy.BonusCharge(c,y));
            if(bonus+earned>0)dead.Add(new(c.Team,s.Year,c.PlayerId,bonus+earned));
            contracts=contracts.Select(x=>x==c?c with{Active=false,TerminatedYear=s.Year,EarnedCash=earned,BonusCash=c.BonusCash?.Where(b=>b.Key<=s.Year).ToDictionary()}:x).ToList();
            contracts.Add(c with{Team=to,SigningBonus=0,BonusCash=c.BonusCash?.Where(b=>b.Key>s.Year).ToDictionary()??[],TerminatedYear=null,EarnedCash=0,SignedYear=s.Year,BonusYears=1,Source=null,Years=c.Years.Where(y=>y.Year>=s.Year).Select(y=>y.Year==s.Year?y with{Salary=y.Salary-(long)(y.Salary*fraction),OtherCompensation=0,OtherCash=0,ProratedBonus=0,Guaranteed=Math.Max(0,y.Guaranteed-(long)(y.Salary*fraction))}:y with{ProratedBonus=0,OtherCompensation=y.OtherCash??Math.Max(0,y.OtherCompensation)}).ToList()});
        }

        return s with { Contracts = contracts, DeadCap = dead, MoraleEffects = s.MoraleEffects.Where(e=>e.PlayerId!=p.Football.PlayerId).ToList(), Players = s.Players.Select(x => x == p ? x with { Football = x.Football with { TeamName = to, DepthChartOrder = 99 }, PracticeSquad = false, Reserve = false, TradeRequested = false, ContractConcernWeeks = 0, Morale = Math.Clamp(x.Morale+5,10,95) } : x).ToList() };
    }
}

public static class ProDraft
{
    public static ProFranchise Generate(ProFranchise s)
    {
        var existing = s.Picks.Where(p => p.Year != s.Year).ToList(); var completedSeason=s.History.LastOrDefault();var order = ProEconomy.CreatePicks(completedSeason is null?s:s with{Games=completedSeason.Games}, s.Year);
        foreach (var p in order) { var old = s.Picks.FirstOrDefault(x => x.Year == s.Year && x.Round == p.Round && x.OriginalTeam == p.OriginalTeam); existing.Add(old is null ? p : p with { Owner = old.Owner }); }
        var random = new Random(s.Year * 31); var basePlayers = ProLeagueService.GeneratePlayers(s.League).Where(p => !p.PracticeSquad).Take(320).ToArray();
        var prospects = basePlayers.Select((p, i) => { var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"rookie-{s.Year}-{i}")); var rating = random.Next(49, 82); return p with { Football = p.Football with { PlayerId = new Guid(bytes[..16]), TeamName = "Draft class", OverallRating = rating, SeasonStartOverall = rating, Skills = ProOffseason.ShiftSkills(p.Football.Skills ?? new PlayerSkillRatings(), rating - p.Football.OverallRating), AwarenessRating = rating, StrengthRating = rating, AgilityRating = rating, PotentialRating = Math.Clamp(rating + random.Next(2, 20), 50, 99), SpeedRating = Math.Clamp(p.Football.SpeedRating + random.Next(-6, 7), 35, 99) }, Age = random.Next(21, 24), PreviousSeasonOverall = null, RookieYear = s.Year, DraftOverall = null, Experience = 0, RatingSource = "Generated prospect · scouting uncertainty", ExternalId = "", Reserve = false }; }).Select(p => ProPlayerRatingService.Refresh(p, true, true)).ToList();
        return AutoScout(ProEconomy.EnsurePickInventory(s with { Picks = existing, Prospects = prospects, DraftIndex = 0, Scouting = [], DraftBoard = [] }));
    }
    public static int PublicGrade(ProPlayer p) => Math.Clamp(p.Football.OverallRating+(p.Football.PlayerId.ToByteArray()[1]%21)-10,40,95);
    public static ProFranchise AutoScout(ProFranchise s)
    {
        if(s.Phase!=ProPhase.Offseason || s.OffseasonStep is not (OffseasonStep.FreeAgency or OffseasonStep.Draft))return s;
        foreach(var team in s.League.Teams)
        {
            var seen=s.Scouting.Where(r=>r.Team==team.Name&&r.Year==s.Year).Select(r=>r.PlayerId).ToHashSet();
            var candidates=s.Prospects.Where(p=>!seen.Contains(p.Football.PlayerId)&&(team.Name!=s.UserTeam||ProCareer.Controls(s,p.Football.Position)))
                .OrderByDescending(p=>team.Name==s.UserTeam&&s.DraftBoard.Contains(p.Football.PlayerId)).ThenByDescending(p=>PublicGrade(p)+ProCpu.Need(s,team.Name,p.Football.Position)*2).ThenBy(p=>p.Football.PlayerId).Take(Math.Max(0,40-seen.Count)).ToArray();
            foreach(var player in candidates)s=Scout(s,team.Name,player.Football.PlayerId);
        }
        return s;
    }
    public static ProDraftPick? Current(ProFranchise s) => s.Picks.Where(p => p.Year == s.Year).OrderBy(p => p.Round).ThenBy(p => p.Slot).ElementAtOrDefault(s.DraftIndex);
    public static ProFranchise Scout(ProFranchise s, string team, Guid id)
    {
        if (s.Phase != ProPhase.Offseason || s.OffseasonStep is not (OffseasonStep.FreeAgency or OffseasonStep.Draft)) throw new InvalidOperationException("Scouting opens during the offseason.");
        if (s.Scouting.Any(r => r.PlayerId == id && r.Team == team && r.Year == s.Year)) return s;
        if (s.Scouting.Count(r => r.Team == team && r.Year == s.Year) >= 40) throw new InvalidOperationException("Your 40-player scouting budget is exhausted.");
        var p = s.Prospects.Single(p => p.Football.PlayerId == id); var random = new Random(id.GetHashCode() ^ s.Year); var grade = Math.Clamp(p.Football.OverallRating + random.Next(-5, 6), 40, 95);
        return s with { Scouting = s.Scouting.Append(new(id, team, s.Year, grade, 75)).ToList() };
    }
    public static long RookieSalary(ProFranchise s,int round)
    {
        var template=(s.Prospects.FirstOrDefault()??s.Players.First()) with{Experience=0,CreditedSeasons=0,PracticeSquad=false};
        return Math.Max(ProFinance.Minimum(s,template,practice:false),(long)(Math.Max(1_100_000,5_000_000/round)*(s.SalaryCap/(double)ProFinance.Base2026)));
    }
    public static ProFranchise Pick(ProFranchise s, Guid id)
    {
        if (s.Phase != ProPhase.Offseason || s.OffseasonStep != OffseasonStep.Draft) throw new InvalidOperationException("The draft is not open.");
        var pick = Current(s) ?? throw new InvalidOperationException("Draft complete."); var p = s.Prospects.Single(p => p.Football.PlayerId == id) with { RookieYear = s.Year, DraftOverall = (pick.Round-1)*32+pick.Slot };
        var salary = RookieSalary(s,pick.Round); var bonus = salary;
        var pool = s with { Players = s.Players.Append(p with { Football = p.Football with { TeamName = "Free agent" } }).ToList() };
        var signed = ProEconomy.Sign(pool, new(id, pick.Owner, 4, salary, bonus, 0), rookieScale: true);
        if (signed == pool)
            signed = ProEconomy.Log(s with {
                Players = s.Players.Append(p with { Football = p.Football with { TeamName = pick.Owner }, Reserve = true, PracticeSquad = false }).ToList(),
                UnsignedRookies = s.UnsignedRookies.Append(new(id, pick.Owner, s.Year, pick.Round, salary, bonus)).ToList()
            }, $"{pick.Owner} selected {p.Football.FullName}; rookie contract pending cap/roster room.");
        return signed with { Prospects = s.Prospects.Where(x => x.Football.PlayerId != id).ToList(), DraftIndex = s.DraftIndex + 1, DraftHistory = s.DraftHistory.Append(new(s.Year, pick.Round, pick.Slot, pick.Owner, p)).ToList() };
    }
    public static ProFranchise SignRookies(ProFranchise s, string? team = null)
    {
        foreach (var rookie in s.UnsignedRookies.Where(r => team is null || r.Team == team).ToArray())
        {
            var pool = s with { Players = s.Players.Select(p => p.Football.PlayerId == rookie.PlayerId ? p with { Football = p.Football with { TeamName = "Free agent" } } : p).ToList() };
            var signed = ProEconomy.Sign(pool, new(rookie.PlayerId, rookie.Team, 4, rookie.Salary, rookie.Bonus, 0), rookieScale: true);
            if (signed == pool) continue;
            s = signed with { UnsignedRookies = signed.UnsignedRookies.Where(r => r.PlayerId != rookie.PlayerId).ToList() };
        }
        return s;
    }
    public static ProFranchise AutoUntilUser(ProFranchise s, bool includeUser = false)
    {
        var guard = 0;
        while (Current(s) is { } pick && (includeUser || pick.Owner != s.UserTeam) && guard++ < 224)
        {
            var salary=RookieSalary(s,pick.Round);
            s = ProCpu.MakeRoom(s, pick.Owner, salary+(salary+3)/4);
            var needs = Enum.GetValues<Position>().ToDictionary(p => p, p => ProCpu.Need(s, pick.Owner, p));
            var chosen = s.Prospects.OrderByDescending(p => p.Football.OverallRating + needs[p.Football.Position] * 2).First(); s = Pick(s, chosen.Football.PlayerId);
        }
        return s;
    }
}

public static class ProCpu
{
    public static int Need(ProFranchise s, string team, Position position) => Math.Max(0, (position switch { Position.QB => 2, Position.OL => 8, Position.WR => 5, Position.CB => 4, Position.RB => 3, Position.K => 2, _ => 2 }) - s.Players.Count(p => p.Football.TeamName == team && !p.PracticeSquad && !p.Reserve && !p.Retired && p.Football.Position == position));
    public static ProFranchise MakeRoom(ProFranchise s, string team, long room,bool reserveRosterSlot=true)
    {
        s=ProCpuBudget.Relieve(s,team,room);
        var startingDepth=ProRoster.Active(s,team);var replacementFloor=DepthMinimum(s);
        long RequiredRoom()=>room+Math.Max(0,startingDepth-ProRoster.Active(s,team))*replacementFloor;
        for (var i = 0; i < 30 && (ProEconomy.Room(s, team) < RequiredRoom() || ProRoster.CountForLimit(s, team) > ProRoster.Limit(s)-(reserveRosterSlot?1:0)); i++)
        {
            var savings=new Dictionary<Guid,long>();
            long Saving(ProPlayer p){if(!savings.TryGetValue(p.Football.PlayerId,out var value))savings[p.Football.PlayerId]=value=s.Contracts.Where(c=>c.Active&&c.PlayerId==p.Football.PlayerId).Sum(c=>ProEconomy.CapHit(c,s.Year))-ProContractInsights.Release(s,p.Football.PlayerId).First().DeadMoney;return value;}
            var capNeeded = ProEconomy.Room(s, team) < RequiredRoom();
            var replacementCosts=s.Players.Where(candidate=>candidate.Football.TeamName=="Free agent"&&!candidate.Retired&&!s.Waivers.Any(w=>w.PlayerId==candidate.Football.PlayerId)).GroupBy(candidate=>candidate.Football.Position).ToDictionary(g=>g.Key,g=>g.Min(candidate=>SigningSalary(s,candidate)));
            bool Replaceable(ProPlayer p)=>Need(s,team,p.Football.Position)==0||capNeeded&&replacementCosts.GetValueOrDefault(p.Football.Position,long.MaxValue)<Saving(p);
            var release = s.Players.Where(p => p.Football.TeamName == team && !p.Retired && !p.PracticeSquad && !p.Reserve && Replaceable(p) && (!capNeeded || Saving(p) > (ProRoster.Active(s,team)<53?replacementFloor:0)))
                .OrderByDescending(p => capNeeded ? Saving(p) / (double)Math.Max(1, p.Football.OverallRating - 45) : 100 - p.Football.OverallRating).ThenByDescending(p => p.Age).FirstOrDefault();
            if (release is null) break;
            var next = ProEconomy.Release(s, team, release.Football.PlayerId, false);
            if (ProEconomy.Room(next, team) <= ProEconomy.Room(s, team) && ProRoster.CountForLimit(s, team) < ProRoster.Limit(s)) break; s = next;
        }
        return s;
    }
    public static long SigningSalary(ProFranchise s,ProPlayer p)
    {
        var market=Math.Max(ProFinance.Minimum(s,p,practice:false),ProEconomy.MarketSalary(s,p));
        return ProOfferTerms.RoundUp(Math.Max(market,ProNegotiation.Evaluate(p,2,market,0,market).CounterSalary));
    }
    static long DepthMinimum(ProFranchise s)=>s.Players.Where(p=>p.Football.TeamName=="Free agent"&&!p.Retired&&!s.Waivers.Any(w=>w.PlayerId==p.Football.PlayerId))
        .Select(p=>SigningSalary(s,p)).DefaultIfEmpty(1_500_000).Min();
    public static ProFranchise Manage(ProFranchise s, bool includeUser = false)
    {
        // Plan against opening-day obligations rather than spending temporary camp exclusions.
        var actualCapRules=s.CapRulesVersion;
        if(ProCapCounting.Offseason(s,s.Year))s=s with{CapRulesVersion=1};
        var teams = s.League.Teams.Where(t => includeUser || t.Name != s.UserTeam || s.Career?.Unemployed == true).ToArray();
        if (s.Phase is ProPhase.Preseason or ProPhase.Offseason)
            foreach (var team in teams)
            {
                var rookies=s.UnsignedRookies.Where(r=>r.Team==team.Name).Sum(r=>r.Salary+(r.Bonus+3)/4);
                var minimum=DepthMinimum(s);
                s=MakeRoom(s,team.Name,rookies+Math.Max(0,53-ProRoster.Active(s,team.Name)-s.UnsignedRookies.Count(r=>r.Team==team.Name))*minimum);
                s=ProDraft.SignRookies(s,team.Name);
            }

        foreach (var team in teams)
        {
            var claim = s.Waivers.Select(w => s.Players.Single(p => p.Football.PlayerId == w.PlayerId)).Where(p => Need(s, team.Name, p.Football.Position) > 0 && ProRoster.CountForLimit(s, team.Name) < ProRoster.Limit(s) && ProEconomy.Room(s, team.Name) >= Math.Max(ProFinance.Minimum(s,p,practice:false), ProEconomy.MarketSalary(s,p))).OrderByDescending(p => p.Football.OverallRating).FirstOrDefault();
            if (claim is not null) s = ProRoster.Claim(s, team.Name, claim.Football.PlayerId);
        }
        var rounds = s.Phase == ProPhase.Offseason || s.Phase == ProPhase.Preseason ? 256 : 4;
        for (var round = 0; round < rounds; round++)
        {
            var repairedBudget=false;var offers = 0;
            foreach (var team in teams.OrderBy(t => ProRoster.Active(s, t.Name)).ThenBy(t => t.Id))
            {
                var count = ProRoster.Active(s, team.Name); if (count >= Math.Min(53, ProRoster.Limit(s))) continue;
                var minimum=DepthMinimum(s); var reserve = s.Phase == ProPhase.Offseason && s.OffseasonStep == OffseasonStep.FreeAgency ? 20_000_000 : 0;
                var room = ProEconomy.Room(s, team.Name) - reserve;
                var target=room<(53-count)*minimum?Math.Max(45,count+1):53;var remaining=Math.Max(1,target-count);
                var budget = Math.Min(room - ProEconomy.Pending(s, team.Name), Math.Min(room / remaining * 2, room - (remaining - 1) * minimum));
                var needs = Enum.GetValues<Position>().ToDictionary(p => p, p => Need(s, team.Name, p));
                var candidate = s.Players.Where(p => p.Football.TeamName == "Free agent" && !p.Retired && !s.Waivers.Any(w => w.PlayerId == p.Football.PlayerId) && !s.Bids.Any(b => b.Team == team.Name && b.PlayerId == p.Football.PlayerId) && SigningSalary(s,p) <= budget)
                    .OrderByDescending(p => needs[p.Football.Position] * 20 + p.Football.OverallRating + (p.Football.PlayerId.ToByteArray()[0] * (team.Id.Sum(c => (int)c)) + s.Year) % 11).FirstOrDefault();
                if (candidate is null)
                {
                    if(count<45){var beforeRepair=s;s=MakeRoom(s,team.Name,reserve+remaining*minimum);repairedBudget|=s!=beforeRepair;}
                    continue;
                }
                try { s = ProEconomy.Offer(s, team.Name, candidate.Football.PlayerId, 2, SigningSalary(s,candidate), 0); offers++; } catch (InvalidOperationException) { }
            }
            s = ProEconomy.ResolveBids(s); if (offers == 0&&!repairedBudget) break;
        }
        return s with{CapRulesVersion=actualCapRules};
    }
    public static ProFranchise Trim(ProFranchise s, string team)
    {
        while (ProRoster.Active(s, team) > 53)
        { var p = s.Players.Where(p => p.Football.TeamName == team && !p.Reserve && !p.PracticeSquad && !p.Retired).OrderBy(p => Need(s, team, p.Football.Position) > 0 ? 1 : 0).ThenBy(p => p.Football.OverallRating).First(); s = ProEconomy.Release(s, team, p.Football.PlayerId, false); }
        return s;
    }
}

public static class ProOffseason
{
    public static PlayerSkillRatings ShiftSkills(PlayerSkillRatings k, int shift)
    {
        int R(int v) => Math.Clamp(v + shift, 1, 99);
        return k with { PassingPower = R(k.PassingPower), PassingAccuracy = R(k.PassingAccuracy), Catching = R(k.Catching), BreakTackle = R(k.BreakTackle), Tackling = R(k.Tackling), RunBlocking = R(k.RunBlocking), PassBlocking = R(k.PassBlocking), BallCarrying = R(k.BallCarrying), Acceleration = R(k.Acceleration), Leadership = R(k.Leadership), Discipline = R(k.Discipline), Jumping = R(k.Jumping), KickPower = R(k.KickPower), KickAccuracy = R(k.KickAccuracy) };
    }
    public static ProFranchise Advance(ProFranchise s)
    {
        s=ProRookieOptions.ManageCpu(s);
        if (s.OffseasonStep == OffseasonStep.Retention)
        {
            s = ProCareer.Review(s);
            foreach(var team in s.League.Teams.Where(t=>t.Name!=s.UserTeam||s.AutoNegotiateExpiring))s=ProRetention.Negotiate(s,team.Name);
            var owners = ProStandings.Build(s).Select(r => { var old = s.Owners.FirstOrDefault(o => o.Team == r.Team.Name) ?? new(r.Team.Name, 8, 65); var security = Math.Clamp(old.Security + (r.Wins - old.TargetWins) * 5, 0, 100); return old with { Security = security, Fired = security < 15 }; }).ToList();
            s=ProFinance.CloseYear(ProFinance.CpuBonuses(s));
            foreach (var p in s.Players.Where(p => !p.Retired && p.Football.TeamName != "Free agent").ToArray())
            { var c = s.Contracts.LastOrDefault(c => c.Active && c.PlayerId == p.Football.PlayerId); if (c is not null && ProContractInsights.PlayingEnd(c) <= s.Year) s = ProEconomy.Expire(s, p.Football.TeamName, p.Football.PlayerId); }
            var nextCap=ProFinance.ForecastCap(s,s.Year+1);
            var year = s.Year + 1; var random = new Random(year * 71);
            var credited=s.Games.Where(g=>g.Phase==ProPhase.RegularSeason&&g.Football.HasPlayed).SelectMany(g=>g.Football.PlayerStats).GroupBy(l=>l.PlayerId).Where(g=>g.Count()>=3).Select(g=>g.Key).ToHashSet();
            var players = s.Players.Select(p =>
            {
                var aged = ProDevelopment.AgeOneYear(p, random);
                var age = aged.Age;
                var retirement = p.Retired || age >= (p.Football.Position is Position.QB or Position.K ? 40 : 36) || age > 32 && aged.Football.OverallRating < 65 && random.NextDouble() < .3;
                return aged with { Experience = p.Experience + 1, CreditedSeasons=(p.CreditedSeasons??p.Experience)+(!p.PracticeSquad&&!p.Reserve&&credited.Contains(p.Football.PlayerId)?1:0), Retired = retirement, Reserve = s.UnsignedRookies.Any(r=>r.PlayerId==p.Football.PlayerId), Morale = Math.Clamp(p.Morale + (60 - p.Morale) / 3, 20, 90) };
            }).ToList();
            foreach (var p in players.Where(p => p.Retired && s.Contracts.Any(c => c.PlayerId == p.Football.PlayerId && c.Active)).ToArray()) s = ProEconomy.Release(s with { Year = year, Games = [] }, p.Football.TeamName, p.Football.PlayerId, false);
            var retired = players.Where(p => p.Retired).Select(p => p.Football.PlayerId).ToHashSet();
            var merged = players.Select(p => retired.Contains(p.Football.PlayerId) ? p with { Football = p.Football with { TeamName = "Retired" }, PracticeSquad = false, Reserve = false } : p).ToList();
            var next = s with { Year = year, Week = 1, Players = merged, SalaryCap = nextCap, Owners = owners, Coaches = s.Coaches.Select(c => c with { Rating = Math.Clamp(c.Rating + random.Next(-1, 3), 40, 95) }).ToList(), Games = [], OffseasonStep = OffseasonStep.FreeAgency, LetExpire = [], Bids = [], Waivers = [], Champion = null };
            return ProDraft.Generate(ProStaff.ManageCpu(next));
        }
        if (s.OffseasonStep == OffseasonStep.FreeAgency) return ProDraft.AutoUntilUser(ProCpu.Manage(s) with { OffseasonStep = OffseasonStep.Draft });
        if (s.OffseasonStep == OffseasonStep.Draft)
        {
            if (ProDraft.Current(s) is not null) throw new InvalidOperationException("Finish all seven draft rounds first.");
            if(ProRookieOptions.Pending(s,s.UserTeam).Any(p=>ProCareer.Controls(s,p.Football.Position)))throw new InvalidOperationException("Decide your fifth-year rookie options before leaving the draft. Open Front Office → Rookie options, or delegate to staff.");
            return s with { OffseasonStep = OffseasonStep.RosterCuts, Players = s.Players.Concat(s.Prospects.Select(p => p with { Football = p.Football with { TeamName = "Free agent" } })).ToList(), Prospects = [] };
        }
        if (ProRoster.CountForLimit(s, s.UserTeam) > 90 || ProEconomy.Room(s, s.UserTeam) < 0) throw new InvalidOperationException("Keep your camp roster at 90 or fewer and get under the cap first. Final 53-player cuts follow preseason.");
        s = ProCpu.Manage(s with { Phase = ProPhase.Preseason, Week = 1 });
        return s with { Phase = ProPhase.Preseason, Week = 1, OffseasonStep = OffseasonStep.Retention, Games = ProSchedule.Build(s.League, s.Year), TaggedTeams = [], Picks = s.Picks.Concat(ProEconomy.CreatePicks(s, s.Year + 1)).DistinctBy(p => (p.Year, p.Round, p.OriginalTeam)).ToList() };
    }
}
