namespace DynastyManager.Core.Models;

public enum OffensivePlaybook { Balanced, Spread, AirRaid, PowerRun, Option }
public enum DefensivePlaybook { Balanced, FourThree, ThreeFour, Nickel }
public enum DefensiveApproach { Balanced, StopRun, ProtectPass, Pressure }
public enum OffensiveConcept { Automatic, InsideRun, OutsideRun, QuickPass, PlayAction, DeepPass }
public enum OffensiveFormation { ShotgunSpread, Pistol, Singleback, IForm, Empty }
public enum DefensiveFormation { FourThree, ThreeFour, Nickel, Dime, GoalLine }
public enum OffensivePersonnel { Ten, Eleven, Twelve, TwentyOne, Empty }
public enum DefensivePersonnel { BaseFourThree, BaseThreeFour, Nickel, Dime, GoalLine }

public sealed record OffensivePlayDefinition(string Id, string Name, OffensiveFormation Formation,
    OffensivePersonnel Personnel, OffensiveConcept Concept, string Description);
public sealed record DefensivePlayDefinition(string Id, string Name, DefensiveFormation Formation,
    DefensivePersonnel Personnel, DefensiveApproach Approach, string Description);

public sealed record TeamGamePlan
{
    public required string TeamName { get; init; }
    public OffensivePlaybook Offense { get; init; }
    public DefensivePlaybook Defense { get; init; }
    public DefensiveApproach Approach { get; init; }
    public OffensiveFormation OffensiveFormation { get; init; }
    public DefensiveFormation DefensiveFormation { get; init; }
    public IReadOnlyList<string> FavoritePlayIds { get; init; } = Array.Empty<string>();
    public IReadOnlyList<OffensiveConcept> OpeningScript { get; init; } = Array.Empty<OffensiveConcept>();
}
