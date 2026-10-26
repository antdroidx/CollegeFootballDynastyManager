namespace ProFootball.Core;
public static class ProRollover
{
    public static ProFranchise MeasureCompletedTeams(ProFranchise s)
    {
        if(s.CapRulesVersion<1)return s;
        foreach(var team in s.League.Teams)
        {
            if(s.RolloverElections.Any(e=>e.Team==team.Name&&e.Year==s.Year)||s.FinancialHistory.Any(h=>h.Team==team.Name&&h.Year==s.Year))continue;
            var games=s.Games.Where(g=>g.Phase==ProPhase.RegularSeason&&g.Football.InvolvesTeam(team.Name)).ToArray();
            if(games.Length==0||games.Any(g=>!g.Football.HasPlayed))continue;
            // Pay already-budgeted CPU season bonuses before measuring unused room.
            s=ProFinance.CpuBonuses(s,team.Name);
            var percentage=team.Name==s.UserTeam&&s.Career?.Unemployed!=true?Math.Clamp(s.CarryoverPercentage,0,100):100;
            var amount=(long)(Math.Max(0,ProEconomy.Room(s,team.Name))*percentage/100m);
            s=s with{RolloverElections=s.RolloverElections.Append(new(team.Name,s.Year,amount,percentage)).ToList()};
        }
        return s;
    }
}
