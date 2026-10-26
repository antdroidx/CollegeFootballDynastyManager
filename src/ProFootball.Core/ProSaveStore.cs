using System.Text.Json;
namespace ProFootball.Core;

public sealed class ProSaveStore(string root)
{
    static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    readonly SemaphoreSlim gate = new(1, 1);
    string FileFor(Guid id, string suffix = "") => Path.Combine(root, id + suffix + ".json");
    public async Task Save(ProFranchise state)
    {
        await gate.WaitAsync(); try
        {
            Directory.CreateDirectory(root);
            if (File.Exists(FileFor(state.Id, ".deleted"))) throw new InvalidOperationException("This franchise was deleted.");
            var path = FileFor(state.Id); var temp = path + ".tmp";
            var text = JsonSerializer.Serialize(state, Json); await File.WriteAllTextAsync(temp, text);
            if (File.Exists(FileFor(state.Id, ".auto1"))) File.Copy(FileFor(state.Id, ".auto1"), FileFor(state.Id, ".auto2"), true);
            if (File.Exists(path)) File.Copy(path, FileFor(state.Id, ".auto1"), true);
            File.Move(temp, path, true);
        }
        finally { gate.Release(); }
    }
    public async Task<ProFranchise> Load(Guid id)
    {
        if (File.Exists(FileFor(id, ".deleted"))) throw new FileNotFoundException("This franchise was deleted.");
        Exception? error = null;
        foreach (var suffix in new[] { "", ".auto1", ".auto2" }) try
            {
                var state = JsonSerializer.Deserialize<ProFranchise>(await File.ReadAllTextAsync(FileFor(id, suffix)), Json) ?? throw new InvalidDataException("Empty save");
                if (state.SchemaVersion is not (1 or 2)) throw new NotSupportedException("This save requires a newer app version.");
                ProLeagueService.Validate(state.League); return ProMorale.NormalizeContracts(ProContractData.CompleteRookieTerms(ProContractData.MigrateAccounting(ProEconomy.EnsurePickInventory(ProCareer.Initialize(ProPlayerRatingService.Migrate(state.SchemaVersion == 1 ? ProEconomy.Initialize(state with { SchemaVersion = 2 }) : state))))));
            }
            catch (NotSupportedException) { throw; }
            catch (Exception e) when (e is IOException or JsonException or InvalidDataException) { error = e; }
        throw new InvalidDataException("No valid save or rolling backup was found.", error);
    }
    public async Task Delete(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("Invalid save slot.", nameof(id));
        await gate.WaitAsync();
        try
        {
            if (!Directory.Exists(root)) return;
            // A tombstone prevents an old autosave task from resurrecting a deleted slot.
            await File.WriteAllTextAsync(FileFor(id, ".deleted"), "deleted");
            foreach (var path in new[] { FileFor(id), FileFor(id, ".auto1"), FileFor(id, ".auto2"), FileFor(id) + ".tmp" })
                if (File.Exists(path)) File.Delete(path);
        }
        finally { gate.Release(); }
    }
    public IEnumerable<Guid> Slots() => Directory.Exists(root) ? Directory.EnumerateFiles(root, "*.json").Select(Path.GetFileNameWithoutExtension)
        .Where(n => Guid.TryParse(n, out _)).Select(n => Guid.Parse(n!)).Where(id => !File.Exists(FileFor(id, ".deleted"))) : [];
}
