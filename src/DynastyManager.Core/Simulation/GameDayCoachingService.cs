using System.Security.Cryptography;
using System.Text;
using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public enum CoachTalkAudience { Team, Offense, Defense }
public enum CoachTalkTone { Encourage, Praise, Challenge, StayComposed }
public sealed record CoachTalkReaction(Guid PlayerId, string PlayerName, int Change, string Response);
public sealed record CoachTalkRecord(string TeamName, CoachTalkTone Tone, int SeasonYear, int Week,
    int Quarter, int EventCount, IReadOnlyList<CoachTalkReaction> Reactions)
{
    public CoachTalkAudience Audience { get; init; } = CoachTalkAudience.Team;
}

/// <summary>Coaching decisions never consume the play simulation's random sequence.</summary>
public static class GameDayCoachingService
{
    public static IReadOnlySet<Guid> Starters(GameDaySession s, string team)
    {
        var depth = team == s.Game.HomeTeamName ? s.HomeDepthChart : s.AwayDepthChart;
        var ids = new HashSet<Guid>();
        foreach (var (position, pool) in depth)
        {
            var count = position switch { Position.OL => 5, Position.RB or Position.WR or Position.DE or Position.DT or Position.OLB or Position.CB => 2, _ => 1 };
            ids.UnionWith(position == Position.OL ? OffensiveLineService.Lineup(pool).Select(p => p.PlayerId) : pool.Take(count).Select(p => p.PlayerId));
        }
        foreach (var offense in new[] { true, false })
        {
            var snap = s.Events.LastOrDefault(e => GameDayEngine.IsScrimmage(e) && (offense ? e.OffenseTeamName : e.DefenseTeamName) == team);
            if (snap is null) continue;
            var active = offense ? snap.OffensePersonnel : snap.DefensePersonnel;
            if (active.Count == 0) continue;
            ids.RemoveWhere(id => IsOffense(s.PlayersById[id].Position) == offense && s.PlayersById[id].Position != Position.K);
            ids.UnionWith(active);
        }
        return ApplySubstitutions(s, new SnapPersonnel { TeamName = team, PlayerIds = ids.ToArray() }).PlayerIds.ToHashSet();
    }

    public static bool IsOffense(Position position) => position is Position.QB or Position.RB or Position.WR or Position.TE or Position.OL;

    public static string TalkAvailability(GameDaySession s, string team)
    {
        if (team != s.Game.HomeTeamName && team != s.Game.AwayTeamName) return "No team selected";
        var stage = s.IsComplete ? 99 : s.Events.Count == 0 ? 0 : s.Quarter;
        var last = s.Events.LastOrDefault();
        var window = stage is 0 or 99 || last?.TimeoutTeamName is not null || last is not null && last.Quarter < s.Quarter;
        if (!window) return "Available at timeouts and quarter breaks";
        if (s.CoachTalks.Any(t => t.TeamName == team && t.EventCount == s.Events.Count && t.Quarter == stage)) return "Talk used for this break";
        if (stage is not (0 or 99) && s.CoachTalks.Count(t => t.TeamName == team && t.Quarter is not (0 or 99) && (t.Quarter <= 2) == (stage <= 2)) >= 2)
            return "Two talks used this half";
        return "Available";
    }

    public static bool UseSecondUnit(GameDaySession s, string team, bool enabled)
    {
        if (s.IsComplete || (team != s.Game.HomeTeamName && team != s.Game.AwayTeamName)) return false;
        foreach (var key in s.ManualStarters.Keys.Where(k => k.Team == team).ToArray()) s.ManualStarters.Remove(key);
        if (enabled) s.SecondUnits.Add(team); else s.SecondUnits.Remove(team);
        return true;
    }

    public static bool Substitute(GameDaySession session, string team, Guid playerId)
    {
        if (session.IsComplete || !session.PlayersById.TryGetValue(playerId, out var player) ||
            player.TeamName != team || player.IsRedshirted || player.CurrentInjury is not null) return false;
        if (team != session.Game.HomeTeamName && team != session.Game.AwayTeamName) return false;
        session.ManualStarters[(team, player.Position)] = playerId;
        return true;
    }

