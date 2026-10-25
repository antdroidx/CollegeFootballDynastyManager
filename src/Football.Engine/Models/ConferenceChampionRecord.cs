namespace DynastyManager.Core.Models;

public sealed record ConferenceChampionRecord
{
    public int SeasonYear { get; init; }
    public required string ConferenceName { get; init; }
    public required string ChampionTeamName { get; init; }
    public required string RunnerUpTeamName { get; init; }
    public int ChampionScore { get; init; }
    public int RunnerUpScore { get; init; }
}
