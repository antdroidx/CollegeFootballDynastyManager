using DynastyManager.Core.Models;
using DynastyManager.Core.Seasons;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Persistence;
using Xunit;

namespace DynastyManager.Core.Tests;

public class CompetingRecruitingTests
{
    [Fact]
    public void NaturalFavoritesAreNotScholarshipOffers()
    {
        var state = State();
        var teams = Teams();
        Assert.NotEmpty(RecruitPreferenceService.GetInitialTopSchools(state, teams, RecruitingSource.HighSchool, Id));
        Assert.Empty(CompetingRecruitingService.GetOffers(state, RecruitingSource.HighSchool, Id));
        Assert.Equal(0, RecruitPreferenceService.GetBestRivalInterest(state, teams["User"], teams, RecruitingSource.HighSchool, Id));
    }

    [Fact]
    public void MultipleCpuSchoolsOfferUserTargetsAndInvestWithinOneWeeklyBudget()
    {
        var teams = Teams();
        var state = State() with { Week = 20,
            HighSchoolRecruitingPool = Enumerable.Range(1, 40).Select(index => Recruit(index)).ToArray() };
        state = InteractiveRecruitingService.ToggleScholarship(state, teams["User"], RecruitingSource.HighSchool, Id);
        state = CompetingRecruitingService.AdvanceOffers(state, teams);
        var offers = CompetingRecruitingService.GetOffers(state, RecruitingSource.HighSchool, Id, activeOnly: true);
        Assert.Contains(offers, item => item.TeamName == "User");
        Assert.Contains(offers, item => item.TeamName == "CPU A");
        Assert.Contains(offers, item => item.TeamName == "CPU B");
        Assert.All(offers, item => Assert.InRange(item.Interest, 0, 100));
        var sameTurn = CompetingRecruitingService.AdvanceOffers(state, teams);
        Assert.Equal(state.RecruitingOffers, sameTurn.RecruitingOffers);
        foreach (var team in teams.Values.Where(item => item.Name != "User"))
        {
            var spent = state.RecruitingOffers.Where(item => item.TeamName == team.Name).Sum(item => item.PointsInvested);
            var budget = Math.Max(350, team.Prestige * 4 + 85 * 28 / 6);
            Assert.InRange(spent, 50, budget);
        }
        var next = CompetingRecruitingService.AdvanceOffers(state with { Week = 21 }, teams);
        Assert.True(next.RecruitingOffers.Sum(item => item.PointsInvested) > state.RecruitingOffers.Sum(item => item.PointsInvested));
        Assert.Equal(next.RecruitingOffers.Count, next.RecruitingOffers.Select(item =>
            (item.SeasonYear, item.Source, item.ProspectId, item.TeamName)).Distinct().Count());
    }

    [Fact]
    public void MixedHsAndPortalPursuitsShareTheCpuWeeklyEffortBudget()
    {
        var teams = Teams();
        var state = State() with { TransferPortalEntries = Enumerable.Range(1000, 150)
            .Select(index => new TransferPortalEntry { SeasonYear = 2027, OriginTeamName = "Other",
                Player = Player(index, "Other", 2) }).ToArray() };
        var market = CompetingRecruitingService.AdvanceOffers(state, teams);
        foreach (var team in teams.Values.Where(item => item.Name != "User"))
        {
            var offers = market.RecruitingOffers.Where(item => item.TeamName == team.Name).ToArray();
            Assert.Contains(offers, item => item.Source == RecruitingSource.HighSchool);
            Assert.Contains(offers, item => item.Source == RecruitingSource.TransferPortal);
            var budget = Math.Max(350, team.Prestige * 4 + 85 * 28 / 6);
            Assert.InRange(offers.Sum(item => item.PointsInvested), 50, (int)(budget * .8));
        }
    }

