using System.Text.Json;
using DynastyManager.Core.Models;

namespace DynastyManager.Data.Persistence;

/// <summary>User-owned starter data, stored separately from dynasty snapshots.</summary>
public sealed class StartingLibraryRepository(string path)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    public async Task<StartingLibrary> LoadAsync() => File.Exists(path)
        ? JsonSerializer.Deserialize<StartingLibrary>(await File.ReadAllTextAsync(path), Options)
            ?? throw new InvalidDataException("The starting library is empty or invalid.")
        : new StartingLibrary();

    public async Task SaveAsync(StartingLibrary library)
    {
        var temporary = path + ".tmp";
        await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(library, Options));
        File.Move(temporary, path, overwrite: true);
    }
}