    public static DynastyPlayer? Specialist(GameDaySession s, string team, DynastyPlayer? fallback)
    {
        if (s.ManualStarters.TryGetValue((team, Position.K), out var id) && s.PlayersById.TryGetValue(id, out var player)) return player;
        if (s.SecondUnits.Contains(team))
        {
            var depth = team == s.Game.HomeTeamName ? s.HomeDepthChart : s.AwayDepthChart;
            return depth.GetValueOrDefault(Position.K)?.FirstOrDefault(p => p.PlayerId != fallback?.PlayerId &&
                (p.NcaaPositionCode ?? 19) == (fallback?.NcaaPositionCode ?? 19)) ?? fallback;
        }
        return fallback;
    }

    public static SnapPersonnel ApplySubstitutions(GameDaySession session, SnapPersonnel unit)
    {
        var ids = unit.PlayerIds.ToArray();
        if (session.SecondUnits.Contains(unit.TeamName))
        {
            var depth = unit.TeamName == session.Game.HomeTeamName ? session.HomeDepthChart : session.AwayDepthChart;
            foreach (var group in unit.PlayerIds.GroupBy(id => session.PlayersById[id].Position))
            {
                if (!depth.TryGetValue(group.Key, out var pool)) continue;
                var slots = group.Select(id => Array.IndexOf(ids, id)).ToArray();
                var first = group.Key == Position.OL ? OffensiveLineService.Lineup(pool).Select(p => p.PlayerId).ToHashSet() : pool.Take(slots.Length).Select(p => p.PlayerId).ToHashSet();
                var reserves = pool.Where(p => !first.Contains(p.PlayerId)).ToList();
                var used = new HashSet<Guid>();
                foreach (var slot in slots)
                {
                    var original = session.PlayersById[ids[slot]];
                    bool Fits(DynastyPlayer p) => !used.Contains(p.PlayerId) &&
                        (group.Key != Position.OL || OffensiveLineService.Label(p) == OffensiveLineService.Label(original));
                    var next = reserves.FirstOrDefault(Fits) ?? pool.FirstOrDefault(p => first.Contains(p.PlayerId) && Fits(p)) ?? pool.FirstOrDefault(Fits);
                    if (next is null) continue;
                    ids[slot] = next.PlayerId; used.Add(next.PlayerId); reserves.Remove(next);
                }
            }
        }
        foreach (var ((team, position), id) in session.ManualStarters)
        {
            if (team != unit.TeamName || ids.Contains(id)) continue;
            var slot = Array.FindIndex(ids, current => session.PlayersById[current].Position == position && (position != Position.OL ||
                OffensiveLineService.Label(session.PlayersById[current]) == OffensiveLineService.Label(session.PlayersById[id])));
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
        return Math.Clamp(unit.Average(p => session.CoachTalks.Where(t => t.Audience != CoachTalkAudience.Team || t.Reactions.Count == 1)
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

    public static CoachTalkRecord? Talk(GameDaySession s, string team, CoachTalkTone tone, CoachTalkAudience audience = CoachTalkAudience.Team)
    {
        if (TalkAvailability(s, team) != "Available") return null;
        var quarter = s.IsComplete ? 99 : s.Events.Count == 0 ? 0 : s.Quarter;
        var previous = s.CoachTalks.Where(t => t.TeamName == team).ToArray();
        var roster = s.Roster.Where(p => audience == CoachTalkAudience.Team ||
            (audience == CoachTalkAudience.Offense ? IsOffense(p.Position) : !IsOffense(p.Position) && p.Position != Position.K)).ToArray();
        var lead = team == s.Game.HomeTeamName ? s.HomeScore - s.AwayScore : s.AwayScore - s.HomeScore;
        var talk = CreateTalk(roster, team, tone, s.Game.SeasonYear, s.Game.Week, quarter,
            s.Events.Count, lead, previous.Count(t => t.Tone == tone)) with { Audience = audience };
        if (talk.Reactions.Count == 0) return null;
        s.CoachTalks.Add(talk); return talk;
    }
}
