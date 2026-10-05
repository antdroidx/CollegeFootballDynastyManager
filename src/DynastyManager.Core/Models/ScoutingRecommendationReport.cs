namespace DynastyManager.Core.Models;

public enum ScoutingRecommendationSection
{
    TopTargets,
    BestFits,
    HiddenGems,
    TeamNeeds,
    HighInterest
}

public sealed record ScoutingRecommendation
{
    public ScoutingRecommendationSection Section { get; init; }
    public Guid ProspectId { get; init; }
    public RecruitingSource Source { get; init; }
    public required string PlayerName { get; init; }
    public Position Position { get; init; }
    public required string Summary { get; init; }
}

public sealed record ScoutingRecommendationReport
{
    public int SeasonYear { get; init; }
    public int GeneratedWeek { get; init; }
    public IReadOnlyList<ScoutingRecommendation> Recommendations { get; init; } =
        Array.Empty<ScoutingRecommendation>();
}
