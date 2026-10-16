using SQLite;

namespace DynastyManager.Data.Persistence;

[Table("dynasty_saves")]
internal sealed class DynastySaveRecord
{
    [PrimaryKey]
    [Column("save_id")]
    public string SaveId { get; set; } = string.Empty;

    [Indexed]
    [Column("dynasty_id")]
    public string DynastyId { get; set; } = string.Empty;

    [Column("dynasty_name")]
    public string DynastyName { get; set; } = string.Empty;

    [Column("user_team_name")]
    public string UserTeamName { get; set; } = string.Empty;

    [Column("season_year")]
    public int SeasonYear { get; set; }

    [Column("week")]
    public int Week { get; set; }

    [Column("phase")]
    public int Phase { get; set; }

    [Column("save_kind")]
    public int SaveKind { get; set; }

    [Column("updated_utc")]
    public DateTime UpdatedUtc { get; set; }

    [Column("schema_version")]
    public int SchemaVersion { get; set; }

    [Column("snapshot_json")]
    public string SnapshotJson { get; set; } = string.Empty;
}
