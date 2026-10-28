namespace DynastyManager.Core.Models;

public sealed record GameTeamStats
{
    public PlayEfficiency? Efficiency { get; init; }
    public int Possessions { get; init; }
    public int PassAttempts { get; init; }
    public int PassCompletions { get; init; }
    public int RushAttempts { get; init; }
    public int PassYards { get; init; }
    public int RushYards { get; init; }
    public int PassingTouchdowns { get; init; }
    public int RushingTouchdowns { get; init; }
    public int InterceptionsThrown { get; init; }
    public int FumblesLost { get; init; }
    public int ExplosivePasses20 { get; init; }
    public int ExplosiveRuns20 { get; init; }
    public int Drops { get; init; }
    public int YardsAfterCatch { get; init; }
    public int SacksAllowed { get; init; }
    public int FieldGoalsMade { get; init; }
    public int FieldGoalsAttempted { get; init; }
    public int ExtraPointsMade { get; init; }
    public int ExtraPointsAttempted { get; init; }
    public int Punts { get; init; }
    public int PuntYards { get; init; }
    public int LongPunt { get; init; }
    public int PuntsInside20 { get; init; }
    public int PuntsInside10 { get; init; }
    public int KickReturns { get; init; }
    public int KickReturnYards { get; init; }
    public int KickReturnTouchdowns { get; init; }
    public int PuntReturns { get; init; }
    public int PuntReturnYards { get; init; }
    public int PuntReturnTouchdowns { get; init; }
    public int Turnovers { get; init; }

    public int TotalYards => PassYards + RushYards;
}
