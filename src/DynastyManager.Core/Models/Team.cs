namespace DynastyManager.Core.Models;

public sealed record Team
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public required string Abbreviation { get; init; }
    public required string ConferenceName { get; init; }
    public string? DivisionName { get; init; }
    public int Prestige { get; init; }
    public int LegacyRegionId { get; init; }
}
