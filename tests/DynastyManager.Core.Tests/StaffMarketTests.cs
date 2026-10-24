using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public sealed class StaffMarketTests
{
    [Fact]
    public void MarketIsPersistentAndProvidesMultipleCandidatesPerRole()
    {
        var teams = Teams();
        var state = BaseState();
        state = StaffManagementService.EnsureLeagueStaff(
            state, teams.Values);

        var first = StaffMarketService.EnsureMarket(state, teams);
        var second = StaffMarketService.EnsureMarket(first, teams);

        Assert.Equal(
            first.StaffMarketCandidates.Select(item => item.CandidateId),
            second.StaffMarketCandidates.Select(item => item.CandidateId));

        var user = teams["User"];
        foreach (var role in Enum.GetValues<StaffRole>()
                     .Where(role => role != StaffRole.HeadCoach))
        {
            var candidates = StaffMarketService.GetCandidatesForTeam(
                first, user, role, 8);
            Assert.True(candidates.Count >= 5);
        }

        Assert.Empty(StaffMarketService.GetCandidatesForTeam(
            first, user, StaffRole.HeadCoach, 8));
        Assert.Contains(first.StaffMarketCandidates, item =>
            item.Origin == StaffCandidateOrigin.FreeAgent);
        Assert.Contains(first.StaffMarketCandidates, item =>
            item.TargetRole == StaffRole.HeadCoach &&
            item.SourceRole is StaffRole.OffensiveCoordinator or
                StaffRole.DefensiveCoordinator);
    }

    [Fact]
    public void CpuFiresBadHeadCoachAndRefillsEveryVacancy()
    {
        var teams = Teams(cpuPrestige: 86);
        var state = BaseState() with
        {
            TeamSeasonHistory = new[]
            {
                Season("CPU", 2, 10),
                Season("User", 8, 4)
            },
            Staff = FullStaff("User", 72, includeHeadCoach: false)
                .Concat(FullStaff("CPU", 62))
                .ToArray()
        };

        var oldHeadCoach = state.Staff.Single(item =>
            item.TeamName == "CPU" &&
            item.Role == StaffRole.HeadCoach);
        state = state with
        {
            Staff = state.Staff.Select(item =>
                item.StaffId == oldHeadCoach.StaffId
                    ? item with { ContractYearsRemaining = 0 }
                    : item).ToArray()
        };

        state = StaffMarketService.EnsureMarket(state, teams);
        var updated = StaffMarketService.RunCpuCarousel(state, teams);

        var cpuStaff = updated.Staff
            .Where(item => item.TeamName == "CPU")
            .ToArray();

        Assert.Equal(
            Enum.GetValues<StaffRole>().Length,
            cpuStaff.Length);
        Assert.DoesNotContain(cpuStaff,
            item => item.StaffId == oldHeadCoach.StaffId &&
                    item.Role == StaffRole.HeadCoach);
        Assert.Contains(updated.StaffMovementHistory, item =>
            item.StaffId == oldHeadCoach.StaffId &&
            item.MovementType == StaffMovementType.Fired);
        Assert.Contains(updated.StaffMovementHistory, item =>
            item.ToTeamName == "CPU" &&
            item.Role == StaffRole.HeadCoach &&
            item.MovementType is
                StaffMovementType.Hired or
                StaffMovementType.Promoted or
                StaffMovementType.Poached);
    }

    [Fact]
    public void SuccessfulCpuHeadCoachIsRetainedAtContractEnd()
    {
        var teams = Teams(cpuPrestige: 78);
        var cpuStaff = FullStaff("CPU", 73).ToArray();
        var headCoach = cpuStaff.Single(item =>
            item.Role == StaffRole.HeadCoach);

        var state = BaseState() with
        {
            TeamSeasonHistory = new[]
            {
                Season("CPU", 11, 1),
                Season("User", 8, 4)
            },
            Staff = FullStaff("User", 70, includeHeadCoach: false)
                .Concat(cpuStaff.Select(item =>
                    item.StaffId == headCoach.StaffId
                        ? item with { ContractYearsRemaining = 0 }
                        : item))
                .ToArray()
        };

        var updated = StaffMarketService.RunCpuCarousel(
            StaffMarketService.EnsureMarket(state, teams),
            teams);

        var retained = updated.Staff.Single(item =>
            item.TeamName == "CPU" &&
            item.Role == StaffRole.HeadCoach);

        Assert.Equal(headCoach.StaffId, retained.StaffId);
        Assert.True(retained.ContractYearsRemaining >= 3);
        Assert.DoesNotContain(updated.StaffMovementHistory, item =>
            item.StaffId == headCoach.StaffId &&
            item.MovementType == StaffMovementType.Fired);
    }

    [Fact]
    public void UserCannotHireHeadCoachBecauseUserIsHeadCoach()
    {
        var teams = Teams();
        var state = BaseState() with
        {
            Staff = FullStaff("User", 78, includeHeadCoach: false)
                .Concat(FullStaff("CPU", 70))
                .ToArray()
        };

        state = StaffMarketService.EnsureMarket(state, teams);
        var coordinator = state.Staff.Single(item =>
            item.TeamName == "User" &&
            item.Role == StaffRole.OffensiveCoordinator);

        var candidate = state.StaffMarketCandidates.Single(item =>
            item.Profile.StaffId == coordinator.StaffId &&
            item.TargetRole == StaffRole.HeadCoach);

        var updated = StaffMarketService.HireUserCandidate(
            state,
            teams["User"],
            candidate.CandidateId,
            teams);

        Assert.DoesNotContain(updated.Staff, item =>
            item.TeamName == "User" &&
            item.Role == StaffRole.HeadCoach);
        Assert.Contains(updated.Staff, item =>
            item.StaffId == coordinator.StaffId &&
            item.TeamName == "User" &&
            item.Role == StaffRole.OffensiveCoordinator);
    }

    private static DynastyState BaseState() => new()
    {
        DynastyId = Guid.Parse("84848484-7373-6262-5151-404040404040"),
        DynastyName = "Staff Market Test",
        UserTeamName = "User",
        SeasonYear = 2027,
        Week = 18,
        Phase = SeasonPhase.TransferPortal
    };

    private static Dictionary<string, Team> Teams(
        int cpuPrestige = 72)
    {
        var teams = new[]
        {
            new Team
            {
                Name = "User",
                Abbreviation = "USR",
                ConferenceName = "Test",
                Prestige = 75,
                LegacyRegionId = 1
            },
            new Team
            {
                Name = "CPU",
                Abbreviation = "CPU",
                ConferenceName = "Test",
                Prestige = cpuPrestige,
                LegacyRegionId = 2
            }
        };

        return teams.ToDictionary(
            item => item.Name,
            StringComparer.OrdinalIgnoreCase);
    }

    private static TeamSeasonHistoryRecord Season(
        string team,
        int wins,
        int losses) => new()
    {
        SeasonYear = 2027,
        TeamName = team,
        Wins = wins,
        Losses = losses,
        ConferenceWins = Math.Min(wins, 7),
        ConferenceLosses = Math.Min(losses, 5)
    };

    private static IEnumerable<StaffMember> FullStaff(
        string team,
        int rating,
        bool includeHeadCoach = true)
    {
        foreach (var role in Enum.GetValues<StaffRole>())
        {
            if (!includeHeadCoach &&
                role == StaffRole.HeadCoach)
            {
                continue;
            }

            yield return Staff(team, role, rating);
        }
    }

    private static StaffMember Staff(
        string team,
        StaffRole role,
        int rating) => new()
    {
        StaffId = Guid.NewGuid(),
        FullName = $"{team} {role}",
        TeamName = team,
        Role = role,
        Reputation = rating,
        Leadership = rating,
        Recruiting = rating,
        PlayerDevelopment = rating,
        GameManagement = rating,
        Scheme = rating,
        SpecialTeams = rating,
        Medical = rating,
        Conditioning = rating,
        TalentEvaluation = rating,
        PotentialEvaluation = rating,
        RegionalKnowledge = rating,
        StaffManagement = rating,
        Age = 44,
        CareerYears = 12,
        GrowthPotential = 3,
        ContractYearsRemaining = 2
    };
}
