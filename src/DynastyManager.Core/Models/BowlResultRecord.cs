namespace DynastyManager.Core.Models;

public sealed record BowlResultRecord
{
    public int SeasonYear { get; init; }
    public required string BowlName { get; init; }
    public required string WinnerTeamName { get; init; }
    public required string LoserTeamName { get; init; }
    public int WinnerScore { get; init; }
    public int LoserScore { get; init; }
}
