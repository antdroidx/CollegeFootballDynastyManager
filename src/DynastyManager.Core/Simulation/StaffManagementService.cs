using System.Security.Cryptography;
using System.Text;
using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class StaffManagementService
{
    private static readonly StaffRole[] RequiredRoles = Enum.GetValues<StaffRole>();
    private static readonly string[] FirstNames =
    {
        "Alex", "Jordan", "Marcus", "Derrick", "Chris", "Taylor", "Cameron",
        "Andre", "Ryan", "Morgan", "Casey", "Devin", "Jamie", "Darius"
    };
    private static readonly string[] LastNames =
    {
        "Bennett", "Cole", "Reed", "Navarro", "Hayes", "Foster", "Brooks",
        "Sullivan", "Price", "Turner", "Morris", "Grant", "Parker", "Davis"
    };

    public static DynastyState EnsureLeagueStaff(
        DynastyState state,
        IEnumerable<Team> teams)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(teams);

        var staff = state.Staff.ToList();
        foreach (var team in teams.OrderBy(team => team.Name, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var role in RequiredRoles)
            {
                if (staff.Any(item =>
                        item.TeamName.Equals(team.Name, StringComparison.OrdinalIgnoreCase) &&
                        item.Role == role))
                {
                    continue;
                }

                staff.Add(CreateStaff(state, team, role, 0));
            }
        }

        return state with { Staff = staff };
    }

    public static StaffMember? GetStaff(
        DynastyState state,
        string teamName,
        StaffRole role) =>
        state.Staff.FirstOrDefault(item =>
            item.TeamName.Equals(teamName, StringComparison.OrdinalIgnoreCase) &&
            item.Role == role);

    public static int GetRoleRating(
        DynastyState state,
        string teamName,
        StaffRole role,
        int fallback = 60) =>
        GetStaff(state, teamName, role)?.OverallRating ?? fallback;

    public static IReadOnlyList<StaffMember> GetTeamStaff(
        DynastyState state,
        string teamName) =>
        state.Staff
            .Where(item => item.TeamName.Equals(teamName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Role)
            .ToArray();

    public static StaffMember GenerateCandidate(
        DynastyState state,
        Team team,
        StaffRole role)
    {
        var current = GetStaff(state, team.Name, role);
        var salt = (current?.TenureYears ?? 0) + state.SeasonYear + 31;
        return CreateStaff(state, team, role, salt);
    }

    public static DynastyState Hire(
        DynastyState state,
        StaffMember candidate)
    {
        var staff = state.Staff
            .Where(item => !(item.TeamName.Equals(
                                candidate.TeamName,
                                StringComparison.OrdinalIgnoreCase) &&
                            item.Role == candidate.Role))
            .Append(candidate with
            {
                TenureYears = 0,
                ContractYearsRemaining = Math.Max(2, candidate.ContractYearsRemaining)
            })
            .ToArray();

        return state with { Staff = staff };
    }

    public static DynastyState AdvanceSeason(DynastyState state)
    {
        if (state.StaffLastAdvancedSeasonYear >= state.SeasonYear)
            return state;

        var staff = state.Staff.Select(item =>
        {
            var growthSeed = SimulationSeed.Create(
                state.DynastyId,
                state.SeasonYear,
                (int)item.Role,
                item.TeamName,
                $"staff-growth-{item.StaffId:N}");
            var growthChance = Math.Clamp(
                8 + item.GrowthPotential * 8 - Math.Max(0, item.Age - 58),
                3,
                48);
            var growth = growthSeed % 100 < growthChance ? 1 : 0;
            var decline = item.Age >= 64 && growthSeed % 100 < Math.Min(35, item.Age - 58)
                ? 1
                : 0;
            var netChange = growth - decline;

            return item with
            {
                Reputation = Clamp(item.Reputation + netChange),
                Leadership = Clamp(item.Leadership + netChange),
                Recruiting = Clamp(item.Recruiting + netChange),
                PlayerDevelopment = Clamp(item.PlayerDevelopment + netChange),
                GameManagement = Clamp(item.GameManagement + netChange),
                Scheme = Clamp(item.Scheme + netChange),
                SpecialTeams = Clamp(item.SpecialTeams + netChange),
                Medical = Clamp(item.Medical + netChange),
                Conditioning = Clamp(item.Conditioning + netChange),
                TalentEvaluation = Clamp(item.TalentEvaluation + netChange),
                PotentialEvaluation = Clamp(item.PotentialEvaluation + netChange),
                RegionalKnowledge = Clamp(item.RegionalKnowledge + netChange),
                StaffManagement = Clamp(item.StaffManagement + netChange),
                Age = item.Age + 1,
                CareerYears = item.CareerYears + 1,
                TenureYears = item.TenureYears + 1,
                ContractYearsRemaining = Math.Max(0, item.ContractYearsRemaining - 1)
            };
        }).ToArray();

        return state with
        {
            Staff = staff,
            StaffLastAdvancedSeasonYear = state.SeasonYear
        };
    }

    private static StaffMember CreateStaff(
        DynastyState state,
        Team team,
        StaffRole role,
        int salt)
    {
        var prestige = ProgramPrestigeService.GetCurrentPrestige(state, team);
        var seed = SimulationSeed.Create(
            state.DynastyId,
            state.SeasonYear,
            (int)role + salt,
            team.Name,
            "staff");
        var name = $"{FirstNames[seed % FirstNames.Length]} " +
                   $"{LastNames[(seed / 17) % LastNames.Length]}";
        var baseRating = Math.Clamp(42 + prestige / 2 + seed % 15, 45, 94);

        int Attribute(string key, int adjustment = 0)
        {
            var value = baseRating + adjustment +
                SimulationSeed.Create(
                    state.DynastyId,
                    state.SeasonYear,
                    salt + (int)role,
                    team.Name,
                    $"staff-{key}") % 13 - 6;
            return Clamp(value);
        }

        return new StaffMember
        {
            StaffId = StableId(state.DynastyId, $"{team.Name}|{role}|{salt}"),
            FullName = name,
            TeamName = team.Name,
            Role = role,
            Reputation = Attribute("rep"),
            Leadership = Attribute("lead", role == StaffRole.HeadCoach ? 5 : 0),
            Recruiting = Attribute("recruit", role is StaffRole.HeadCoach or StaffRole.OffensiveCoordinator or StaffRole.DefensiveCoordinator ? 3 : 0),
            PlayerDevelopment = Attribute("develop", role is StaffRole.StrengthConditioningDirector or StaffRole.OffensiveCoordinator or StaffRole.DefensiveCoordinator ? 4 : 0),
            GameManagement = Attribute("game", role == StaffRole.HeadCoach ? 5 : 0),
            Scheme = Attribute("scheme", role is StaffRole.OffensiveCoordinator or StaffRole.DefensiveCoordinator ? 6 : 0),
            SpecialTeams = Attribute("st", role == StaffRole.SpecialTeamsCoordinator ? 8 : -5),
            Medical = Attribute("medical", role == StaffRole.MedicalTrainingDirector ? 9 : -5),
            Conditioning = Attribute("conditioning", role == StaffRole.StrengthConditioningDirector ? 9 : -5),
            TalentEvaluation = Attribute("talent", role == StaffRole.ChiefScout ? 8 : -4),
            PotentialEvaluation = Attribute("potential", role == StaffRole.ChiefScout ? 8 : -4),
            RegionalKnowledge = Attribute("region", role == StaffRole.ChiefScout ? 7 : -4),
            StaffManagement = Attribute("management", role == StaffRole.ChiefScout ? 7 : 0),
            Age = 34 + seed % 27,
            CareerYears = seed % 18,
            GrowthPotential = Math.Clamp(
                5 - Math.Max(0, (34 + seed % 27) - 42) / 8 +
                SimulationSeed.Create(
                    state.DynastyId,
                    state.SeasonYear,
                    salt + (int)role,
                    team.Name,
                    "staff-growth-potential") % 2,
                1,
                5),
            ContractYearsRemaining = 2 + seed % 4
        };
    }

    private static int Clamp(int value) => Math.Clamp(value, 35, 99);

    private static Guid StableId(Guid dynastyId, string purpose) =>
        new(MD5.HashData(Encoding.UTF8.GetBytes($"{dynastyId:N}|{purpose}")));
}
