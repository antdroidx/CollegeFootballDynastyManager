namespace DynastyManager.Core.Models;

public enum GameDayMode { Simulate, Watch, Play }
public enum SnapOutcome
{
    Run, Completion, Incompletion, Sack, Turnover, Touchdown, FieldGoal, Punt, MissedFieldGoal,
    Interception, Fumble, Kickoff, Scramble, ExtraPoint, MissedExtraPoint, TwoPointConversion, FailedTwoPointConversion,
    Kneel, Spike, Penalty, ClockStoppage
}

public sealed record GameSnapEvent
{
    public int Sequence { get; init; }
    public int PossessionNumber { get; init; }
    public int Quarter { get; init; }
    public int ClockSeconds { get; init; }
    public required string OffenseTeamName { get; init; }
    public required string DefenseTeamName { get; init; }
    public required string OffensivePlayId { get; init; }
    public required string DefensivePlayId { get; init; }
    public PlayCallSource CallSource { get; init; }
    public int Down { get; init; }
    public int Distance { get; init; }
    public int StartYardLine { get; init; }
    public int BallEndYardLine { get; init; }
    public int KickLandingYardLine { get; init; }
    public int Yards { get; init; }
    public int AirYards { get; init; }
    public int YardsAfterCatch { get; init; }
    public int PreSnapSeconds { get; init; }
    public int LiveBallSeconds { get; init; }
    public int ReturnYards { get; init; }
    public double KickHangTimeSeconds { get; init; }
    public double NearestCoverageYardsAtCatch { get; init; }
    public bool IsTouchback { get; init; }
    public bool IsFairCatch { get; init; }
    public bool IsQuarterbackKeep { get; init; }
    public bool IsBootleg { get; init; }
    public bool IsQuarterbackScramble { get; init; }
    public bool IsDrop { get; init; }
    public double PassReleaseProgress { get; init; }
    public double TargetSeparation { get; init; }
    public string? Description { get; init; }
    public int Points { get; init; }
    public bool IsOnsideKick { get; init; }
    public bool KickingTeamRecovered { get; init; }
    public SnapOutcome Outcome { get; init; }
    public Guid? QuarterbackId { get; init; }
    public Guid? BallCarrierId { get; init; }
    public Guid? TargetId { get; init; }
    public Guid? PrimaryDefenderId { get; init; }
    public Guid? KickerId { get; init; }
    public Guid? ReturnerId { get; init; }
    public PlayCallSource DefensiveCallSource { get; init; }
    public IReadOnlyList<Guid> OffensePersonnel { get; init; } = Array.Empty<Guid>();
    public IReadOnlyList<Guid> DefensePersonnel { get; init; } = Array.Empty<Guid>();
}

public sealed class GameDaySession
{
    public required ScheduledGame Game { get; init; }
    public required TeamSimulationProfile HomeProfile { get; init; }
    public required TeamSimulationProfile AwayProfile { get; init; }
    public required IReadOnlyList<DynastyPlayer> Roster { get; init; }
    public IReadOnlyList<DynastyPlayer> HomeRoster { get; init; } = Array.Empty<DynastyPlayer>();
    public IReadOnlyList<DynastyPlayer> AwayRoster { get; init; } = Array.Empty<DynastyPlayer>();
    public IReadOnlyDictionary<Guid, DynastyPlayer> PlayersById { get; init; } =
        new Dictionary<Guid, DynastyPlayer>();
    public IReadOnlyDictionary<Position, IReadOnlyList<DynastyPlayer>> HomeDepthChart { get; init; } =
        new Dictionary<Position, IReadOnlyList<DynastyPlayer>>();
    public IReadOnlyDictionary<Position, IReadOnlyList<DynastyPlayer>> AwayDepthChart { get; init; } =
        new Dictionary<Position, IReadOnlyList<DynastyPlayer>>();
    public GameDayMode Mode { get; set; }
    public required Random Random { get; init; }
    public GameFatigueState HomeFatigue { get; } = new();
    public GameFatigueState AwayFatigue { get; } = new();
    public List<GameSnapEvent> Events { get; } = [];
    public string PossessionTeamName { get; set; } = string.Empty;
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public int Quarter { get; set; } = 1;
    public int ClockSeconds { get; set; } = 900;
    public int Down { get; set; } = 1;
    public int Distance { get; set; } = 10;
    public int YardLine { get; set; } = 25;
    public int PossessionNumber { get; set; } = 1;
    public bool KickoffPending { get; set; } = true;
    public string KickoffTeamName { get; set; } = string.Empty;
    public string? ControlledTeamName { get; set; }
    public int HomeTimeouts { get; set; } = 3;
    public int AwayTimeouts { get; set; } = 3;
    public bool ClockStopped { get; set; }
    public bool ClockRestartsOnReady { get; set; }
    public int PlayClockSeconds { get; set; } = 25;
    public int LastPreSnapSeconds { get; set; }
    public int TwoMinuteTimeoutHalf { get; set; }
    public bool IsComplete { get; set; }
    public bool TryPending { get; set; }
    public bool OnsideKickRequested { get; set; }
    public int OvertimeRound { get; set; }
    public int OvertimePossessions { get; set; }
    public string OvertimeFirstTeam { get; set; } = string.Empty;
}
