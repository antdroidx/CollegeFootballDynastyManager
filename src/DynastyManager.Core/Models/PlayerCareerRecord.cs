namespace DynastyManager.Core.Models;

public sealed record PlayerCareerRecord
{
    public Guid PlayerId { get; init; }
    public required string PlayerName { get; init; }
    public Position Position { get; init; }
    public int? OriginalRecruitingStars { get; init; }
    public bool RecruitingStarsEstimated { get; init; }
    public int HeightInches { get; init; }
    public int WeightPounds { get; init; }
    public NcaaPlayerAppearance? Appearance { get; init; }
    public int FirstRecordedSeason { get; init; }
    public int LastRecordedSeason { get; init; }
}
