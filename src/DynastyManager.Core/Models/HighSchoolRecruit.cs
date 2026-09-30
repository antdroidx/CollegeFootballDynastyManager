namespace DynastyManager.Core.Models;

public sealed record HighSchoolRecruit
{
    public Guid RecruitId { get; init; }
    public int SeasonYear { get; init; }
    public required string FullName { get; init; }
    public Position Position { get; init; }
    public int Stars { get; init; }
    public int TalentLevel { get; init; }
    public int OverallRating { get; init; }
    public int PotentialRating { get; init; }
    public int RegionId { get; init; }
    public string? CommittedTeamName { get; init; }
}
