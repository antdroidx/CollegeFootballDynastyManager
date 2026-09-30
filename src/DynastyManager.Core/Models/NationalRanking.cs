namespace DynastyManager.Core.Models;

public sealed record NationalRanking
{
    public int Rank { get; init; }
    public required string TeamName { get; init; }
    public required string ConferenceName { get; init; }
    public int Wins { get; init; }
    public int Losses { get; init; }
    public double Score { get; init; }
    public double PointsPerGame { get; init; }
    public double OpponentPointsPerGame { get; init; }
}
