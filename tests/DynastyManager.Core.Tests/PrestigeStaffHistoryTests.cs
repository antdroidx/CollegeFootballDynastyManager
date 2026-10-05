using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public sealed class PrestigeStaffHistoryTests
{
    [Fact]
    public void LeagueStaffCreatesOnlyTheSevenCoreRolesPerProgram()
    {
        var teams = new[] { Team("User", 72), Team("Rival", 78) };
        var state = State();

        state = ProgramPrestigeService.EnsureInitialPrestige(state, teams);
        state = StaffManagementService.EnsureLeagueStaff(state, teams);

        Assert.Equal(14, state.Staff.Count);
        Assert.Equal(
            Enum.GetValues<StaffRole>().Length,
            StaffManagementService.GetTeamStaff(state, "User").Count);
        Assert.Contains(state.Staff, item =>
            item.TeamName == "User" && item.Role == StaffRole.SpecialTeamsCoordinator);
        Assert.Contains(state.Staff, item =>
            item.TeamName == "User" && item.Role == StaffRole.MedicalTrainingDirector);
        Assert.Contains(state.Staff, item =>
            item.TeamName == "User" && item.Role == StaffRole.StrengthConditioningDirector);
        Assert.Contains(state.Staff, item =>
            item.TeamName == "User" && item.Role == StaffRole.ChiefScout);
    }

    [Fact]
    public void ChampionshipSeasonRaisesPrestigeWithoutWildSwing()
    {
        var user = Team("User", 60);
        var rival = Team("Rival", 70);
        var teams = new[] { user, rival };
        var lookup = teams.ToDictionary(
            item => item.Name,
            StringComparer.OrdinalIgnoreCase);

        var games = Enumerable.Range(1, 12)
            .Select(week => Game(week, "User", "Rival", 35, 17))
            .ToArray();

        var state = State() with
        {
            Schedule = games,
            NationalChampionshipHistory = new[]
            {
                new NationalChampionRecord
                {
                    SeasonYear = 2026,
                    ChampionTeamName = "User",
                    RunnerUpTeamName = "Rival",
                    ChampionScore = 31,
                    RunnerUpScore = 24
                }
            }
        };

        state = ProgramPrestigeService.EnsureInitialPrestige(state, teams);
        state = ProgramPrestigeService.ApplySeasonResults(state, lookup);

        var snapshot = state.ProgramPrestigeHistory.Single(item =>
            item.TeamName == "User" && item.SeasonYear == 2026);

        Assert.True(snapshot.EndingPrestige > snapshot.StartingPrestige);
        Assert.InRange(snapshot.Change, 3, 7);
        Assert.Contains("National championship", snapshot.Reasons);
    }

    [Fact]
    public void FinalizedSeasonStoresTeamHistoryStatsAndAwards()
    {
        var user = Team("User", 75);
        var rival = Team("Rival", 70);
        var lookup = new[] { user, rival }.ToDictionary(
            item => item.Name,
            StringComparer.OrdinalIgnoreCase);

        var state = State() with
        {
            Schedule = new[]
            {
                Game(1, "User", "Rival", 28, 21)
            },
            ActiveRoster = new[]
            {
                Player("User QB", "User", Position.QB, 86, 1),
                Player("User RB", "User", Position.RB, 84, 1),
                Player("Rival QB", "Rival", Position.QB, 80, 1),
                Player("Rival RB", "Rival", Position.RB, 79, 1)
            }
        };

        state = DynastyHistoryService.FinalizeSeason(state, lookup);

        Assert.Contains(state.TeamSeasonHistory,
            item => item.TeamName == "User" && item.Wins == 1);
        Assert.Contains(state.PlayerSeasonStats,
            item => item.TeamName == "User" && item.PassYards > 0);
        Assert.Contains(state.PlayerAwardHistory,
            item => item.SeasonYear == 2026);
    }

    [Fact]
    public void EliteChiefScoutBuildsLargerScoutingTeam()
    {
        var team = Team("User", 85);
        var state = State() with
        {
            HighSchoolRecruitingPool = new[]
            {
                new HighSchoolRecruit
                {
                    RecruitId = Guid.NewGuid(),
                    SeasonYear = 2026,
                    FullName = "Prospect",
                    Position = Position.QB,
                    StarRating = 4,
                    TrueOverallRating = 80,
                    PotentialRating = 91,
                    HomeState = 1,
                    HomeRegion = 1
                }
            },
            Staff = new[]
            {
                new StaffMember
                {
                    FullName = "Elite Evaluator",
                    TeamName = "User",
                    Role = StaffRole.ChiefScout,
                    TalentEvaluation = 94,
                    PotentialEvaluation = 93,
                    RegionalKnowledge = 92,
                    StaffManagement = 95
                }
            }
        };

        state = ScoutingDepartmentService.EnsureDepartment(state, team);

        Assert.Equal(5, state.ScoutingStaff.Count);
        Assert.All(state.ScoutingStaff,
            scout => Assert.InRange(scout.TalentEvaluation, 45, 96));
    }

    private static DynastyState State() => new()
    {
        DynastyId = Guid.Parse("90909090-8080-7070-6060-505050505050"),
        DynastyName = "Systems Test",
        UserTeamName = "User",
        SeasonYear = 2026,
        Week = 13,
        Phase = SeasonPhase.Postseason
    };

    private static Team Team(string name, int prestige) => new()
    {
        Name = name,
        Abbreviation = name[..Math.Min(3, name.Length)].ToUpperInvariant(),
        ConferenceName = "Test",
        Prestige = prestige,
        LegacyRegionId = 1
    };

    private static DynastyPlayer Player(
        string name,
        string team,
        Position position,
        int overall,
        int depth) => new()
    {
        PlayerId = Guid.NewGuid(),
        FullName = name,
        TeamName = team,
        Position = position,
        ClassYear = 2,
        TalentLevel = 7,
        OverallRating = overall,
        PotentialRating = Math.Min(99, overall + 5),
        SpeedRating = overall,
        StrengthRating = overall,
        AgilityRating = overall,
        AwarenessRating = overall,
        TechniqueRating = overall,
        DurabilityRating = 85,
        DepthChartOrder = depth
    };

    private static ScheduledGame Game(
        int week,
        string home,
        string away,
        int homeScore,
        int awayScore) => new()
    {
        GameId = $"game-{week}-{home}-{away}",
        SeasonYear = 2026,
        Week = week,
        HomeTeamName = home,
        AwayTeamName = away,
        GameType = ScheduledGameType.Conference,
        SimulationSeed = week,
        HasPlayed = true,
        HomeScore = homeScore,
        AwayScore = awayScore,
        HomeStats = new GameTeamStats
        {
            Possessions = 12,
            PassAttempts = 30,
            RushAttempts = 36,
            PassYards = 260,
            RushYards = 180,
            Turnovers = 1
        },
        AwayStats = new GameTeamStats
        {
            Possessions = 11,
            PassAttempts = 32,
            RushAttempts = 31,
            PassYards = 210,
            RushYards = 120,
            Turnovers = 2
        }
    };
}
