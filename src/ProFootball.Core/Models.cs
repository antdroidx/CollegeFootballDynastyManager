using DynastyManager.Core.Models;
namespace ProFootball.Core;

public sealed record ProTeam(string Id, string City, string Nickname, string Abbreviation, string Conference, string Division, int SeedRank, string Primary, string Secondary)
{ public string Name => City + " " + Nickname; public string? LogoFile { get; init; } }
public sealed record ProLeague(int Version, string Name, int ActiveRosterLimit, int PracticeSquadLimit, List<ProTeam> Teams);
public enum ProPhase { Preseason, RegularSeason, Playoffs, Offseason }
public sealed record ProPlayer(DynastyPlayer Football, int Age, int Experience, bool PracticeSquad)
{
    public bool Reserve { get; init; }
    public bool Retired { get; init; }
    public string RatingSource { get; init; } = "Generated";
    public string ExternalId { get; init; } = "";
    public string RosterStatus { get; init; } = "";
    public int Morale { get; init; } = 60;
}
public sealed record ProGame(ScheduledGame Football, ProPhase Phase, int Round = 0);
public sealed record SeasonArchive(int Year, string Champion, List<ProGame> Games);
public sealed record ProFranchise
{
    public int SchemaVersion { get; init; } = 2;
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = "My franchise";
    public required ProLeague League { get; init; }
    public required string UserTeam { get; init; }
    public int Year { get; init; } = 2026;
    public int Week { get; init; } = 1;
    public ProPhase Phase { get; init; } = ProPhase.Preseason;
    public List<ProPlayer> Players { get; init; } = [];
    public List<ProGame> Games { get; init; } = [];
    public List<SeasonArchive> History { get; init; } = [];
    public long SalaryCap { get; init; } = 301_200_000;
    public string DataSnapshot { get; init; } = "Legacy fictional franchise";
    public List<ProCoach> Coaches { get; init; } = [];
    public List<CoachCandidate> CoachCandidates { get; init; } = [];
    public List<ProContract> Contracts { get; init; } = [];
    public List<DeadCapCharge> DeadCap { get; init; } = [];
    public List<FreeAgentBid> Bids { get; init; } = [];
    public List<ProDraftPick> Picks { get; init; } = [];
    public List<ProPlayer> Prospects { get; init; } = [];
    public List<DraftSelection> DraftHistory { get; init; } = [];
    public List<ScoutReport> Scouting { get; init; } = [];
    public List<Guid> DraftBoard { get; init; } = [];
    public List<WaiverEntry> Waivers { get; init; } = [];
    public List<string> Transactions { get; init; } = [];
    public List<string> TaggedTeams { get; init; } = [];
    public List<OwnerExpectation> Owners { get; init; } = [];
    public OffseasonStep OffseasonStep { get; init; }
    public int DraftIndex { get; init; }
    public string? Champion { get; init; }
}
public sealed record ProStanding(ProTeam Team, int Wins, int Losses, int Ties, double Percentage, double DivisionPercentage, double ConferencePercentage, double OpponentPercentage, int Differential);

public enum OffseasonStep { Retention, FreeAgency, Draft, RosterCuts }
public sealed record ProCoach(string TeamId, StaffRole Role, string Name, string Title, string Source)
{ public bool Vacant { get; init; } public int Rating { get; init; } = 70; public int Development { get; init; } = 65; }
public sealed record NflSnapshot(int Version, int Season, int Week, string RetrievedUtc, int MaddenMatched, int Estimated, List<ProPlayer> Players, List<ProCoach> Coaches) { public List<CoachCandidate> Candidates { get; init; } = []; }
public sealed record ContractYear(int Year, long Salary, long Guaranteed);
public sealed record ProContract(Guid PlayerId, string Team, int SignedYear, int BonusYears, long SigningBonus, List<ContractYear> Years)
{ public bool Active { get; init; } = true; public bool Tagged { get; init; } public bool Estimated { get; init; } = true; }
public sealed record DeadCapCharge(string Team, int Year, Guid PlayerId, long Amount);
public sealed record FreeAgentBid(Guid PlayerId, string Team, int Years, long AnnualSalary, long Bonus, int Priority);
public sealed record ProDraftPick(Guid Id, int Year, int Round, int Slot, string OriginalTeam, string Owner);
public sealed record DraftSelection(int Year, int Round, int Slot, string Team, ProPlayer Player);
public sealed record ScoutReport(Guid PlayerId, string Team, int Year, int Grade, int Confidence);
public sealed record WaiverEntry(Guid PlayerId, int ExpiresAfterWeek, List<string> Claims);
public sealed record OwnerExpectation(string Team, int TargetWins, int Security, bool Fired = false);

public sealed record CoachCandidate(string Name, string SourceTeam, string Title, string Source);
