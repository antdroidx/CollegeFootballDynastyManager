namespace DynastyManager.Core.Models;

public sealed record GamePeriodScore(int Period, int HomePoints, int AwayPoints);
public sealed record GameDayBoxScore
{
    public required string HomeTeamName { get; init; }
    public required string AwayTeamName { get; init; }
    public int HomeScore { get; init; }
    public int AwayScore { get; init; }
    public int Quarter { get; init; }
    public int ClockSeconds { get; init; }
    public bool IsComplete { get; init; }
    public required GameTeamStats HomeStats { get; init; }
    public required GameTeamStats AwayStats { get; init; }
    public IReadOnlyList<GamePeriodScore> Periods { get; init; } = [];
    public IReadOnlyList<PlayerSeasonStatLine> Players { get; init; } = [];
}
