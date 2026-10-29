namespace DynastyManager.Core.Models;

public sealed record ScoutStaff
{
    public Guid ScoutId { get; init; } = Guid.NewGuid();
    public required string FullName { get; init; }
    public int TalentEvaluation { get; init; }
    public int PotentialEvaluation { get; init; }
    public int RegionalKnowledge { get; init; }
    public int WorkRate { get; init; }
}
