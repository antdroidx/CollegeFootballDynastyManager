namespace DynastyManager.Core.Models;

public enum PlayerDevelopmentStage
{
    Offseason,
    Midseason
}

public sealed record PlayerDevelopmentRecord
{
    public int SeasonYear { get; init; }
    public Guid PlayerId { get; init; }
    public required string PlayerName { get; init; }
    public required string TeamName { get; init; }
    public Position Position { get; init; }
    public int BeforeOverall { get; init; }
    public int AfterOverall { get; init; }
    public int BeforePotential { get; init; }
    public int AfterPotential { get; init; }
    public PlayerDevelopmentStage Stage { get; init; } =
        PlayerDevelopmentStage.Offseason;
    public int OverallChange => AfterOverall - BeforeOverall;
}
