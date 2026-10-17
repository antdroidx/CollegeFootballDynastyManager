namespace DynastyManager.Core.Models;

public sealed record PlayerCareerRecord
{
    public Guid PlayerId { get; init; }
    public required string PlayerName { get; init; }
    public Position Position { get; init; }
    public int? OriginalRecruitingStars { get; init; }
    public int FirstRecordedSeason { get; init; }
    public int LastRecordedSeason { get; init; }
}
