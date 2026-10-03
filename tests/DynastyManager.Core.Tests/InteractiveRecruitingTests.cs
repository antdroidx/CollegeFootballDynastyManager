using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Import;
using Xunit;

namespace DynastyManager.Core.Tests;

public class InteractiveRecruitingTests
{
    [Fact]
    public void ScoutingSpendsPointsAndZeroBalanceDoesNotRefill()
    {
        var user = Team("User", 80);
        var rival = Team("Rival", 75);
        var prospect = PortalPlayer("Portal QB", "Old School", Position.QB, 84);

        var state = PortalState(prospect) with
        {
            RecruitingPointsRemaining =
                InteractiveRecruitingService.ScoutCost,
            RecruitingPointsPhase = SeasonPhase.TransferPortal
        };

        state = InteractiveRecruitingService.Scout(
            state,
            RecruitingSource.TransferPortal,
            prospect.PlayerId);

        Assert.Equal(0, state.RecruitingPointsRemaining);

        var interaction =
            InteractiveRecruitingService.GetInteraction(
                state,
                RecruitingSource.TransferPortal,
                prospect.PlayerId);

        Assert.Equal(
            InteractiveRecruitingService.ScoutStep,
            interaction.ScoutingPercent);

        var reloaded =
            InteractiveRecruitingService.EnsurePhaseInitialized(
                state,
                user);

        Assert.Equal(0, reloaded.RecruitingPointsRemaining);
    }

    [Fact]
    public void ScholarshipAndPitchesCanWinPortalCommitment()
    {
        var user = Team("User", 100);
        var rival = Team("Rival", 70);
        var prospect = PortalPlayer(
            "Impact Transfer",
            "Old School",
            Position.QB,
            88);

        var state = PortalState(prospect);
        state = InteractiveRecruitingService.EnsurePhaseInitialized(
            state,
            user);

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

        var signed = Assert.Single(
            state.ActiveRoster,
            player => player.PlayerId == prospect.PlayerId);

        Assert.Equal(user.Name, signed.TeamName);

        var commitment = Assert.Single(
            state.RecruitingCommitments,
            record => record.ProspectId == prospect.PlayerId);

        Assert.Equal(user.Name, commitment.TeamName);
        Assert.Equal(
            RecruitingSource.TransferPortal,
            commitment.Source);
    }

    [Fact]
    public void RecruitingPointsRefreshOnEachRecruitingWeek()
    {
        var user = Team("User", 80);

        var weekOne = new DynastyState
        {
            DynastyName = "Weekly Recruiting",
            UserTeamName = user.Name,
            SeasonYear = 2027,
            Week = 20,
            Phase = SeasonPhase.Recruiting,
            RecruitingPointsRemaining = 0,
            RecruitingPointsPhase = SeasonPhase.Recruiting,
            RecruitingPointsWeek = 20
        };

        var sameWeek =
            InteractiveRecruitingService.EnsurePhaseInitialized(
                weekOne,
                user);

        Assert.Equal(0, sameWeek.RecruitingPointsRemaining);

        var weekTwo =
            InteractiveRecruitingService.EnsurePhaseInitialized(
                weekOne with { Week = 21 },
                user);

        Assert.True(weekTwo.RecruitingPointsRemaining > 0);
        Assert.Equal(21, weekTwo.RecruitingPointsWeek);
        Assert.Equal(
            SeasonPhase.Recruiting,
            weekTwo.RecruitingPointsPhase);
    }

    [Fact]
    public void ScoutedRecruitWithoutScholarshipDoesNotResolve()
    {
        var user = Team("User", 80);
        var rival = Team("Rival", 75);
        var recruit = new HighSchoolRecruit
        {
            RecruitId = Guid.Parse(
                "12121212-3434-5656-7878-909090909090"),
            SeasonYear = 2027,
            FullName = "Scouted Only",
            Position = Position.WR,
            StarRating = 4,
            TrueOverallRating = 79,
            PotentialRating = 90
        };

        var state = new DynastyState
        {
            DynastyName = "Scouting Test",
            UserTeamName = user.Name,
            SeasonYear = 2027,
            Week = 20,
            Phase = SeasonPhase.Recruiting,
            RecruitingPointsRemaining = 500,
            RecruitingPointsPhase = SeasonPhase.Recruiting,
            RecruitingPointsWeek = 20,
            HighSchoolRecruitingPool = new[] { recruit }
        };

        state = InteractiveRecruitingService.Scout(
            state,
            RecruitingSource.HighSchool,
            recruit.RecruitId);

        state = InteractiveRecruitingService.ResolveCurrentPhase(
            state,
            user,
            new[] { user, rival }.ToDictionary(
                team => team.Name,
                StringComparer.OrdinalIgnoreCase));

        Assert.Empty(state.RecruitingCommitments);

        var interaction =
            InteractiveRecruitingService.GetInteraction(
                state,
                RecruitingSource.HighSchool,
                recruit.RecruitId);

        Assert.Null(interaction.CommittedTeamName);
        Assert.Equal(
            InteractiveRecruitingService.ScoutStep,
            interaction.ScoutingPercent);
    }

    [Fact]
    public void HighSchoolPoolUsesScaledSizeAndValidRatings()
    {
        var rows = new[]
        {
            new ImportedPlayerRow
            {
                TeamName = "Test",
                FullName = "John Smith",
                Position = Position.QB,
                Year = 1,
                LegacyTalentLevel = 5
            },
            new ImportedPlayerRow
            {
                TeamName = "Test",
                FullName = "Marcus Williams",
                Position = Position.WR,
                Year = 1,
                LegacyTalentLevel = 5
            }
        };

        var pool = HighSchoolRecruitingPoolFactory.Create(
            Guid.Parse("22222222-3333-4444-5555-666666666666"),
            2027,
            138,
            rows);

        Assert.Equal(3450, pool.Count);
        Assert.All(pool, recruit =>
        {
            Assert.InRange(recruit.StarRating, 1, 5);
            Assert.InRange(recruit.TrueOverallRating, 50, 91);
            Assert.InRange(
                recruit.PotentialRating,
                recruit.TrueOverallRating,
                99);
            Assert.InRange(recruit.HomeRegion, 0, 49);
        });
    }

    private static DynastyState PortalState(DynastyPlayer prospect) =>
        new()
        {
            DynastyId =
                Guid.Parse("11111111-2222-3333-4444-555555555555"),
            DynastyName = "Recruiting Test",
            UserTeamName = "User",
            SeasonYear = 2026,
            Week = 19,
            Phase = SeasonPhase.TransferPortal,
            ActiveRoster = Array.Empty<DynastyPlayer>(),
            TransferPortalEntries = new[]
            {
                new TransferPortalEntry
                {
                    SeasonYear = 2026,
                    OriginTeamName = prospect.TeamName,
                    Player = prospect
                }
            }
        };

    private static DynastyPlayer PortalPlayer(
        string name,
        string team,
        Position position,
        int overall) =>
        new()
        {
            PlayerId =
                Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            FullName = name,
            TeamName = team,
            Position = position,
            ClassYear = 3,
            TalentLevel = 8,
            OverallRating = overall
        };

    private static Team Team(string name, int prestige) =>
        new()
        {
            Name = name,
            Abbreviation = name,
            ConferenceName = "Test",
            Prestige = prestige
        };
}
