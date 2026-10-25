namespace DynastyManager.Core.Models;

public sealed record ConferenceStanding
{
    public required string TeamName { get; init; }
    public required string ConferenceName { get; init; }
    public int ConferenceWins { get; init; }
    public int ConferenceLosses { get; init; }
    public int OverallWins { get; init; }
    public int OverallLosses { get; init; }
    public int ConferencePointsFor { get; init; }
    public int ConferencePointsAgainst { get; init; }

    public int ConferencePointDifferential =>
        ConferencePointsFor - ConferencePointsAgainst;
}
