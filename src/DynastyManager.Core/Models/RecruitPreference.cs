namespace DynastyManager.Core.Models;

public sealed record RecruitPreference
{
    public RecruitPitchType Type { get; init; }
    public int Importance { get; init; }
}
