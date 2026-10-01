namespace DynastyManager.Core.Models;

public sealed record RecruitingInteraction
{
    public Guid ProspectId { get; init; }
    public RecruitingSource Source { get; init; }
    public int SeasonYear { get; init; }
    public int ScoutingPercent { get; init; }
    public bool ScholarshipOffered { get; init; }
    public int UserInterest { get; init; }
    public int RivalInterest { get; init; }
    public string? CommittedTeamName { get; init; }
}
