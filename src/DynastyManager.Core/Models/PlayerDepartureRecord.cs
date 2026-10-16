namespace DynastyManager.Core.Models;

public sealed record PlayerDepartureRecord
{
    public int SeasonYear { get; init; }
    public Guid PlayerId { get; init; }
    public required string FullName { get; init; }
    public required string TeamName { get; init; }
    public required Position Position { get; init; }
    public int OverallRating { get; init; }
    public PlayerDepartureReason Reason { get; init; }
}
