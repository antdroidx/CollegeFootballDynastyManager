namespace DynastyManager.Core.Models;

public sealed record DynastyPlayer
{
    public Guid PlayerId { get; init; }
    public required string FullName { get; init; }
    public required string TeamName { get; init; }
    public required Position Position { get; init; }
    public int ClassYear { get; init; }
    public int TalentLevel { get; init; }
    public int OverallRating { get; init; }
    public int PotentialRating { get; init; }
    public int SpeedRating { get; init; }
    public int StrengthRating { get; init; }
    public int AgilityRating { get; init; }
    public int AwarenessRating { get; init; }
    public int TechniqueRating { get; init; }
    public int DurabilityRating { get; init; }
    public PlayerInjury? CurrentInjury { get; init; }
    public bool IsWalkOn { get; init; }
    public int DepthChartOrder { get; init; }
    public bool IsRedshirted { get; init; }
    public bool HasRedshirted { get; init; }
}
