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
                Schedule = new[] { played },
                ConferenceChampionshipHistory = new[]
                {
                    new ConferenceChampionRecord
                    {
                        SeasonYear = 2025,
                        ConferenceName = "Test",
                        ChampionTeamName = "Washington",
                        RunnerUpTeamName = "Oregon",
                        ChampionScore = 31,
                        RunnerUpScore = 24
                    }
                },
                CollegeFootballPlayoffHistory = new[]
                {
                    new CollegeFootballPlayoffSeedRecord
                    {
                        SeasonYear = 2025,
                        Seed = 1,
                        NationalRank = 1,
                        TeamName = "Washington",
                        ConferenceName = "Test",
                        IsConferenceChampion = true,
                        IsAutomaticBid = true
                    }
                },
                NationalChampionshipHistory = new[]
                {
                    new NationalChampionRecord
                    {
                        SeasonYear = 2025,
                        ChampionTeamName = "Washington",
                        RunnerUpTeamName = "Oregon",
                        ChampionScore = 35,
                        RunnerUpScore = 28
                    }
                },
                BowlHistory = new[]
                {
                    new BowlResultRecord
                    {
                        SeasonYear = 2025,
                        BowlName = "Carnation Bowl",
                        WinnerTeamName = "Washington",
                        LoserTeamName = "Oregon",
                        WinnerScore = 27,
                        LoserScore = 20
                    }
                },
                ActiveRoster = new[]
                {
                    new DynastyPlayer
                    {
                        PlayerId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        FullName = "Active Player",
                        TeamName = "Washington",
                        Position = Position.QB,
                        ClassYear = 3,
                        TalentLevel = 8,
                        OverallRating = 86,
                        DepthChartOrder = 1,
                        IsRedshirted = true,
                        HasRedshirted = false
                    }
                },
                TransferPortalEntries = new[]
                {
                    new TransferPortalEntry
                    {
                        SeasonYear = 2025,
                        OriginTeamName = "Oregon",
                        Player = new DynastyPlayer
                        {
                            PlayerId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                            FullName = "Portal Player",
                            TeamName = "Oregon",
                            Position = Position.WR,
                            ClassYear = 3,
                            TalentLevel = 7,
                            OverallRating = 82
                        }
                    }
                },
                RecentPlayerDepartures = new[]
                {
                    new PlayerDepartureRecord
                    {
                        SeasonYear = 2025,
                        PlayerId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                        FullName = "Graduated Player",
                        TeamName = "Washington",
                        Position = Position.OL,
                        OverallRating = 80,
                        Reason = PlayerDepartureReason.Graduation
                    }
                }
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
            var champion = Assert.Single(
                loaded.ConferenceChampionshipHistory);
            Assert.Equal("Washington", champion.ChampionTeamName);
            Assert.Equal(2025, champion.SeasonYear);

            var playoffSeed = Assert.Single(
                loaded.CollegeFootballPlayoffHistory);
            Assert.Equal(1, playoffSeed.Seed);
            Assert.True(playoffSeed.IsAutomaticBid);

            var nationalChampion = Assert.Single(
                loaded.NationalChampionshipHistory);
            Assert.Equal("Washington", nationalChampion.ChampionTeamName);

            var bowlResult = Assert.Single(loaded.BowlHistory);
            Assert.Equal("Carnation Bowl", bowlResult.BowlName);
            Assert.Equal("Washington", bowlResult.WinnerTeamName);

            var activePlayer = Assert.Single(loaded.ActiveRoster);
            Assert.Equal("Active Player", activePlayer.FullName);
            Assert.Equal(3, activePlayer.ClassYear);
            Assert.Equal(1, activePlayer.DepthChartOrder);
            Assert.True(activePlayer.IsRedshirted);
            Assert.False(activePlayer.HasRedshirted);

            var portalPlayer = Assert.Single(loaded.TransferPortalEntries);
            Assert.Equal("Portal Player", portalPlayer.Player.FullName);
            Assert.Equal("Oregon", portalPlayer.OriginTeamName);

            var departure = Assert.Single(loaded.RecentPlayerDepartures);
            Assert.Equal("Graduated Player", departure.FullName);
            Assert.Equal(
                PlayerDepartureReason.Graduation,
                departure.Reason);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
