namespace DynastyManager.Core.Models;

public sealed record TeamSimulationProfile
{
    public required string TeamName { get; init; }
    public double PassOffenseRating { get; init; }
    public double RushOffenseRating { get; init; }
    public double PassDefenseRating { get; init; }
    public double RushDefenseRating { get; init; }
    public double SpecialTeamsRating { get; init; }
    public TeamGamePlan? GamePlan { get; init; }
    public int Morale { get; init; } = 50;
    public int RosterSize { get; init; }
    public int HeadCoachLeadership { get; init; } = 60;
    public int HeadCoachGameManagement { get; init; } = 60;
    public int OffensiveCoordinatorScheme { get; init; } = 60;
    public int OffensiveCoordinatorGameManagement { get; init; } = 60;
    public int DefensiveCoordinatorScheme { get; init; } = 60;
    public int DefensiveCoordinatorGameManagement { get; init; } = 60;

    public double OffenseRating =>
        (PassOffenseRating + RushOffenseRating) / 2.0;

    public double DefenseRating =>
        (PassDefenseRating + RushDefenseRating) / 2.0;
}
