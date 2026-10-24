using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public sealed class UserCoachCareerTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void CoordinatorStaffRecruitsTheOtherSideEvenWithManualAssistanceOff(int savedWorkload)
    {
        var own = new HighSchoolRecruit { RecruitId = Guid.NewGuid(), FullName = "Quarterback", Position = Position.QB, SeasonYear = 2028, TrueOverallRating = 70, PotentialRating = 80, StarRating = 3, HomeRegion = 1 };
        var other = own with { RecruitId = Guid.NewGuid(), FullName = "Cornerback", Position = Position.CB };
        var state = State() with { Phase = SeasonPhase.Recruiting, UserCoachRole = StaffRole.OffensiveCoordinator,
            RecruitingAssistanceEnabled = false, RecruitingAssistanceWorkloadPercent = savedWorkload,
            RecruitingPointsRemaining = 400, HighSchoolRecruitingPool = [own, other] };
        var result = CpuRecruitingService.ApplyWeeklyUserAssistance(state, Teams()["User"]);
        Assert.Contains(result.RecruitingInteractions, r => r.ProspectId == other.RecruitId && r.WasCpuAssisted && r.ScholarshipOffered);
        Assert.DoesNotContain(result.RecruitingInteractions, r => r.ProspectId == own.RecruitId && r.WasCpuAssisted);
        Assert.True(result.RecruitingPointsRemaining >= 200);
    }

    [Fact]
    public void CoordinatorVacanciesDoNotRequireAHeadCoachVacancy()
    {
        var teams = Teams();
        var state = StaffManagementService.EnsureLeagueStaff(State() with { UserCoachRole = StaffRole.DefensiveCoordinator }, teams.Values);
        state = state with { UserHeadCoach = state.UserHeadCoach! with { Reputation = 95 },
            TeamSeasonHistory = [Season("User", 14, 0, true, true, true, 1)],
            Staff = state.Staff.Select(member => member with { ContractYearsRemaining = member.Role == StaffRole.DefensiveCoordinator ? 1 : 5 }).ToArray() };
        var offers = UserCoachCareerService.GenerateJobOffers(state, teams).UserCoachJobOffers;
        Assert.Contains(offers, o => o.Role == StaffRole.DefensiveCoordinator);
        Assert.DoesNotContain(offers, o => o.Role == StaffRole.HeadCoach);
    }

    [Fact]
    public void SuccessfulCoordinatorReceivesBothCoordinatorAndHeadCoachOffers()
    {
        var teams = Teams();
        var state = StaffManagementService.EnsureLeagueStaff(State() with { UserCoachRole = StaffRole.OffensiveCoordinator }, teams.Values);
        state = state with { UserHeadCoach = state.UserHeadCoach! with { Reputation = 95 },
            TeamSeasonHistory = [Season("User", 14, 0, true, true, true, 1)],
            Staff = state.Staff.Select(member => member with { ContractYearsRemaining = 1 }).ToArray() };
        var offered = UserCoachCareerService.GenerateJobOffers(state, teams);
        Assert.Contains(offered.UserCoachJobOffers, o => o.Role == StaffRole.HeadCoach);
        Assert.Contains(offered.UserCoachJobOffers, o => o.Role == StaffRole.OffensiveCoordinator);
    }

    [Fact]
    public void CoordinatorCannotInjectAnOffSidePlayIntoTheEngine()
    {
        var manual = GameDayEngineTests.Session(GameDayMode.Play);
        var automatic = GameDayEngineTests.Session(GameDayMode.Play);
        if (manual.KickoffPending) { GameDayEngine.Step(manual); GameDayEngine.Step(automatic); }
        manual.ControlledTeamName = manual.PossessionTeamName;
        automatic.ControlledTeamName = automatic.PossessionTeamName;
        manual.UserCoachRole = automatic.UserCoachRole = StaffRole.DefensiveCoordinator;
        var denied = GameDayEngine.Step(manual, PlaybookCatalog.OffensivePlays.First().Id);
        var cpu = GameDayEngine.Step(automatic);
        Assert.Equal(cpu.OffensivePlayId, denied.OffensivePlayId);
        Assert.Equal(cpu.Yards, denied.Yards);
        Assert.Equal(cpu.Outcome, denied.Outcome);
    }

    [Fact]
    public void FiringCanBeDisabledWithoutErasingPerformanceOrEndingTheContract()
    {
        var teams = Teams();
        var source = StaffManagementService.EnsureLeagueStaff(State(), teams.Values);
        source = source with { UserCoachJobSecurity = 25, UserHeadCoach = source.UserHeadCoach! with { ContractYearsRemaining = 1 },
            TeamSeasonHistory = [Season("User", 0, 12)] };
        var fired = UserCoachCareerService.EvaluateCompletedSeason(source, teams);
        var protectedCoach = UserCoachCareerService.EvaluateCompletedSeason(source with { UserCoachFiringEnabled = false }, teams);
        Assert.True(fired.UserCoachIsFired);
        Assert.False(protectedCoach.UserCoachIsFired);
        Assert.Equal(fired.UserCoachJobSecurity, protectedCoach.UserCoachJobSecurity);
        Assert.True(protectedCoach.UserHeadCoach!.ContractYearsRemaining > 0);
    }

    [Theory]
    [InlineData(StaffRole.OffensiveCoordinator)]
    [InlineData(StaffRole.DefensiveCoordinator)]
    public void CoordinatorOccupiesTheirOwnRoleAndKeepsASeparateHeadCoach(StaffRole role)
    {
        var teams = Teams();
        var state = StaffManagementService.EnsureLeagueStaff(State() with { UserCoachRole = role, UserCoachFiringEnabled = false }, teams.Values);
        Assert.Equal(role, state.UserHeadCoach!.Role);
        Assert.Equal(state.UserHeadCoach.StaffId, StaffManagementService.GetStaff(state, "User", role)!.StaffId);
        Assert.NotEqual(state.UserHeadCoach.StaffId, StaffManagementService.GetStaff(state, "User", StaffRole.HeadCoach)!.StaffId);
        Assert.DoesNotContain(state.Staff, s => s.TeamName == "User" && s.Role == role);
        var restored = System.Text.Json.JsonSerializer.Deserialize<DynastyState>(System.Text.Json.JsonSerializer.Serialize(state))!;
        Assert.Equal(role, restored.UserCoachRole);
        Assert.False(restored.UserCoachFiringEnabled);
        Assert.Equal(role, StaffManagementService.EnsureUserHeadCoach(restored, teams.Values).UserHeadCoach!.Role);
    }

    [Fact]
    public void OldSaveDefaultsToHeadCoachWithFiringEnabled()
    {
        var state = System.Text.Json.JsonSerializer.Deserialize<DynastyState>("""{"DynastyName":"Old","UserTeamName":"User"}""")!;
        Assert.Equal(StaffRole.HeadCoach, state.UserCoachRole);
        Assert.True(state.UserCoachFiringEnabled);
    }

    [Fact]
    public void CoordinatorCanAcceptHeadCoachPromotionAndKeepsTheirIdentity()
    {
        var teams = Teams();
        var state = StaffManagementService.EnsureLeagueStaff(State() with { UserCoachRole = StaffRole.OffensiveCoordinator }, teams.Values);
        var offer = new UserCoachJobOffer { OfferId = Guid.NewGuid(), TeamName = "Rival", SeasonYear = state.SeasonYear, Role = StaffRole.HeadCoach, Reason = "Promotion" };
        state = state with { UserCoachJobOffers = [offer] };
        var promoted = UserCoachCareerService.AcceptJobOffer(state, offer.OfferId, teams);
        Assert.Equal(StaffRole.HeadCoach, promoted.UserCoachRole);
        Assert.Equal(state.UserHeadCoach!.StaffId, promoted.UserHeadCoach!.StaffId);
        Assert.Equal(StaffRole.HeadCoach, promoted.UserHeadCoach.Role);
        Assert.True(UserCoachControlService.ControlsOffense(promoted));
        Assert.True(UserCoachControlService.ControlsDefense(promoted));
        Assert.NotNull(StaffManagementService.GetStaff(promoted, "User", StaffRole.OffensiveCoordinator));
    }

    [Fact]
    public void CoordinatorPlanChangesPreserveTheOtherSide()
    {
        var original = new TeamGamePlan { TeamName = "User", Offense = OffensivePlaybook.AirRaid, Defense = DefensivePlaybook.ThreeFour, Tempo = OffensiveTempo.ChewClock };
        var state = State() with { UserCoachRole = StaffRole.DefensiveCoordinator, TeamGamePlans = [original] };
        var changed = GamePlanService.SetPlan(state, original with { Offense = OffensivePlaybook.PowerRun, Tempo = OffensiveTempo.UpTempo, Defense = DefensivePlaybook.Nickel });
        var plan = Assert.Single(changed.TeamGamePlans);
        Assert.Equal(original.Offense, plan.Offense);
        Assert.Equal(original.Tempo, plan.Tempo);
        Assert.Equal(DefensivePlaybook.Nickel, plan.Defense);
    }

    [Fact]
    public void CoordinatorCannotScoutOtherSideButCpuAssistanceCan()
    {
        var recruit = new HighSchoolRecruit { RecruitId = Guid.NewGuid(), FullName = "Defender", Position = Position.CB, SeasonYear = 2028 };
        var state = State() with { UserCoachRole = StaffRole.OffensiveCoordinator, RecruitingPointsRemaining = 100, HighSchoolRecruitingPool = [recruit] };
        Assert.Same(state, InteractiveRecruitingService.Scout(state, RecruitingSource.HighSchool, recruit.RecruitId));
        var assisted = InteractiveRecruitingService.Scout(state, RecruitingSource.HighSchool, recruit.RecruitId, cpuAssisted: true);
        Assert.True(assisted.RecruitingPointsRemaining < state.RecruitingPointsRemaining);
        Assert.True(Assert.Single(assisted.RecruitingInteractions).ScoutingPercent > 0);
    }

    [Fact]
    public void CoordinatorTalkOnlyReactsOnTheirSideAndCannotSubstituteOtherSide()
    {
        var session = GameDayEngineTests.Session(GameDayMode.Play);
        session.ControlledTeamName = session.Game.HomeTeamName;
        session.UserCoachRole = StaffRole.DefensiveCoordinator;
        var talk = GameDayCoachingService.Talk(session, session.ControlledTeamName, CoachTalkTone.Encourage, CoachTalkAudience.Team)!;
        Assert.Equal(CoachTalkAudience.Defense, talk.Audience);
        Assert.All(talk.Reactions, r => Assert.False(GameDayCoachingService.IsOffense(session.PlayersById[r.PlayerId].Position)));
        var qb = session.Roster.First(p => p.TeamName == session.ControlledTeamName && p.Position == Position.QB);
        Assert.False(GameDayCoachingService.Substitute(session, session.ControlledTeamName, qb.PlayerId));
    }

    [Fact]
    public void UserHeadCoachIsSeparateFromStaffButFeedsHeadCoachRatings()
    {
        var teams = Teams();
        var state = StaffManagementService.EnsureLeagueStaff(
            State(),
            teams.Values);

        Assert.NotNull(state.UserHeadCoach);
        Assert.DoesNotContain(
            state.Staff,
            item =>
                item.TeamName == "User" &&
                item.Role == StaffRole.HeadCoach);

        var coach = StaffManagementService.GetStaff(
            state,
            "User",
            StaffRole.HeadCoach);

        Assert.NotNull(coach);
        Assert.Equal(
            state.UserHeadCoach!.StaffId,
            coach!.StaffId);
        Assert.Equal(
            state.UserHeadCoach.OverallRating,
            StaffManagementService.GetRoleRating(
                state,
                "User",
                StaffRole.HeadCoach));
    }

    [Fact]
    public void UserHeadCoachRatingChangesSimulationProfile()
    {
        var teams = Teams();
        var state = StaffManagementService.EnsureLeagueStaff(
            State(),
            teams.Values);
        var baseCoach = state.UserHeadCoach!;

        var low = state with
        {
            UserHeadCoach = baseCoach with
            {
                Leadership = 45,
                Recruiting = 45,
                PlayerDevelopment = 45,
                GameManagement = 45
            }
        };
        var high = state with
        {
            UserHeadCoach = baseCoach with
            {
                Leadership = 95,
                Recruiting = 95,
                PlayerDevelopment = 95,
                GameManagement = 95
            }
        };

        var lowProfile =
            DynastyRosterSimulationProfileBuilder
                .Build(low, teams.Values)["User"];
        var highProfile =
            DynastyRosterSimulationProfileBuilder
                .Build(high, teams.Values)["User"];

        Assert.True(
            highProfile.PassOffenseRating >
            lowProfile.PassOffenseRating);
        Assert.True(
            highProfile.RushDefenseRating >
            lowProfile.RushDefenseRating);
        Assert.True(
            highProfile.SpecialTeamsRating >
            lowProfile.SpecialTeamsRating);
    }

    [Fact]
    public void StrongSeasonDevelopsCoachAndGeneratesJobOffers()
    {
        var teams = Teams();
        var state = StaffManagementService.EnsureLeagueStaff(
            State(),
            teams.Values);
        var coach = state.UserHeadCoach! with
        {
            Reputation = 68,
            Leadership = 66,
            Recruiting = 66,
            PlayerDevelopment = 66,
            GameManagement = 66,
            ContractYearsRemaining = 2
        };

        state = state with
        {
            UserHeadCoach = coach,
            TeamSeasonHistory = new[]
            {
                Season(
                    "User",
                    11,
                    2,
                    conferenceChampion: true,
                    playoff: true,
                    finalRank: 6,
                    recruitingAverage: 84),
                Season(
                    "Rival",
                    4,
                    8,
                    recruitingAverage: 72),
                Season(
                    "Rebuild",
                    3,
                    9,
                    recruitingAverage: 70)
            },
            ProgramPrestigeHistory = new[]
            {
                Prestige("User", 70, 75),
                Prestige("Rival", 78, 78),
                Prestige("Rebuild", 55, 55)
            },
            PlayerDevelopmentHistory = new[]
            {
                Development("User", 2),
                Development("User", 2),
                Development("User", 1)
            }
        };

        var updated =
            UserCoachCareerService.EvaluateCompletedSeason(
                state,
                teams);

        Assert.False(updated.UserCoachIsFired);
        Assert.True(
            updated.UserCoachJobSecurity >
            state.UserCoachJobSecurity);
        Assert.True(
            updated.UserHeadCoach!.OverallRating >=
            coach.OverallRating);
        Assert.True(
            updated.UserHeadCoach.Reputation >
            coach.Reputation);
        Assert.NotEmpty(
            updated.UserCoachJobOffers.Where(item =>
                item.Status ==
                    UserCoachJobOfferStatus.Pending));
        Assert.Single(updated.UserCoachCareerHistory);
    }

    [Fact]
    public void DisasterSeasonCanFireUserAndCreatesLandingOptions()
    {
        var teams = Teams();
        var state = StaffManagementService.EnsureLeagueStaff(
            State(),
            teams.Values);
        state = state with
        {
            UserCoachJobSecurity = 52,
            TeamSeasonHistory = new[]
            {
                Season(
                    "User",
                    2,
                    10,
                    finalRank: 0,
                    recruitingAverage: 64)
            },
            ProgramPrestigeHistory = new[]
            {
                Prestige("User", 82, 76),
                Prestige("Rival", 78, 78),
                Prestige("Rebuild", 55, 55)
            }
        };

        var updated =
            UserCoachCareerService.EvaluateCompletedSeason(
                state,
                teams);

        Assert.True(updated.UserCoachIsFired);
        Assert.Equal(
            "User",
            updated.UserCoachFiredFromTeamName);
        Assert.True(
            updated.UserCoachJobSecurity <= 20);
        Assert.True(
            updated.UserCoachJobOffers.Count(item =>
                item.Status ==
                    UserCoachJobOfferStatus.Pending) >= 3);
        Assert.True(
            updated.UserCoachCareerHistory
                .Single()
                .WasFired);
    }

    [Fact]
    public void AcceptingJobOfferMovesUserAndRestoresCpuHeadCoachAtOldSchool()
    {
        var teams = Teams();
        var state = StaffManagementService.EnsureLeagueStaff(
            State(),
            teams.Values);

        var offer = new UserCoachJobOffer
        {
            OfferId = Guid.Parse(
                "99999999-8888-7777-6666-555555555555"),
            SeasonYear = state.SeasonYear,
            TeamName = "Rebuild",
            ProgramPrestige = 55,
            ContractYears = 4,
            FitScore = 88,
            Reason = "Test offer"
        };

        state = state with
        {
            UserCoachIsFired = true,
            UserCoachFiredFromTeamName = "User",
            UserCoachJobOffers = new[] { offer },
            StaffMarketSeasonYear = 0,
            StaffMarketProcessedSeasonYear = 0
        };

        Assert.Contains(
            state.Staff,
            item =>
                item.TeamName == "Rebuild" &&
                item.Role == StaffRole.HeadCoach);

        var moved =
            UserCoachCareerService.AcceptJobOffer(
                state,
                offer.OfferId,
                teams);

        Assert.Equal("Rebuild", moved.UserTeamName);
        Assert.Equal(
            "Rebuild",
            moved.UserHeadCoach!.TeamName);
        Assert.False(moved.UserCoachIsFired);
        Assert.Equal(
            UserCoachJobOfferStatus.Accepted,
            moved.UserCoachJobOffers
                .Single(item =>
                    item.OfferId == offer.OfferId)
                .Status);

        Assert.DoesNotContain(
            moved.Staff,
            item =>
                item.TeamName == "Rebuild" &&
                item.Role == StaffRole.HeadCoach);
        Assert.Contains(
            moved.Staff,
            item =>
                item.TeamName == "User" &&
                item.Role == StaffRole.HeadCoach);
    }

    private static DynastyState State() => new()
    {
        DynastyId = Guid.Parse(
            "12345678-1234-5678-9012-123456789012"),
        DynastyName = "Coach Career Test",
        UserTeamName = "User",
        SeasonYear = 2028,
        Week = 19,
        Phase = SeasonPhase.TransferPortal,
        UserCoachJobSecurity = 70
    };

    private static Dictionary<string, Team> Teams()
    {
        var teams = new[]
        {
            Team("User", 72),
            Team("Rival", 78),
            Team("Rebuild", 55),
            Team("Small", 45),
            Team("Lower", 40)
        };

        return teams.ToDictionary(
            item => item.Name,
            StringComparer.OrdinalIgnoreCase);
    }

    private static Team Team(
        string name,
        int prestige) => new()
    {
        Name = name,
        Abbreviation =
            name[..Math.Min(3, name.Length)]
                .ToUpperInvariant(),
        ConferenceName = "Test",
        Prestige = prestige,
        LegacyRegionId = 1
    };

    private static TeamSeasonHistoryRecord Season(
        string team,
        int wins,
        int losses,
        bool conferenceChampion = false,
        bool playoff = false,
        bool nationalChampion = false,
        int finalRank = 0,
        double recruitingAverage = 75) => new()
    {
        SeasonYear = 2028,
        TeamName = team,
        Wins = wins,
        Losses = losses,
        ConferenceWins = Math.Min(wins, 8),
        ConferenceLosses = Math.Min(losses, 6),
        ConferenceChampion = conferenceChampion,
        PlayoffParticipant = playoff,
        NationalChampion = nationalChampion,
        FinalRanking = finalRank,
        RecruitingAverageRating = recruitingAverage
    };

    private static ProgramPrestigeSnapshot Prestige(
        string team,
        int starting,
        int ending) => new()
    {
        SeasonYear = 2028,
        TeamName = team,
        StartingPrestige = starting,
        EndingPrestige = ending,
        Reasons = new[] { "Test" }
    };

    private static PlayerDevelopmentRecord Development(
        string team,
        int change) => new()
    {
        SeasonYear = 2028,
        PlayerId = Guid.NewGuid(),
        PlayerName = "Player",
        TeamName = team,
        Position = Position.QB,
        BeforeOverall = 70,
        AfterOverall = 70 + change,
        BeforePotential = 80,
        AfterPotential = 80,
        Stage = PlayerDevelopmentStage.Midseason
    };
}
