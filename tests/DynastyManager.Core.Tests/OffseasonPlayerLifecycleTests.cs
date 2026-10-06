using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class OffseasonPlayerLifecycleTests
{
    [Fact]
    public void EnterTransferPortalRemovesGraduatesAndAdvancesReturners()
    {
        var senior = Player("Senior", "Test", Position.QB, 4, 80);
        var junior = Player("Junior", "Test", Position.QB, 3, 85);
        var sophomore = Player("Sophomore", "Test", Position.RB, 2, 82);

        var state = State(new[] { senior, junior, sophomore });

        var updated =
            OffseasonPlayerLifecycleService.EnterTransferPortal(state);

        Assert.DoesNotContain(
            updated.ActiveRoster,
            player => player.PlayerId == senior.PlayerId);

        var graduation = Assert.Single(
            updated.RecentPlayerDepartures,
            departure =>
                departure.PlayerId == senior.PlayerId);

        Assert.Equal(
            PlayerDepartureReason.Graduation,
            graduation.Reason);

        var juniorResult = updated.ActiveRoster
            .Concat(updated.TransferPortalEntries.Select(entry => entry.Player))
            .Single(player => player.PlayerId == junior.PlayerId);

        Assert.Equal(4, juniorResult.ClassYear);
    }

    [Fact]
    public void CompletedRedshirtSeasonPreservesClassYearAndMarksRedshirtUsed()
    {
        var redshirtSenior = Player(
            "Redshirt Senior",
            "Test",
            Position.QB,
            4,
            88) with
        {
            IsRedshirted = true
        };

        var state = State(new[] { redshirtSenior });

        var updated =
            OffseasonPlayerLifecycleService.EnterTransferPortal(state);

        var player = updated.ActiveRoster
            .Concat(updated.TransferPortalEntries.Select(entry => entry.Player))
            .Single(candidate =>
                candidate.PlayerId == redshirtSenior.PlayerId);

        Assert.Equal(4, player.ClassYear);
        Assert.False(player.IsRedshirted);
        Assert.True(player.HasRedshirted);
        Assert.DoesNotContain(
            updated.RecentPlayerDepartures,
            departure =>
                departure.PlayerId == redshirtSenior.PlayerId);
    }

    [Fact]
    public void LargeRosterProducesPortalInsideRequestedRangeWithoutDuplicates()
    {
        var players = new List<DynastyPlayer>();

        for (var team = 1; team <= 130; team++)
        {
            for (var player = 1; player <= DynastyRosterRules.MaximumRosterSize; player++)
            {
                players.Add(Player(
                    $"Player {team}-{player}",
                    $"Team {team}",
                    PositionFor(player),
                    1 + player % 3,
                    65 + player % 25));
            }
        }

        var updated =
            OffseasonPlayerLifecycleService.EnterTransferPortal(
                State(players));

        Assert.InRange(
            updated.TransferPortalEntries.Count,
            OffseasonPlayerLifecycleService.MinimumPortalSize,
            OffseasonPlayerLifecycleService.MaximumPortalSize);

        var activeIds = updated.ActiveRoster
            .Select(player => player.PlayerId)
            .ToHashSet();

        Assert.DoesNotContain(
            updated.TransferPortalEntries,
            entry => activeIds.Contains(entry.Player.PlayerId));

        Assert.Equal(
            updated.TransferPortalEntries.Count,
            updated.TransferPortalEntries
                .Select(entry => entry.Player.PlayerId)
                .Distinct()
                .Count());
    }

    [Fact]
    public void ProcessingSamePortalSeasonIsIdempotent()
    {
        var players = Enumerable.Range(1, 80)
            .Select(index => Player(
                $"Player {index}",
                "Test",
                PositionFor(index),
                1 + index % 3,
                75))
            .ToArray();

        var once =
            OffseasonPlayerLifecycleService.EnterTransferPortal(
                State(players));

        var twice =
            OffseasonPlayerLifecycleService.EnterTransferPortal(once);

        Assert.Equal(once, twice);
    }

    private static DynastyState State(
        IEnumerable<DynastyPlayer> players) =>
        new()
        {
            DynastyId =
                Guid.Parse("8d340aab-343b-4e7c-85c6-c785633f75d2"),
            DynastyName = "Offseason Test",
            UserTeamName = "Test",
            SeasonYear = 2026,
            Week = 19,
            Phase = SeasonPhase.TransferPortal,
            ActiveRoster = players.ToArray()
        };

    private static DynastyPlayer Player(
        string name,
        string team,
        Position position,
        int classYear,
        int overall) =>
        new()
        {
            PlayerId = Guid.NewGuid(),
            FullName = name,
            TeamName = team,
            Position = position,
            ClassYear = classYear,
            TalentLevel = Math.Clamp(
                (int)Math.Round((overall - 50) / 4.5),
                1,
                10),
            OverallRating = overall
        };

    private static Position PositionFor(int index) =>
        (Position)(index % Enum.GetValues<Position>().Length);
}
