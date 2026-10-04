using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class TransferPortalMarketTests
{
    [Fact]
    public void RegularSeasonPortalAttritionRemovesSomePlayersAndKeepsHoldovers()
    {
        var entries = Enumerable.Range(1, 400)
            .Select(index => new TransferPortalEntry
            {
                SeasonYear = 2026,
                OriginTeamName = $"School {index}",
                WeeksInPortal = 4,
                Player = Player(
                    Guid.NewGuid(),
                    $"Player {index}",
                    $"School {index}",
                    Position.WR,
                    4,
                    62)
            })
            .ToArray();

        var state = new DynastyState
        {
            DynastyId = Guid.Parse(
                "12121212-3434-5656-7878-909090909090"),
            DynastyName = "Portal Attrition",
            UserTeamName = "User",
            SeasonYear = 2027,
            Week = 5,
            Phase = SeasonPhase.RegularSeason,
            TransferPortalEntries = entries
        };

        var updated =
            TransferPortalMarketService.AdvanceRegularSeasonWeek(
                state);

        Assert.NotEmpty(updated.TransferPortalExitHistory);
        Assert.NotEmpty(updated.TransferPortalEntries);
        Assert.Equal(
            entries.Length,
            updated.TransferPortalEntries.Count +
            updated.TransferPortalExitHistory.Count);

        Assert.All(
            updated.TransferPortalEntries,
            entry => Assert.Equal(5, entry.WeeksInPortal));
    }

    [Fact]
    public void MidseasonTransferCommitmentJoinsFollowingSeason()
    {
        var user = Team("User", 100);
        var rival = Team("Rival", 70);
        var prospect = Player(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            "Unsigned Transfer",
            "Old School",
            Position.QB,
            3,
            84);

        var state = new DynastyState
        {
            DynastyId = Guid.Parse(
                "11111111-2222-3333-4444-555555555555"),
            DynastyName = "Midseason Portal",
            UserTeamName = user.Name,
            SeasonYear = 2027,
            Week = 6,
            Phase = SeasonPhase.RegularSeason,
            TransferPortalEntries = new[]
            {
                new TransferPortalEntry
                {
                    SeasonYear = 2026,
                    OriginTeamName = prospect.TeamName,
                    WeeksInPortal = 8,
                    Player = prospect
                }
            }
        };

        state = InteractiveRecruitingService
            .EnsurePhaseInitialized(state, user);

        state = InteractiveRecruitingService.ToggleScholarship(
            state,
            RecruitingSource.TransferPortal,
            prospect.PlayerId);

        for (var index = 0; index < 5; index++)
        {
            state = InteractiveRecruitingService.Pitch(
                state,
                user,
                RecruitingSource.TransferPortal,
                prospect.PlayerId);
        }

        state = InteractiveRecruitingService.ResolveCurrentPhase(
            state,
            user,
            new[] { user, rival }.ToDictionary(
                team => team.Name,
                StringComparer.OrdinalIgnoreCase));

        Assert.DoesNotContain(
            state.ActiveRoster,
            player => player.PlayerId == prospect.PlayerId);

        var pending = Assert.Single(
            state.PendingTransferCommitments,
            commitment =>
                commitment.ProspectId == prospect.PlayerId);

        Assert.Equal(2028, pending.JoinSeasonYear);
        Assert.Equal(user.Name, pending.TeamName);
        Assert.DoesNotContain(
            state.TransferPortalEntries,
            entry => entry.Player.PlayerId == prospect.PlayerId);

        var commitment = Assert.Single(
            state.RecruitingCommitments,
            record => record.ProspectId == prospect.PlayerId);

        Assert.Equal(2028, commitment.JoinSeasonYear);
    }

    [Fact]
    public void PendingTransferMaterializesDuringUpcomingOffseason()
    {
        var prospect = Player(
            Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff"),
            "Future Transfer",
            "Old School",
            Position.CB,
            2,
            82);

        var state = new DynastyState
        {
            DynastyName = "Pending Transfer",
            UserTeamName = "User",
            SeasonYear = 2027,
            Week = 19,
            Phase = SeasonPhase.TransferPortal,
            PendingTransferCommitments = new[]
            {
                new PendingTransferCommitment
                {
                    ProspectId = prospect.PlayerId,
                    CommittedSeasonYear = 2027,
                    CommittedWeek = 8,
                    JoinSeasonYear = 2028,
                    TeamName = "User",
                    OriginTeamName = "Old School",
                    Player = prospect,
                    WasUserCommitment = true
                }
            }
        };

        var updated =
            TransferPortalMarketService.MaterializePendingCommitments(
                state);

        var joined = Assert.Single(
            updated.ActiveRoster,
            player => player.PlayerId == prospect.PlayerId);

        Assert.Equal("User", joined.TeamName);
        Assert.Empty(updated.PendingTransferCommitments);
    }

    [Fact]
    public void PortalEntryWindowPreservesExistingHoldover()
    {
        var holdover = Player(
            Guid.Parse("cccccccc-dddd-eeee-ffff-000000000001"),
            "Patient Holdover",
            "Old School",
            Position.RB,
            3,
            76);

        var active = Enumerable.Range(1, 90)
            .Select(index => Player(
                Guid.NewGuid(),
                $"Active {index}",
                "User",
                (Position)(index %
                    Enum.GetValues<Position>().Length),
                1 + index % 3,
                70 + index % 15))
            .ToArray();

        var state = new DynastyState
        {
            DynastyId = Guid.Parse(
                "dddddddd-eeee-ffff-0000-111111111111"),
            DynastyName = "Holdover Test",
            UserTeamName = "User",
            SeasonYear = 2027,
            Week = 19,
            Phase = SeasonPhase.TransferPortal,
            ActiveRoster = active,
            TransferPortalEntries = new[]
            {
                new TransferPortalEntry
                {
                    SeasonYear = 2026,
                    OriginTeamName = holdover.TeamName,
                    WeeksInPortal = 13,
                    Player = holdover
                }
            }
        };

        var updated =
            OffseasonPlayerLifecycleService.EnterTransferPortal(
                state);

        Assert.Contains(
            updated.TransferPortalEntries,
            entry => entry.Player.PlayerId == holdover.PlayerId);
        Assert.Equal(
            2027,
            updated.TransferPortalEntryWindowSeasonYear);
    }

    private static DynastyPlayer Player(
        Guid id,
        string name,
        string team,
        Position position,
        int classYear,
        int overall) =>
        new()
        {
            PlayerId = id,
            FullName = name,
            TeamName = team,
            Position = position,
            ClassYear = classYear,
            TalentLevel = 7,
            OverallRating = overall,
            PotentialRating = Math.Min(99, overall + 8)
        };

    private static Team Team(
        string name,
        int prestige) =>
        new()
        {
            Name = name,
            Abbreviation = name,
            ConferenceName = "Test",
            Prestige = prestige
        };
}
