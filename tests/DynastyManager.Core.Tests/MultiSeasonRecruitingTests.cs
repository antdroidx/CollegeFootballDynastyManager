using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Import;
using Xunit;

namespace DynastyManager.Core.Tests;

public class MultiSeasonRecruitingTests
{
    [Fact]
    public void RecruitingPoolsAreSeasonSpecific()
    {
        var dynastyId =
            Guid.Parse("90909090-8080-7070-6060-505050505050");

        var first = HighSchoolRecruitingPoolFactory.Create(
            dynastyId,
            2027,
            2,
            Array.Empty<ImportedPlayerRow>());

        var second = HighSchoolRecruitingPoolFactory.Create(
            dynastyId,
            2028,
            2,
            Array.Empty<ImportedPlayerRow>());

        Assert.All(first, recruit =>
            Assert.Equal(2027, recruit.SeasonYear));

        Assert.All(second, recruit =>
            Assert.Equal(2028, recruit.SeasonYear));

        Assert.Empty(
            first.Select(recruit => recruit.RecruitId)
                .Intersect(second.Select(recruit => recruit.RecruitId)));
    }

    [Fact]
    public void PortalTransitionCleansDuplicatePlayersAndStaleRecruitPool()
    {
        var id =
            Guid.Parse("11111111-aaaa-bbbb-cccc-222222222222");

        var player = new DynastyPlayer
        {
            PlayerId = id,
            FullName = "Duplicate Player",
            TeamName = "Test",
            Position = Position.QB,
            ClassYear = 2,
            TalentLevel = 7,
            OverallRating = 82,
            PotentialRating = 90
        };

        var state = new DynastyState
        {
            DynastyName = "Multi Season",
            UserTeamName = "Test",
            SeasonYear = 2028,
            Week = 19,
            Phase = SeasonPhase.TransferPortal,
            ActiveRoster = new[]
            {
                player,
                player with
                {
                    ClassYear = 1,
                    OverallRating = 78
                }
            },
            HighSchoolRecruitingPool = new[]
            {
                new HighSchoolRecruit
                {
                    RecruitId = Guid.NewGuid(),
                    SeasonYear = 2027,
                    FullName = "Stale Recruit",
                    Position = Position.WR,
                    StarRating = 4,
                    TrueOverallRating = 78,
                    PotentialRating = 88
                }
            }
        };

        var updated =
            OffseasonPlayerLifecycleService.EnterTransferPortal(state);

        Assert.Empty(updated.HighSchoolRecruitingPool);

        Assert.Single(
            updated.ActiveRoster
                .Concat(updated.TransferPortalEntries.Select(entry => entry.Player))
                .Where(candidate => candidate.PlayerId == id));
    }

    [Fact]
    public void CpuRecruitingIgnoresDuplicateProspectIdsAndAlreadyRosteredPlayers()
    {
        var prospectId =
            Guid.Parse("33333333-aaaa-bbbb-cccc-444444444444");

        var existing = new DynastyPlayer
        {
            PlayerId = prospectId,
            FullName = "Already Signed",
            TeamName = "User",
            Position = Position.WR,
            ClassYear = 1,
            TalentLevel = 8,
            OverallRating = 82,
            PotentialRating = 91
        };

        var duplicateRecruit = new HighSchoolRecruit
        {
            RecruitId = prospectId,
            SeasonYear = 2028,
            FullName = "Already Signed",
            Position = Position.WR,
            StarRating = 5,
            TrueOverallRating = 82,
            PotentialRating = 91
        };

        var state = new DynastyState
        {
            DynastyName = "Duplicate Guard",
            UserTeamName = "User",
            SeasonYear = 2028,
            Week = 20,
            Phase = SeasonPhase.Recruiting,
            ActiveRoster = new[] { existing },
            HighSchoolRecruitingPool = new[]
            {
                duplicateRecruit,
                duplicateRecruit
            }
        };

        var team = new Team
        {
            Name = "User",
            Abbreviation = "USR",
            ConferenceName = "Test",
            Prestige = 80
        };

        var updated = CpuRecruitingService.ApplyPhaseAssistance(
            state,
            new Dictionary<string, Team>(
                StringComparer.OrdinalIgnoreCase)
            {
                [team.Name] = team
            });

        Assert.Single(
            updated.ActiveRoster.Where(player =>
                player.PlayerId == prospectId));
    }
}
