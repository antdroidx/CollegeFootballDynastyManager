namespace DynastyManager.Core.Models;

public sealed record Conference
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public string? DivisionOneName { get; init; }
    public string? DivisionTwoName { get; init; }
}