    [Fact]
    public void MarketDoesNotDependOnTeamDictionaryIterationOrder()
    {
        var teams = Teams();
        var state = State() with { HighSchoolRecruitingPool = Enumerable.Range(1, 50).Select(Recruit).ToArray() };
        var normal = CompetingRecruitingService.AdvanceOffers(state, teams);
        var reversed = CompetingRecruitingService.AdvanceOffers(state, teams.Reverse().ToDictionary(item => item.Key, item => item.Value));
        Assert.Equal(normal.RecruitingOffers, reversed.RecruitingOffers);
        Assert.Equal(CompetingRecruitingService.ResolveWeek(normal, teams).RecruitingCommitments,
            CompetingRecruitingService.ResolveWeek(reversed, teams).RecruitingCommitments);
    }

    [Fact]
    public void DuplicatePoolRowsStillProduceOneDecisionAndOneOfferPerSchool()
    {
        var state = State() with { HighSchoolRecruitingPool = new[] { Recruit(1), Recruit(1) } };
        var result = CompetingRecruitingService.ResolveWeek(state, Teams());
        Assert.Single(result.RecruitingCommitments);
        Assert.Single(result.ActiveRoster);
        Assert.Equal(result.RecruitingOffers.Count, result.RecruitingOffers.Select(item => item.TeamName).Distinct().Count());
    }

    [Fact]
    public void OnlyActualOfferingSchoolsCanWinAndNoOfferCanStayUnsigned()
    {
        var teams = Teams();
        var state = Prepared(State() with { RecruitingOffers = new[] { Offer("CPU A", 65) } });
        var result = CompetingRecruitingService.ResolveWeek(state, teams);
        Assert.Equal("CPU A", Assert.Single(result.RecruitingCommitments).TeamName);
        Assert.Equal(RecruitingOfferStatus.Signed, Assert.Single(result.RecruitingOffers).Status);
        var noOffers = CompetingRecruitingService.ResolveWeek(Prepared(State()), teams);
        Assert.Empty(noOffers.RecruitingCommitments);
        Assert.Empty(noOffers.ActiveRoster);
    }

    [Theory]
    [InlineData(100, 20, "User")]
    [InlineData(20, 100, "CPU A")]
    public void UserAndCpuCompeteForTheSameProspect(int userInterest, int cpuInterest, string expected)
    {
        var state = Prepared(State() with
        {
            RecruitingOffers = new[] { Offer("CPU A", cpuInterest), Offer("User", userInterest) },
            RecruitingInteractions = new[] { new RecruitingInteraction
            {
                ProspectId = Id, Source = RecruitingSource.HighSchool, SeasonYear = 2027,
                ScholarshipOffered = true, UserInterest = userInterest
            } }
        });
        var result = CompetingRecruitingService.ResolveWeek(state, Teams());
        Assert.Equal(expected, Assert.Single(result.RecruitingCommitments).TeamName);
        Assert.Equal(expected, InteractiveRecruitingService.GetInteraction(result, RecruitingSource.HighSchool, Id).CommittedTeamName);
        Assert.Single(result.RecruitingOffers, item => item.Status == RecruitingOfferStatus.Signed);
        Assert.Single(result.RecruitingOffers, item => item.Status == RecruitingOfferStatus.Lost);
    }

    [Fact]
    public void WithdrawnOffersCannotWinAndUserActionsUpdateRealOffer()
    {
        var teams = Teams();
        var state = InteractiveRecruitingService.ToggleScholarship(State() with { RecruitingPointsRemaining = 500 },
            teams["User"], RecruitingSource.HighSchool, Id);
        state = InteractiveRecruitingService.Pitch(state, teams["User"], RecruitingSource.HighSchool, Id);
        var offer = Assert.Single(state.RecruitingOffers);
        Assert.Equal(50, offer.PointsInvested);
        Assert.Equal(state.Week, offer.LastContactWeek);
        state = InteractiveRecruitingService.ToggleScholarship(state, teams["User"], RecruitingSource.HighSchool, Id);
        Assert.Equal(RecruitingOfferStatus.Withdrawn, Assert.Single(state.RecruitingOffers).Status);
        state = Prepared(state with { RecruitingOffers = state.RecruitingOffers.Append(Offer("CPU A", 65)).ToArray() });
        var result = CompetingRecruitingService.ResolveWeek(state, teams);
        Assert.Equal("CPU A", Assert.Single(result.RecruitingCommitments).TeamName);
    }

