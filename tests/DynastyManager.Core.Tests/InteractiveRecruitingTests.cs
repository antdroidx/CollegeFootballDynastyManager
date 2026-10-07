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
            50,
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
    public void TargetBoardToggleDoesNotSpendRecruitingPoints()
    {
        var prospect = PortalPlayer(
            "Board Target",
            "Old School",
            Position.CB,
            82);

        var state = PortalState(prospect) with
        {
            RecruitingPointsRemaining = 400
        };

        state = InteractiveRecruitingService.ToggleTargetBoard(
            state,
            RecruitingSource.TransferPortal,
            prospect.PlayerId);

        Assert.Equal(400, state.RecruitingPointsRemaining);

        var interaction =
            InteractiveRecruitingService.GetInteraction(
                state,
                RecruitingSource.TransferPortal,
                prospect.PlayerId);

        Assert.True(interaction.IsOnTargetBoard);
    }

    [Fact]
    public void ScoutingGraduallyRevealsRecruitPriorities()
    {
        var prospect = PortalPlayer(
            "Priority Target",
            "Old School",
            Position.OLB,
            83);

        var state = PortalState(prospect) with
        {
            RecruitingPointsRemaining = 500
        };

        Assert.Empty(
            RecruitPreferenceService.GetRevealedPreferences(
                state,
                RecruitingSource.TransferPortal,
                prospect.PlayerId));

        state = InteractiveRecruitingService.Scout(
            state,
            RecruitingSource.TransferPortal,
            prospect.PlayerId);

        Assert.Equal(
            2,
            RecruitPreferenceService.GetRevealedPreferences(
                state,
                RecruitingSource.TransferPortal,
                prospect.PlayerId).Count);

        state = InteractiveRecruitingService.Scout(
            state,
            RecruitingSource.TransferPortal,
            prospect.PlayerId);

        Assert.Equal(
            4,
            RecruitPreferenceService.GetRevealedPreferences(
                state,
                RecruitingSource.TransferPortal,
                prospect.PlayerId).Count);
    }

    [Fact]
    public void TransferScoutingStartsMorePreciseAndConvergesQuickly()
    {
        var prospect = PortalPlayer(
            "Known College Player",
            "Old School",
            Position.CB,
            84);

        var state = PortalState(prospect) with
        {
            RecruitingPointsRemaining = 200
        };

        var initial =
            InteractiveRecruitingService
                .GetScoutedOverallRange(
                    state,
                    RecruitingSource.TransferPortal,
                    prospect.PlayerId);
        Assert.True(
            initial.Maximum - initial.Minimum <= 8);

        state = InteractiveRecruitingService.Scout(
            state,
            RecruitingSource.TransferPortal,
            prospect.PlayerId);

        var afterOneScout =
            InteractiveRecruitingService
                .GetScoutedOverallRange(
                    state,
                    RecruitingSource.TransferPortal,
                    prospect.PlayerId);

        Assert.True(
            afterOneScout.Maximum -
            afterOneScout.Minimum <= 2);
        Assert.Equal(
            50,
            InteractiveRecruitingService.GetInteraction(
                state,
                RecruitingSource.TransferPortal,
                prospect.PlayerId).ScoutingPercent);
    }

    [Fact]
    public void HighInterestRecruitCanCommitBeforeFinalWeek()
    {
        var user = Team("User", 80);
        var rival = Team("Rival", 70);
        var recruit = Recruit(
            "Early Commit",
            3,
            74,
            88,
            Position.WR,
            1);

        var state = new DynastyState
        {
            DynastyId = Guid.Parse(
                "12121212-1212-3434-5656-787878787878"),
            DynastyName = "Early Commitment",
            UserTeamName = user.Name,
            SeasonYear = 2027,
            Week = DynastyManager.Core.Seasons
                .SeasonProgression.FirstRecruitingWeek,
            Phase = SeasonPhase.Recruiting,
            HighSchoolRecruitingPool =
                new[] { recruit },
            RecruitingInteractions = new[]
            {
                new RecruitingInteraction
                {
                    ProspectId = recruit.RecruitId,
                    Source =
                        RecruitingSource.HighSchool,
                    SeasonYear = 2027,
                    IsOnTargetBoard = true,
                    ScholarshipOffered = true,
                    UserInterest = 130,
                    RivalInterest = 55
                }
            }
        };

        var resolved =
            InteractiveRecruitingService
                .ResolveCurrentPhase(
                    state,
                    user,
                    new[] { user, rival }
                        .ToDictionary(
                            team => team.Name,
                            StringComparer.OrdinalIgnoreCase));

        var commitment =
            Assert.Single(
                resolved.RecruitingCommitments);

        Assert.Equal(
            user.Name,
            commitment.TeamName);
        Assert.Equal(
            state.Week,
            commitment.CommittedWeek);
    }

    [Fact]
    public void SpecificPitchRecordsPitchTypeAndAddsTargetToBoard()
    {
        var user = Team("User", 85);
        var prospect = PortalPlayer(
            "Pitch Target",
            "Old School",
            Position.WR,
            84);

        var state = PortalState(prospect) with
        {
            RecruitingPointsRemaining = 500
        };

        state = InteractiveRecruitingService.Pitch(
            state,
            user,
            RecruitingSource.TransferPortal,
            prospect.PlayerId,
            RecruitPitchType.PlayingTime);

        var interaction =
            InteractiveRecruitingService.GetInteraction(
                state,
                RecruitingSource.TransferPortal,
                prospect.PlayerId);

        Assert.Equal(
            RecruitPitchType.PlayingTime,
            interaction.LastPitchType);
        Assert.True(interaction.IsOnTargetBoard);
        Assert.True(interaction.UserInterest > 0);
        Assert.Equal(
            500 - InteractiveRecruitingService.PitchCost,
            state.RecruitingPointsRemaining);
    }

    [Fact]
    public void EarlyRecruitingWeekDoesNotForceLowInterestOfferToResolve()
    {
        var user = Team("User", 80);
        var rival = Team("Rival", 75);
        var recruit = new HighSchoolRecruit
        {
            RecruitId = Guid.Parse(
                "91919191-8282-7373-6464-555555555555"),
            SeasonYear = 2027,
            FullName = "Patient Recruit",
            Position = Position.QB,
            StarRating = 4,
            TrueOverallRating = 78,
            PotentialRating = 91
        };

        var state = new DynastyState
        {
            DynastyId = Guid.Parse(
                "01010101-0202-0303-0404-050505050505"),
            DynastyName = "Patient Recruiting",
            UserTeamName = user.Name,
            SeasonYear = 2027,
            Week = 20,
            Phase = SeasonPhase.Recruiting,
            RecruitingPointsRemaining = 500,
            RecruitingPointsPhase = SeasonPhase.Recruiting,
            RecruitingPointsWeek = 20,
            HighSchoolRecruitingPool = new[] { recruit }
        };

        state = InteractiveRecruitingService.ToggleScholarship(
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
        Assert.Null(
            InteractiveRecruitingService.GetInteraction(
                state,
                RecruitingSource.HighSchool,
                recruit.RecruitId).CommittedTeamName);
    }

    [Fact]
    public void FullAutoAssistanceCanUseEntireWeeklyRecruitingBudget()
    {
        var user = Team("User", 65);
        var recruits = Enumerable.Range(1, 30)
            .Select(index =>
                Recruit(
                    $"Recruit {index}",
                    2 + index % 2,
                    66 + index % 9,
                    78 + index % 12,
                    Enum.GetValues<Position>()[
                        index % Enum.GetValues<Position>().Length],
                    1))
            .ToArray();

        var state = new DynastyState
        {
            DynastyId =
                Guid.Parse("99999999-8888-7777-6666-555555555555"),
            DynastyName = "Full Auto Recruiting",
            UserTeamName = user.Name,
            SeasonYear = 2027,
            Week = 20,
            Phase = SeasonPhase.Recruiting,
            RecruitingAssistanceEnabled = true,
            RecruitingAssistanceWorkloadPercent = 100,
            RecruitingPointsRemaining = 500,
            RecruitingPointsPhase = SeasonPhase.Recruiting,
            RecruitingPointsWeek = 20,
            HighSchoolRecruitingPool = recruits
        };

        var assisted =
            CpuRecruitingService.ApplyWeeklyUserAssistance(
                state,
                user);

        Assert.True(
            assisted.RecruitingPointsRemaining <
            InteractiveRecruitingService.PitchCost);

        Assert.True(
            assisted.RecruitingInteractions.Count(item =>
                item.WasCpuAssisted &&
                item.IsOnTargetBoard &&
                item.ScholarshipOffered) > 3);

        Assert.Contains(
            assisted.RecruitingInteractions,
            item =>
                item.WasCpuAssisted &&
                item.ScoutingPercent > 0);

        Assert.Contains(
            assisted.RecruitingInteractions,
            item =>
                item.WasCpuAssisted &&
                item.LastPitchType is not null);
    }

    [Fact]
    public void HalfWorkloadLeavesAboutHalfTheWeeklyPointsForUser()
    {
        var user = Team("User", 70);
        var recruits = Enumerable.Range(1, 30)
            .Select(index =>
                Recruit(
                    $"Shared Recruit {index}",
                    2 + index % 2,
                    67 + index % 8,
                    80 + index % 10,
                    Enum.GetValues<Position>()[
                        index % Enum.GetValues<Position>().Length],
                    1))
            .ToArray();

        var state = new DynastyState
        {
            DynastyId =
                Guid.Parse("88888888-7777-6666-5555-444444444444"),
            DynastyName = "Shared Recruiting",
            UserTeamName = user.Name,
            SeasonYear = 2027,
            Week = 20,
            Phase = SeasonPhase.Recruiting,
            RecruitingAssistanceEnabled = true,
            RecruitingAssistanceWorkloadPercent = 50,
            RecruitingPointsRemaining = 600,
            RecruitingPointsPhase = SeasonPhase.Recruiting,
            RecruitingPointsWeek = 20,
            HighSchoolRecruitingPool = recruits
        };

        var assisted =
            CpuRecruitingService.ApplyWeeklyUserAssistance(
                state,
                user);

        var spent =
            state.RecruitingPointsRemaining -
            assisted.RecruitingPointsRemaining;

        Assert.InRange(spent, 275, 300);
        Assert.InRange(
            assisted.RecruitingPointsRemaining,
            300,
            325);
    }

    [Fact]
    public void AssistanceDoesNotTakeOverManualRecruitingTargets()
    {
        var user = Team("User", 70);
        var manual = Recruit(
            "Manual Target",
            3,
            72,
            86,
            Position.QB,
            1);
        var auto = Recruit(
            "Auto Target",
            2,
            68,
            82,
            Position.RB,
            1);

        var state = new DynastyState
        {
            DynastyId =
                Guid.Parse("77777777-6666-5555-4444-333333333333"),
            DynastyName = "Manual Priority Recruiting",
            UserTeamName = user.Name,
            SeasonYear = 2027,
            Week = 20,
            Phase = SeasonPhase.Recruiting,
            RecruitingAssistanceEnabled = true,
            RecruitingAssistanceWorkloadPercent = 100,
            RecruitingPointsRemaining = 250,
            RecruitingPointsPhase = SeasonPhase.Recruiting,
            RecruitingPointsWeek = 20,
            HighSchoolRecruitingPool =
                new[] { manual, auto },
            RecruitingInteractions = new[]
            {
                new RecruitingInteraction
                {
                    ProspectId = manual.RecruitId,
                    Source = RecruitingSource.HighSchool,
                    SeasonYear = 2027,
                    IsOnTargetBoard = true,
                    ScholarshipOffered = true,
                    UserInterest = 15,
                    WasCpuAssisted = false
                }
            }
        };

        var assisted =
            CpuRecruitingService.ApplyWeeklyUserAssistance(
                state,
                user);

        var manualAfter =
            InteractiveRecruitingService.GetInteraction(
                assisted,
                RecruitingSource.HighSchool,
                manual.RecruitId);

        Assert.False(manualAfter.WasCpuAssisted);
        Assert.Equal(15, manualAfter.UserInterest);
        Assert.Null(manualAfter.LastPitchType);
        Assert.Equal(0, manualAfter.ScoutingPercent);
    }

    [Fact]
    public void LowPrestigeAssistanceAvoidsUnrealisticFiveStarReach()
    {
        var user = Team("User", 25);
        var elite = Recruit(
            "Elite Reach",
            5,
            90,
            98,
            Position.QB,
            4);
        var realistic = Enumerable.Range(1, 8)
            .Select(index =>
                Recruit(
                    $"Local Prospect {index}",
                    1,
                    59 + index,
                    75 + index,
                    Position.RB,
                    1))
            .ToArray();

        var state = new DynastyState
        {
            DynastyId =
                Guid.Parse("66666666-5555-4444-3333-222222222222"),
            DynastyName = "Realistic Assistance",
            UserTeamName = user.Name,
            SeasonYear = 2027,
            Week = 20,
            Phase = SeasonPhase.Recruiting,
            RecruitingAssistanceEnabled = true,
            RecruitingAssistanceWorkloadPercent = 100,
            RecruitingPointsRemaining = 300,
            RecruitingPointsPhase = SeasonPhase.Recruiting,
            RecruitingPointsWeek = 20,
            HighSchoolRecruitingPool =
                new[] { elite }.Concat(realistic).ToArray()
        };

        var assisted =
            CpuRecruitingService.ApplyWeeklyUserAssistance(
                state,
                user);

        Assert.DoesNotContain(
            assisted.RecruitingInteractions,
            item =>
                item.ProspectId == elite.RecruitId &&
                item.ScholarshipOffered);
        Assert.Contains(
            assisted.RecruitingInteractions,
            item =>
                item.WasCpuAssisted &&
                item.ScholarshipOffered);
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

    private static HighSchoolRecruit Recruit(
        string name,
        int stars,
        int overall,
        int potential,
        Position position,
        int region) => new()
    {
        RecruitId = Guid.NewGuid(),
        SeasonYear = 2027,
        FullName = name,
        Position = position,
        StarRating = stars,
        TrueOverallRating = overall,
        PotentialRating = potential,
        HomeState = region,
        HomeRegion = region
    };

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
