using System.Text.Json;
using System.Text.Json.Nodes;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Persistence;
using SQLite;
using Xunit;

namespace DynastyManager.Core.Tests;

public class CoachCareerHistoryTests
{
    [Fact]
    public void FinalizationRecordsTheOutgoingCoachesOnceByIdentity()
    {
        var user = Coach(1, "User", StaffRole.HeadCoach);
        var cpu = Coach(2, "CPU", StaffRole.HeadCoach);
        var assistant = Coach(3, "User", StaffRole.OffensiveCoordinator);
        var state = State() with { UserHeadCoach = user, Staff = [cpu, assistant], Schedule = [Game(2026)] };
        var result = DynastyHistoryService.FinalizeSeason(state, Teams());
        Assert.Equal(3, result.CoachSeasonHistory.Count);
        Assert.Equal(1, CoachCareerService.GetSummary(result, user.StaffId).HeadCoachWins);
        Assert.Equal(1, CoachCareerService.GetSummary(result, cpu.StaffId).HeadCoachLosses);
        Assert.Equal(0, CoachCareerService.GetSummary(result, assistant.StaffId).HeadCoachWins);
        Assert.Equal(1, CoachCareerService.GetSummary(result, assistant.StaffId).StaffSeasons);
        var incoming = Coach(4, "CPU", StaffRole.HeadCoach);
        var afterCarousel = result with { Staff = [incoming], UserCoachIsFired = true };
        Assert.Same(afterCarousel, CoachCareerService.RecordCompletedSeason(afterCarousel));
        Assert.Same(afterCarousel, DynastyHistoryService.FinalizeSeason(afterCarousel, Teams()));
        Assert.Empty(CoachCareerService.GetSummary(afterCarousel, incoming.StaffId).Seasons);
        // Every fixture coach deliberately has the same name: IDs must keep their careers separate.
        Assert.Equal(3, result.CoachSeasonHistory.Select(item => item.StaffId).Distinct().Count());
    }

    [Fact]
    public void PromotionAndJobChangesKeepStaffResultsSeparateFromHeadCoachingTotals()
    {
        var coach = Coach(1, "User", StaffRole.OffensiveCoordinator);
        var state = State() with { Staff = [coach], TeamSeasonHistory = [Season(2026, "User", 10, 3) with
            { NationalChampion = true, ConferenceChampion = true, PlayoffParticipant = true, WonBowl = true }] };
        state = CoachCareerService.RecordCompletedSeason(state);
        var before = CoachCareerService.GetSummary(state, coach.StaffId);
        Assert.Equal(0, before.NationalTitles);
        Assert.Equal(0, before.HeadCoachWins);
        coach = coach with { TeamName = "CPU", Role = StaffRole.HeadCoach };
        state = state with { SeasonYear = 2027, Staff = [coach], TeamSeasonHistory = state.TeamSeasonHistory
            .Append(Season(2027, "CPU", 9, 4) with { PlayoffParticipant = true, WonBowl = false }).ToArray() };
        state = CoachCareerService.RecordCompletedSeason(state);
        state = state with { SeasonYear = 2028, TeamSeasonHistory = state.TeamSeasonHistory
            .Append(Season(2028, "CPU", 13, 0) with { NationalChampion = true, ConferenceChampion = true,
                PlayoffParticipant = true, WonBowl = true }).ToArray() };
        state = CoachCareerService.RecordCompletedSeason(state);
        state = state with { Staff = [], SeasonYear = 2029 };
        var summary = CoachCareerService.GetSummary(state, coach.StaffId);
        Assert.Equal(22, summary.HeadCoachWins);
        Assert.Equal(4, summary.HeadCoachLosses);
        Assert.Equal(22.0 / 26, summary.HeadCoachWinPercentage, 8);
        Assert.Equal(2, summary.HeadCoachSeasons);
        Assert.Equal(1, summary.StaffSeasons);
        Assert.Equal(1, summary.NationalTitles);
        Assert.Equal(1, summary.ConferenceTitles);
        Assert.Equal(2, summary.PlayoffAppearances);
        Assert.Equal(1, summary.BowlWins);
        Assert.Equal(1, summary.BowlLosses);
        Assert.Equal(new[] { "User", "CPU", "CPU" }, summary.Seasons.Select(item => item.TeamName));
    }

