using DynastyManager.Core.Models;
using DynastyManager.Data.Persistence;
using Xunit;

namespace DynastyManager.Core.Tests;

public sealed class DynastySaveRepositoryTests
{
    [Fact]
    public async Task SaveAndLoadRoundTripPreservesMidSeasonState()
    {
        var path = TempDatabasePath();

        try
        {
            await using var repository = new SqliteDynastySaveRepository(path);

            var state = new DynastyState
            {
                DynastyName = "Washington Test Dynasty",
                UserTeamName = "Washington",
                SeasonYear = 2031,
                Week = 8,
                Phase = SeasonPhase.RegularSeason,
                ScoutingStaff = new[]
                {
                    new ScoutStaff
                    {
                        FullName = "Save Test Scout",
                        TalentEvaluation = 82,
                        PotentialEvaluation = 79,
                        RegionalKnowledge = 85,
                        WorkRate = 76
                    }
                },
                ScoutAssignments = new[]
                {
                    new ScoutAssignment
                    {
                        ScoutId = Guid.Parse("12121212-3434-5656-7878-909090909090"),
                        Scope = ScoutAssignmentScope.TeamNeeds
                    }
                }
            };

            var saveId = await repository.SaveAsync(state, SaveKind.Manual);
            var loaded = await repository.LoadAsync(saveId);

            Assert.NotNull(loaded);
            Assert.Equal(state.DynastyId, loaded.DynastyId);
            Assert.Equal("Washington", loaded.UserTeamName);
            Assert.Equal(2031, loaded.SeasonYear);
            Assert.Equal(8, loaded.Week);
            Assert.Equal(SeasonPhase.RegularSeason, loaded.Phase);
            Assert.Single(loaded.ScoutingStaff);
            Assert.Single(loaded.ScoutAssignments);
            Assert.Equal(DynastyState.CurrentSchemaVersion,
                loaded.SchemaVersion);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public async Task ReusingSaveIdUpdatesSameSlot()
    {
        var path = TempDatabasePath();

        try
        {
            await using var repository = new SqliteDynastySaveRepository(path);

            var state = new DynastyState
            {
                DynastyName = "Career",
                UserTeamName = "Washington",
                SeasonYear = 2030,
                Week = 3,
                Phase = SeasonPhase.RegularSeason
            };

            var saveId = await repository.SaveAsync(state, SaveKind.AutosaveCurrent);

            var later = state with { Week = 4 };
            await repository.SaveAsync(later, SaveKind.AutosaveCurrent, saveId);

            var slots = await repository.ListAsync();
            var loaded = await repository.LoadAsync(saveId);

            Assert.Single(slots);
            Assert.NotNull(loaded);
            Assert.Equal(4, loaded.Week);
            Assert.Equal(SaveKind.AutosaveCurrent, slots[0].Kind);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public async Task SaveListContainsFriendlyMetadataWithoutOpeningSnapshot()
    {
        var path = TempDatabasePath();

        try
        {
            await using var repository = new SqliteDynastySaveRepository(path);

            await repository.SaveAsync(new DynastyState
            {
                DynastyName = "Road to Glory",
                UserTeamName = "Oregon State",
                SeasonYear = 2034,
                Week = 12,
                Phase = SeasonPhase.RegularSeason
            }, SaveKind.Manual);

            var slot = Assert.Single(await repository.ListAsync());

            Assert.Equal("Road to Glory", slot.DynastyName);
            Assert.Equal("Oregon State", slot.UserTeamName);
            Assert.Equal(2034, slot.SeasonYear);
            Assert.Equal(12, slot.Week);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public async Task RollingWeeklyAutosaveKeepsLatestThreeCheckpoints()
    {
        var path = TempDatabasePath();

        try
        {
            await using var repository = new SqliteDynastySaveRepository(path);

            var state = new DynastyState
            {
                DynastyName = "Three Deep",
                UserTeamName = "Washington",
                SeasonYear = 2032,
                Week = 1,
                Phase = SeasonPhase.RegularSeason
            };

            for (var week = 1; week <= 4; week++)
            {
                state = state with { Week = week };
                await repository.SaveRollingWeeklyAsync(state);
            }

            var slots = (await repository.ListAsync())
                .Where(slot => slot.Kind is SaveKind.WeeklyAuto1 or SaveKind.WeeklyAuto2 or SaveKind.WeeklyAuto3)
                .OrderBy(slot => slot.Week)
                .ToArray();

            Assert.Equal(3, slots.Length);
            Assert.Equal(new[] { 2, 3, 4 }, slots.Select(slot => slot.Week).ToArray());
            Assert.Contains(slots, slot => slot.Kind == SaveKind.WeeklyAuto1 && slot.Week == 4);
            Assert.Contains(slots, slot => slot.Kind == SaveKind.WeeklyAuto2 && slot.Week == 2);
            Assert.Contains(slots, slot => slot.Kind == SaveKind.WeeklyAuto3 && slot.Week == 3);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    private static string TempDatabasePath() =>
        Path.Combine(Path.GetTempPath(), $"cfdm-{Guid.NewGuid():N}.db3");

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
