namespace DynastyManager.Core.Models;

public sealed record PlayerSeasonStatLine
{
    public int SeasonYear { get; init; }
    public Guid PlayerId { get; init; }
    public required string PlayerName { get; init; }
    public required string TeamName { get; init; }
    public int? OriginalRecruitingStars { get; init; }
    public bool RecruitingStarsEstimated { get; init; }
    public int? JerseyNumber { get; init; }
    public int HeightInches { get; init; }
    public int WeightPounds { get; init; }
    public NcaaPlayerAppearance? Appearance { get; init; }
    public Position Position { get; init; }
    public int Games { get; init; }
    public int TrackedGames { get; init; }
    public int OffensiveSnaps { get; init; }
    public int DefensiveSnaps { get; init; }
    public int SpecialTeamsSnaps { get; init; }
    public int PassBlockSnaps { get; init; }
    public int RunBlockSnaps { get; init; }
    public int PassBlockWins { get; init; }
    public int RunBlockWins { get; init; }
    public int PressuresAllowed { get; init; }
    public int SacksAllowed { get; init; }
    public int Dropbacks { get; init; }
    public int PressuredDropbacks { get; init; }
    public int TimesSacked { get; init; }
    public int SackYardsLost { get; init; }
    public int Targets { get; init; }
    public int TrackedReceptions { get; init; }
    public int LongestPass { get; init; }
    public int BrokenTackles { get; init; }
    public int LongestRush { get; init; }
    public int LongestReception { get; init; }
    public int DefensivePressures { get; init; }
    public int ForcedFumbles { get; init; }
    public int CoverageTargets { get; init; }
    public int CoverageReceptions { get; init; }
    public int CoverageYards { get; init; }
    public int Penalties { get; init; }
    public int PenaltyYards { get; init; }
    public int LongFieldGoal { get; init; }
    public int FieldGoalsMadeUnder40 { get; init; }
    public int FieldGoalsAttemptedUnder40 { get; init; }
    public int FieldGoalsMade40To49 { get; init; }
    public int FieldGoalsAttempted40To49 { get; init; }
    public int FieldGoalsMade50Plus { get; init; }
    public int FieldGoalsAttempted50Plus { get; init; }
    public int PuntTouchbacks { get; init; }
    public int TrackedPunts { get; init; }
    public int NetPuntYards { get; init; }
    public int FairCatches { get; init; }

    public int PassAttempts { get; init; }
    public int PassCompletions { get; init; }
    public int PassYards { get; init; }
    public int PassingTouchdowns { get; init; }
    public int InterceptionsThrown { get; init; }
    public int RushAttempts { get; init; }
    public int RushYards { get; init; }
    public int RushingTouchdowns { get; init; }
    public int RushingFumbles { get; init; }
    public int Rushing20Plus { get; init; }
    public int Receptions { get; init; }
    public int ReceivingYards { get; init; }
    public int ReceivingTouchdowns { get; init; }
    public int YardsAfterCatch { get; init; }
    public int Drops { get; init; }
    public int ReceivingFumbles { get; init; }
    public int Receiving20Plus { get; init; }
    public int Tackles { get; init; }
    public int TacklesForLoss { get; init; }
    public int Sacks { get; init; }
    public int PassesDefended { get; init; }
    public int Interceptions { get; init; }
    public int DefensiveReturnYards { get; init; }
    public int DefensiveTouchdowns { get; init; }
    public int FumbleRecoveries { get; init; }
    public int ExtraPointsMade { get; init; }
    public int ExtraPointsAttempted { get; init; }
    public int FieldGoalsMade { get; init; }
    public int FieldGoalsAttempted { get; init; }
    public int Punts { get; init; }
    public int PuntYards { get; init; }
    public int LongPunt { get; init; }
    public int PuntsInside20 { get; init; }
    public int PuntsInside10 { get; init; }
    public int KickReturns { get; init; }
    public int KickReturnYards { get; init; }
    public int KickReturnTouchdowns { get; init; }
    public int PuntReturns { get; init; }
    public int PuntReturnYards { get; init; }
    public int PuntReturnTouchdowns { get; init; }
    public int Touchdowns { get; init; }
    public int Turnovers { get; init; }

    public int ScrimmageYards => PassYards + RushYards + ReceivingYards;
}
