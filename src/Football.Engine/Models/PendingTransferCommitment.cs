namespace DynastyManager.Core.Models;

public sealed record PendingTransferCommitment
{
    public Guid ProspectId { get; init; }
    public int CommittedSeasonYear { get; init; }
    public int CommittedWeek { get; init; }
    public int JoinSeasonYear { get; init; }
    public required string TeamName { get; init; }
    public required string OriginTeamName { get; init; }
    public required DynastyPlayer Player { get; init; }
    public bool WasUserCommitment { get; init; }
}
