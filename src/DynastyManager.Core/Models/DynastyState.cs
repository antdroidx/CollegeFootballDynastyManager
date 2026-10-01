namespace DynastyManager.Core.Models;

public sealed record DynastyState
{
    public const int CurrentSchemaVersion = 3;

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
    public IReadOnlyList<HighSchoolRecruit> HighSchoolRecruitingPool { get; init; } =
        Array.Empty<HighSchoolRecruit>();
    public IReadOnlyList<RecruitingInteraction> RecruitingInteractions { get; init; } =
        Array.Empty<RecruitingInteraction>();
    public IReadOnlyList<RecruitingCommitmentRecord> RecruitingCommitments { get; init; } =
        Array.Empty<RecruitingCommitmentRecord>();
    public int RecruitingPointsRemaining { get; init; }
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
}
