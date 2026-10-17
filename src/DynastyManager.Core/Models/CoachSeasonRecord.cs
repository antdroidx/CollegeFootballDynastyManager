namespace DynastyManager.Core.Models;

public sealed record CoachSeasonRecord
{
    public Guid StaffId { get; init; }
    public required string CoachName { get; init; }
    public int SeasonYear { get; init; }
    public required string TeamName { get; init; }
    public StaffRole Role { get; init; }
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
}

public sealed record CoachCareerSummary
{
    public Guid StaffId { get; init; }
    public required string CoachName { get; init; }
    public IReadOnlyList<CoachSeasonRecord> Seasons { get; init; } = Array.Empty<CoachSeasonRecord>();
    public int HeadCoachSeasons => Seasons.Count(season => season.Role == StaffRole.HeadCoach);
    public int HeadCoachWins => Seasons.Where(season => season.Role == StaffRole.HeadCoach).Sum(season => season.Wins);
    public int HeadCoachLosses => Seasons.Where(season => season.Role == StaffRole.HeadCoach).Sum(season => season.Losses);
    public double HeadCoachWinPercentage => HeadCoachWins + HeadCoachLosses == 0 ? 0 : (double)HeadCoachWins / (HeadCoachWins + HeadCoachLosses);
    public int ConferenceTitles => Seasons.Count(season => season.Role == StaffRole.HeadCoach && season.ConferenceChampion);
    public int PlayoffAppearances => Seasons.Count(season => season.Role == StaffRole.HeadCoach && season.PlayoffParticipant);
    public int NationalTitles => Seasons.Count(season => season.Role == StaffRole.HeadCoach && season.NationalChampion);
    public int BowlWins => Seasons.Count(season => season.Role == StaffRole.HeadCoach && season.WonBowl == true);
    public int BowlLosses => Seasons.Count(season => season.Role == StaffRole.HeadCoach && season.WonBowl == false);
    public int StaffSeasons => Seasons.Count - HeadCoachSeasons;
}
