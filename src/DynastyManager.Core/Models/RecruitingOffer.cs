namespace DynastyManager.Core.Models;

public enum RecruitingOfferStatus { Active, Withdrawn, Signed, Lost }

public sealed record RecruitingOffer
{
    public int SeasonYear { get; init; }
    public RecruitingSource Source { get; init; }
    public Guid ProspectId { get; init; }
    public required string TeamName { get; init; }
    public int Interest { get; init; }
    public int PointsInvested { get; init; }
    public int OfferedWeek { get; init; }
    public int LastContactWeek { get; init; } = -1;
    public SeasonPhase? LastContactPhase { get; init; }
    public bool WasCpuAssisted { get; init; }
    public RecruitingOfferStatus Status { get; init; } = RecruitingOfferStatus.Active;
}
