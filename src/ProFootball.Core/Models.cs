using DynastyManager.Core.Models;
namespace ProFootball.Core;

public sealed record ProTeam(string Id, string City, string Nickname, string Abbreviation, string Conference, string Division, int SeedRank, string Primary, string Secondary)
{ public string Name => City + " " + Nickname; public string? LogoFile { get; init; } }
public sealed record ProLeague(int Version, string Name, int ActiveRosterLimit, int PracticeSquadLimit, List<ProTeam> Teams);
public enum ProPhase { Preseason, RegularSeason, Playoffs, Offseason }
public sealed record ProPlayer(DynastyPlayer Football, int Age, int Experience, bool PracticeSquad)
{
    public int? PreviousSeasonOverall { get; init; }
    public int? CreditedSeasons {get;init;}
    public int? RookieYear { get; init; }
    public int? DraftOverall { get; init; }
    public bool Reserve { get; init; }
    public bool Retired { get; init; }
    public string? AppearanceOrigin {get;init;}
    public string RatingSource { get; init; } = "Generated";
    public string ExternalId { get; init; } = "";
    public string RosterStatus { get; init; } = "";
    public int Morale { get; init; } = 60;
    public int ContractConcernWeeks { get; init; }
    public bool TradeRequested { get; init; }
}
public sealed record ProGame(ScheduledGame Football, ProPhase Phase, int Round = 0);
public sealed record SeasonArchive(int Year, string Champion, List<ProGame> Games);
public sealed record ProFranchise
{
    public int SchemaVersion { get; init; } = 2;
    public List<ProJune1Designation> June1Designations {get;init;}=[];
    public int RatingSystemVersion { get; init; }
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = "My franchise";
    public required ProLeague League { get; init; }
    public required string UserTeam { get; init; }
    public int Year { get; init; } = 2026;
    public int Week { get; init; } = 1;
    public int TransactionTick { get; init; }
    public ProPhase Phase { get; init; } = ProPhase.Preseason;
    public List<ProPlayer> Players { get; init; } = [];
    public List<ProGame> Games { get; init; } = [];
    public List<SeasonArchive> History { get; init; } = [];
    public List<ProSeasonHonors> Honors { get; init; } = [];
    public List<ProRookieSeasonUsage> RookieParticipation { get; init; } = [];
    public List<ProSalarySample> SalarySamples { get; init; } = [];
    public List<ProOptionTender> OptionTenders { get; init; } = [];
    public List<ProOptionDecision> RookieOptionDecisions { get; init; } = [];
    public bool AutoManageRookieOptions { get; init; }
    public List<ProFinanceYear> FinancialHistory { get; init; } = [];
    public int CapRulesVersion {get;init;}
    public List<ProTeamCapAllocation> TeamCapAllocations {get;init;}=[];
    public List<ProRolloverElection> RolloverElections {get;init;}=[];
    public int CarryoverPercentage { get; init; } = 100;
    public long SalaryCap { get; init; } = 301_200_000;
    public string DataSnapshot { get; init; } = "Legacy fictional franchise";
    public List<TeamGamePlan> GamePlans { get; init; } = [];
    public ProUserCareer? Career { get; init; }
    public List<ProJobOffer> JobOffers { get; init; } = [];
    public List<ProCareerSeason> CoachingHistory { get; init; } = [];
    public List<ProGmDecision> GmDecisions { get; init; } = [];
    public List<ProCoach> Coaches { get; init; } = [];
    public List<CoachCandidate> CoachCandidates { get; init; } = [];
    public List<ProContract> Contracts { get; init; } = [];
    public List<DeadCapCharge> DeadCap { get; init; } = [];
    public List<FreeAgentBid> Bids { get; init; } = [];
    public List<ProTradeRecord> TradeHistory { get; init; } = [];
    public List<Guid> TradeBlock { get; init; } = [];
    public List<ProCpuTradeOffer> TradeOffers {get;init;}=[];
    public int LastTradeMarketTick {get;init;}=-1;
    public List<ProWeeklyTalk> WeeklyTalks { get; init; } = [];
    public List<ProMoraleEffect> MoraleEffects { get; init; } = [];
    public int ContractMoraleVersion { get; init; }
    public int LastMoraleTick { get; init; } = -1;
    public List<ProDraftPick> Picks { get; init; } = [];
    public List<ProPlayer> Prospects { get; init; } = [];
    public List<DraftSelection> DraftHistory { get; init; } = [];
    public List<ScoutReport> Scouting { get; init; } = [];
    public List<Guid> DraftBoard { get; init; } = [];
    public List<WaiverEntry> Waivers { get; init; } = [];
    public List<ProNewsStory> News { get; init; } = [];
    public List<string> ReadNewsIds { get; init; } = [];
    public List<ProTransactionEntry> TransactionLog { get; init; } = [];
    public List<string> Transactions { get; init; } = [];
    public List<string> TaggedTeams { get; init; } = [];
    public List<OwnerExpectation> Owners { get; init; } = [];
    public OffseasonStep OffseasonStep { get; init; }
    public List<PendingRookie> UnsignedRookies { get; init; } = [];
    public bool AutoNegotiateExpiring { get; init; }
    public List<Guid> LetExpire { get; init; } = [];
    public List<string> ReadNotificationIds { get; init; } = [];
    public int DraftIndex { get; init; }
    public string? Champion { get; init; }
}
public sealed record ProStanding(ProTeam Team, int Wins, int Losses, int Ties, double Percentage, double DivisionPercentage, double ConferencePercentage, double OpponentPercentage, int Differential);

