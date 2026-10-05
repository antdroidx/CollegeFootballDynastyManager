using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class CollegeFootballPlayoffService
{
    public const int PlayoffTeamCount = 12;
    public const int AutomaticConferenceChampionBids = 5;
    public const int FirstRoundWeek = 15;
    public const int QuarterfinalWeek = 16;
    public const int SemifinalWeek = 17;
    public const int NationalChampionshipWeek = 18;

    public static IReadOnlyList<CollegeFootballPlayoffSeedRecord> SelectField(
        int seasonYear,
        IReadOnlyList<NationalRanking> rankings,
        IReadOnlyList<ConferenceChampionRecord> conferenceChampions)
    {
        ArgumentNullException.ThrowIfNull(rankings);
        ArgumentNullException.ThrowIfNull(conferenceChampions);

        var championNames = conferenceChampions
            .Where(record => record.SeasonYear == seasonYear)
            .Select(record => record.ChampionTeamName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var automaticBidNames = rankings
            .Where(ranking => championNames.Contains(ranking.TeamName))
            .Take(AutomaticConferenceChampionBids)
            .Select(ranking => ranking.TeamName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var selected = rankings
            .Where(ranking => automaticBidNames.Contains(ranking.TeamName))
            .ToList();

        foreach (var ranking in rankings)
        {
            if (selected.Count >= PlayoffTeamCount)
                break;

            if (selected.Any(item => item.TeamName.Equals(
                    ranking.TeamName,
                    StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            selected.Add(ranking);
        }

        return selected
            .OrderBy(ranking => ranking.Rank)
            .Take(PlayoffTeamCount)
            .Select((ranking, index) => new CollegeFootballPlayoffSeedRecord
            {
                SeasonYear = seasonYear,
                Seed = index + 1,
                NationalRank = ranking.Rank,
                TeamName = ranking.TeamName,
                ConferenceName = ranking.ConferenceName,
                IsConferenceChampion = championNames.Contains(ranking.TeamName),
                IsAutomaticBid = automaticBidNames.Contains(ranking.TeamName)
            })
            .ToArray();
    }

    public static DynastyState InitializePlayoff(
        DynastyState state,
        IReadOnlyDictionary<string, Team> teamsByName,
        IReadOnlyDictionary<string, TeamSimulationProfile>? profilesByTeam = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(teamsByName);

        if (state.Phase != SeasonPhase.Postseason ||
            state.Week != FirstRoundWeek)
        {
            return state;
        }

        var existingField = state.CollegeFootballPlayoffHistory
            .Where(record => record.SeasonYear == state.SeasonYear)
            .OrderBy(record => record.Seed)
            .ToArray();

        var field = existingField.Length == PlayoffTeamCount
            ? existingField
            : SelectField(
                state.SeasonYear,
                NationalRankingService.Build(state, teamsByName, profilesByTeam),
                state.ConferenceChampionshipHistory);

        var playoffHistory = state.CollegeFootballPlayoffHistory
            .Where(record => record.SeasonYear != state.SeasonYear)
            .Concat(field)
            .ToArray();

        if (state.Schedule.Any(game =>
                game.SeasonYear == state.SeasonYear &&
                game.GameType == ScheduledGameType.CollegeFootballPlayoff))
        {
            return state with { CollegeFootballPlayoffHistory = playoffHistory };
        }

        var firstRound = new[]
        {
            CreateGame(state, field, PostseasonRound.FirstRound, 1, FirstRoundWeek, 5, 12, false),
            CreateGame(state, field, PostseasonRound.FirstRound, 2, FirstRoundWeek, 6, 11, false),
            CreateGame(state, field, PostseasonRound.FirstRound, 3, FirstRoundWeek, 7, 10, false),
            CreateGame(state, field, PostseasonRound.FirstRound, 4, FirstRoundWeek, 8, 9, false)
        };

        return state with
        {
            CollegeFootballPlayoffHistory = playoffHistory,
            Schedule = state.Schedule.Concat(firstRound).ToArray()
        };
    }

    public static DynastyState SimulateCurrentRound(
        DynastyState state,
        IReadOnlyDictionary<string, Team> teamsByName,
        IReadOnlyDictionary<string, TeamSimulationProfile>? profilesByTeam = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(teamsByName);

        if (state.Phase != SeasonPhase.Postseason)
            return state;

        var changed = false;
        var schedule = state.Schedule.Select(game =>
        {
            if (game.SeasonYear != state.SeasonYear ||
                game.GameType != ScheduledGameType.CollegeFootballPlayoff ||
                game.Week != state.Week ||
                game.HasPlayed)
            {
                return game;
            }

            if (!teamsByName.TryGetValue(game.HomeTeamName, out var homeTeam) ||
                !teamsByName.TryGetValue(game.AwayTeamName, out var awayTeam))
            {
                throw new InvalidOperationException(
                    "Playoff game references a team that is not available.");
            }

            TeamSimulationProfile? homeProfile = null;
            TeamSimulationProfile? awayProfile = null;

            if (profilesByTeam is not null)
            {
                profilesByTeam.TryGetValue(game.HomeTeamName, out homeProfile);
                profilesByTeam.TryGetValue(game.AwayTeamName, out awayProfile);
            }

            changed = true;
            return DeterministicGameSimulator.Simulate(
                game, homeTeam, awayTeam, homeProfile, awayProfile);
        }).ToArray();

        if (!changed)
            return state;

        var updated = state with { Schedule = schedule };

        return state.Week == NationalChampionshipWeek
            ? RecordNationalChampion(updated)
            : updated;
    }

    public static DynastyState ScheduleNextRound(DynastyState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.Phase != SeasonPhase.Postseason)
            return state;

        var field = state.CollegeFootballPlayoffHistory
            .Where(record => record.SeasonYear == state.SeasonYear)
            .OrderBy(record => record.Seed)
            .ToArray();

        if (field.Length != PlayoffTeamCount)
            return state;

        return state.Week switch
        {
            FirstRoundWeek => ScheduleQuarterfinals(state, field),
            QuarterfinalWeek => ScheduleSemifinals(state, field),
            SemifinalWeek => ScheduleNationalChampionship(state, field),
            _ => state
        };
    }

    private static DynastyState ScheduleQuarterfinals(
        DynastyState state,
        IReadOnlyList<CollegeFootballPlayoffSeedRecord> field)
    {
        if (HasRound(state, PostseasonRound.Quarterfinal))
            return state;

        var winner5v12 = Winner(state, PostseasonRound.FirstRound, 1);
        var winner6v11 = Winner(state, PostseasonRound.FirstRound, 2);
        var winner7v10 = Winner(state, PostseasonRound.FirstRound, 3);
        var winner8v9 = Winner(state, PostseasonRound.FirstRound, 4);

        if (new[] { winner5v12, winner6v11, winner7v10, winner8v9 }
            .Any(name => name is null))
        {
            return state;
        }

        var games = new[]
        {
            CreateGame(state, field, PostseasonRound.Quarterfinal, 1, QuarterfinalWeek, TeamForSeed(field, 1), winner8v9!, true),
            CreateGame(state, field, PostseasonRound.Quarterfinal, 2, QuarterfinalWeek, TeamForSeed(field, 4), winner5v12!, true),
            CreateGame(state, field, PostseasonRound.Quarterfinal, 3, QuarterfinalWeek, TeamForSeed(field, 2), winner7v10!, true),
            CreateGame(state, field, PostseasonRound.Quarterfinal, 4, QuarterfinalWeek, TeamForSeed(field, 3), winner6v11!, true)
        };

        return state with { Schedule = state.Schedule.Concat(games).ToArray() };
    }

    private static DynastyState ScheduleSemifinals(
        DynastyState state,
        IReadOnlyList<CollegeFootballPlayoffSeedRecord> field)
    {
        if (HasRound(state, PostseasonRound.Semifinal))
            return state;

        var qf1 = Winner(state, PostseasonRound.Quarterfinal, 1);
        var qf2 = Winner(state, PostseasonRound.Quarterfinal, 2);
        var qf3 = Winner(state, PostseasonRound.Quarterfinal, 3);
        var qf4 = Winner(state, PostseasonRound.Quarterfinal, 4);

        if (new[] { qf1, qf2, qf3, qf4 }.Any(name => name is null))
            return state;

        var games = new[]
        {
            CreateGame(state, field, PostseasonRound.Semifinal, 1, SemifinalWeek, qf1!, qf2!, true),
            CreateGame(state, field, PostseasonRound.Semifinal, 2, SemifinalWeek, qf3!, qf4!, true)
        };

        return state with { Schedule = state.Schedule.Concat(games).ToArray() };
    }

    private static DynastyState ScheduleNationalChampionship(
        DynastyState state,
        IReadOnlyList<CollegeFootballPlayoffSeedRecord> field)
    {
        if (HasRound(state, PostseasonRound.NationalChampionship))
            return state;

        var semifinal1 = Winner(state, PostseasonRound.Semifinal, 1);
        var semifinal2 = Winner(state, PostseasonRound.Semifinal, 2);

        if (semifinal1 is null || semifinal2 is null)
            return state;

        var game = CreateGame(
            state,
            field,
            PostseasonRound.NationalChampionship,
            1,
            NationalChampionshipWeek,
            semifinal1,
            semifinal2,
            true);

        return state with { Schedule = state.Schedule.Append(game).ToArray() };
    }

    private static DynastyState RecordNationalChampion(DynastyState state)
    {
        if (state.NationalChampionshipHistory.Any(record =>
                record.SeasonYear == state.SeasonYear))
        {
            return state;
        }

        var game = state.Schedule.FirstOrDefault(game =>
            game.SeasonYear == state.SeasonYear &&
            game.GameType == ScheduledGameType.CollegeFootballPlayoff &&
            game.PostseasonRound == PostseasonRound.NationalChampionship &&
            game.HasPlayed);

        if (game?.HomeScore is not int homeScore ||
            game.AwayScore is not int awayScore)
        {
            return state;
        }

        var homeWon = homeScore > awayScore;

        return state with
        {
            NationalChampionshipHistory =
                state.NationalChampionshipHistory.Append(
                    new NationalChampionRecord
                    {
                        SeasonYear = state.SeasonYear,
                        ChampionTeamName =
                            homeWon ? game.HomeTeamName : game.AwayTeamName,
                        RunnerUpTeamName =
                            homeWon ? game.AwayTeamName : game.HomeTeamName,
                        ChampionScore = homeWon ? homeScore : awayScore,
                        RunnerUpScore = homeWon ? awayScore : homeScore
                    }).ToArray()
        };
    }

    private static bool HasRound(DynastyState state, PostseasonRound round) =>
        state.Schedule.Any(game =>
            game.SeasonYear == state.SeasonYear &&
            game.GameType == ScheduledGameType.CollegeFootballPlayoff &&
            game.PostseasonRound == round);

    private static string? Winner(
        DynastyState state,
        PostseasonRound round,
        int bracketSlot) =>
        state.Schedule.FirstOrDefault(game =>
            game.SeasonYear == state.SeasonYear &&
            game.GameType == ScheduledGameType.CollegeFootballPlayoff &&
            game.PostseasonRound == round &&
            game.PlayoffBracketSlot == bracketSlot &&
            game.HasPlayed)?.WinnerTeamName;

    private static string TeamForSeed(
        IReadOnlyList<CollegeFootballPlayoffSeedRecord> field,
        int seed) =>
        field.Single(record => record.Seed == seed).TeamName;

    private static ScheduledGame CreateGame(
        DynastyState state,
        IReadOnlyList<CollegeFootballPlayoffSeedRecord> field,
        PostseasonRound round,
        int bracketSlot,
        int week,
        int homeSeed,
        int awaySeed,
        bool neutralSite) =>
        CreateGame(
            state, field, round, bracketSlot, week,
            TeamForSeed(field, homeSeed),
            TeamForSeed(field, awaySeed),
            neutralSite);

    private static ScheduledGame CreateGame(
        DynastyState state,
        IReadOnlyList<CollegeFootballPlayoffSeedRecord> field,
        PostseasonRound round,
        int bracketSlot,
        int week,
        string homeTeam,
        string awayTeam,
        bool neutralSite)
    {
        var homeSeed = field.Single(record =>
            record.TeamName.Equals(
                homeTeam,
                StringComparison.OrdinalIgnoreCase)).Seed;
        var awaySeed = field.Single(record =>
            record.TeamName.Equals(
                awayTeam,
                StringComparison.OrdinalIgnoreCase)).Seed;

        var seed = SimulationSeed.Create(
            state.DynastyId,
            state.SeasonYear,
            week,
            $"{round}|{bracketSlot}|{homeTeam}",
            awayTeam);

        return new ScheduledGame
        {
            GameId =
                $"{state.SeasonYear}-CFP-{(int)round}-{bracketSlot}-{seed:X8}",
            SeasonYear = state.SeasonYear,
            Week = week,
            HomeTeamName = homeTeam,
            AwayTeamName = awayTeam,
            GameType = ScheduledGameType.CollegeFootballPlayoff,
            PostseasonRound = round,
            PlayoffBracketSlot = bracketSlot,
            HomeSeed = homeSeed,
            AwaySeed = awaySeed,
            IsNeutralSite = neutralSite,
            SimulationSeed = seed
        };
    }
}
