namespace DynastyManager.Core.Models;

public enum StaffMovementType
{
    Fired,
    Hired,
    Promoted,
    Poached,
    Retained
}

public sealed record StaffMovementRecord
{
    public int SeasonYear { get; init; }
    public Guid StaffId { get; init; }
    public required string FullName { get; init; }
    public StaffRole Role { get; init; }
    public StaffMovementType MovementType { get; init; }
    public string? FromTeamName { get; init; }
    public string? ToTeamName { get; init; }
    public int OverallRating { get; init; }
    public required string Reason { get; init; }
}
