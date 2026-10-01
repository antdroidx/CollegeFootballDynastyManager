namespace DynastyManager.Core.Models;

public sealed record RecruitingCommitmentRecord
{
    public int SeasonYear { get; init; }
    public Guid ProspectId { get; init; }
    public RecruitingSource Source { get; init; }
    public required string PlayerName { get; init; }
    public required string TeamName { get; init; }
    public required Position Position { get; init; }
    public int OverallRating { get; init; }
    public bool WasCpuAssisted { get; init; }
}
