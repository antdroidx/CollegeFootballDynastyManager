using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class GameMoraleService
{
    public static int SeasonMorale(DynastyState state, string team)
    {
        var games = state.Schedule.Where(g => g.SeasonYear == state.SeasonYear && g.HasPlayed && g.InvolvesTeam(team))
            .OrderByDescending(g => g.Week).ThenBy(g => g.GameId).Take(4).ToArray();
        var form = games.Sum(g => g.WinnerTeamName == team ? 6 : -4);
        var recovery = games.Take(2).Sum(g => ((g.HomeTeamName == team ? g.HomeEndingMorale : g.AwayEndingMorale) - 50) / 8);
        var talks = state.CoachTalks.Concat(state.Schedule.Where(g => g.HasPlayed && g.SeasonYear == state.SeasonYear).SelectMany(g => g.CoachTalks.Where(t => t.Quarter == 99 && t.Reactions.Count > 1)))
            .Where(t => t.TeamName == team && t.SeasonYear == state.SeasonYear && t.Week <= state.Week && state.Week - t.Week < 3)
            .Sum(t => t.Reactions.Select(r => (double)r.Change).DefaultIfEmpty().Average() * (3 - (state.Week - t.Week)) / 3);
        var coach = state.Staff.FirstOrDefault(s => s.TeamName == team && s.Role == StaffRole.HeadCoach);
        return Math.Clamp(50 + form + recovery + (int)Math.Round(talks) + ((coach?.Leadership ?? 60) - 60) / 12, 25, 75);
    }
    public static int PlayerMorale(DynastyState state, DynastyPlayer player)
    {
        var recent = state.Schedule.Where(g => g.SeasonYear == state.SeasonYear && g.HasPlayed && g.InvolvesTeam(player.TeamName))
            .OrderByDescending(g => g.Week).Take(4).SelectMany(g => g.PlayerStats)
            .Where(p => p.PlayerId == player.PlayerId).ToArray();
        var success = recent.Sum(p => p.PassingTouchdowns + p.RushingTouchdowns + p.ReceivingTouchdowns + p.Interceptions + p.Sacks);
        var setbacks = recent.Sum(p => p.InterceptionsThrown + p.RushingFumbles + p.ReceivingFumbles);
        var steadiness = Math.Clamp((player.Skills?.Leadership ?? 60) / 100d, .4, .95);
        var talkEffect = state.CoachTalks.Concat(state.Schedule.Where(g => g.HasPlayed && g.SeasonYear == state.SeasonYear).SelectMany(g => g.CoachTalks))
            .Where(t => t.TeamName == player.TeamName && t.SeasonYear == state.SeasonYear && t.Week <= state.Week && state.Week - t.Week < 3)
            .Sum(t => (t.Reactions.FirstOrDefault(r => r.PlayerId == player.PlayerId)?.Change ?? 0) * (3 - (state.Week - t.Week)) / 3d);
        var personal = Math.Clamp(success - setbacks * 2, -6, 6) * (1 - steadiness * .4);
        return (int)Math.Round(Math.Clamp(50 + (SeasonMorale(state, player.TeamName) - 50) * .65 + personal + Math.Clamp(talkEffect, -8, 8) -
            (player.CurrentInjury is null ? 0 : 6), 25, 80));
    }

    public static int SeasonDelta(DynastyState state, DynastyPlayer player) =>
        player.RatingBaselineSeason == state.SeasonYear && player.SeasonStartOverall > 0 ? player.OverallRating - player.SeasonStartOverall : 0;

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
            var beneficiary = e.DefensiveScore ? e.DefenseTeamName : e.OffenseTeamName;
            change += beneficiary == team ? delta : -delta;
        }
        change += s.CoachTalks.Where(t => t.TeamName == team && t.Audience == CoachTalkAudience.Team && t.Reactions.Count > 1).Sum(t => t.Reactions.Select(r => (double)r.Change).DefaultIfEmpty().Average() * Math.Max(0, 1 - (s.Events.Count - t.EventCount) / 24d));
        var steadiness = Math.Clamp(1.2 - profile.HeadCoachLeadership * .005, .65, 1);
        return (int)Math.Round(Math.Clamp(profile.Morale + Math.Clamp(change * steadiness, -12, 12), 20, 80));
    }
    public static double MatchupShift(GameDaySession s, string offense, string defense) => Math.Clamp((InGame(s,offense)-InGame(s,defense))/20d,-3,3);
    public static string Label(int value) => value >= 65 ? "Confident" : value <= 35 ? "Low" : "Steady";
}
