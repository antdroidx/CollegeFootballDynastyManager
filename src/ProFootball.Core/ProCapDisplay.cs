namespace ProFootball.Core;
/// <summary>One full-season budget for user-facing cap figures; temporary camp counting remains transaction-internal.</summary>
public static class ProCapDisplay
{
    public static ProCapBudget Budget(ProFranchise s,string team,int? year=null)=>ProFinance.Budget(s with{CapRulesVersion=1},team,year);
    public static long Room(ProFranchise s,string team)=>Budget(s,team).Available;
    public static string? ActionNeeded(ProFranchise s,string team)
    {
        var room=Room(s,team);
        return room<0?$"Clear ${-room:N0} before regular-season play. All current roster salaries are included.":null;
    }
}
