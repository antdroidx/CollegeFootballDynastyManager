namespace DynastyManager.Core.Models;

public enum OffensivePlaybook { Balanced, Spread, AirRaid, PowerRun, Option, Flexbone, Custom }
public enum DefensivePlaybook { Balanced, FourThree, ThreeFour, Nickel, Custom }
public enum DefensiveApproach { Balanced, StopRun, ProtectPass, Pressure }
public enum OffensiveConcept { Automatic, InsideRun, OutsideRun, QuickPass, PlayAction, DeepPass }
public enum OffensiveFormation { ShotgunSpread, Pistol, Singleback, IForm, Empty, Flexbone }
public enum DefensiveFormation { FourThree, ThreeFour, Nickel, Dime, GoalLine }
public enum OffensivePersonnel { Ten, Eleven, Twelve, TwentyOne, Empty, TripleBack }
public enum DefensivePersonnel { BaseFourThree, BaseThreeFour, Nickel, Dime, GoalLine }
public enum PlayCallSource { Cpu, User }
public enum OffensiveTempo { Automatic, Normal, UpTempo, ChewClock }

public sealed record OffensivePlayDefinition(string Id, string Name, OffensiveFormation Formation,
    OffensivePersonnel Personnel, OffensiveConcept Concept, string Description)
{
    public IReadOnlyList<PlayAssignment> Assignments { get; init; } = [];
}
public sealed record DefensivePlayDefinition(string Id, string Name, DefensiveFormation Formation,
    DefensivePersonnel Personnel, DefensiveApproach Approach, string Description)
{
    public IReadOnlyList<PlayAssignment> Assignments { get; init; } = [];
}

public enum AssignmentTask { Route, Block, Rush, Man, Zone }
public sealed record PlayAssignment
{
    public Position Position { get; init; }
    public int RoleIndex { get; init; }
    public double Depth { get; init; }
    public double Lane { get; init; } = .5;
    public AssignmentTask Task { get; init; }
    public string Route { get; init; } = "Curls";
}

public sealed record TeamGamePlan
{
    public required string TeamName { get; init; }
    public OffensivePlaybook Offense { get; init; }
    public DefensivePlaybook Defense { get; init; }
    public DefensiveApproach Approach { get; init; }
    public OffensiveTempo Tempo { get; init; }
    public OffensiveFormation OffensiveFormation { get; init; }
    public DefensiveFormation DefensiveFormation { get; init; }
    public IReadOnlyList<string> CustomOffensivePlayIds { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> CustomDefensivePlayIds { get; init; } = Array.Empty<string>();
    public IReadOnlyList<OffensivePlayDefinition> CreatedOffensivePlays { get; init; } = [];
    public IReadOnlyList<DefensivePlayDefinition> CreatedDefensivePlays { get; init; } = [];
    public IReadOnlyList<string> OpeningPlayIds { get; init; } = Array.Empty<string>();
    public IReadOnlyList<OffensiveConcept> OpeningScript { get; init; } = Array.Empty<OffensiveConcept>();
}

public sealed record ResolvedPlayCall(
    OffensivePlayDefinition Offense,
    DefensivePlayDefinition Defense,
    PlayCallSource Source);
