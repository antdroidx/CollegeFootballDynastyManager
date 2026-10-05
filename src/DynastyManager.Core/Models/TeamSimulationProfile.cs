namespace DynastyManager.Core.Models;

public sealed record TeamSimulationProfile
{
    public required string TeamName { get; init; }
    public double PassOffenseRating { get; init; }
    public double RushOffenseRating { get; init; }
    public double PassDefenseRating { get; init; }
    public double RushDefenseRating { get; init; }
    public double SpecialTeamsRating { get; init; }
    public int RosterSize { get; init; }

    public double OffenseRating =>
        (PassOffenseRating + RushOffenseRating) / 2.0;

    public double DefenseRating =>
        (PassDefenseRating + RushDefenseRating) / 2.0;
}
