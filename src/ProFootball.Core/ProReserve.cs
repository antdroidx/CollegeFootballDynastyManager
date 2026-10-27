namespace ProFootball.Core;

/// <summary>Activation of the existing imported reserve pool; no source medical dates are inferred.</summary>
public static class ProReserve
{
    public static string? ActivationReason(ProFranchise s,string team,Guid id)
    {
        var p=s.Players.FirstOrDefault(p=>p.Football.PlayerId==id);
        if(p is null||p.Football.TeamName!=team)return "This player does not belong to this team.";
        if(p.Retired)return "Retired players cannot be activated.";
        if(!p.Reserve)return "This player is not on the reserve list.";
        if(s.UnsignedRookies.Any(r=>r.PlayerId==id))return "Sign this rookie's reserved contract before activation.";
        if(p.Football.CurrentInjury is {WeeksRemaining:>0} injury)return $"Not medically available: {injury.WeeksRemaining} weeks of recovery remain.";
        var status=p.RosterStatus.Trim().ToUpperInvariant();
        // These source codes do not establish reinstatement or a completed return window.
        if(status is not ("" or "RES" or "INA" or "DEV"))return "League clearance or reserve-return eligibility must be established for status "+status+".";
        var c=s.Contracts.LastOrDefault(c=>c.Active&&c.PlayerId==id&&c.Team==team);
        if(c is null||!c.Years.Any(y=>y.Year==s.Year&&!y.IsVoid))return "A signed playing contract for this season is required.";
        if(ProRoster.CountForLimit(s,team)>=ProRoster.Limit(s))return ProRoster.IsCamp(s)?"The 90-player camp roster is full. Make room before activation.":"The 53-player active roster is full. Make room before activation.";
        var next=Proposal(s,p,c);
        if(ProEconomy.Room(next,team)-ProEconomy.Pending(next,team)<0)return "Insufficient cap space after activation and pending offers. Make room before activation.";
        return null;
    }
    static ProFranchise Proposal(ProFranchise s,ProPlayer p,ProContract c)
    {
        var id=p.Football.PlayerId;
        var rows=c.Years.Select(y=>y.Year==s.Year?y with{Salary=Math.Max(y.Salary,ProFinance.ContractMinimum(s,p,c,y.Year))}:y).ToList();
        return s with{InternationalPracticeDesignations=s.InternationalPracticeDesignations.Where(d=>d.PlayerId!=id).ToList(),Players=s.Players.Select(x=>x.Football.PlayerId==id?x with{Reserve=false,PracticeSquad=false,RosterStatus="ACT",Football=x.Football with{DepthChartOrder=99}}:x).ToList(),Contracts=s.Contracts.Select(x=>x==c?c with{Years=rows}:x).ToList()};
    }
    public static ProFranchise Activate(ProFranchise s,string team,Guid id)
    {
        if(ActivationReason(s,team,id) is {} reason)throw new InvalidOperationException(reason);
        var p=s.Players.Single(p=>p.Football.PlayerId==id);
        var c=s.Contracts.Last(c=>c.Active&&c.PlayerId==id&&c.Team==team);
        var next=Proposal(s,p,c);
        return ProEconomy.Log(next,$"{team} activated {p.Football.FullName} from reserve.");
    }
}