    [Fact]
    public void FullSchoolsDoNotRedirectRecruitsToUnrelatedSchools()
    {
        var state = Prepared(State() with
        {
            ActiveRoster = Enumerable.Range(1000, 85).Select(index => Player(index, "CPU A", 3)).ToArray(),
            RecruitingOffers = new[] { Offer("CPU A", 100) }
        });
        var result = CompetingRecruitingService.ResolveWeek(state, Teams());
        Assert.Empty(result.RecruitingCommitments);
        Assert.Equal(85, result.ActiveRoster.Count);
        Assert.Equal(RecruitingOfferStatus.Withdrawn, Assert.Single(result.RecruitingOffers).Status);
        var alternative = CompetingRecruitingService.ResolveWeek(Prepared(state with
        {
            RecruitingOffers = state.RecruitingOffers.Append(Offer("CPU B", 65)).ToArray()
        }), Teams());
        Assert.Equal("CPU B", Assert.Single(alternative.RecruitingCommitments).TeamName);
    }

    [Fact]
    public void CpuWithdrawsOutstandingOffersWhenRosterIsFull()
    {
        var state = State() with { RecruitingOffers = new[] { Offer("CPU A", 90) },
            ActiveRoster = Enumerable.Range(1000, 85).Select(index => Player(index, "CPU A", 3)).ToArray() };
        var result = CompetingRecruitingService.AdvanceOffers(state, Teams());
        Assert.DoesNotContain(result.RecruitingOffers, item => item.TeamName == "CPU A" && item.Status == RecruitingOfferStatus.Active);
    }

