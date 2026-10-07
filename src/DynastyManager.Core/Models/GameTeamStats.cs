namespace DynastyManager.Core.Models;

public sealed record GameTeamStats
{
    public int Possessions { get; init; }
    public int PassAttempts { get; init; }
    public int RushAttempts { get; init; }
    public int PassYards { get; init; }
    public int RushYards { get; init; }
    public int Turnovers { get; init; }

    public int TotalYards => PassYards + RushYards;
}
