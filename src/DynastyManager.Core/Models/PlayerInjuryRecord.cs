namespace DynastyManager.Core.Models;

public sealed record PlayerInjuryRecord
{
    public Guid InjuryId { get; init; }
    public int SeasonYear { get; init; }
    public int Week { get; init; }
    public Guid PlayerId { get; init; }
    public required string PlayerName { get; init; }
    public required string TeamName { get; init; }
    public Position Position { get; init; }
    public InjuryBodyArea BodyArea { get; init; }
    public InjurySeverity Severity { get; init; }
    public int InitialWeeks { get; init; }
    public bool IsMedicalRedshirt { get; init; }
    public int SpeedLoss { get; init; }
    public int StrengthLoss { get; init; }
    public int AgilityLoss { get; init; }
    public int AwarenessLoss { get; init; }
    public int TechniqueLoss { get; init; }
    public int DurabilityLoss { get; init; }
    public int PotentialLoss { get; init; }
}