    [Fact]
    public void ResolutionIsIdempotentAcrossOldEntryPoints()
    {
        var teams = Teams();
        var state = Prepared(State() with { RecruitingOffers = new[] { Offer("User", 100) } });
        var first = InteractiveRecruitingService.ResolveCurrentPhase(state, teams["User"], teams);
        var again = CpuRecruitingService.AdvanceCpuRecruitingWeek(first, teams);
        again = CpuRecruitingService.ApplyPhaseAssistance(again, teams);
        Assert.Single(again.RecruitingCommitments);
        Assert.Single(again.ActiveRoster);
        Assert.Equal(first.RecruitingOffers, again.RecruitingOffers);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MidseasonOffersReserveNextSeasonSpaceWithoutJoiningNow(bool graduating)
    {
        var state = State() with { Phase = SeasonPhase.RegularSeason, Week = 8,
            ActiveRoster = Enumerable.Range(1000, 85).Select(index => Player(index, "User", graduating ? 4 : 3)).ToArray(),
            HighSchoolRecruitingPool = [],
            TransferPortalEntries = new[] { new TransferPortalEntry
            { SeasonYear = 2027, OriginTeamName = "Other", Player = Player(1, "Other", 2) } },
            RecruitingOffers = new[] { Offer("User", 100) with { Source = RecruitingSource.TransferPortal } }
        };
        state = Prepared(state);
        var result = CompetingRecruitingService.ResolveWeek(state, Teams());
        Assert.Equal(85, result.ActiveRoster.Count);
        if (graduating)
        {
            var pending = Assert.Single(result.PendingTransferCommitments);
            Assert.Equal(2028, pending.JoinSeasonYear);
            Assert.True(pending.WasUserCommitment);
        }
        else Assert.Empty(result.PendingTransferCommitments);
    }

    [Fact]
    public void TransferCannotSignForTheSchoolItJustLeft()
    {
        var state = Prepared(State() with { Phase = SeasonPhase.Recruiting,
            HighSchoolRecruitingPool = [], TransferPortalEntries = new[] { new TransferPortalEntry
            { SeasonYear = 2027, OriginTeamName = "CPU A", Player = Player(1, "CPU A", 2) } },
            RecruitingOffers = new[] { Offer("CPU A", 100), Offer("User", 100) }.Select(item =>
                item with { Source = RecruitingSource.TransferPortal }).ToArray()
        });
        var result = CompetingRecruitingService.ResolveWeek(state, Teams());
        Assert.Equal("User", Assert.Single(result.RecruitingCommitments).TeamName);
    }

    [Fact]
    public void TakingOverASchoolPreservesItsActualOutstandingOffers()
    {
        var state = State() with { UserTeamName = "CPU A", RecruitingOffers = new[] { Offer("CPU A", 87) } };
        var interaction = InteractiveRecruitingService.GetInteraction(state, RecruitingSource.HighSchool, Id);
        Assert.True(interaction.ScholarshipOffered);
        Assert.Equal(87, interaction.UserInterest);
        var withdrawn = InteractiveRecruitingService.ToggleScholarship(state, Teams()["CPU A"], RecruitingSource.HighSchool, Id);
        Assert.Equal(RecruitingOfferStatus.Withdrawn, Assert.Single(withdrawn.RecruitingOffers).Status);
    }

    [Fact]
    public void MultipleProspectsCannotOverbookTheSameRosterOpening()
    {
        var state = Prepared(State() with
        {
            ActiveRoster = Enumerable.Range(1000, 84).Select(index => Player(index, "User", 2)).ToArray(),
            HighSchoolRecruitingPool = Enumerable.Range(1, 20).Select(Recruit).ToArray(),
            RecruitingOffers = Enumerable.Range(1, 20).Select(index => Offer("User", 100) with
                { ProspectId = GuidFor(index) }).ToArray()
        });
        var result = CompetingRecruitingService.ResolveWeek(state, Teams());
        Assert.Equal(85, result.ActiveRoster.Count);
        Assert.Single(result.RecruitingCommitments);
        Assert.Single(result.RecruitingOffers, item => item.Status == RecruitingOfferStatus.Signed);
    }

    [Fact]
    public async Task SaveReloadPreservesOfferEffortAndDoesNotRerollOrSpendTwice()
    {
        var path = Path.Combine(Path.GetTempPath(), $"offer-test-{Guid.NewGuid():N}.db");
        try
        {
            await using var repository = new SqliteDynastySaveRepository(path);
            var teams = Teams();
            var state = CompetingRecruitingService.AdvanceOffers(State(), teams);
            var save = await repository.SaveAsync(state, SaveKind.Manual);
            var loaded = (await repository.LoadAsync(save))!;
            Assert.Equal(18, loaded.SchemaVersion);
            Assert.Equal(state.RecruitingOffers, loaded.RecruitingOffers);
            Assert.Equal(state.RecruitingOffers, CompetingRecruitingService.AdvanceOffers(loaded, teams).RecruitingOffers);
            var expected = CompetingRecruitingService.ResolveWeek(state, teams);
            var actual = CompetingRecruitingService.ResolveWeek(loaded, teams);
            Assert.Equal(expected.RecruitingCommitments, actual.RecruitingCommitments);
            await repository.SaveAsync(actual, SaveKind.Manual, save);
            var afterResolution = (await repository.LoadAsync(save))!;
            Assert.Equal(actual.RecruitingCommitments,
                CompetingRecruitingService.ResolveWeek(afterResolution, teams).RecruitingCommitments);
        }
        finally
        {
            foreach (var suffix in new[] { "", "-wal", "-shm" })
                if (File.Exists(path + suffix)) File.Delete(path + suffix);
        }
    }

    [Fact]
    public async Task Schema17SaveKeepsExistingCommitmentsAndStartsOffersAtUpgradeWeek()
    {
        var path = Path.Combine(Path.GetTempPath(), $"legacy-offers-{Guid.NewGuid():N}.db");
        try
        {
            await using var repository = new SqliteDynastySaveRepository(path);
            var state = State() with { SchemaVersion = 17, Week = 23,
                HighSchoolRecruitingPool = new[] { Recruit(1), Recruit(99) },
                ActiveRoster = new[] { Player(99, "CPU A", 1) },
                RecruitingCommitments = new[] { new RecruitingCommitmentRecord
                { SeasonYear = 2027, ProspectId = GuidFor(99), Source = RecruitingSource.HighSchool,
                    TeamName = "CPU A", PlayerName = "Existing Signee", Position = Recruit(99).Position, OverallRating = 80 } },
                RecruitingInteractions = new[] { new RecruitingInteraction
                { SeasonYear = 2027, Source = RecruitingSource.HighSchool, ProspectId = Id,
                    ScholarshipOffered = true, UserInterest = 82 } }
            };
            var save = await repository.SaveAsync(state, SaveKind.Manual);
            var loaded = (await repository.LoadAsync(save))!;
            var market = CompetingRecruitingService.AdvanceOffers(loaded, Teams());
            Assert.Equal(state.RecruitingCommitments, market.RecruitingCommitments);
            Assert.Equal(state.ActiveRoster, market.ActiveRoster);
            Assert.DoesNotContain(market.RecruitingOffers, item => item.ProspectId == GuidFor(99));
            Assert.All(market.RecruitingOffers, item => Assert.Equal(23, item.OfferedWeek));
            Assert.Contains(market.RecruitingOffers, item => item.TeamName == "User" && item.Interest == 82);
        }
        finally
        {
            foreach (var suffix in new[] { "", "-wal", "-shm" })
                if (File.Exists(path + suffix)) File.Delete(path + suffix);
        }
    }

    [Fact]
    public void ExistingUserScholarshipsAreImportedWithoutInventingPastCpuOffers()
    {
        var state = State() with { Week = 23, RecruitingInteractions = new[] { new RecruitingInteraction
        { SeasonYear = 2027, Source = RecruitingSource.HighSchool, ProspectId = Id, ScholarshipOffered = true, UserInterest = 82 } } };
        var market = CompetingRecruitingService.AdvanceOffers(state, Teams());
        Assert.Contains(market.RecruitingOffers, item => item.TeamName == "User" && item.Interest == 82);
        Assert.All(market.RecruitingOffers, item => Assert.Equal(23, item.OfferedWeek));
        Assert.Empty(market.RecruitingCommitments);
        Assert.Equal(market.RecruitingOffers, CompetingRecruitingService.AdvanceOffers(market, Teams()).RecruitingOffers);
    }

    private static DynastyState Prepared(DynastyState state) => state with
    {
        RecruitingOffersProcessedSeasonYear = state.SeasonYear,
        RecruitingOffersProcessedWeek = state.Week, RecruitingOffersProcessedPhase = state.Phase
    };
    private static Guid Id => GuidFor(1);
    private static Guid GuidFor(int value) { var bytes = new byte[16]; BitConverter.GetBytes(value).CopyTo(bytes, 0); return new Guid(bytes); }
    private static Team Team(string name, int prestige) => new() { Name = name, Prestige = prestige, Abbreviation = name, ConferenceName = "Test" };
    private static Dictionary<string, Team> Teams() => new[] { Team("User", 65), Team("CPU A", 60), Team("CPU B", 90) }.ToDictionary(item => item.Name);
    private static HighSchoolRecruit Recruit(int index) => new() { RecruitId = GuidFor(index), SeasonYear = 2027,
        FullName = $"Recruit {index}", Position = (Position)(index % Enum.GetValues<Position>().Length),
        StarRating = 3, TrueOverallRating = 74, PotentialRating = 88, HomeState = index % 50 };
    private static DynastyPlayer Player(int index, string team, int year) => new()
    { PlayerId = GuidFor(index), FullName = $"Player {index}", TeamName = team, Position = Recruit(index).Position,
        ClassYear = year, OverallRating = 80, PotentialRating = 90 };
    private static DynastyState State() => new() { DynastyId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
        DynastyName = "Competing offers", UserTeamName = "User", SeasonYear = 2027,
        Phase = SeasonPhase.Recruiting, Week = SeasonProgression.LastRecruitingWeek, HighSchoolRecruitingPool = new[] { Recruit(1) } };
    private static RecruitingOffer Offer(string team, int interest) => new() { SeasonYear = 2027,
        Source = RecruitingSource.HighSchool, ProspectId = Id, TeamName = team, Interest = interest, OfferedWeek = 20 };
}
