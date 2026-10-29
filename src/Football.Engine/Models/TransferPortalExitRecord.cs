namespace DynastyManager.Core.Models;

public sealed record TransferPortalExitRecord
{
    public int EntrySeasonYear { get; init; }
    public int ExitSeasonYear { get; init; }
    public int ExitWeek { get; init; }
    public Guid PlayerId { get; init; }
    public required string PlayerName { get; init; }
    public required string OriginTeamName { get; init; }
    public Position Position { get; init; }
    public int OverallRating { get; init; }
    public TransferPortalExitDestination Destination { get; init; }
}
