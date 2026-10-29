namespace ProFootball.Core;
public static class ProSeasonEligibility
{
    public static string? Issue(ProFranchise s,string team)
    {
        var problems=new List<string>();var room=ProEconomy.Room(s.Phase==ProPhase.Preseason?s with{Phase=ProPhase.RegularSeason}:s,team);var active=ProRoster.Active(s,team);
        if(room<0)problems.Add($"${-room:N0} over the salary cap");
        if(s.UnsignedRookies.Any(r=>r.Team==team))problems.Add("unsigned drafted rookies; clear cap/roster room and sign rookie contracts");
        if(active>53)problems.Add($"{active} active players; maximum 53");
        return problems.Count==0?null:team+": "+string.Join("; ",problems)+". Resolve contracts/cap and final roster cuts before regular-season play or advancement.";
    }
    public static void Require(ProFranchise s,string team)
    {if(Issue(s,team) is {} issue)throw new InvalidOperationException(issue);}
}
