using System.Security.Cryptography;
using DynastyManager.Core.Models;
using ProFootball.Core;

// Developer-only calibration evidence; never changes contracts, franchises or market behavior.
static class MarketAudit
{
    sealed record Sample(string Name,string Team,string Position,int Overall,int Age,int SignedYear,int? RookieYear,long AnnualValue,long CurrentEstimate,string Source);
    public static object Create(ProFranchise state)
    {
        var players=state.Players.ToDictionary(p=>p.Football.PlayerId);
        var excluded=new Dictionary<string,int>();var samples=new List<Sample>();
        void Exclude(string reason)=>excluded[reason]=excluded.GetValueOrDefault(reason)+1;
        foreach(var c in state.Contracts)
        {
            if(c.Estimated||c.Source?.StartsWith("OverTheCap")!=true){Exclude("generated_or_unsourced");continue;}
            if(!c.Active||!players.TryGetValue(c.PlayerId,out var p)||p.Retired||p.Football.TeamName!=c.Team){Exclude("inactive_or_owner_mismatch");continue;}
            if(p.PracticeSquad){Exclude("practice_squad");continue;}
            // A new veteran extension can be signed before the original rookie term ends.
            // Exclude original drafted/UDFA deals, rather than using age alone.
            var originalRookie=p.RookieYear is int rookie?c.SignedYear<=rookie: p.Experience<4;
            if(originalRookie){Exclude("original_rookie_or_unknown_young_term");continue;}
            if(c.SignedYear<state.Year-3||c.SignedYear>state.Year||ProContractInsights.PlayingEnd(c)<state.Year){Exclude("outside_recent_playing_window");continue;}
            if(c.ReportedAnnualValue is not >0){Exclude("missing_reported_annual_value");continue;}
            var position=p.Football.Position==Position.K&&p.Football.NcaaPositionCode is int code?"K/"+code:p.Football.Position.ToString();
            samples.Add(new(p.Football.FullName,c.Team,position,p.Football.OverallRating,p.Age,c.SignedYear,p.RookieYear,c.ReportedAnnualValue.Value,ProEconomy.MarketSalary(state,p),c.Source));
        }
        double Median(IEnumerable<double> source)
        {
            var a=source.Order().ToArray();return a.Length==0?0:a.Length%2==1?a[a.Length/2]:(a[a.Length/2-1]+a[a.Length/2])/2;
        }
        object Summary(IEnumerable<Sample> source)
        {
            var rows=source.ToArray();
            return new{count=rows.Length,medianOverall=Median(rows.Select(x=>(double)x.Overall)),medianReportedAnnualValue=Median(rows.Select(x=>(double)x.AnnualValue)),medianExistingEstimate=Median(rows.Select(x=>(double)x.CurrentEstimate)),medianReportedToEstimate=Median(rows.Select(x=>x.AnnualValue/(double)Math.Max(1,x.CurrentEstimate))),minAnnualValue=rows.Min(x=>x.AnnualValue),maxAnnualValue=rows.Max(x=>x.AnnualValue)};
        }
        string Hash(string resource)
        {
            using var stream=typeof(ProContractData).Assembly.GetManifestResourceStream("ProFootball.Core."+resource)!;
            return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        }
        return new{year=state.Year,baseCap=ProFinance.Base2026,ratingVersion=state.RatingSystemVersion,
            contractPackSha256=Hash("nfl-contracts.json"),rosterPackSha256=Hash("nfl-snapshot.json"),
            scope="Read-only market calibration of sourced reported annual values against canonical game ratings. These are contract headline averages, not current cap hits, remaining cash, verified guarantees or opening liabilities. No salary/cap/contract/save is changed.",
            selection="Same-team sourced active veteran deals signed within three seasons, playing in2026, positive reported annual value. Original rookie/UDFA deals, practice squad and generated contracts excluded. New extensions after rookie signing year retained. No normalization to historical signing-year cap; all estimates use the2026 base for a same-year comparison. Sparse samples require judgment before fitting a curve.",
            totalSamples=samples.Count,excluded,
            positions=samples.GroupBy(x=>x.Position).OrderBy(g=>g.Key).Select(g=>new{position=g.Key,summary=Summary(g),ratingBands=g.GroupBy(x=>x.Overall/10*10).OrderBy(x=>x.Key).Select(b=>new{minimumOverall=b.Key,maximumOverall=b.Key+9,summary=Summary(b)}),players=g.OrderByDescending(x=>x.Overall).ThenBy(x=>x.Name).ToArray()}).ToArray()};
    }
}
