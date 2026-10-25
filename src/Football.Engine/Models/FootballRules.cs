namespace DynastyManager.Core.Models;

public sealed record FootballRules
{
    public static FootballRules College { get; } = new();
    public static FootballRules Professional { get; } = new() { TimedOvertime = true, HashMarkFraction = 70.75 / 160, StopClockOnFirstDown = false, ExtraPointDistance = 33, KickoffTouchbackYardLine = 35 };
    public bool TimedOvertime { get; init; }
    public bool Postseason { get; init; }
    public int QuarterSeconds { get; init; } = 900;
    public int RegularOvertimeSeconds { get; init; } = 600;
    public int PlayoffOvertimeSeconds { get; init; } = 900;
    public double FieldWidthYards { get; init; } = 160d / 3;
    public double HashMarkFraction { get; init; } = .30;
    public bool StopClockOnFirstDown { get; init; } = true;
    public int KickoffTouchbackYardLine { get; init; } = 25;
    public int ExtraPointDistance { get; init; } = 20;
}
