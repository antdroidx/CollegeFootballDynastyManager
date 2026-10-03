namespace DynastyManager.Core.Models;

public sealed record HighSchoolRecruit
{
    public Guid RecruitId { get; init; }
    public int SeasonYear { get; init; }
    public required string FullName { get; init; }
    public required Position Position { get; init; }
    public int StarRating { get; init; }
    public int TrueOverallRating { get; init; }
    public int PotentialRating { get; init; }
    public int HomeRegion { get; init; }
}
