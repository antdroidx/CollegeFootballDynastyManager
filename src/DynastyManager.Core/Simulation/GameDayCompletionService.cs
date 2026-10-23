namespace DynastyManager.Core.Simulation;

/// <summary>Serializes final persistence and navigation without treating an in-flight save as complete.</summary>
public sealed class GameDayCompletionService
{
    private readonly SemaphoreSlim saveGate = new(1, 1);
    private readonly SemaphoreSlim closeGate = new(1, 1);
    public bool IsSaved { get; private set; }
    public bool IsSaving { get; private set; }
    public bool IsClosed { get; private set; }
    public async Task EnsureSavedAsync(Func<Task> save)
    {
        await saveGate.WaitAsync();
        try
        {
            if (IsSaved) return;
            IsSaving = true;
            await save();
            IsSaved = true;
        }
        finally { IsSaving = false; saveGate.Release(); }
    }
    public async Task CloseAsync(Func<Task> save, Func<Task> close)
    {
        await closeGate.WaitAsync();
        try
        {
            if (IsClosed) return;
            await EnsureSavedAsync(save);
            await close();
            IsClosed = true;
        }
        finally { closeGate.Release(); }
    }
}
