using DynastyManager.Core.Models;
namespace ProFootball.Core;

public static class ProTrade
{
    public static bool Available(ProFranchise s, ProDraftPick p) => p.Year > s.Year || p.Year == s.Year && !s.DraftHistory.Any(h => h.Year == p.Year && h.Round == p.Round && h.Slot == p.Slot);
    public static double PlayerValue(ProPlayer p) => Math.Pow(Math.Max(1, p.Football.OverallRating - 48), 2) * Math.Clamp((36 - p.Age) / 10d, .25, 1.5) * (p.Football.Position == Position.QB ? 1.7 : 1);
    public static double PickValue(ProDraftPick p) => 1800 / Math.Pow((p.Round - 1) * 32 + p.Slot, .58);
    public static ProFranchise Propose(ProFranchise s, string from, string to, IReadOnlyList<Guid> givePlayers, IReadOnlyList<Guid> getPlayers, IReadOnlyList<Guid> givePicks, IReadOnlyList<Guid> getPicks)
    {
        if (from == to || !s.League.Teams.Any(t => t.Name == from) || !s.League.Teams.Any(t => t.Name == to) || s.Phase == ProPhase.Playoffs || s.Phase == ProPhase.RegularSeason && s.Week > 9) throw new InvalidOperationException("Trades are unavailable after the deadline or between the same team.");
        if (givePlayers.Concat(getPlayers).Distinct().Count() != givePlayers.Count + getPlayers.Count || givePicks.Concat(getPicks).Distinct().Count() != givePicks.Count + getPicks.Count) throw new InvalidOperationException("Duplicate trade assets.");
        var a = givePlayers.Select(id => s.Players.Single(p => p.Football.PlayerId == id && p.Football.TeamName == from && !p.Retired)).ToArray();
        var b = getPlayers.Select(id => s.Players.Single(p => p.Football.PlayerId == id && p.Football.TeamName == to && !p.Retired)).ToArray();
        var ap = givePicks.Select(id => s.Picks.Single(p => p.Id == id && p.Owner == from && Available(s, p))).ToArray(); var bp = getPicks.Select(id => s.Picks.Single(p => p.Id == id && p.Owner == to && Available(s, p))).ToArray();
        if (a.Length + b.Length + ap.Length + bp.Length == 0) throw new InvalidOperationException("Select trade assets.");
        if (a.Sum(PlayerValue) + ap.Sum(PickValue) < (b.Sum(PlayerValue) + bp.Sum(PickValue)) * 1.05) throw new InvalidOperationException("The CPU rejected this package. Offer more value.");
        var next = s;
        foreach (var p in a) next = Move(next, p, to); foreach (var p in b) next = Move(next, p, from);
        if (ProEconomy.Room(next, from) < 0 || ProEconomy.Room(next, to) < 0 || ProRoster.Active(next, from) > ProRoster.Limit(next) || ProRoster.Active(next, to) > ProRoster.Limit(next)) throw new InvalidOperationException("Trade violates the salary cap or active-roster limit.");
        return ProEconomy.Log(next with { Picks = next.Picks.Select(p => givePicks.Contains(p.Id) ? p with { Owner = to } : getPicks.Contains(p.Id) ? p with { Owner = from } : p).ToList() }, $"Trade completed: {from} / {to}.");
    }
    static ProFranchise Move(ProFranchise s, ProPlayer p, string to)
    {
        var c = s.Contracts.LastOrDefault(c => c.Active && c.PlayerId == p.Football.PlayerId); var contracts = s.Contracts.ToList(); var dead = s.DeadCap.ToList();
        if (c is not null) { var bonus = Enumerable.Range(s.Year, Math.Max(0, c.SignedYear + c.BonusYears - s.Year)).Sum(y => ProEconomy.BonusCharge(c, y)); dead.Add(new(c.Team, s.Year, c.PlayerId, bonus)); contracts = contracts.Select(x => x == c ? c with { Active = false } : x).ToList(); contracts.Add(c with { Team = to, SigningBonus = 0, SignedYear = s.Year, BonusYears = 1, Years = c.Years.Select(y => y with { ProratedBonus = 0 }).ToList() }); }
        return s with { Contracts = contracts, DeadCap = dead, Players = s.Players.Select(x => x == p ? x with { Football = x.Football with { TeamName = to, DepthChartOrder = 99 }, PracticeSquad = false, Reserve = false } : x).ToList() };
    }
}

