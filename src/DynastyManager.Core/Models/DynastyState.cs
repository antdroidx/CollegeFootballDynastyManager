namespace DynastyManager.Core.Models;

public sealed record DynastyState
{
    public const int CurrentSchemaVersion = 2;

    public Guid DynastyId { get; init; } = Guid.NewGuid();
    public required string DynastyName { get; init; }
    public required string UserTeamName { get; init; }
    public int SeasonYear { get; init; }
    public int Week { get; init; }
    public SeasonPhase Phase { get; init; } = SeasonPhase.Preseason;
    public IReadOnlyList<ScheduledGame> Schedule { get; init; } = Array.Empty<ScheduledGame>();
    public IReadOnlyList<ConferenceChampionRecord> ConferenceChampionshipHistory { get; init; } =
        Array.Empty<ConferenceChampionRecord>();
    public IReadOnlyList<CollegeFootballPlayoffSeedRecord> CollegeFootballPlayoffHistory { get; init; } =
        Array.Empty<CollegeFootballPlayoffSeedRecord>();
    public IReadOnlyList<NationalChampionRecord> NationalChampionshipHistory { get; init; } =
        Array.Empty<NationalChampionRecord>();
    public IReadOnlyList<BowlResultRecord> BowlHistory { get; init; } =
        Array.Empty<BowlResultRecord>();
    public IReadOnlyList<DynastyPlayer> ActiveRoster { get; init; } =
        Array.Empty<DynastyPlayer>();
    public IReadOnlyList<TransferPortalEntry> TransferPortalEntries { get; init; } =
        Array.Empty<TransferPortalEntry>();
    public IReadOnlyList<PlayerDepartureRecord> RecentPlayerDepartures { get; init; } =
        Array.Empty<PlayerDepartureRecord>();
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
}
