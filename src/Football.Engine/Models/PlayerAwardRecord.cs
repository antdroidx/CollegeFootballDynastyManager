namespace DynastyManager.Core.Models;

public sealed record PlayerAwardRecord
{
    public int SeasonYear { get; init; }
    public required string AwardName { get; init; }
    public Guid PlayerId { get; init; }
    public required string PlayerName { get; init; }
    public required string TeamName { get; init; }
    public Position Position { get; init; }
    public bool IsAllAmerican { get; init; }
    public bool IsPreseason { get; init; }
    public string? ConferenceName { get; init; }
    public int SelectionTeam { get; init; } = 1;
    public required string Summary { get; init; }
}
