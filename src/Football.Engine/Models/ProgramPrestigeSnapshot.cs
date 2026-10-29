namespace DynastyManager.Core.Models;

public sealed record ProgramPrestigeSnapshot
{
    public int SeasonYear { get; init; }
    public required string TeamName { get; init; }
    public int StartingPrestige { get; init; }
    public int EndingPrestige { get; init; }
    public int Wins { get; init; }
    public int Losses { get; init; }
    public int FinalRanking { get; init; }
    public bool WonConference { get; init; }
    public bool MadePlayoff { get; init; }
    public bool WonNationalChampionship { get; init; }
    public int RecruitingClassScore { get; init; }
    public IReadOnlyList<string> Reasons { get; init; } = Array.Empty<string>();

    public int Change => EndingPrestige - StartingPrestige;
}
