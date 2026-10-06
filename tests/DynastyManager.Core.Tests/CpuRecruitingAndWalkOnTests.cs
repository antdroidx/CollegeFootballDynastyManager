using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class CpuRecruitingAndWalkOnTests
{
    [Fact]
    public void SkippingManualRecruitingUsesVisibleAssistanceThenWalkOns()
    {
        var user = Team("User", 80);
        var rival = Team("Rival", 70);
        var teams = new[] { user, rival }
            .ToDictionary(
                team => team.Name,
                StringComparer.OrdinalIgnoreCase);

        var state = new DynastyState
        {
            DynastyId =
                Guid.Parse("10101010-2020-3030-4040-505050505050"),
            DynastyName = "CPU Assist Test",
            UserTeamName = user.Name,
            SeasonYear = 2027,
            Week = 20,
            Phase = SeasonPhase.Recruiting,
            RecruitingAssistanceEnabled = true,
            ActiveRoster = BuildRoster(user.Name, 60)
                .Concat(BuildRoster(rival.Name, 60))
                .ToArray(),
            HighSchoolRecruitingPool = BuildRecruitPool(80)
        };

        var assisted =
            CpuRecruitingService.ApplyWeeklyUserAssistance(
                state,
                user);

        Assert.Contains(
            assisted.RecruitingInteractions,
            interaction =>
                interaction.WasCpuAssisted &&
                interaction.IsOnTargetBoard &&
                interaction.ScholarshipOffered);

        var finalized = CpuRecruitingService.ApplyPhaseAssistance(
            assisted,
            teams);

        Assert.Equal(
            60,
            finalized.ActiveRoster.Count(player =>
                player.TeamName == user.Name));

        Assert.DoesNotContain(
            finalized.RecruitingCommitments,
            commitment =>
                commitment.TeamName == user.Name &&
                commitment.Source == RecruitingSource.HighSchool);

        var filled = WalkOnRosterService.FillAllTeams(
            finalized with
            {
                Phase = SeasonPhase.RosterManagement
            },
            teams);

        Assert.Equal(
            DynastyRosterRules.MaximumRosterSize,
            filled.ActiveRoster.Count(player =>
                player.TeamName == user.Name));

        Assert.Contains(
            filled.ActiveRoster,
            player =>
                player.TeamName == user.Name &&
                player.IsWalkOn);
    }

    [Fact]
    public void TransferCpuAssistIsCappedSoHighSchoolRecruitingStillMatters()
    {
        var user = Team("User", 85);
        var teams = new[] { user }
            .ToDictionary(
                team => team.Name,
                StringComparer.OrdinalIgnoreCase);

        var portal = Enumerable.Range(1, 30)
            .Select(index => new TransferPortalEntry
            {
                SeasonYear = 2027,
                OriginTeamName = "Other",
                Player = Player(
                    $"Transfer {index}",
                    "Other",
                    PositionFor(index),
                    3,
                    75 + index % 10)
            })
            .ToArray();

        var state = new DynastyState
        {
            DynastyId =
                Guid.Parse("11111111-2222-3333-4444-666666666666"),
            DynastyName = "Transfer Assist Test",
            UserTeamName = user.Name,
            SeasonYear = 2027,
            Week = 19,
            Phase = SeasonPhase.TransferPortal,
            ActiveRoster = BuildRoster(user.Name, 50),
            TransferPortalEntries = portal
        };

        var updated = CpuRecruitingService.ApplyPhaseAssistance(
            state,
            teams);

        var additions = updated.ActiveRoster.Count(player =>
            player.TeamName == user.Name) - 50;

        Assert.Equal(
            CpuRecruitingService.MaximumCpuTransferAdditionsPerTeam,
            additions);
    }

    [Fact]
    public void DisabledUserAssistanceStillLetsCpuTeamsRecruit()
    {
        var user = Team("User", 80);
        var rival = Team("Rival", 70);
        var teams = new[] { user, rival }.ToDictionary(
            team => team.Name, StringComparer.OrdinalIgnoreCase);
        var state = new DynastyState
        {
            DynastyName = "Assistance Toggle Test",
            UserTeamName = user.Name,
            SeasonYear = 2027,
            Week = 20,
            Phase = SeasonPhase.Recruiting,
            RecruitingAssistanceEnabled = false,
            ActiveRoster = BuildRoster(user.Name, 60)
                .Concat(BuildRoster(rival.Name, 60)).ToArray(),
            HighSchoolRecruitingPool = BuildRecruitPool(80)
        };

        var updated = CpuRecruitingService.ApplyPhaseAssistance(
            state, teams, state.RecruitingAssistanceEnabled);

        Assert.Equal(60, updated.ActiveRoster.Count(player =>
            player.TeamName == user.Name));
        Assert.True(updated.ActiveRoster.Count(player =>
            player.TeamName == rival.Name) > 60);
        Assert.DoesNotContain(updated.RecruitingCommitments,
            commitment => commitment.TeamName == user.Name);
        Assert.Contains(updated.RecruitingCommitments,
            commitment => commitment.TeamName == rival.Name);
    }

    [Fact]
    public void WalkOnsFillEveryTeamToExactlyEightyFivePlayers()
    {
        var user = Team("User", 80);
        var rival = Team("Rival", 65);
        var teams = new[] { user, rival }
            .ToDictionary(
                team => team.Name,
                StringComparer.OrdinalIgnoreCase);

        var state = new DynastyState
        {
            DynastyId =
                Guid.Parse("abababab-cdcd-efef-1212-343434343434"),
            DynastyName = "Walk On Test",
            UserTeamName = user.Name,
            SeasonYear = 2027,
            Week = 21,
            Phase = SeasonPhase.RosterManagement,
            ActiveRoster = BuildRoster(user.Name, 72)
                .Concat(BuildRoster(rival.Name, 81))
                .ToArray()
        };

        var updated = WalkOnRosterService.FillAllTeams(
            state,
            teams);

        Assert.Equal(
            85,
            updated.ActiveRoster.Count(player =>
                player.TeamName == user.Name));

        Assert.Equal(
            85,
            updated.ActiveRoster.Count(player =>
                player.TeamName == rival.Name));

        Assert.Equal(
            13,
            updated.ActiveRoster.Count(player =>
                player.TeamName == user.Name &&
                player.IsWalkOn));

        Assert.Equal(
            4,
            updated.ActiveRoster.Count(player =>
                player.TeamName == rival.Name &&
                player.IsWalkOn));

        Assert.All(
            updated.ActiveRoster.Where(player => player.IsWalkOn),
            player => Assert.Equal(1, player.ClassYear));
    }

    private static IReadOnlyList<DynastyPlayer> BuildRoster(
        string teamName,
        int count) =>
        Enumerable.Range(1, count)
            .Select(index => Player(
                $"{teamName} Player {index}",
                teamName,
                PositionFor(index),
                1 + index % 4,
                70 + index % 12))
            .ToArray();

    private static IReadOnlyList<HighSchoolRecruit> BuildRecruitPool(
        int count) =>
        Enumerable.Range(1, count)
            .Select(index => new HighSchoolRecruit
            {
                RecruitId = GuidFrom(index),
                FullName = $"Recruit {index}",
                Position = PositionFor(index),
                StarRating = 3 + index % 3,
                TrueOverallRating = 70 + index % 18,
                PotentialRating = 82 + index % 12,
                HomeRegion = index % 50
            })
            .ToArray();

    private static DynastyPlayer Player(
        string name,
        string teamName,
        Position position,
        int classYear,
        int overall) =>
        new()
        {
            PlayerId = Guid.NewGuid(),
            FullName = name,
            TeamName = teamName,
            Position = position,
            ClassYear = classYear,
            TalentLevel = 6,
            OverallRating = overall
        };

    private static Position PositionFor(int index)
    {
        var positions = Enum.GetValues<Position>();
        return positions[index % positions.Length];
    }

    private static Guid GuidFrom(int value)
    {
        var bytes = new byte[16];
        BitConverter.GetBytes(value).CopyTo(bytes, 0);
        return new Guid(bytes);
    }

    private static Team Team(string name, int prestige) =>
        new()
        {
            Name = name,
            Abbreviation = name,
            ConferenceName = "Test",
            Prestige = prestige
        };
}
