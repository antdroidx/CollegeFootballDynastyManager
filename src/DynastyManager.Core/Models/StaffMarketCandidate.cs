namespace DynastyManager.Core.Models;

public enum StaffCandidateOrigin
{
    FreeAgent,
    FiredStaff,
    ActiveStaff
}

public sealed record StaffMarketCandidate
{
    public Guid CandidateId { get; init; } = Guid.NewGuid();
    public int SeasonYear { get; init; }
    public required StaffMember Profile { get; init; }
    public StaffRole TargetRole { get; init; }
    public StaffCandidateOrigin Origin { get; init; }
    public string? SourceTeamName { get; init; }
    public StaffRole? SourceRole { get; init; }
    public int GrowthPotential { get; init; } = 3;
    public int MinimumProgramPrestige { get; init; } = 45;
    public required string StyleLabel { get; init; }
    public bool IsAvailable { get; init; } = true;
}
