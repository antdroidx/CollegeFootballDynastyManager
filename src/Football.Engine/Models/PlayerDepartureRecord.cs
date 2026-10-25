namespace DynastyManager.Core.Models;

public sealed record PlayerDepartureRecord
{
    public int SeasonYear { get; init; }
    public Guid PlayerId { get; init; }
    public required string FullName { get; init; }
    public required string TeamName { get; init; }
    public required Position Position { get; init; }
    public int OverallRating { get; init; }
    public int Leadership { get; init; }
    public int Awareness { get; init; }
    public int Discipline { get; init; }
    public NcaaPlayerAppearance? Appearance { get; init; }
    public PlayerDepartureReason Reason { get; init; }
}
