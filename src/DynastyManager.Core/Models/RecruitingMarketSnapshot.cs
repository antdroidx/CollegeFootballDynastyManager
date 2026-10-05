namespace DynastyManager.Core.Models;

public sealed record RecruitingMarketSnapshot
{
    public required string LeaderTeamName { get; init; }
    public int LeaderScore { get; init; }
}
