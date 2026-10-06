namespace DynastyManager.Core.Models;

public sealed record Player
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required Position PrimaryPosition { get; init; }
    public IReadOnlyCollection<Position> SecondaryPositions { get; init; } = Array.Empty<Position>();

    public string FullName => $"{FirstName} {LastName}";

    public bool CanPlay(Position position) =>
        PrimaryPosition == position || SecondaryPositions.Contains(position);
}