public static class ProDraft
{
    public static ProFranchise Generate(ProFranchise s)
    {
        var existing = s.Picks.Where(p => p.Year != s.Year).ToList(); var completedSeason=s.History.LastOrDefault();var order = ProEconomy.CreatePicks(completedSeason is null?s:s with{Games=completedSeason.Games}, s.Year);
        foreach (var p in order) { var old = s.Picks.FirstOrDefault(x => x.Year == s.Year && x.Round == p.Round && x.OriginalTeam == p.OriginalTeam); existing.Add(old is null ? p : p with { Owner = old.Owner }); }
        var random = new Random(s.Year * 31); var basePlayers = ProLeagueService.GeneratePlayers(s.League).Where(p => !p.PracticeSquad).Take(320).ToArray();
        var prospects = basePlayers.Select((p, i) => { var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"rookie-{s.Year}-{i}")); var rating = random.Next(49, 82); return p with { Football = p.Football with { PlayerId = new Guid(bytes[..16]), TeamName = "Draft class", OverallRating = rating, SeasonStartOverall = rating, Skills = ProOffseason.ShiftSkills(p.Football.Skills ?? new PlayerSkillRatings(), rating - p.Football.OverallRating), AwarenessRating = rating, StrengthRating = rating, AgilityRating = rating, PotentialRating = Math.Clamp(rating + random.Next(2, 20), 50, 99), SpeedRating = Math.Clamp(p.Football.SpeedRating + random.Next(-6, 7), 35, 99) }, Age = random.Next(21, 24), Experience = 0, RatingSource = "Generated prospect · scouting uncertainty", ExternalId = "", Reserve = false }; }).ToList();
        return s with { Picks = existing, Prospects = prospects, DraftIndex = 0, Scouting = [], DraftBoard = [] };
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
    public static ProFranchise Pick(ProFranchise s, Guid id)
    {
        if (s.Phase != ProPhase.Offseason || s.OffseasonStep != OffseasonStep.Draft) throw new InvalidOperationException("The draft is not open.");
        var pick = Current(s) ?? throw new InvalidOperationException("Draft complete."); var p = s.Prospects.Single(p => p.Football.PlayerId == id);
        var salary = Math.Max(1_100_000, 5_000_000 / pick.Round); var bonus = salary;
        var pool = s with { Players = s.Players.Append(p with { Football = p.Football with { TeamName = "Free agent" } }).ToList() };
        var signed = ProEconomy.Sign(pool, new(id, pick.Owner, 4, salary, bonus, 0));
        if (signed == pool) throw new InvalidOperationException($"{pick.Owner}: make cap/roster room before drafting (room {ProEconomy.Room(s, pick.Owner):N0}, active {ProRoster.Active(s, pick.Owner)}).");
        return signed with { Prospects = s.Prospects.Where(x => x.Football.PlayerId != id).ToList(), DraftIndex = s.DraftIndex + 1, DraftHistory = s.DraftHistory.Append(new(s.Year, pick.Round, pick.Slot, pick.Owner, p)).ToList() };
    }
    public static ProFranchise AutoUntilUser(ProFranchise s, bool includeUser = false)
    {
        var guard = 0;
        while (Current(s) is { } pick && (includeUser || pick.Owner != s.UserTeam) && guard++ < 224)
        {
            s = ProCpu.MakeRoom(s, pick.Owner, 7_000_000);
            var needs = Enum.GetValues<Position>().ToDictionary(p => p, p => ProCpu.Need(s, pick.Owner, p));
            var chosen = s.Prospects.OrderByDescending(p => p.Football.OverallRating + needs[p.Football.Position] * 2).First(); s = Pick(s, chosen.Football.PlayerId);
        }
        return s;
    }
}