    [Fact]
    public void LiveRecordDoesNotDoubleCountCompletedYearsOrCreditNewHires()
    {
        var coach = Coach(1, "User", StaffRole.HeadCoach);
        var state = State() with { Phase = SeasonPhase.RegularSeason, UserHeadCoach = coach, Schedule = [Game(2026)] };
        Assert.Equal((1, 0), CoachCareerService.GetCurrentRecord(state, coach));
        Assert.Empty(CoachCareerService.GetSummary(state, coach.StaffId).Seasons);
        state = state with { TeamSeasonHistory = [Season(2026, "User", 1, 0)] };
        Assert.Null(CoachCareerService.GetCurrentRecord(state, coach));
        Assert.Null(CoachCareerService.GetCurrentRecord(state, Coach(2, "User", StaffRole.HeadCoach)));
        var emptyYear = State() with { SeasonYear = 2027, Staff = [coach], TeamSeasonHistory = [Season(2026, "User", 1, 0)] };
        Assert.Same(emptyYear, CoachCareerService.RecordCompletedSeason(emptyYear));
    }

    [Fact]
    public async Task RecordedCoachCareersSurviveReloadAndDeparture()
    {
        var user = Coach(1, "CPU", StaffRole.HeadCoach);
        var state = State() with { UserHeadCoach = user, UserTeamName = "CPU",
            Staff = [Coach(2, "User", StaffRole.HeadCoach)], UserCoachCareerHistory =
            [ Legacy(2024, "User", 8, 5) with { ConferenceChampion = true }, Legacy(2025, "CPU", 11, 2) with { PlayoffParticipant = true } ],
            TeamSeasonHistory = [Season(2024, "User", 8, 5) with { ConferenceWins = 5, ConferenceLosses = 3, FinalRanking = 19, WonBowl = true }] };
        var path = Path.Combine(Path.GetTempPath(), $"coach-career-{Guid.NewGuid():N}.db");
        try
        {
            await using var repository = new SqliteDynastySaveRepository(path);
            state = CoachCareerService.RestoreUserHistory(state);
            var id = await repository.SaveAsync(state, SaveKind.Manual);
            var loaded = await repository.LoadAsync(id);
            Assert.NotNull(loaded);
            Assert.Equal(DynastyState.CurrentSchemaVersion, loaded.SchemaVersion);
            var summary = CoachCareerService.GetSummary(loaded, user.StaffId);
            Assert.Equal(19, summary.HeadCoachWins);
            Assert.Equal(7, summary.HeadCoachLosses);
            Assert.Equal(2, summary.Seasons.Count);
            Assert.Equal(1, summary.ConferenceTitles);
            Assert.Equal(1, summary.PlayoffAppearances);
            Assert.Equal(1, summary.BowlWins);
            Assert.Equal(5, summary.Seasons[0].ConferenceWins);
            Assert.Equal(19, summary.Seasons[0].FinalRanking);
            Assert.Empty(CoachCareerService.GetSummary(loaded, state.Staff[0].StaffId).Seasons);
            Assert.Equal(2, CoachCareerService.RestoreUserHistory(loaded).CoachSeasonHistory.Count);
            var archived = loaded with { Staff = [], UserHeadCoach = null };
            var saved = await repository.SaveAsync(archived, SaveKind.Manual);
            var reloaded = await repository.LoadAsync(saved);
            Assert.NotNull(reloaded);
            Assert.Equal(19, CoachCareerService.GetSummary(reloaded, user.StaffId).HeadCoachWins);
            Assert.Equal(2, reloaded.CoachSeasonHistory.Count);
        }
        finally { foreach (var suffix in new[] { "", "-wal", "-shm" }) if (File.Exists(path + suffix)) File.Delete(path + suffix); }
    }

    private static DynastyState State() => new() { DynastyName = "Coach records", UserTeamName = "User", SeasonYear = 2026, Phase = SeasonPhase.Postseason };
    private static StaffMember Coach(int id, string team, StaffRole role) => new() { StaffId = new Guid(id, 2222, 3333, 4, 4, 5, 5, 5, 5, 5, 5), FullName = "Coach Smith", TeamName = team, Role = role };
    private static TeamSeasonHistoryRecord Season(int year, string team, int wins, int losses) => new() { SeasonYear = year, TeamName = team, Wins = wins, Losses = losses };
    private static UserCoachCareerRecord Legacy(int year, string team, int wins, int losses) => new() { SeasonYear = year, TeamName = team, Wins = wins, Losses = losses, Summary = "Legacy year" };
    private static Dictionary<string, Team> Teams() => new[] { "User", "CPU" }.ToDictionary(name => name, name => new Team { Name = name, Abbreviation = name, ConferenceName = "Test", Prestige = 70 });
    private static ScheduledGame Game(int year) => new() { GameId = $"{year}-game", SeasonYear = year, Week = 1, HomeTeamName = "User", AwayTeamName = "CPU", HasPlayed = true, HomeScore = 21, AwayScore = 14 };
}
