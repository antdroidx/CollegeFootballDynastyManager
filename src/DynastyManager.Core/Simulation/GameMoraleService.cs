using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class GameMoraleService
{
    public static int SeasonMorale(DynastyState state, string team)
    {
        var games = state.Schedule.Where(g => g.SeasonYear == state.SeasonYear && g.HasPlayed && g.InvolvesTeam(team))
            .OrderByDescending(g => g.Week).ThenBy(g => g.GameId).Take(4).ToArray();
        var form = games.Sum(g => g.WinnerTeamName == team ? 5 : -5);
        var coach = state.Staff.FirstOrDefault(s => s.TeamName == team && s.Role == StaffRole.HeadCoach);
        return Math.Clamp(50 + form + ((coach?.Leadership ?? 60) - 60) / 12, 25, 75);
    }
    public static int TrainingModifier(int morale) => morale >= 65 ? 1 : morale <= 35 ? -1 : 0;
    public static int InGame(GameDaySession s, string team)
    {
        var profile = team == s.Game.HomeTeamName ? s.HomeProfile : s.AwayProfile;
        var change = 0d;
        foreach (var e in s.Events.TakeLast(8))
        {
            var scoring = e.Outcome == SnapOutcome.Touchdown || e.Points >= 6;
            var turnover = e.Outcome is SnapOutcome.Interception or SnapOutcome.Fumble or SnapOutcome.Turnover;
            var delta = scoring ? 5 : turnover ? -4 : e.IsPassBreakup || e.Outcome == SnapOutcome.Sack ? -1.5 : e.Yards >= 20 ? 2 : 0;
            change += e.OffenseTeamName == team ? delta : -delta;
        }
        var steadiness = Math.Clamp(1.2 - profile.HeadCoachLeadership * .005, .65, 1);
        return (int)Math.Round(Math.Clamp(profile.Morale + Math.Clamp(change * steadiness, -12, 12), 20, 80));
    }
    public static double MatchupShift(GameDaySession s, string offense, string defense) => Math.Clamp((InGame(s,offense)-InGame(s,defense))/20d,-3,3);
    public static string Label(int value) => value >= 65 ? "Confident" : value <= 35 ? "Low" : "Steady";
}
