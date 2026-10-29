namespace DynastyManager.Core.Models;

/// <summary>Original NCAA appearance codes. Null fields retain procedural defaults.</summary>
public sealed record NcaaPlayerAppearance
{
    public string? SkinColorHex {get;init;}
    public int? SkinTone { get; init; }
    public int? HairColor { get; init; }
    public int? HairStyle { get; init; }
    public int? Face { get; init; }
    // App extension: 0 clean-shaven, 1 stubble, 2 goatee, 3 beard.
    public int? FacialHair { get; init; }
}
