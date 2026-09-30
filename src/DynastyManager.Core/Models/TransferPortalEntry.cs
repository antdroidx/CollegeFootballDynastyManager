namespace DynastyManager.Core.Models;

public sealed record TransferPortalEntry
{
    public int SeasonYear { get; init; }
    public required string OriginTeamName { get; init; }
    public required DynastyPlayer Player { get; init; }
}
