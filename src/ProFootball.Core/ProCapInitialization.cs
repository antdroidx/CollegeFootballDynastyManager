using System.Globalization;
using System.Reflection;
namespace ProFootball.Core;
public sealed record ProTeamCapAllocation(int Year,string TeamAbbreviation,long BaseCap,long Rollover,long OtherAdjustments,long AdjustedCap);
public sealed record ProCapBudget(long BaseCap,long Rollover,long OtherAdjustments,long AdjustedCap,long CommittedSalary,long AllocatedBonus,long OtherCapCharges,long DeadMoney,long RolloverReserved)
{
    public long OffseasonSalaryExcluded {get;init;}
    public long InternationalPracticeSalaryExcluded {get;init;}
    public long RosterSalary=>checked(CommittedSalary+OffseasonSalaryExcluded+InternationalPracticeSalaryExcluded);
    public long PlayerCommitments=>checked(CommittedSalary+AllocatedBonus);
    public long TotalCommitments=>checked(PlayerCommitments+OtherCapCharges+DeadMoney+RolloverReserved);
    public long Available=>checked(AdjustedCap-TotalCommitments);
}
public sealed record ProRolloverElection(string Team,int Year,long Amount,int Percentage);
public static class ProCapInitialization
{
    public static string Canonical(string abbreviation)=>abbreviation.Trim().ToUpperInvariant() switch {"WSH"=>"WAS",var value=>value};
    public static IReadOnlyList<ProTeamCapAllocation> Load(ProLeague league)
    {
        return LoadResource("nfl_2026_salary_caps.csv",league);
    }
    public static IReadOnlyList<ProTeamCapAllocation> LoadOriginal(ProLeague league)=>LoadResource("nfl_2026_salary_caps.user-original.csv",league);
    static IReadOnlyList<ProTeamCapAllocation> LoadResource(string name,ProLeague league)
    {
        using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("ProFootball.Core."+name)??throw new InvalidDataException("Missing 2026 cap data.");
        using var reader=new StreamReader(stream);return Parse(reader.ReadToEnd(),league);
    }
    public static IReadOnlyList<ProTeamCapAllocation> Parse(string csv,ProLeague league)
    {
        var lines=csv.TrimStart('\uFEFF').Split('\n',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
        const string header="team_abbr,team_name,base_cap_2026,rollover_from_2025,other_adjustments,adjusted_cap_2026";
        if(lines.Length!=33||lines[0]!=header)throw new InvalidDataException("The cap file must have its six named columns and exactly 32 team rows.");
        var expected=league.Teams.Select(t=>Canonical(t.Abbreviation)).ToHashSet(StringComparer.Ordinal);var seen=new HashSet<string>();var rows=new List<ProTeamCapAllocation>();
        if(expected.Count!=32)throw new InvalidDataException("The league must contain 32 unique NFL abbreviations.");
        foreach(var line in lines.Skip(1))
        {
            var fields=line.Split(',').Select(v=>v.Trim()).ToArray();if(fields.Length!=6)throw new InvalidDataException("Invalid cap row.");
            var team=Canonical(fields[0]);if(!expected.Contains(team)||!seen.Add(team))throw new InvalidDataException("Unknown or duplicate cap team: "+team);
            long Amount(int column)=>long.TryParse(fields[column],NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out var value)?value:throw new InvalidDataException("Cap amounts must be integer dollars.");
            var baseCap=Amount(2);var rollover=Amount(3);var adjustments=Amount(4);var adjusted=Amount(5);
            if(baseCap!=ProFinance.Base2026||rollover<0||adjusted<=0||checked(baseCap+rollover+adjustments)!=adjusted)throw new InvalidDataException("Cap amounts do not reconcile for "+team);
            rows.Add(new(2026,team,baseCap,rollover,adjustments,adjusted));
        }
        if(!expected.SetEquals(seen))throw new InvalidDataException("Missing NFL cap teams.");return rows;
    }
    public static ProFranchise Initialize(ProFranchise s)
    {
        if(s.Year!=2026||s.TeamCapAllocations.Count>0)return s;
        return s with{CapRulesVersion=3,SalaryCap=ProFinance.Base2026,TeamCapAllocations=Load(s.League).ToList()};
    }
    public static bool CanCorrectOpeningBudgets(ProFranchise s) => s.Year==2026&&s.CapRulesVersion==2&&s.SalaryCap==ProFinance.Base2026
        &&!s.FinancialHistory.Any(h=>h.Year==2026)&&s.TeamCapAllocations.Count==32
        &&LoadOriginal(s.League).All(a=>s.TeamCapAllocations.Contains(a));
    public static ProFranchise CorrectOpeningBudgets(ProFranchise s)
    {
        if(!CanCorrectOpeningBudgets(s))throw new InvalidOperationException("Only unchanged 2026 opening cap budgets can be updated before the year closes.");
        return s with{CapRulesVersion=3,TeamCapAllocations=Load(s.League).ToList()};
    }
    public static long RoundForDisplay(long dollars)=>checked((long)Math.Round(dollars/250_000m,0,MidpointRounding.AwayFromZero)*250_000);
    public static ProTeamCapAllocation? Allocation(ProFranchise s,string team,int year)
    {
        var abbreviation=Canonical(s.League.Teams.Single(t=>t.Name==team).Abbreviation);
        return s.TeamCapAllocations.SingleOrDefault(a=>a.Year==year&&Canonical(a.TeamAbbreviation)==abbreviation);
    }
}
