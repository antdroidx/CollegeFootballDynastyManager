using DynastyManager.Core.Models;

namespace DynastyManager.Data.Persistence;

public interface IDynastySaveRepository : IAsyncDisposable
{
    Task InitializeAsync();
    Task<Guid> SaveAsync(DynastyState state, SaveKind kind, Guid? saveId = null);
    Task<DynastyState?> LoadAsync(Guid saveId);
    Task<IReadOnlyList<DynastySaveInfo>> ListAsync();
    Task DeleteAsync(Guid saveId);
}
