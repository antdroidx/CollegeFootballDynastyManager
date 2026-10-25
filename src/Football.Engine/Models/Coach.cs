namespace DynastyManager.Core.Models;

public enum CoachRole
{
    HeadCoach,
    OffensiveCoordinator,
    DefensiveCoordinator
}

public sealed record Coach
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public required string TeamName { get; init; }
    public CoachRole Role { get; init; } = CoachRole.HeadCoach;
    public int Rating { get; init; }
}
