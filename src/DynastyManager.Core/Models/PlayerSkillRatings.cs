namespace DynastyManager.Core.Models;

/// <summary>Persisted football skills on the same 1–99 scale as core ratings.</summary>
public sealed record PlayerSkillRatings
{
    public int PassingPower { get; init; }
    public int PassingAccuracy { get; init; }
    public int Catching { get; init; }
    public int BreakTackle { get; init; }
    public int Tackling { get; init; }
    public int RunBlocking { get; init; }
    public int PassBlocking { get; init; }
    public int BallCarrying { get; init; }
    public int Acceleration { get; init; }
    public int Leadership { get; init; }
    public int Discipline { get; init; }
    public int Jumping { get; init; }
    public int KickPower { get; init; }
    public int KickAccuracy { get; init; }
}
