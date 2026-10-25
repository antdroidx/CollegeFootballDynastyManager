namespace DynastyManager.Core.Models;

public sealed record PlayerInjury
{
    public Guid InjuryId { get; init; } = Guid.NewGuid();
    public int SeasonYear { get; init; }
    public int StartWeek { get; init; }
    public InjuryBodyArea BodyArea { get; init; }
    public InjurySeverity Severity { get; init; }
    public int InitialWeeks { get; init; }
    public int WeeksRemaining { get; init; }
    public bool IsMedicalRedshirt { get; init; }
}
