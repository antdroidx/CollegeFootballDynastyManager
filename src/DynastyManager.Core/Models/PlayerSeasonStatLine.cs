namespace DynastyManager.Core.Models;

public sealed record PlayerSeasonStatLine
{
    public int SeasonYear { get; init; }
    public Guid PlayerId { get; init; }
    public required string PlayerName { get; init; }
    public required string TeamName { get; init; }
    public int? OriginalRecruitingStars { get; init; }
    public Position Position { get; init; }
    public int Games { get; init; }
    public int PassAttempts { get; init; }
    public int PassCompletions { get; init; }
    public int PassYards { get; init; }
    public int PassingTouchdowns { get; init; }
    public int InterceptionsThrown { get; init; }
    public int RushAttempts { get; init; }
    public int RushYards { get; init; }
    public int RushingTouchdowns { get; init; }
    public int RushingFumbles { get; init; }
    public int Rushing20Plus { get; init; }
    public int Receptions { get; init; }
    public int ReceivingYards { get; init; }
    public int ReceivingTouchdowns { get; init; }
    public int YardsAfterCatch { get; init; }
    public int Drops { get; init; }
    public int ReceivingFumbles { get; init; }
    public int Receiving20Plus { get; init; }
    public int Tackles { get; init; }
    public int TacklesForLoss { get; init; }
    public int Sacks { get; init; }
    public int PassesDefended { get; init; }
    public int Interceptions { get; init; }
    public int FumbleRecoveries { get; init; }
    public int ExtraPointsMade { get; init; }
    public int ExtraPointsAttempted { get; init; }
    public int FieldGoalsMade { get; init; }
    public int FieldGoalsAttempted { get; init; }
    public int Punts { get; init; }
    public int PuntYards { get; init; }
    public int LongPunt { get; init; }
    public int PuntsInside20 { get; init; }
    public int PuntsInside10 { get; init; }
    public int KickReturns { get; init; }
    public int KickReturnYards { get; init; }
    public int KickReturnTouchdowns { get; init; }
    public int PuntReturns { get; init; }
    public int PuntReturnYards { get; init; }
    public int PuntReturnTouchdowns { get; init; }
    public int Touchdowns { get; init; }
    public int Turnovers { get; init; }

    public int ScrimmageYards => PassYards + RushYards + ReceivingYards;
}
