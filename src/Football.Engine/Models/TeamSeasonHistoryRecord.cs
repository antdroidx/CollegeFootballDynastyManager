namespace DynastyManager.Core.Models;

public sealed record TeamSeasonHistoryRecord
{
    public int SeasonYear { get; init; }
    public required string TeamName { get; init; }
    public int Wins { get; init; }
    public int Losses { get; init; }
    public int ConferenceWins { get; init; }
    public int ConferenceLosses { get; init; }
    public int FinalRanking { get; init; }
    public bool ConferenceChampion { get; init; }
    public bool PlayoffParticipant { get; init; }
    public bool NationalChampion { get; init; }
    public string? BowlName { get; init; }
    public bool? WonBowl { get; init; }
    public int RecruitingCommitments { get; init; }
    public double RecruitingAverageRating { get; init; }
}
