namespace DynastyManager.Core.Models;

public sealed record HallOfFameEntry
{
    public Guid PlayerId { get; init; }
    public required string PlayerName { get; init; }
    public required string TeamName { get; init; }
    public Position Position { get; init; }
    public int InductionYear { get; init; }
    public int CareerScore { get; init; }
    public int HonorsScore { get; init; }
    public int RecordedSeasons { get; init; }
    public required string Citation { get; init; }
}
