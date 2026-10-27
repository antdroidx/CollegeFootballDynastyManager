namespace ProFootball.Core;
/// <summary>Only an explicitly verified club/year designation excludes weekly practice pay.</summary>
public static class ProInternationalPractice
{
    public static bool Exempt(ProFranchise s,string team,Guid id,int year)=>s.InternationalPracticeDesignations.Any(d=>d.Team==team&&d.PlayerId==id&&d.Year==year)
        &&s.Players.Any(p=>p.Football.PlayerId==id&&p.Football.TeamName==team&&p.PracticeSquad&&!p.Reserve&&!p.Retired);
    public static long ContractExcludedSalary(ProFranchise s,ProContract c,int year)
    {
        var row=c.Years.FirstOrDefault(y=>y.Year==year);
        return c.Active&&Exempt(s,c.Team,c.PlayerId,year)&&row is not null&&!row.IsVoid?Math.Max(0,row.Salary+Math.Min(0,row.OtherCompensation)):0;
    }
    public static long ExcludedSalary(ProFranchise s,string team,int year)=>s.Contracts.Where(c=>c.Active&&c.Team==team&&Exempt(s,team,c.PlayerId,year))
        .Sum(c=>ContractExcludedSalary(s,c,year));
    public static ProFranchise End(ProFranchise s,Guid id)=>s with{InternationalPracticeDesignations=s.InternationalPracticeDesignations.Where(d=>d.PlayerId!=id).ToList()};
    public static int OrdinaryPlayers(ProFranchise s,string team)=>s.Players.Count(p=>p.Football.TeamName==team&&p.PracticeSquad&&!p.Retired&&!Exempt(s,team,p.Football.PlayerId,s.Year));
}
