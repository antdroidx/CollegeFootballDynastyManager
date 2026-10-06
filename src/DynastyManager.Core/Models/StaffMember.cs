namespace DynastyManager.Core.Models;

public enum StaffRole
{
    HeadCoach,
    OffensiveCoordinator,
    DefensiveCoordinator,
    SpecialTeamsCoordinator,
    MedicalTrainingDirector,
    StrengthConditioningDirector,
    ChiefScout
}

public sealed record StaffMember
{
    public Guid StaffId { get; init; } = Guid.NewGuid();
    public required string FullName { get; init; }
    public required string TeamName { get; init; }
    public StaffRole Role { get; init; }
    public int Reputation { get; init; }
    public int Leadership { get; init; }
    public int Recruiting { get; init; }
    public int PlayerDevelopment { get; init; }
    public int GameManagement { get; init; }
    public int Scheme { get; init; }
    public int SpecialTeams { get; init; }
    public int Medical { get; init; }
    public int Conditioning { get; init; }
    public int TalentEvaluation { get; init; }
    public int PotentialEvaluation { get; init; }
    public int RegionalKnowledge { get; init; }
    public int StaffManagement { get; init; }
    public int Age { get; init; } = 45;
    public int CareerYears { get; init; }
    public int GrowthPotential { get; init; } = 3;
    public int TenureYears { get; init; }
    public int ContractYearsRemaining { get; init; } = 3;

    public int OverallRating => Role switch
    {
        StaffRole.HeadCoach => Average(Leadership, Recruiting, PlayerDevelopment, GameManagement),
        StaffRole.OffensiveCoordinator => Average(Scheme, PlayerDevelopment, Recruiting, GameManagement),
        StaffRole.DefensiveCoordinator => Average(Scheme, PlayerDevelopment, Recruiting, GameManagement),
        StaffRole.SpecialTeamsCoordinator => Average(SpecialTeams, PlayerDevelopment, GameManagement),
        StaffRole.MedicalTrainingDirector => Average(Medical, Leadership, PlayerDevelopment),
        StaffRole.StrengthConditioningDirector => Average(Conditioning, PlayerDevelopment, Leadership),
        StaffRole.ChiefScout => Average(TalentEvaluation, PotentialEvaluation, RegionalKnowledge, StaffManagement),
        _ => Reputation
    };

    private static int Average(params int[] values) =>
        values.Length == 0 ? 0 : (int)Math.Round(values.Average());
}
