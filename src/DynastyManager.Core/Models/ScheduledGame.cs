namespace DynastyManager.Core.Models;

public sealed record ScheduledGame
{
    public required string GameId { get; init; }
    public int SeasonYear { get; init; }
    public int Week { get; init; }
    public required string HomeTeamName { get; init; }
    public required string AwayTeamName { get; init; }
    public ScheduledGameType GameType { get; init; } = ScheduledGameType.NonConference;
    public PostseasonRound PostseasonRound { get; init; } = PostseasonRound.None;
    public int PlayoffBracketSlot { get; init; }
    public int? HomeSeed { get; init; }
    public int? AwaySeed { get; init; }
    public bool IsNeutralSite { get; init; }
    public string? BowlName { get; init; }
    public int SimulationSeed { get; init; }
    public bool HasPlayed { get; init; }
    public int? HomeScore { get; init; }
    public int? AwayScore { get; init; }
    public GameTeamStats? HomeStats { get; init; }
    public GameTeamStats? AwayStats { get; init; }

    public bool InvolvesTeam(string teamName) =>
        HomeTeamName.Equals(teamName, StringComparison.OrdinalIgnoreCase) ||
        AwayTeamName.Equals(teamName, StringComparison.OrdinalIgnoreCase);

    public string? WinnerTeamName =>
        !HasPlayed || HomeScore is null || AwayScore is null
            ? null
            : HomeScore > AwayScore ? HomeTeamName : AwayTeamName;
}
