using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;

public sealed class SharedRatingsTests
{
    [Fact] public void EveryProPlayerUsesCollegeOverallAndAllNineteenAttributes()
    {
        var state = ProLeagueService.Create("Seattle Seahawks");
        Assert.Equal(ProPlayerRatingService.Version, state.RatingSystemVersion);
        foreach (var p in state.Players)
        {
            Assert.Equal(PlayerRatingService.CalculateOverall(p.Football), p.Football.OverallRating);
            var groups = PlayerAttributeCatalog.Groups(p.Football);
            Assert.Equal(new[] { "Character traits", "Physical traits", "Skill traits" }, groups.Select(g => g.Title));
            Assert.Equal(19, groups.Sum(g => g.Attributes.Length));
            Assert.All(groups.SelectMany(g => g.Attributes), a => Assert.InRange(a.Rating, 1, 99));
            Assert.Equal(p.Football.Skills!.BallCarrying, groups.SelectMany(g => g.Attributes).Single(a => a.Name == "Ball carrying").Rating);
        }
    }
    [Fact] public void MigrationIsIdempotentAndPreservesIdentityAndFinancialState()
    {
        var current = ProLeagueService.Create("Seattle Seahawks");
        var old = current with { RatingSystemVersion = 0, Players = current.Players.Select(p => p with { Football = p.Football with { OverallRating = 50 } }).ToList() };
        var migrated = ProPlayerRatingService.Migrate(old);
        Assert.Same(migrated, ProPlayerRatingService.Migrate(migrated));
        Assert.Equal(old.Id, migrated.Id); Assert.Same(old.Contracts, migrated.Contracts); Assert.Same(old.Games, migrated.Games);
        Assert.Equal(old.Players.Select(p => p.Football.PlayerId), migrated.Players.Select(p => p.Football.PlayerId));
        Assert.All(migrated.Players, p => Assert.Equal(PlayerRatingService.CalculateOverall(p.Football), p.Football.OverallRating));
    }
    [Fact] public void ProgressionAndDraftGenerationRetainSharedOverallCalculation()
    {
        var state = ProLeagueService.Create("Seattle Seahawks") with { Phase = ProPhase.Offseason };
        state = ProOffseason.Advance(state);
        Assert.NotEmpty(state.Prospects);
        Assert.All(state.Players.Concat(state.Prospects), p => Assert.Equal(PlayerRatingService.CalculateOverall(p.Football), p.Football.OverallRating));
    }
    [Fact] public async Task OldSaveAdoptsSharedRatingsWithoutReplacingContracts()
    {
        var directory = Path.Combine(Path.GetTempPath(), "pro-rating-migration-" + Guid.NewGuid());
        try
        {
            var original = ProLeagueService.Create("Seattle Seahawks") with { RatingSystemVersion = 0 };
            var player = original.Players.First();
            original = original with { Players = original.Players.Select(p => p == player ? p with { Football = p.Football with { OverallRating = 50, SeasonStartOverall = 50 } } : p).ToList() };
            var store = new ProSaveStore(directory); await store.Save(original);
            var loaded = await store.Load(original.Id);
            Assert.Equal(1, loaded.RatingSystemVersion);
            Assert.All(loaded.Players, p => Assert.Equal(PlayerRatingService.CalculateOverall(p.Football), p.Football.OverallRating));
            Assert.Equal(original.Contracts.Select(c => ProEconomy.CapHit(c, original.Year)), loaded.Contracts.Select(c => ProEconomy.CapHit(c, loaded.Year)));
            await store.Save(loaded); var twice = await store.Load(loaded.Id);
            Assert.Equal(loaded.Players.Select(p => p.Football.OverallRating), twice.Players.Select(p => p.Football.OverallRating));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
    [Fact] public void ProfessionalPasserRatingUsesNflFormula()
    {
        var line = new PlayerSeasonStatLine { PlayerName = "Test", TeamName = "Test", PassAttempts = 10, PassCompletions = 10, PassYards = 300, PassingTouchdowns = 5 };
        Assert.Equal(158.333333, ProStatistics.PasserRating(line), 5);
        Assert.Equal(0, ProStatistics.PasserRating(line with { PassAttempts = 0 }));
    }
}
