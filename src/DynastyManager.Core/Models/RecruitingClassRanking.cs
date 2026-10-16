namespace DynastyManager.Core.Models;

public sealed record RecruitingClassRanking
{
    public int Rank { get; init; }
    public required string TeamName { get; init; }
    public int CommitCount { get; init; }
    public int HighSchoolCount { get; init; }
    public int TransferCount { get; init; }
    public double TeamScore { get; init; }
    public double AverageRecruitRating { get; init; }
    public double BestRecruitRating { get; init; }
    public double GaussianSigma { get; init; }
}
