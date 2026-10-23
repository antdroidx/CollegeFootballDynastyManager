using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;
namespace DynastyManager.Core.Tests;

public sealed class GameDayCompletionTests
{
    [Fact]
    public async Task CloseWaitsForInFlightSaveAndDoesNotSaveOrNavigateTwice()
    {
        var service = new GameDayCompletionService();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var saves = 0; var closes = 0;
        async Task Save() { Interlocked.Increment(ref saves); await release.Task; }
        Task Close() { Interlocked.Increment(ref closes); return Task.CompletedTask; }
        var saving = service.EnsureSavedAsync(Save);
        Assert.True(service.IsSaving); Assert.False(service.IsSaved);
        var first = service.CloseAsync(Save, Close);
        var second = service.CloseAsync(Save, Close);
        Assert.False(first.IsCompleted); Assert.Equal(0, closes);
        release.SetResult();
        await Task.WhenAll(saving, first, second);
        Assert.Equal(1, saves); Assert.Equal(1, closes);
        Assert.True(service.IsSaved); Assert.True(service.IsClosed); Assert.False(service.IsSaving);
    }

    [Fact]
    public async Task SaveFailureLeavesGameOpenAndRetryable()
    {
        var service = new GameDayCompletionService(); var closes = 0;
        await Assert.ThrowsAsync<IOException>(() => service.CloseAsync(() => throw new IOException("disk unavailable"), () => { closes++; return Task.CompletedTask; }));
        Assert.False(service.IsSaved); Assert.False(service.IsClosed); Assert.False(service.IsSaving); Assert.Equal(0, closes);
        await service.CloseAsync(() => Task.CompletedTask, () => { closes++; return Task.CompletedTask; });
        Assert.True(service.IsSaved); Assert.True(service.IsClosed); Assert.Equal(1, closes);
    }

    [Fact]
    public async Task NavigationFailureCanBeRetriedWithoutResaving()
    {
        var service = new GameDayCompletionService(); var saves = 0;
        Task Save() { saves++; return Task.CompletedTask; }
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CloseAsync(Save, () => throw new InvalidOperationException("navigation busy")));
        Assert.True(service.IsSaved); Assert.False(service.IsClosed);
        await service.CloseAsync(Save, () => Task.CompletedTask);
        Assert.Equal(1, saves); Assert.True(service.IsClosed);
    }

    [Fact]
    public async Task SimulatedRemainderRetainsExistingPlaysAndSavesFinishedStatsOnce()
    {
        var session = GameDayEngineTests.Session(GameDayMode.Play);
        GameDayEngine.Step(session, "off-spread-verts");
        var opening = session.Events.ToArray();
        await Task.Run(() => GameDayEngine.SimulateToEnd(session));
        Assert.True(session.IsComplete); Assert.Equal(opening, session.Events.Take(opening.Length));
        var completion = new GameDayCompletionService(); ScheduledGame? stored = null; var saves = 0;
        Task Save() { stored = GameDayEngine.Finish(session); saves++; return Task.CompletedTask; }
        await Task.WhenAll(completion.EnsureSavedAsync(Save), completion.CloseAsync(Save, () => Task.CompletedTask));
        Assert.NotNull(stored); Assert.True(stored.HasPlayed); Assert.NotEmpty(stored.PlayerStats);
        Assert.Equal(session.HomeScore, stored.HomeScore); Assert.Equal(session.AwayScore, stored.AwayScore);
        Assert.Equal(1, saves);
    }
}
