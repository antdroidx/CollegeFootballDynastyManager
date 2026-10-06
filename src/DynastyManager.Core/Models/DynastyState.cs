namespace DynastyManager.Core.Models;

public sealed record DynastyState
{
    public const int CurrentSchemaVersion = 14;

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
    public IReadOnlyList<TransferPortalExitRecord> TransferPortalExitHistory { get; init; } =
        Array.Empty<TransferPortalExitRecord>();
    public IReadOnlyList<PendingTransferCommitment> PendingTransferCommitments { get; init; } =
        Array.Empty<PendingTransferCommitment>();
    public int TransferPortalEntryWindowSeasonYear { get; init; }
    public IReadOnlyList<PlayerDepartureRecord> RecentPlayerDepartures { get; init; } =
        Array.Empty<PlayerDepartureRecord>();
    public IReadOnlyList<PlayerInjuryRecord> InjuryHistory { get; init; } =
        Array.Empty<PlayerInjuryRecord>();
    public IReadOnlyList<PlayerDevelopmentRecord> PlayerDevelopmentHistory { get; init; } =
        Array.Empty<PlayerDevelopmentRecord>();
    public IReadOnlyList<HighSchoolRecruit> HighSchoolRecruitingPool { get; init; } =
        Array.Empty<HighSchoolRecruit>();
    public IReadOnlyList<RecruitingInteraction> RecruitingInteractions { get; init; } =
        Array.Empty<RecruitingInteraction>();
    public IReadOnlyList<RecruitingCommitmentRecord> RecruitingCommitments { get; init; } =
        Array.Empty<RecruitingCommitmentRecord>();
    public IReadOnlyList<StaffMember> Staff { get; init; } =
        Array.Empty<StaffMember>();
    public StaffMember? UserHeadCoach { get; init; }
    public int UserCoachJobSecurity { get; init; } = 70;
    public bool UserCoachIsFired { get; init; }
    public string? UserCoachFiredFromTeamName { get; init; }
    public int UserCoachLastEvaluatedSeasonYear { get; init; }
    public IReadOnlyList<UserCoachJobOffer> UserCoachJobOffers { get; init; } =
        Array.Empty<UserCoachJobOffer>();
    public IReadOnlyList<UserCoachCareerRecord> UserCoachCareerHistory { get; init; } =
        Array.Empty<UserCoachCareerRecord>();
    public IReadOnlyList<StaffMarketCandidate> StaffMarketCandidates { get; init; } =
        Array.Empty<StaffMarketCandidate>();
    public IReadOnlyList<StaffMovementRecord> StaffMovementHistory { get; init; } =
        Array.Empty<StaffMovementRecord>();
    public int StaffMarketSeasonYear { get; init; }
    public int StaffMarketProcessedSeasonYear { get; init; }
    public int StaffLastAdvancedSeasonYear { get; init; }
    public IReadOnlyList<ProgramPrestigeSnapshot> ProgramPrestigeHistory { get; init; } =
        Array.Empty<ProgramPrestigeSnapshot>();
    public IReadOnlyList<TeamSeasonHistoryRecord> TeamSeasonHistory { get; init; } =
        Array.Empty<TeamSeasonHistoryRecord>();
    public IReadOnlyList<PlayerSeasonStatLine> PlayerSeasonStats { get; init; } =
        Array.Empty<PlayerSeasonStatLine>();
    public IReadOnlyList<PlayerAwardRecord> PlayerAwardHistory { get; init; } =
        Array.Empty<PlayerAwardRecord>();
    public IReadOnlyList<ScoutStaff> ScoutingStaff { get; init; } =
        Array.Empty<ScoutStaff>();
    public IReadOnlyList<ScoutAssignment> ScoutAssignments { get; init; } =
        Array.Empty<ScoutAssignment>();
    public ScoutingRecommendationReport? ScoutingRecommendationReport { get; init; }
    public int RecruitingPointsRemaining { get; init; }
    public SeasonPhase? RecruitingPointsPhase { get; init; }
    public int RecruitingPointsWeek { get; init; } = -1;
    public bool RecruitingAssistanceEnabled { get; init; } = true;
    public int RecruitingAssistanceSeasonYear { get; init; }
    public int RecruitingAssistanceWeek { get; init; } = -1;
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
}
