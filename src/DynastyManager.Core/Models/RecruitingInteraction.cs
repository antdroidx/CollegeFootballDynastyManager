namespace DynastyManager.Core.Models;

public sealed record RecruitingInteraction
{
    public int SeasonYear { get; init; }
    public Guid CandidateId { get; init; }
    public RecruitingCandidateType CandidateType { get; init; }
    public bool IsScouted { get; init; }
    public bool ScholarshipOffered { get; init; }
    public int UserInterest { get; init; }
    public int RecruitingPointsSpent { get; init; }
    public string? CommittedTeamName { get; init; }
}
