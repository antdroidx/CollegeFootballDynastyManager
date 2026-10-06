using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public sealed class UserCoachCareerTests
{
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
