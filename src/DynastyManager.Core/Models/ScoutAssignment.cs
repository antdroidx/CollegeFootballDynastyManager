namespace DynastyManager.Core.Models;

public enum ScoutAssignmentScope
{
    National,
    State,
    Region,
    Position,
    Offense,
    Defense,
    SpecialTeams,
    TeamNeeds
}

public sealed record ScoutAssignment
{
    public Guid ScoutId { get; init; }
    public ScoutAssignmentScope Scope { get; init; }
    public string? Target { get; init; }
}
