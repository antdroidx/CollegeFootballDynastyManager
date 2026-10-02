using DynastyManager.Core.Models;
using DynastyManager.Data.Persistence;
using Xunit;

namespace DynastyManager.Core.Tests;

public class TransferPortalPersistenceTests
{
    [Fact]
    public async Task SaveRoundTripPreservesPersistentPortalMarket()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"cfdm-portal-{Guid.NewGuid():N}.db3");

        try
        {
            var player = new DynastyPlayer
            {
                PlayerId = Guid.Parse(
                    "11111111-aaaa-bbbb-cccc-222222222222"),
                FullName = "Portal Holdover",
                TeamName = "Old School",
                Position = Position.WR,
                ClassYear = 3,
                TalentLevel = 7,
                OverallRating = 81,
                PotentialRating = 88
            };

            var futurePlayer = player with
            {
                PlayerId = Guid.Parse(
                    "33333333-aaaa-bbbb-cccc-444444444444"),
                FullName = "Future Commit"
            };

            var state = new DynastyState
            {
                DynastyName = "Portal Persistence",
                UserTeamName = "Washington",
                SeasonYear = 2028,
                Week = 7,
                Phase = SeasonPhase.RegularSeason,
                TransferPortalEntryWindowSeasonYear = 2027,
                RecruitingPointsRemaining = 375,
                RecruitingPointsPhase =
                    SeasonPhase.RegularSeason,
                RecruitingPointsWeek = 7,
                TransferPortalEntries = new[]
                {
                    new TransferPortalEntry
                    {
                        SeasonYear = 2027,
                        OriginTeamName = "Old School",
                        WeeksInPortal = 12,
                        Player = player
                    }
                },
                PendingTransferCommitments = new[]
                {
                    new PendingTransferCommitment
                    {
                        ProspectId = futurePlayer.PlayerId,
                        CommittedSeasonYear = 2028,
                        CommittedWeek = 7,
                        JoinSeasonYear = 2029,
                        TeamName = "Washington",
                        OriginTeamName = "Old School",
                        Player = futurePlayer,
                        WasUserCommitment = true
                    }
                },
                TransferPortalExitHistory = new[]
                {
                    new TransferPortalExitRecord
                    {
                        EntrySeasonYear = 2027,
                        ExitSeasonYear = 2028,
                        ExitWeek = 6,
                        PlayerId = Guid.Parse(
                            "55555555-aaaa-bbbb-cccc-666666666666"),
                        PlayerName = "FCS Bound",
                        OriginTeamName = "Another School",
                        Position = Position.CB,
                        OverallRating = 68,
                        Destination =
                            TransferPortalExitDestination.Fcs
                    }
                }
            };

            await using var repository =
                new SqliteDynastySaveRepository(path);

            var id = await repository.SaveAsync(
                state,
                SaveKind.Manual);
            var loaded = await repository.LoadAsync(id);

            Assert.NotNull(loaded);
            Assert.Equal(
                2027,
                loaded.TransferPortalEntryWindowSeasonYear);
            Assert.Equal(7, loaded.RecruitingPointsWeek);

            var holdover = Assert.Single(
                loaded.TransferPortalEntries);
            Assert.Equal(12, holdover.WeeksInPortal);

            var pending = Assert.Single(
                loaded.PendingTransferCommitments);
            Assert.Equal(2029, pending.JoinSeasonYear);
            Assert.True(pending.WasUserCommitment);

            var exit = Assert.Single(
                loaded.TransferPortalExitHistory);
            Assert.Equal(
                TransferPortalExitDestination.Fcs,
                exit.Destination);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