public enum OffseasonStep { Retention, FreeAgency, Draft, RosterCuts }
public sealed record ProCoach(string TeamId, StaffRole Role, string Name, string Title, string Source)
{ public bool Vacant { get; init; } public int Rating { get; init; } = 70; public int Development { get; init; } = 65; }
public sealed record NflSnapshot(int Version, int Season, int Week, string RetrievedUtc, int MaddenMatched, int Estimated, List<ProPlayer> Players, List<ProCoach> Coaches) { public List<CoachCandidate> Candidates { get; init; } = []; }
public sealed record ContractYear(int Year, long Salary, long Guaranteed)
{ public long? ProratedBonus { get; init; } public long OtherCompensation { get; init; } public bool IsVoid { get; init; } public long? OtherCash { get; init; } public long? SourceGuaranteed { get; init; } public long GuaranteedOtherCash {get;init;} }
public sealed record ProContract(Guid PlayerId, string Team, int SignedYear, int BonusYears, long SigningBonus, List<ContractYear> Years)
{ public int? SalaryAssignmentYear {get;init;} public double SalaryAssignmentFraction {get;init;} public Guid? ReleaseId {get;init;} public List<ProBonusPayment>? BonusPayments {get;init;} public List<ProCashVesting>? CashVesting {get;init;} public int? OriginalRookieYear { get; init; } public int AccountingVersion { get; init; } public long? ReportedAnnualValue { get; init; } public Dictionary<int,long>? BonusCash {get;init;} public int? TerminatedYear { get; init; } public long EarnedCash { get; init; } public bool FutureTermsEstimated { get; init; } public int LastChangedYear { get; init; } public string? Source { get; init; } public bool Active { get; init; } = true; public bool Tagged { get; init; } public bool Estimated { get; init; } = true; }
public sealed record DeadCapCharge(string Team, int Year, Guid PlayerId, long Amount) { public long CashAmount {get;init;} public Guid? ReleaseId {get;init;} public bool June1Hold {get;init;} public bool RetainedContract {get;init;} public int? FundingYear {get;init;} public bool RetainedBonus {get;init;} }
public sealed record FreeAgentBid(Guid PlayerId, string Team, int Years, long AnnualSalary, long Bonus, int Priority);
public sealed record ProDraftPick(Guid Id, int Year, int Round, int Slot, string OriginalTeam, string Owner);
public sealed record DraftSelection(int Year, int Round, int Slot, string Team, ProPlayer Player);
public sealed record ScoutReport(Guid PlayerId, string Team, int Year, int Grade, int Confidence);
public sealed record WaiverEntry(Guid PlayerId, int ExpiresAfterWeek, List<string> Claims)
{ public Guid? ReleaseId {get;init;} public List<ProContract>? OriginalContracts {get;init;} public double ReleasedFraction {get;init;} public int ReleasedYear {get;init;} }
public sealed record OwnerExpectation(string Team, int TargetWins, int Security, bool Fired = false);

public sealed record CoachCandidate(string Name, string SourceTeam, string Title, string Source);

public sealed record ProUserCareer(string Name, StaffRole Role, bool FiringEnabled)
{ public int Reputation { get; init; } = 50; public int Security { get; init; } = 65; public bool Unemployed { get; init; } public int LastReviewedYear { get; init; } }
public sealed record ProJobOffer(Guid Id, string Team, StaffRole Role, int Year, string Reason);
public sealed record ProCareerSeason(int Year, string Team, StaffRole Role, int Wins, int Losses, int Reputation, int Security, bool Fired);
public sealed record ProGmDecision(Guid Id, int Year, int Week, string Action, bool Approved, string Reason);

public sealed record ProTradeRecord(Guid Id, int Year, int Week, string From, string To, List<Guid> GivenPlayers, List<Guid> ReceivedPlayers, List<ProDraftPick> GivenPicks, List<ProDraftPick> ReceivedPicks) { public int TransactionTick {get;init;}=-1; }

public sealed record ProNewsStory(string Id, int Year, int Week, string Category, string Headline, string Body, string? Team = null, string? GameId = null);

public sealed record ProWeeklyTalk(int Year,int Week,ProPhase Phase,int Tick,DynastyManager.Core.Simulation.CoachTalkRecord Talk);
public sealed record ProMoraleEffect(Guid PlayerId,string Reason,int RemainingChange,int NextDecayTick);

public sealed record ProAward(string Title,Guid PlayerId,string Name,string Team,double Score);
public sealed record ProAllStarSelection(ProPlayer Player,string Conference);
public sealed record ProSeasonHonors(int Year,List<ProAward> Awards,List<ProAllStarSelection> Selections)
{public ScheduledGame? Showcase {get;init;} public List<ProAllProSelection> AllPros {get;init;}=[];}
public sealed record ProAllProSelection(ProPlayer Player,int Team);

public sealed record PendingRookie(Guid PlayerId, string Team, int Year, int Round, long Salary, long Bonus);

public sealed record ProJune1Designation(string Team,int Year,Guid PlayerId,Guid ReleaseId);

public sealed record ProBonusPayment(int Year,long Amount,int AllocationYears,bool GuaranteedAtSigning) {public bool PaidAtExecution {get;init;} }
public sealed record ProCashVesting(int PaymentYear,long GuaranteedCash,int AfterSeason);
