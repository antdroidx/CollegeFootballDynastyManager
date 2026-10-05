namespace DynastyManager.Core.Models;

public sealed record PlayerSeasonStatLine
{
    public int SeasonYear { get; init; }
    public Guid PlayerId { get; init; }
    public required string PlayerName { get; init; }
    public required string TeamName { get; init; }
    public Position Position { get; init; }
    public int Games { get; init; }
    public int PassAttempts { get; init; }
    public int PassYards { get; init; }
    public int RushAttempts { get; init; }
    public int RushYards { get; init; }
    public int Touchdowns { get; init; }
    public int Turnovers { get; init; }

    public int ScrimmageYards => PassYards + RushYards;
}