public static class ProCpu
{
    public static int Need(ProFranchise s, string team, Position position) => Math.Max(0, (position switch { Position.QB => 2, Position.OL => 8, Position.WR => 5, Position.CB => 4, Position.RB => 3, Position.K => 2, _ => 2 }) - s.Players.Count(p => p.Football.TeamName == team && !p.PracticeSquad && !p.Reserve && !p.Retired && p.Football.Position == position));
    public static ProFranchise MakeRoom(ProFranchise s, string team, long room)
    {
        for (var i = 0; i < 30 && (ProEconomy.Room(s, team) < room || ProRoster.Active(s, team) >= ProRoster.Limit(s)); i++)
        {
            long Saving(ProPlayer p) => s.Contracts.Where(c => c.Active && c.PlayerId == p.Football.PlayerId).Sum(c => ProEconomy.CapHit(c, s.Year)) - ProEconomy.DeadOnRelease(s, p.Football.PlayerId);
            var capNeeded = ProEconomy.Room(s, team) < room;
            var release = s.Players.Where(p => p.Football.TeamName == team && !p.Retired && !p.PracticeSquad && !p.Reserve && Need(s, team, p.Football.Position) == 0 && (!capNeeded || Saving(p) > 0))
                .OrderByDescending(p => capNeeded ? Saving(p) / (double)Math.Max(1, p.Football.OverallRating - 45) : 100 - p.Football.OverallRating).ThenByDescending(p => p.Age).FirstOrDefault();
            if (release is null) break;
            var next = ProEconomy.Release(s, team, release.Football.PlayerId, false);
            if (ProEconomy.Room(next, team) <= ProEconomy.Room(s, team) && ProRoster.Active(s, team) < ProRoster.Limit(s)) break; s = next;
        }
        return s;
    }
    public static ProFranchise Manage(ProFranchise s, bool includeUser = false)
    {
        var teams = s.League.Teams.Where(t => includeUser || t.Name != s.UserTeam).ToArray();
        if (s.Phase == ProPhase.Preseason)
            foreach (var team in teams) s = MakeRoom(s, team.Name, Math.Max(0, 53 - ProRoster.Active(s, team.Name)) * 1_000_000);

        foreach (var team in teams)
        {
            var claim = s.Waivers.Select(w => s.Players.Single(p => p.Football.PlayerId == w.PlayerId)).Where(p => Need(s, team.Name, p.Football.Position) > 0 && ProRoster.Active(s, team.Name) < ProRoster.Limit(s) && ProEconomy.Room(s, team.Name) >= Math.Max(1_000_000, ProEconomy.MarketSalary(p))).OrderByDescending(p => p.Football.OverallRating).FirstOrDefault();
            if (claim is not null) s = ProRoster.Claim(s, team.Name, claim.Football.PlayerId);
        }
        var rounds = s.Phase == ProPhase.Offseason || s.Phase == ProPhase.Preseason ? 256 : 4;
        for (var round = 0; round < rounds; round++)
        {
            var offers = 0;
            foreach (var team in teams.OrderBy(t => ProRoster.Active(s, t.Name)).ThenBy(t => t.Id))
            {
                var count = ProRoster.Active(s, team.Name); if (count >= Math.Min(53, ProRoster.Limit(s))) continue;
                var remaining = Math.Max(1, 53 - count); var reserve = s.Phase == ProPhase.Offseason && s.OffseasonStep == OffseasonStep.FreeAgency ? 20_000_000 : 0;
                var room = ProEconomy.Room(s, team.Name) - reserve; var budget = Math.Min(room - ProEconomy.Pending(s, team.Name), Math.Min(room / remaining * 2, room - (remaining - 1) * 1_000_000));
                var needs = Enum.GetValues<Position>().ToDictionary(p => p, p => Need(s, team.Name, p));
                var candidate = s.Players.Where(p => p.Football.TeamName == "Free agent" && !p.Retired && !s.Waivers.Any(w => w.PlayerId == p.Football.PlayerId) && !s.Bids.Any(b => b.Team == team.Name && b.PlayerId == p.Football.PlayerId) && Math.Max(1_000_000, ProEconomy.MarketSalary(p)) <= budget)
                    .OrderByDescending(p => needs[p.Football.Position] * 20 + p.Football.OverallRating + (p.Football.PlayerId.ToByteArray()[0] * (team.Id.Sum(c => (int)c)) + s.Year) % 11).FirstOrDefault();
                if (candidate is null) continue;
                try { s = ProEconomy.Offer(s, team.Name, candidate.Football.PlayerId, 2, Math.Max(1_000_000, ProEconomy.MarketSalary(candidate)), 0); offers++; } catch (InvalidOperationException) { }
            }
            s = ProEconomy.ResolveBids(s); if (offers == 0) break;
        }
        return s;
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
        if (s.OffseasonStep == OffseasonStep.Retention)
        {
            var owners = ProStandings.Build(s).Select(r => { var old = s.Owners.FirstOrDefault(o => o.Team == r.Team.Name) ?? new(r.Team.Name, 8, 65); var security = Math.Clamp(old.Security + (r.Wins - old.TargetWins) * 5, 0, 100); return old with { Security = security, Fired = security < 15 }; }).ToList();
            foreach (var p in s.Players.Where(p => !p.Retired && p.Football.TeamName != "Free agent").ToArray())
            { var c = s.Contracts.LastOrDefault(c => c.Active && c.PlayerId == p.Football.PlayerId); if (c is not null && c.Years.Max(y => y.Year) <= s.Year) s = ProEconomy.Release(s, p.Football.TeamName, p.Football.PlayerId, false); }
            var year = s.Year + 1; var random = new Random(year * 71);
            var players = s.Players.Select(p =>
            {
                var age = p.Age + 1; var peak = p.Football.Position == Position.QB ? 30 : p.Football.Position == Position.K ? 31 : p.Football.Position == Position.RB ? 25 : 27;
                var retirement = p.Retired || age >= (p.Football.Position is Position.QB or Position.K ? 40 : 36) || age > 32 && p.Football.OverallRating < 65 && random.NextDouble() < .3;
                var shift = retirement ? 0 : age <= peak ? random.Next(0, 3) : -random.Next(1, 4); var overall = Math.Clamp(p.Football.OverallRating + shift, 35, Math.Max(35, p.Football.PotentialRating));
                return p with { Age = age, Experience = p.Experience + 1, Retired = retirement, Reserve = false, Morale = Math.Clamp(p.Morale + (60 - p.Morale) / 3, 20, 90), Football = p.Football with { OverallRating = overall, SeasonStartOverall = overall, SpeedRating = Math.Clamp(p.Football.SpeedRating + (age > peak ? -1 : 0), 25, 99), AwarenessRating = Math.Clamp(p.Football.AwarenessRating + shift, 25, 99), Skills = ShiftSkills(p.Football.Skills ?? new PlayerSkillRatings(), shift), CurrentInjury = null } };
            }).ToList();
            foreach (var p in players.Where(p => p.Retired && s.Contracts.Any(c => c.PlayerId == p.Football.PlayerId && c.Active)).ToArray()) s = ProEconomy.Release(s with { Year = year }, p.Football.TeamName, p.Football.PlayerId, false);
            var retired = players.Where(p => p.Retired).Select(p => p.Football.PlayerId).ToHashSet();
            var merged = players.Select(p => retired.Contains(p.Football.PlayerId) ? p with { Football = p.Football with { TeamName = "Retired" }, PracticeSquad = false, Reserve = false } : p).ToList();
            var next = s with { Year = year, Week = 1, Players = merged, SalaryCap = (long)(s.SalaryCap * 1.05), Owners = owners, Coaches = s.Coaches.Select(c => c with { Rating = Math.Clamp(c.Rating + random.Next(-1, 3), 40, 95) }).ToList(), Games = [], OffseasonStep = OffseasonStep.FreeAgency, Bids = [], Waivers = [], Champion = null };
            return ProDraft.Generate(ProStaff.ManageCpu(next));
        }
        if (s.OffseasonStep == OffseasonStep.FreeAgency) return ProDraft.AutoUntilUser(ProCpu.Manage(s) with { OffseasonStep = OffseasonStep.Draft });
        if (s.OffseasonStep == OffseasonStep.Draft)
        {
            if (ProDraft.Current(s) is not null) throw new InvalidOperationException("Finish all seven draft rounds first.");
            return s with { OffseasonStep = OffseasonStep.RosterCuts, Players = s.Players.Concat(s.Prospects.Select(p => p with { Football = p.Football with { TeamName = "Free agent" } })).ToList(), Prospects = [] };
        }
        if (ProRoster.Active(s, s.UserTeam) > 53 || ProEconomy.Room(s, s.UserTeam) < 0) throw new InvalidOperationException("Trim your active roster to 53 and get under the cap first.");
        foreach (var team in s.League.Teams.Where(t => t.Name != s.UserTeam)) s = ProCpu.Trim(s, team.Name);
        s = ProCpu.Manage(s with { Phase = ProPhase.Preseason, Week = 1 });
        return s with { Phase = ProPhase.Preseason, Week = 1, OffseasonStep = OffseasonStep.Retention, Games = ProSchedule.Build(s.League, s.Year), TaggedTeams = [], Picks = s.Picks.Concat(ProEconomy.CreatePicks(s, s.Year + 1)).DistinctBy(p => (p.Year, p.Round, p.OriginalTeam)).ToList() };
    }
}
