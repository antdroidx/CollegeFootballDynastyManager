using System.Reflection;
using System.Text.Json;
namespace ProFootball.Core;
internal static class ProReviewedOpeningRoster
{
    sealed record Released(Guid PlayerId,string Team,string SourceUrl);
    sealed record International(Guid PlayerId,string Team,int Year,string SourceUrl);
    sealed record ContractReview(ProContract Contract,bool PracticeSquad,string SourceUrl,string SourceSha256,long ExpectedCap,string ExpectedRosterTeam,string? OwnershipSourceUrl);
    sealed record Pack(int Version,string ObservedOn,List<Released> ReleasedPlayers,List<International> InternationalPracticePlayers,List<ContractReview> Contracts);
    internal static ProFranchise InitializeNew(ProFranchise s)
    {
        using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("ProFootball.Core.pro-reviewed-opening-roster.json")!;
        var pack=JsonSerializer.Deserialize<Pack>(stream)!;
        if(pack.Version!=1||pack.ObservedOn!=s.OpeningFinancialSnapshot)throw new InvalidDataException("Opening roster evidence date differs");
        foreach(var released in pack.ReleasedPlayers)
        {
            var p=s.Players.Single(p=>p.Football.PlayerId==released.PlayerId);
            if(p.Football.TeamName!=released.Team||s.Contracts.Any(c=>c.PlayerId==released.PlayerId&&(!c.Estimated||c.LastChangedYear!=0)))
                throw new InvalidDataException("Reviewed released-player factory baseline differs");
            s=s with{Contracts=s.Contracts.Where(c=>c.PlayerId!=released.PlayerId).ToList(),Players=s.Players.Select(x=>x==p?p with{Football=p.Football with{TeamName="Free agent"},PracticeSquad=false,Reserve=false,RosterStatus="FA"}:x).ToList()};
        }
        foreach(var review in pack.Contracts)
        {
            var c=review.Contract;var p=s.Players.Single(p=>p.Football.PlayerId==c.PlayerId);
            if(p.Football.TeamName!=review.ExpectedRosterTeam||(p.Football.TeamName!=c.Team&&review.OwnershipSourceUrl is null)||review.SourceSha256.Length!=64||ProEconomy.CapHit(c,2026)!=review.ExpectedCap||c.BonusCash!.Values.Any(v=>v!=0)||c.Years.Any(y=>ProEconomy.BonusCharge(c,y.Year)!=0))
                throw new InvalidDataException("Current-club source review differs");
            s=s with{Contracts=s.Contracts.Where(x=>x.PlayerId!=c.PlayerId).Append(c).ToList(),Players=s.Players.Select(x=>x==p?p with{Football=p.Football with{TeamName=c.Team},PracticeSquad=review.PracticeSquad,Reserve=false,RosterStatus=review.PracticeSquad?"PRA":"ACT"}:x).ToList()};
        }
        foreach(var d in pack.InternationalPracticePlayers)
        {
            var p=s.Players.Single(p=>p.Football.PlayerId==d.PlayerId);
            if(p.Football.TeamName!=d.Team||!p.PracticeSquad||d.Year!=2026||s.InternationalPracticeDesignations.Any(x=>x.Team==d.Team&&x.Year==d.Year))
                throw new InvalidDataException("Invalid international practice designation");
            s=s with{InternationalPracticeDesignations=s.InternationalPracticeDesignations.Append(new(d.Team,d.Year,d.PlayerId)).ToList(),Players=s.Players.Select(x=>x==p?p with{Reserve=false}:x).ToList()};
        }
        return s;
    }
}
