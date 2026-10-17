namespace DynastyManager.Core.Models;

public enum OffensivePlaybook { Balanced, Spread, AirRaid, PowerRun, Option }
public enum DefensivePlaybook { Balanced, FourThree, ThreeFour, Nickel }
public enum DefensiveApproach { Balanced, StopRun, ProtectPass, Pressure }
public enum OffensiveConcept { Automatic, InsideRun, OutsideRun, QuickPass, PlayAction, DeepPass }

public sealed record TeamGamePlan
{
    public required string TeamName { get; init; }
    public OffensivePlaybook Offense { get; init; }
    public DefensivePlaybook Defense { get; init; }
    public DefensiveApproach Approach { get; init; }
    public IReadOnlyList<OffensiveConcept> OpeningScript { get; init; } = Array.Empty<OffensiveConcept>();
}
