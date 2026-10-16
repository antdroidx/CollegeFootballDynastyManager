using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public sealed class RecruitInterestTests
{
    [Fact]
    public void RecruitHasSchoolInterestBeforeAnyRecruitingAction()
    {
        var recruit = Recruit();
        var teams = Teams();
        var state = State(recruit);

        var interest =
            RecruitPreferenceService.GetInitialSchoolInterest(
                state,
                teams["User"],
                RecruitingSource.HighSchool,
                recruit.RecruitId);

        var topSchools =
            RecruitPreferenceService.GetInitialTopSchools(
                state,
                teams,
                RecruitingSource.HighSchool,
                recruit.RecruitId,
                5);

        Assert.InRange(interest, 5, 95);
        Assert.Equal(5, topSchools.Count);
        Assert.All(
            topSchools,
            item => Assert.InRange(
                item.Interest,
                5,
                95));
        Assert.True(
            topSchools.Zip(
                    topSchools.Skip(1),
                    (left, right) =>
                        left.Interest >= right.Interest)
                .All(value => value));
    }

    [Fact]
    public void ScholarshipOfferBuildsFromNaturalInterest()
    {
        var recruit = Recruit();
        var teams = Teams();
        var user = teams["User"];
        var state = State(recruit);

        var baseline =
            RecruitPreferenceService.GetInitialSchoolInterest(
                state,
                user,
                RecruitingSource.HighSchool,
                recruit.RecruitId);

        state =
            InteractiveRecruitingService.ToggleScholarship(
                state,
                user,
                RecruitingSource.HighSchool,
                recruit.RecruitId);

        var interaction =
            InteractiveRecruitingService.GetInteraction(
                state,
                RecruitingSource.HighSchool,
                recruit.RecruitId);

        Assert.True(interaction.ScholarshipOffered);
        Assert.Equal(
            Math.Min(100, baseline + 15),
            interaction.UserInterest);
    }

    [Fact]
    public void RepeatedRecruitingActionsNeverExceedOneHundredInterest()
    {
        var recruit = Recruit();
        var teams = Teams();
        var user = teams["User"];
        var state = State(recruit) with
        {
            RecruitingPointsRemaining = 5000
        };

        state =
            InteractiveRecruitingService.ToggleScholarship(
                state,
                user,
                RecruitingSource.HighSchool,
                recruit.RecruitId);

        for (var index = 0; index < 12; index++)
        {
            state =
                InteractiveRecruitingService.Pitch(
                    state,
                    user,
                    RecruitingSource.HighSchool,
                    recruit.RecruitId,
                    RecruitPitchType.ProgramPrestige);
        }

        var interaction =
            InteractiveRecruitingService.GetInteraction(
                state,
                RecruitingSource.HighSchool,
                recruit.RecruitId);

        Assert.InRange(
            interaction.UserInterest,
            0,
            100);
        Assert.Equal(100, interaction.UserInterest);
    }

    [Fact]
    public void LegacyInflatedInterestIsClampedForGameLogicAndDisplay()
    {
        var recruit = Recruit();
        var state = State(recruit) with
        {
            RecruitingInteractions = new[]
            {
                new RecruitingInteraction
                {
                    ProspectId = recruit.RecruitId,
                    Source = RecruitingSource.HighSchool,
                    SeasonYear = 2027,
                    UserInterest = 323,
                    RivalInterest = 118
                }
            }
        };

        var interaction =
            InteractiveRecruitingService.GetInteraction(
                state,
                RecruitingSource.HighSchool,
                recruit.RecruitId);

        Assert.Equal(100, interaction.UserInterest);
        Assert.Equal(100, interaction.RivalInterest);
    }

    [Fact]
    public void TransferDoesNotPreferSchoolItJustLeft()
    {
        var teams = Teams();
        var player = new DynastyPlayer
        {
            PlayerId = Guid.Parse(
                "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            FullName = "Portal Player",
            TeamName = "Old School",
            Position = Position.WR,
            ClassYear = 3,
            TalentLevel = 7,
            OverallRating = 80,
            PotentialRating = 88
        };

        var state = new DynastyState
        {
            DynastyId = Guid.Parse(
                "11111111-2222-3333-4444-555555555555"),
            DynastyName = "Portal Favorites",
            UserTeamName = "User",
            SeasonYear = 2027,
            TransferPortalEntries = new[]
            {
                new TransferPortalEntry
                {
                    SeasonYear = 2027,
                    OriginTeamName = "Old School",
                    Player = player
                }
            }
        };

        var favorites =
            RecruitPreferenceService.GetInitialTopSchools(
                state,
                teams,
                RecruitingSource.TransferPortal,
                player.PlayerId,
                5);

        Assert.DoesNotContain(
            favorites,
            item => item.TeamName == "Old School");
    }

    private static DynastyState State(
        HighSchoolRecruit recruit) => new()
    {
        DynastyId = Guid.Parse(
            "99999999-8888-7777-6666-555555555555"),
        DynastyName = "Interest Test",
        UserTeamName = "User",
        SeasonYear = 2027,
        Phase = SeasonPhase.Recruiting,
        HighSchoolRecruitingPool = new[] { recruit }
    };

    private static HighSchoolRecruit Recruit() => new()
    {
        RecruitId = Guid.Parse(
            "12345678-90ab-cdef-1234-567890abcdef"),
        SeasonYear = 2027,
        FullName = "Interested Recruit",
        Position = Position.WR,
        StarRating = 4,
        TrueOverallRating = 80,
        PotentialRating = 92,
        HomeState = 12,
        HomeRegion = 1
    };

    private static Dictionary<string, Team> Teams()
    {
        var teams = new[]
        {
            Team("User", 75, 1),
            Team("Old School", 70, 1),
            Team("Regional", 72, 1),
            Team("Power", 92, 3),
            Team("Academic", 78, 2),
            Team("Development", 82, 4),
            Team("Rebuild", 50, 0)
        };

        return teams.ToDictionary(
            item => item.Name,
            StringComparer.OrdinalIgnoreCase);
    }

    private static Team Team(
        string name,
        int prestige,
        int region) => new()
    {
        Name = name,
        Abbreviation =
            name[..Math.Min(3, name.Length)]
                .ToUpperInvariant(),
        ConferenceName = "Test",
        Prestige = prestige,
        LegacyRegionId = region
    };
}
