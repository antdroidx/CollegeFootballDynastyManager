using DynastyManager.Core.Models;
namespace ProFootball.Core;

public enum ProOptionTier { Base, PlayingTime, OneProBowl, MultipleProBowls }
public sealed record ProSalarySample(int Year, long LeagueCap, Dictionary<string,List<long>> Salaries);
public sealed record ProOptionTender(int DecisionYear, string PositionGroup, ProOptionTier Tier, long Salary, bool EstimatedHistory);
public sealed record ProOptionDecision(Guid PlayerId, int RookieYear, int DecisionYear, bool Exercised, long Salary, ProOptionTier Tier) { public string Team { get; init; } = ""; }

public static class ProRookieOptions
{
    public static string Group(Position p) => p switch { Position.OLB or Position.MLB => "LB", Position.FS or Position.SS => "S", _ => p.ToString() };
    public static ProSalarySample Sample(ProFranchise s)
    {
        var players=s.Players.ToDictionary(p=>p.Football.PlayerId);
        var rows=s.Contracts.Where(c=>c.Active && c.Years.Any(y=>y.Year==s.Year&&!y.IsVoid) && players.ContainsKey(c.PlayerId))
            .GroupBy(c=>c.PlayerId).Select(g=>(Group(players[g.Key].Football.Position), Pay:g.Sum(c=>Math.Max(0,ProEconomy.CapHit(c,s.Year)))));
        return new(s.Year,s.SalaryCap,rows.Where(r=>r.Pay>0).GroupBy(r=>r.Item1).ToDictionary(g=>g.Key,g=>g.Select(r=>r.Pay).OrderDescending().ToList()));
    }
    public static ProFranchise CaptureYear(ProFranchise s) => s with { SalarySamples=s.SalarySamples.Where(h=>h.Year!=s.Year).Append(Sample(s)).OrderBy(h=>h.Year).ToList() };
    public static int? Origin(ProPlayer p,ProContract c) => c.PracticeOrigin?null:c.OriginalRookieYear ??
        (p.RookieYear is int r && c.SignedYear==r && ProContractInsights.PlayingEnd(c)<=r+4 ? r : null);
    public static bool InWindow(ProFranchise s,int rookie) => s.Year==rookie+2 && (s.Phase==ProPhase.Playoffs || s.Phase==ProPhase.Offseason&&s.OffseasonStep==OffseasonStep.Retention)
        || s.Year==rookie+3 && s.Phase==ProPhase.Offseason && s.OffseasonStep is OffseasonStep.FreeAgency or OffseasonStep.Draft;
    public static IReadOnlyList<ProPlayer> Pending(ProFranchise s,string? team=null) => s.Players.Where(p=>!p.Retired&&!p.PracticeSquad&&!p.Reserve
        && p.DraftOverall is >=1 and <=32 && (team is null||p.Football.TeamName==team)
        && s.Contracts.LastOrDefault(c=>c.Active&&c.PlayerId==p.Football.PlayerId&&c.Team==p.Football.TeamName) is {} c
        && Origin(p,c) is int r && r>=2018 && InWindow(s,r) && ProContractInsights.PlayingEnd(c)==r+3
        && !s.RookieOptionDecisions.Any(d=>d.PlayerId==p.Football.PlayerId&&d.RookieYear==r)).ToArray();
    public static ProOptionTier Tier(ProFranchise s,ProPlayer p,int rookie)
    {
        var bowls=s.Honors.Where(h=>h.Year>=rookie&&h.Year<=rookie+2&&h.Selections.Any(x=>x.Player.Football.PlayerId==p.Football.PlayerId)).Select(h=>h.Year).Distinct().Count();
        if(bowls>=2)return ProOptionTier.MultipleProBowls;
        if(bowls==1)return ProOptionTier.OneProBowl;
        var offense=p.Football.Position is Position.QB or Position.RB or Position.WR or Position.TE or Position.OL;
        return ProRookieParticipation.MeetsPlayingTimeTier(s.RookieParticipation,p.Football.PlayerId,rookie,offense)?ProOptionTier.PlayingTime:ProOptionTier.Base;
    }
    static ProOptionTender Calculate(ProFranchise s,int year,string group,ProOptionTier tier)
    {
        var samples=s.SalarySamples.Where(h=>h.Year>=year-5&&h.Year<year).ToList();
        if(!samples.Any(h=>h.Year==s.Year)&&s.Year<year)samples.Add(Sample(s));
        if(samples.Count==0)samples.Add(Sample(s));
        var count=tier switch{ProOptionTier.Base=>23,ProOptionTier.PlayingTime=>18,ProOptionTier.OneProBowl=>10,_=>5};
        var skip=tier is ProOptionTier.Base or ProOptionTier.PlayingTime?2:0;
        var ratios=samples.Where(h=>h.LeagueCap>0&&h.Salaries.ContainsKey(group)).Select(h=>h.Salaries[group].OrderDescending().Skip(skip).Take(count).Select(v=>v/(double)h.LeagueCap).DefaultIfEmpty(0).Average()).ToArray();
        var amount=ratios.Length==0?0:ratios.Average()*ProFinance.ForecastCap(s,year);
        return new(year,group,tier,Math.Max(250_000,(long)Math.Ceiling(amount/250_000)*250_000),samples.Select(h=>h.Year).Distinct().Count()<5||ratios.Length<5);
    }
    public static ProFranchise Prepare(ProFranchise s)
    {
        var rows=s.OptionTenders.ToList();
        foreach(var p in Pending(s))
        {
            var c=s.Contracts.Last(c=>c.Active&&c.PlayerId==p.Football.PlayerId);var year=Origin(p,c)!.Value+3;var group=Group(p.Football.Position);
            foreach(var tier in Enum.GetValues<ProOptionTier>())if(!rows.Any(q=>q.DecisionYear==year&&q.PositionGroup==group&&q.Tier==tier))rows.Add(Calculate(s,year,group,tier));
        }
        return s with{OptionTenders=rows};
    }
    public static ProOptionTender Quote(ProFranchise s,ProPlayer p)
    {
        var c=s.Contracts.Last(c=>c.Active&&c.PlayerId==p.Football.PlayerId);var rookie=Origin(p,c)??throw new InvalidOperationException("Not an original first-round rookie contract.");
        var year=rookie+3;var tier=Tier(s,p,rookie);var group=Group(p.Football.Position);
        var quote=s.OptionTenders.LastOrDefault(q=>q.DecisionYear==year&&q.PositionGroup==group&&q.Tier==tier)??Calculate(s,year,group,tier);
        var minimum=ProFinance.Minimum(s,p,rookie+4,practice:false);
        return quote with{Salary=Math.Max(quote.Salary,(long)Math.Ceiling(minimum/250_000d)*250_000)};
    }
    public static ProFranchise Decide(ProFranchise s,string team,Guid id,bool exercise)
    {
        var p=Pending(s,team).SingleOrDefault(p=>p.Football.PlayerId==id)??throw new InvalidOperationException("This rookie option is unavailable or already decided.");
        s=Prepare(s);var c=s.Contracts.Last(c=>c.Active&&c.PlayerId==id&&c.Team==team);var rookie=Origin(p,c)!.Value;var q=Quote(s,p);var optionYear=rookie+4;
        if(exercise)
        {
            var changed=ProContractInsights.NormalizeVoidCharges(c with{OriginalRookieYear=rookie,ReportedAnnualValue=null,
                Years=c.Years.Where(y=>y.Year!=optionYear).Select(y=>y.Year==rookie+3?y with{Guaranteed=y.Salary}:y)
                    .Append(new ContractYear(optionYear,q.Salary,q.Salary){ProratedBonus=ProEconomy.BonusCharge(c,optionYear)}).OrderBy(y=>y.Year).ToList()});
            var proposed=s with{Contracts=s.Contracts.Select(x=>x==c?changed:x).ToList()};
            if(changed.Years.Where(y=>y.Year>=s.Year).Any(y=>ProFinance.Budget(proposed,team,y.Year).TotalCommitments+ProEconomy.PendingForYear(proposed,team,y.Year)>Math.Max(ProFinance.Limit(s,team,y.Year),ProFinance.Budget(s,team,y.Year).TotalCommitments+ProEconomy.PendingForYear(s,team,y.Year))))
                throw new InvalidOperationException("The option would exceed future cap space. Clear room or decline it.");
            s=proposed;
        }
        return ProEconomy.Log(s with{RookieOptionDecisions=s.RookieOptionDecisions.Append(new ProOptionDecision(id,rookie,q.DecisionYear,exercise,exercise?q.Salary:0,q.Tier){Team=team}).ToList()},
            team+(exercise?" exercised ":" declined ")+p.Football.FullName+"'s fifth-year option"+(exercise?$" for ${q.Salary:N0}, fully guaranteed.":"; the original four-year deal remains."));
    }
    public static ProFranchise ManageCpu(ProFranchise s,bool includeUser=false,string? onlyTeam=null)
    {
        s=Prepare(s);
        foreach(var p in Pending(s,onlyTeam).Where(p=>includeUser||p.Football.TeamName!=s.UserTeam||s.AutoManageRookieOptions||!ProCareer.Controls(s,p.Football.Position)).ToArray())
        {
            var exercise=p.Football.OverallRating>=73&&Quote(s,p).Salary<=ProEconomy.MarketSalary(s,p)*1.25;
            if(exercise)
            {
                try
                {
                    var preview=Decide(s,p.Football.TeamName,p.Football.PlayerId,true);var optionYear=Origin(p,s.Contracts.Last(c=>c.Active&&c.PlayerId==p.Football.PlayerId))!.Value+4;
                    exercise=ProFinance.Budget(preview,p.Football.TeamName,optionYear).TotalCommitments+ProEconomy.PendingForYear(preview,p.Football.TeamName,optionYear)+ProCpuBudget.DepthReserve(preview,p.Football.TeamName,optionYear)<=ProFinance.Limit(s,p.Football.TeamName,optionYear);
                }
                catch(InvalidOperationException){exercise=false;}
            }
            try
            {
                s=p.Football.TeamName==s.UserTeam&&ProCareer.Controls(s,p.Football.Position)
                    ? ProManagement.Submit(s,"Staff rookie option",[p.Football.PlayerId],0,x=>Decide(x,p.Football.TeamName,p.Football.PlayerId,exercise))
                    : Decide(s,p.Football.TeamName,p.Football.PlayerId,exercise);
                if(Pending(s,p.Football.TeamName).Any(x=>x.Football.PlayerId==p.Football.PlayerId))s=Decide(s,p.Football.TeamName,p.Football.PlayerId,false);
            }
            catch(InvalidOperationException){s=Decide(s,p.Football.TeamName,p.Football.PlayerId,false);}
        }
        return s;
    }
}
