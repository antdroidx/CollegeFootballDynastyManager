using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static partial class CollegeFootballPlayoffService
{
    private static int FieldSize(int requested, int teams) => new[] { 12, 8, 4, 2 }.FirstOrDefault(n => n <= requested && n <= teams);

    private static DynastyState InitializeSmallField(DynastyState state, IReadOnlyList<CollegeFootballPlayoffSeedRecord> field)
    {
        if (field.Count < 2) return state;
        var round = field.Count == 8 ? PostseasonRound.Quarterfinal : field.Count == 4 ? PostseasonRound.Semifinal : PostseasonRound.NationalChampionship;
        var week = field.Count == 8 ? QuarterfinalWeek : field.Count == 4 ? SemifinalWeek : NationalChampionshipWeek;
        // Standard seeded bracket ordering keeps 1 and 2 in opposite halves.
        var order = field.Count == 8 ? new[] { 1, 8, 4, 5, 2, 7, 3, 6 } : field.Count == 4 ? new[] { 1, 4, 2, 3 } : new[] { 1, 2 };
        var games = Enumerable.Range(0, field.Count / 2).Select(i => CreateGame(state, field, round, i + 1, week,
            order[i * 2], order[i * 2 + 1], true));
        return state with { Schedule = state.Schedule.Concat(games).ToArray() };
    }

    private static DynastyState ScheduleSmallField(DynastyState state, IReadOnlyList<CollegeFootballPlayoffSeedRecord> field)
    {
        var current = state.Schedule.Where(g => g.SeasonYear == state.SeasonYear &&
            g.GameType == ScheduledGameType.CollegeFootballPlayoff && g.Week == state.Week).OrderBy(g => g.PlayoffBracketSlot).ToArray();
        if (current.Length < 2 || current.Any(g => !g.HasPlayed)) return state;
        var round = current[0].PostseasonRound == PostseasonRound.Quarterfinal ? PostseasonRound.Semifinal : PostseasonRound.NationalChampionship;
        if (HasRound(state, round)) return state;
        var week = round == PostseasonRound.Semifinal ? SemifinalWeek : NationalChampionshipWeek;
        var games = Enumerable.Range(0, current.Length / 2).Select(i => CreateGame(state, field, round, i + 1, week,
            current[i * 2].WinnerTeamName!, current[i * 2 + 1].WinnerTeamName!, true));
        return state with { Schedule = state.Schedule.Concat(games).ToArray() };
    }
}
