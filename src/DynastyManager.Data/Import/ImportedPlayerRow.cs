using DynastyManager.Core.Models;

namespace DynastyManager.Data.Import;

public sealed record ImportedPlayerRow
{
    public required string TeamName { get; init; }
    public required string FullName { get; init; }
    public required Position Position { get; init; }
    public int Year { get; init; }
    public int LegacyTalentLevel { get; init; }
}
