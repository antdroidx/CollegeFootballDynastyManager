using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class CoachCareerService
{
    // Call before the offseason carousel: incoming coaches must not inherit the outgoing staff's season.
    public static DynastyState RecordCompletedSeason(DynastyState state)
    {
        if (state.CoachHistoryLastRecordedSeasonYear >= state.SeasonYear) return state;
        var seasons = state.TeamSeasonHistory.Where(item => item.SeasonYear == state.SeasonYear)
            .ToDictionary(item => item.TeamName, StringComparer.OrdinalIgnoreCase);
        if (seasons.Count == 0) return state;
        var history = state.CoachSeasonHistory.ToList();
        var keys = history.Select(item => (item.StaffId, item.SeasonYear, item.TeamName, item.Role)).ToHashSet();
        var staff = state.Staff.Where(item => !(item.Role == state.UserCoachRole && state.UserHeadCoach is not null &&
            item.TeamName.Equals(state.UserTeamName, StringComparison.OrdinalIgnoreCase)))
            .Select(item => StaffManagementService.GetStaff(state, item.TeamName, item.Role) ?? item).ToList();
        if (state.UserHeadCoach is not null && !state.UserCoachIsFired) staff.Add(state.UserHeadCoach);
        // A team's first snapshot owns the year; replaying after a carousel cannot attach it to new hires.
        var recordedTeams = history.Where(item => item.SeasonYear == state.SeasonYear)
            .Select(item => item.TeamName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var coach in staff.Where(item => !recordedTeams.Contains(item.TeamName)))
        {
            if (!seasons.TryGetValue(coach.TeamName, out var season) ||
                !keys.Add((coach.StaffId, season.SeasonYear, season.TeamName, coach.Role))) continue;
            history.Add(FromSeason(coach, season));
        }
        return state with { CoachSeasonHistory = history.ToArray(), CoachHistoryLastRecordedSeasonYear = state.SeasonYear };
    }

    // Only the user's recorded career is attributable in older saves; never infer historic CPU identities from today's staff.
    public static DynastyState RestoreUserHistory(DynastyState state)
    {
        if (state.UserHeadCoach is null || state.UserCoachCareerHistory.Count == 0) return state;
        var history = state.CoachSeasonHistory.ToList();
        var coach = state.UserHeadCoach;
        foreach (var item in state.UserCoachCareerHistory)
        {
            if (history.Any(record => record.StaffId == coach.StaffId && record.SeasonYear == item.SeasonYear &&
                record.TeamName.Equals(item.TeamName, StringComparison.OrdinalIgnoreCase) && record.Role == item.Role)) continue;
            var season = state.TeamSeasonHistory.FirstOrDefault(record => record.SeasonYear == item.SeasonYear &&
                record.TeamName.Equals(item.TeamName, StringComparison.OrdinalIgnoreCase)) ?? new TeamSeasonHistoryRecord
                {
                    SeasonYear = item.SeasonYear, TeamName = item.TeamName, Wins = item.Wins, Losses = item.Losses,
                    ConferenceChampion = item.ConferenceChampion, PlayoffParticipant = item.PlayoffParticipant,
                    NationalChampion = item.NationalChampion
                };
            season = season with
            {
                Wins = item.Wins, Losses = item.Losses,
                ConferenceChampion = season.ConferenceChampion || item.ConferenceChampion,
                PlayoffParticipant = season.PlayoffParticipant || item.PlayoffParticipant,
                NationalChampion = season.NationalChampion || item.NationalChampion
            };
            history.Add(FromSeason(coach with { TeamName = item.TeamName, Role = item.Role }, season));
        }
        return state with { CoachSeasonHistory = history.ToArray() };
    }

    public static CoachCareerSummary GetSummary(DynastyState state, Guid staffId, string fallbackName = "Coach")
    {
        var seasons = state.CoachSeasonHistory.Where(item => item.StaffId == staffId)
            .OrderBy(item => item.SeasonYear).ThenBy(item => item.TeamName).ThenBy(item => item.Role).ToArray();
        return new CoachCareerSummary { StaffId = staffId, CoachName = seasons.LastOrDefault()?.CoachName ?? fallbackName, Seasons = seasons };
    }

    public static (int Wins, int Losses)? GetCurrentRecord(DynastyState state, StaffMember coach)
    {
        if (state.UserCoachIsFired && state.UserHeadCoach?.StaffId == coach.StaffId) return null;
        if (state.Phase is not (SeasonPhase.RegularSeason or SeasonPhase.ConferenceChampionship or SeasonPhase.Postseason) ||
            state.TeamSeasonHistory.Any(item => item.SeasonYear == state.SeasonYear && item.TeamName.Equals(coach.TeamName, StringComparison.OrdinalIgnoreCase))) return null;
        var games = state.Schedule.Where(game => game.SeasonYear == state.SeasonYear && game.HasPlayed && game.InvolvesTeam(coach.TeamName)).ToArray();
        if (games.Length == 0) return null;
        var wins = games.Count(game => game.WinnerTeamName?.Equals(coach.TeamName, StringComparison.OrdinalIgnoreCase) == true);
        return (wins, games.Length - wins);
    }

    private static CoachSeasonRecord FromSeason(StaffMember coach, TeamSeasonHistoryRecord season) => new()
    {
        StaffId = coach.StaffId, CoachName = coach.FullName, Role = coach.Role, TeamName = season.TeamName,
        SeasonYear = season.SeasonYear, Wins = season.Wins, Losses = season.Losses,
        ConferenceWins = season.ConferenceWins, ConferenceLosses = season.ConferenceLosses,
        FinalRanking = season.FinalRanking, ConferenceChampion = season.ConferenceChampion,
        PlayoffParticipant = season.PlayoffParticipant, NationalChampion = season.NationalChampion,
        BowlName = season.BowlName, WonBowl = season.WonBowl
    };
}
