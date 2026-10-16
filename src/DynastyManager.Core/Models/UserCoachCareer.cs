namespace DynastyManager.Core.Models;

public enum UserCoachJobOfferStatus
{
    Pending,
    Accepted,
    Declined,
    Expired
}

public sealed record UserCoachJobOffer
{
    public Guid OfferId { get; init; } = Guid.NewGuid();
    public int SeasonYear { get; init; }
    public required string TeamName { get; init; }
    public int ProgramPrestige { get; init; }
    public int ContractYears { get; init; } = 4;
    public int FitScore { get; init; }
    public required string Reason { get; init; }
    public UserCoachJobOfferStatus Status { get; init; } =
        UserCoachJobOfferStatus.Pending;
}

public sealed record UserCoachCareerRecord
{
    public int SeasonYear { get; init; }
    public required string TeamName { get; init; }
    public int Wins { get; init; }
    public int Losses { get; init; }
    public int ExpectedWins { get; init; }
    public int OverallBefore { get; init; }
    public int OverallAfter { get; init; }
    public int ReputationBefore { get; init; }
    public int ReputationAfter { get; init; }
    public int LeadershipChange { get; init; }
    public int RecruitingChange { get; init; }
    public int PlayerDevelopmentChange { get; init; }
    public int GameManagementChange { get; init; }
    public int JobSecurityBefore { get; init; }
    public int JobSecurityAfter { get; init; }
    public bool ConferenceChampion { get; init; }
    public bool PlayoffParticipant { get; init; }
    public bool NationalChampion { get; init; }
    public bool WasFired { get; init; }
    public required string Summary { get; init; }
}
