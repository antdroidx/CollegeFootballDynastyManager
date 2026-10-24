using System.Security.Cryptography;
using System.Text;
using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public enum CoachTalkTone { Encourage, Praise, Challenge, StayComposed }
public sealed record CoachTalkReaction(Guid PlayerId, string PlayerName, int Change, string Response);
public sealed record CoachTalkRecord(string TeamName, CoachTalkTone Tone, int SeasonYear, int Week,
    int Quarter, int EventCount, IReadOnlyList<CoachTalkReaction> Reactions);

/// <summary>Coaching decisions never consume the play simulation's random sequence.</summary>
public static class GameDayCoachingService
{
    public static bool Substitute(GameDaySession session, string team, Guid playerId)
    {
        if (session.IsComplete || !session.PlayersById.TryGetValue(playerId, out var player) ||
            player.TeamName != team || player.IsRedshirted || player.CurrentInjury is not null) return false;
        if (team != session.Game.HomeTeamName && team != session.Game.AwayTeamName) return false;
        session.ManualStarters[(team, player.Position)] = playerId;
        return true;
    }

    public static SnapPersonnel ApplySubstitutions(GameDaySession session, SnapPersonnel unit)
    {
        var ids = unit.PlayerIds.ToArray();
        foreach (var ((team, position), id) in session.ManualStarters)
        {
            if (team != unit.TeamName || ids.Contains(id)) continue;
            var slot = Array.FindIndex(ids, current => session.PlayersById[current].Position == position);
            if (slot >= 0) ids[slot] = id;
        }
        return unit with { PlayerIds = ids };
    }

    public static int LivePlayerMorale(GameDaySession session, DynastyPlayer player, int seasonMorale)
    {
        var profile = player.TeamName == session.Game.HomeTeamName ? session.HomeProfile : session.AwayProfile;
        var teamChange = GameMoraleService.InGame(session, player.TeamName) - profile.Morale;
        var talks = session.CoachTalks.Where(t => t.TeamName == player.TeamName)
            .Sum(t => t.Reactions.FirstOrDefault(r => r.PlayerId == player.PlayerId)?.Change *
                Math.Max(0, 1 - (session.Events.Count - t.EventCount) / 24d) ?? 0);
        var events = session.Events.TakeLast(12);
        var personal = events.Sum(e => e.QuarterbackId == player.PlayerId && e.Outcome == SnapOutcome.Interception ? -3 :
            e.Points >= 6 && (e.BallCarrierId == player.PlayerId || e.TargetId == player.PlayerId || e.QuarterbackId == player.PlayerId) ? 2 : 0);
        return (int)Math.Round(Math.Clamp(seasonMorale + teamChange * .5 + talks + personal, 20, 85));
    }

    public static double IndividualTalkShift(GameDaySession session, IReadOnlyList<DynastyPlayer> unit)
    {
        if (unit.Count == 0) return 0;
        return Math.Clamp(unit.Average(p => session.CoachTalks.Where(t => t.Reactions.Count == 1)
            .Sum(t => (t.Reactions.FirstOrDefault(r => r.PlayerId == p.PlayerId)?.Change ?? 0) *
                Math.Max(0, 1 - (session.Events.Count - t.EventCount) / 24d))) / 5, -1, 1);
    }

    public static string LiveReason(GameDaySession s, DynastyPlayer p)
    {
        var reaction = s.CoachTalks.LastOrDefault(t => t.TeamName == p.TeamName && t.Reactions.Any(r => r.PlayerId == p.PlayerId) && s.Events.Count - t.EventCount < 24)?.Reactions.FirstOrDefault(r => r.PlayerId == p.PlayerId);
        if (reaction is not null) return reaction.Response;
        if (s.Events.TakeLast(12).Any(e => e.QuarterbackId == p.PlayerId && e.Outcome == SnapOutcome.Interception)) return "Recent interception";
        return GameMoraleService.InGame(s, p.TeamName) > (p.TeamName == s.Game.HomeTeamName ? s.HomeProfile : s.AwayProfile).Morale ? "Positive team momentum" : "Recent form and team momentum";
    }

    public static CoachTalkRecord CreateTalk(IReadOnlyList<DynastyPlayer> roster, string team, CoachTalkTone tone,
        int year, int week, int quarter, int eventCount, int context, int repetition)
    {
        var reactions = roster.Where(p => p.TeamName == team).Select(p =>
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{p.PlayerId}:{year}:{week}:{quarter}:{tone}:{eventCount}"));
            var disposition = bytes[0] % 9 - 4;
            var leadership = p.Skills?.Leadership ?? 60;
            var fit = tone switch
            {
                CoachTalkTone.Praise => context >= 0 ? 2 : -3,
                CoachTalkTone.Challenge => leadership >= 65 ? 2 : -2,
                CoachTalkTone.Encourage => context < 0 ? 3 : 1,
                _ => leadership < 65 ? 2 : 1
            };
            var change = Math.Clamp(disposition + fit - repetition * 2, -5, 5);
            var response = change > 0 ? tone == CoachTalkTone.Challenge ? "Motivated" : "Reassured" :
                change < 0 ? tone == CoachTalkTone.Praise ? "Praise felt undeserved" : "Felt pressured or unfairly criticized" : "No response";
            return new CoachTalkReaction(p.PlayerId, p.FullName, change, response);
        }).ToArray();
        return new(team, tone, year, week, quarter, eventCount, reactions);
    }

    public static CoachTalkRecord? Talk(GameDaySession s, string team, CoachTalkTone tone, Guid? playerId = null)
    {
        if (team != s.Game.HomeTeamName && team != s.Game.AwayTeamName) return null;
        var quarter = s.IsComplete ? 99 : s.Events.Count == 0 ? 0 : s.Quarter;
        var previous = s.CoachTalks.Where(t => t.TeamName == team &&
            (playerId.HasValue ? t.Reactions.Count == 1 && t.Reactions[0].PlayerId == playerId : t.Reactions.Count > 1)).ToArray();
        if (previous.Any(t => t.Quarter == quarter && (quarter is 0 or 99 || s.Events.Count - t.EventCount < 8)) ||
            quarter is not (0 or 99) && previous.Count(t => t.Quarter is not (0 or 99) && (t.Quarter <= 2) == (quarter <= 2)) >= 2) return null;
        var lead = team == s.Game.HomeTeamName ? s.HomeScore - s.AwayScore : s.AwayScore - s.HomeScore;
        var talk = CreateTalk(playerId.HasValue ? s.Roster.Where(p => p.PlayerId == playerId).ToArray() : s.Roster, team, tone, s.Game.SeasonYear, s.Game.Week, quarter,
            s.Events.Count, lead, previous.Count(t => t.Tone == tone));
        if (talk.Reactions.Count == 0) return null;
        s.CoachTalks.Add(talk);
        return talk;
    }
}
