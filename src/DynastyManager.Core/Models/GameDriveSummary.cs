namespace DynastyManager.Core.Models;

public enum DriveOutcome { Punt, Touchdown, FieldGoal, Turnover }

public sealed record GameDriveSummary
{
    public required string TeamName { get; init; }
    public int PossessionNumber { get; init; }
    public OffensiveConcept Concept { get; init; }
    public DefensiveApproach DefenseFaced { get; init; }
    public int PassAttempts { get; init; }
    public int RushAttempts { get; init; }
    public int PassYards { get; init; }
    public int RushYards { get; init; }
    public DriveOutcome Outcome { get; init; }
    public int Points { get; init; }
    public bool IsOvertime { get; init; }
}
