namespace DynastyManager.Core.Models;

public sealed record CollegeFootballPlayoffSeedRecord
{
    public int SeasonYear { get; init; }
    public int Seed { get; init; }
    public int NationalRank { get; init; }
    public required string TeamName { get; init; }
    public required string ConferenceName { get; init; }
    public bool IsConferenceChampion { get; init; }
    public bool IsAutomaticBid { get; init; }
}
