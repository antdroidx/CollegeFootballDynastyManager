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
}
