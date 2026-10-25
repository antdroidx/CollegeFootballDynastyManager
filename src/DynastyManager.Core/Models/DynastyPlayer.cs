namespace DynastyManager.Core.Models;

public sealed record DynastyPlayer
{
    // Set on derived game personnel only; natural roster data remains unchanged.
    public int? PlayingPositionCode { get; init; }
    public Guid PlayerId { get; init; }
    public required string FullName { get; init; }
    public required string TeamName { get; init; }
    public required Position Position { get; init; }
    public int? NcaaPositionCode { get; init; }
    public int ClassYear { get; init; }
    public int? OriginalRecruitingStars { get; init; }
    public bool RecruitingStarsEstimated { get; init; }
    public int? JerseyNumber { get; init; }
    public int HeightInches { get; init; }
    public int WeightPounds { get; init; }
    public NcaaPlayerAppearance? Appearance { get; init; }
    public int TalentLevel { get; init; }
    public int OverallRating { get; init; }
    public int RatingBaselineSeason { get; init; }
    public int SeasonStartOverall { get; init; }
    public int PotentialRating { get; init; }
    public int SpeedRating { get; init; }
    public int StrengthRating { get; init; }
    public int AgilityRating { get; init; }
    public int AwarenessRating { get; init; }
    public int InjuryRating { get; init; }
    public PlayerSkillRatings? Skills { get; init; }
    public PlayerInjury? CurrentInjury { get; init; }
    public bool IsWalkOn { get; init; }
    public int DepthChartOrder { get; init; }
    public bool IsRedshirted { get; init; }
    public bool HasRedshirted { get; init; }
}
