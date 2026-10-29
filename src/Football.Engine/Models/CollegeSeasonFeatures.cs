namespace DynastyManager.Core.Models;

public enum PollKind { Media, Coaches, Computer }
public sealed record PollSnapshot
{
    public int SeasonYear { get; init; }
    public int Week { get; init; }
    public SeasonPhase Phase { get; init; }
    public PollKind Kind { get; init; }
    public IReadOnlyList<NationalRanking> Rankings { get; init; } = [];
}
public sealed record DisciplineEventDefinition(string Id, string Title, string Description, int Severity, bool TeamWide = false);
public sealed record DisciplineIncident
{
    public int SeasonYear { get; init; }
    public int Week { get; init; }
    public required string TeamName { get; init; }
    public required string EventId { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public Guid? PlayerId { get; init; }
    public int Severity { get; init; }
    public bool SanctionIssued { get; init; }
}
public sealed record ProgramConductRecord
{
    public required string TeamName { get; init; }
    public int Score { get; init; } = 80; // High is healthy; low means elevated scrutiny.
    public int LastProcessedYear { get; init; }
    public int LastProcessedWeek { get; init; } = -1;
    public int LastSanctionYear { get; init; }
    public int PostseasonBanSeason { get; init; }
}
public sealed record LeagueMovementRecord(int SeasonYear, string TeamName, string FromConference, string ToConference, bool Promotion);
public sealed record ReplayPlayer(Guid PlayerId, string FullName, string TeamName, Position Position, int? JerseyNumber, int OverallRating, int SpeedRating, int AgilityRating);
public sealed record GameOutlook(string Favorite, double Spread, double HomeWinProbability, double TotalPoints)
{
    public string Label => Spread < .5 ? $"Pick'em · O/U {TotalPoints:F1}" : $"{Favorite} −{Spread:F1} · O/U {TotalPoints:F1}";
}
