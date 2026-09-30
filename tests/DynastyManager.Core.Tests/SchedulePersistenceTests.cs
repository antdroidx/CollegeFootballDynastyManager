using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Persistence;
using Xunit;

namespace DynastyManager.Core.Tests;

public class SchedulePersistenceTests
{
    [Fact]
    public async Task SaveRoundTripPreservesScheduleResults()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"cfdm-schedule-{Guid.NewGuid():N}.db3");

        try
        {
            var home = new Team
            {
                Name = "Washington",
                Abbreviation = "WASH",
                ConferenceName = "Test",
                Prestige = 85
            };
            var away = new Team
            {
                Name = "Oregon",
                Abbreviation = "ORE",
                ConferenceName = "Test",
                Prestige = 82
            };
            var pending = new ScheduledGame
            {
                GameId = "2026-01-test",
                SeasonYear = 2026,
                Week = 1,
                HomeTeamName = home.Name,
                AwayTeamName = away.Name,
                SimulationSeed = 424242
            };
            var played = DeterministicGameSimulator.Simulate(pending, home, away);

            var state = new DynastyState
            {
                DynastyName = "Schedule Test",
                UserTeamName = home.Name,
                SeasonYear = 2026,
                Week = 2,
                Phase = SeasonPhase.RegularSeason,
                Schedule = new[] { played }
            };

            await using var repository = new SqliteDynastySaveRepository(path);
            var saveId = await repository.SaveAsync(state, SaveKind.Manual);
            var loaded = await repository.LoadAsync(saveId);

            Assert.NotNull(loaded);
            var loadedGame = Assert.Single(loaded.Schedule);
            Assert.True(loadedGame.HasPlayed);
            Assert.Equal(played.HomeScore, loadedGame.HomeScore);
            Assert.Equal(played.AwayScore, loadedGame.AwayScore);
            Assert.Equal(played.SimulationSeed, loadedGame.SimulationSeed);
            Assert.NotNull(loadedGame.HomeStats);
            Assert.NotNull(loadedGame.AwayStats);
            Assert.Equal(played.HomeStats?.TotalYards, loadedGame.HomeStats.TotalYards);
            Assert.Equal(played.AwayStats?.Turnovers, loadedGame.AwayStats.Turnovers);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
