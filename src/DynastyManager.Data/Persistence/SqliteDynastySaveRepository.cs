using System.Text.Json;
using DynastyManager.Core.Models;
using SQLite;

namespace DynastyManager.Data.Persistence;

public sealed class SqliteDynastySaveRepository : IDynastySaveRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly SQLiteAsyncConnection _database;
    private bool _initialized;

    public SqliteDynastySaveRepository(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
            throw new ArgumentException("A database path is required.", nameof(databasePath));

        _database = new SQLiteAsyncConnection(
            databasePath,
            SQLiteOpenFlags.ReadWrite |
            SQLiteOpenFlags.Create |
            SQLiteOpenFlags.FullMutex);
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        await _database.CreateTableAsync<DynastySaveRecord>();
        _initialized = true;
    }

    public async Task<Guid> SaveAsync(DynastyState state, SaveKind kind, Guid? saveId = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        await InitializeAsync();

        var id = saveId ?? Guid.NewGuid();
        var now = DateTime.UtcNow;

        var record = new DynastySaveRecord
        {
            SaveId = id.ToString("D"),
            DynastyId = state.DynastyId.ToString("D"),
            DynastyName = state.DynastyName,
            UserTeamName = state.UserTeamName,
            SeasonYear = state.SeasonYear,
            Week = state.Week,
            Phase = (int)state.Phase,
            SaveKind = (int)kind,
            UpdatedUtc = now,
            SchemaVersion = state.SchemaVersion,
            SnapshotJson = JsonSerializer.Serialize(state, JsonOptions)
        };

        await _database.InsertOrReplaceAsync(record);
        return id;
    }

    public async Task<DynastyState?> LoadAsync(Guid saveId)
    {
        await InitializeAsync();

        var key = saveId.ToString("D");
        var record = await _database.Table<DynastySaveRecord>()
            .Where(x => x.SaveId == key)
            .FirstOrDefaultAsync();

        if (record is null)
            return null;

        if (record.SchemaVersion > DynastyState.CurrentSchemaVersion)
        {
            throw new NotSupportedException(
                $"Save schema {record.SchemaVersion} is newer than this app supports ({DynastyState.CurrentSchemaVersion}).");
        }

        return JsonSerializer.Deserialize<DynastyState>(record.SnapshotJson, JsonOptions);
    }

    public async Task<IReadOnlyList<DynastySaveInfo>> ListAsync()
    {
        await InitializeAsync();

        var records = await _database.Table<DynastySaveRecord>()
            .OrderByDescending(x => x.UpdatedUtc)
            .ToListAsync();

        return records.Select(ToInfo).ToArray();
    }

    public async Task DeleteAsync(Guid saveId)
    {
        await InitializeAsync();
        await _database.DeleteAsync<DynastySaveRecord>(saveId.ToString("D"));
    }

    public async ValueTask DisposeAsync()
    {
        await _database.CloseAsync();
    }

    private static DynastySaveInfo ToInfo(DynastySaveRecord record) =>
        new(
            Guid.Parse(record.SaveId),
            Guid.Parse(record.DynastyId),
            record.DynastyName,
            record.UserTeamName,
            record.SeasonYear,
            record.Week,
            (SeasonPhase)record.Phase,
            (SaveKind)record.SaveKind,
            record.UpdatedUtc,
            record.SchemaVersion);
}
