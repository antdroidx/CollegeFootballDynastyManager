namespace DynastyManager.Core.Models;

/// <summary>Original NCAA appearance codes. Null fields retain procedural defaults.</summary>
public sealed record NcaaPlayerAppearance
{
    public int? SkinTone { get; init; }
    public int? HairColor { get; init; }
    public int? HairStyle { get; init; }
    public int? Face { get; init; }
}
