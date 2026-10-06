using System.Security.Cryptography;
using System.Text;
using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class StaffMarketService
{
    private static readonly StaffRole[] Roles = Enum.GetValues<StaffRole>();

    private static readonly string[] FirstNames =
    {
        "Aaron", "Adrian", "Brandon", "Bryce", "Cedric", "Damien", "Elliot",
        "Gavin", "Isaac", "Jalen", "Jerome", "Keith", "Landon", "Malcolm",
        "Nathan", "Quentin", "Rashad", "Sean", "Terrence", "Victor", "Xavier"
    };

    private static readonly string[] LastNames =
    {
        "Andrews", "Banks", "Carson", "Douglas", "Ellis", "Fields", "Goodwin",
        "Harris", "Ingram", "Jefferson", "Keller", "Lawson", "Mitchell",
        "Norris", "Owens", "Peterson", "Quinn", "Robinson", "Stewart",
        "Underwood", "West"
    };

    public static DynastyState EnsureMarket(
        DynastyState state,
        IReadOnlyDictionary<string, Team> teamsByName)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(teamsByName);

        if (state.StaffMarketSeasonYear == state.SeasonYear &&
            state.StaffMarketCandidates.Count > 0)
        {
            return state;
        }

        var candidates = new List<StaffMarketCandidate>();

        foreach (var role in Roles)
        {
            for (var index = 0; index < 18; index++)
            {
                candidates.Add(CreateExternalCandidate(
                    state,
                    role,
                    index,
                    teamsByName));
            }
        }

        foreach (var staff in state.Staff)
        {
            if (!teamsByName.TryGetValue(staff.TeamName, out var sourceTeam))
                continue;

            var sourcePrestige =
                ProgramPrestigeService.GetCurrentPrestige(state, sourceTeam);

            if ((staff.Role is StaffRole.OffensiveCoordinator or
                 StaffRole.DefensiveCoordinator) &&
                staff.OverallRating >= 68)
            {
                candidates.Add(new StaffMarketCandidate
                {
                    CandidateId = CandidateId(
                        state.DynastyId,
                        state.SeasonYear,
                        staff.StaffId,
                        StaffRole.HeadCoach),
                    SeasonYear = state.SeasonYear,
                    Profile = staff,
                    TargetRole = StaffRole.HeadCoach,
                    Origin = StaffCandidateOrigin.ActiveStaff,
                    SourceTeamName = staff.TeamName,
                    SourceRole = staff.Role,
                    GrowthPotential = staff.GrowthPotential,
                    MinimumProgramPrestige =
                        Math.Clamp(sourcePrestige - 14, 35, 88),
                    StyleLabel = "Rising Coordinator"
                });
            }

            if (staff.Role == StaffRole.HeadCoach &&
                staff.OverallRating >= 76 &&
                sourcePrestige <= 90)
            {
                candidates.Add(new StaffMarketCandidate
                {
                    CandidateId = CandidateId(
                        state.DynastyId,
                        state.SeasonYear,
                        staff.StaffId,
                        StaffRole.HeadCoach),
                    SeasonYear = state.SeasonYear,
                    Profile = staff,
                    TargetRole = StaffRole.HeadCoach,
                    Origin = StaffCandidateOrigin.ActiveStaff,
                    SourceTeamName = staff.TeamName,
                    SourceRole = staff.Role,
                    GrowthPotential = staff.GrowthPotential,
                    MinimumProgramPrestige =
                        Math.Clamp(sourcePrestige + 2, 40, 92),
                    StyleLabel = "Established Head Coach"
                });
            }

            if (staff.Role != StaffRole.HeadCoach &&
                staff.OverallRating >= 80)
            {
                candidates.Add(new StaffMarketCandidate
                {
                    CandidateId = CandidateId(
                        state.DynastyId,
                        state.SeasonYear,
                        staff.StaffId,
                        staff.Role),
                    SeasonYear = state.SeasonYear,
                    Profile = staff,
                    TargetRole = staff.Role,
                    Origin = StaffCandidateOrigin.ActiveStaff,
                    SourceTeamName = staff.TeamName,
                    SourceRole = staff.Role,
                    GrowthPotential = staff.GrowthPotential,
                    MinimumProgramPrestige =
                        Math.Clamp(sourcePrestige + 1, 40, 92),
                    StyleLabel = GetStyleLabel(staff, staff.Role)
                });
            }
        }

        return state with
        {
            StaffMarketCandidates = candidates
                .GroupBy(item => item.CandidateId)
                .Select(group => group.First())
                .ToArray(),
            StaffMarketSeasonYear = state.SeasonYear
        };
    }

    public static DynastyState RunCpuCarousel(
        DynastyState state,
        IReadOnlyDictionary<string, Team> teamsByName)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(teamsByName);

        if (state.StaffMarketProcessedSeasonYear == state.SeasonYear)
            return state;

        state = EnsureMarket(state, teamsByName);

        var staff = state.Staff.ToList();
        var candidates = state.StaffMarketCandidates.ToList();
        var history = state.StaffMovementHistory.ToList();
        var vacancies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var team in teamsByName.Values
                     .Where(team => !team.Name.Equals(
                         state.UserTeamName,
                         StringComparison.OrdinalIgnoreCase))
                     .OrderByDescending(team =>
                         ProgramPrestigeService.GetCurrentPrestige(state, team)))
        {
            var season = GetLatestSeason(state, team.Name);
            var expectedWins = GetExpectedWins(state, team);
            var winDelta = (season?.Wins ?? expectedWins) - expectedWins;

            var teamStaff = staff
                .Where(item => item.TeamName.Equals(
                    team.Name,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();

            foreach (var member in teamStaff)
            {
                if (!ShouldCpuFire(
                        state,
                        member,
                        team,
                        season,
                        winDelta))
                {
                    if (member.ContractYearsRemaining <= 0)
                    {
                        ReplaceStaffInList(
                            staff,
                            member with
                            {
                                ContractYearsRemaining =
                                    member.Role == StaffRole.HeadCoach ? 4 : 3
                            });
                    }
                    continue;
                }

                ReleaseToMarket(
                    state,
                    member,
                    "Performance and program expectations triggered a staff change.",
                    StaffMovementType.Fired,
                    null,
                    staff,
                    candidates,
                    history,
                    teamsByName);
                vacancies.Add(VacancyKey(team.Name, member.Role));

                if (member.Role == StaffRole.HeadCoach)
                {
                    foreach (var coordinatorRole in new[]
                             {
                                 StaffRole.OffensiveCoordinator,
                                 StaffRole.DefensiveCoordinator,
                                 StaffRole.SpecialTeamsCoordinator
                             })
                    {
                        var coordinator = staff.FirstOrDefault(item =>
                            item.TeamName.Equals(
                                team.Name,
                                StringComparison.OrdinalIgnoreCase) &&
                            item.Role == coordinatorRole);
                        if (coordinator is null)
                            continue;

                        var turnoverRoll = SimulationSeed.Create(
                            state.DynastyId,
                            state.SeasonYear,
                            (int)coordinatorRole,
                            team.Name,
                            $"new-hc-turnover-{member.StaffId:N}") % 100;
                        var threshold = coordinatorRole ==
                            StaffRole.SpecialTeamsCoordinator
                                ? 25
                                : 45;

                        if (turnoverRoll >= threshold)
                            continue;

                        ReleaseToMarket(
                            state,
                            coordinator,
                            "Coordinator departed during the head-coaching change.",
                            StaffMovementType.Fired,
                            null,
                            staff,
                            candidates,
                            history,
                            teamsByName);
                        vacancies.Add(VacancyKey(
                            team.Name,
                            coordinatorRole));
                    }
                }
            }
        }

        FillCpuVacancies(
            state,
            teamsByName,
            staff,
            candidates,
            history,
            vacancies);

        return state with
        {
            Staff = staff,
            StaffMarketCandidates = candidates,
            StaffMovementHistory = history,
            StaffMarketProcessedSeasonYear = state.SeasonYear
        };
    }

    public static IReadOnlyList<StaffMarketCandidate> GetCandidatesForTeam(
        DynastyState state,
        Team team,
        StaffRole role,
        int maximum = 8)
    {
        if (role == StaffRole.HeadCoach &&
            team.Name.Equals(
                state.UserTeamName,
                StringComparison.OrdinalIgnoreCase))
        {
            return Array.Empty<StaffMarketCandidate>();
        }

        var prestige =
            ProgramPrestigeService.GetCurrentPrestige(state, team);

        var available = state.StaffMarketCandidates
            .Where(item =>
                item.IsAvailable &&
                item.TargetRole == role &&
                !(item.SourceTeamName?.Equals(
                    team.Name,
                    StringComparison.OrdinalIgnoreCase) == true &&
                  item.SourceRole == role))
            .Select(item => new
            {
                Candidate = item,
                Fit = GetCandidateFitScore(state, team, item)
            })
            .OrderByDescending(item =>
                prestige + 8 >= item.Candidate.MinimumProgramPrestige)
            .ThenByDescending(item => item.Fit)
            .ThenByDescending(item => item.Candidate.Profile.Reputation)
            .Take(Math.Clamp(maximum, 1, 12))
            .Select(item => item.Candidate)
            .ToArray();

        return available;
    }

    public static int GetProjectedOverall(
        StaffMarketCandidate candidate) =>
        GetProjectedOverall(candidate.Profile, candidate.TargetRole);

    private static int GetProjectedOverall(
        StaffMember profile,
        StaffRole role) =>
        role switch
        {
            StaffRole.HeadCoach =>
                Average(
                    profile.Leadership,
                    profile.Recruiting,
                    profile.PlayerDevelopment,
                    profile.GameManagement),
            StaffRole.OffensiveCoordinator or
                StaffRole.DefensiveCoordinator =>
                Average(
                    profile.Scheme,
                    profile.PlayerDevelopment,
                    profile.Recruiting,
                    profile.GameManagement),
            StaffRole.SpecialTeamsCoordinator =>
                Average(
                    profile.SpecialTeams,
                    profile.PlayerDevelopment,
                    profile.GameManagement),
            StaffRole.MedicalTrainingDirector =>
                Average(
                    profile.Medical,
                    profile.Leadership,
                    profile.PlayerDevelopment),
            StaffRole.StrengthConditioningDirector =>
                Average(
                    profile.Conditioning,
                    profile.PlayerDevelopment,
                    profile.Leadership),
            StaffRole.ChiefScout =>
                Average(
                    profile.TalentEvaluation,
                    profile.PotentialEvaluation,
                    profile.RegionalKnowledge,
                    profile.StaffManagement),
            _ => profile.OverallRating
        };

    public static int GetCandidateFitScore(
        DynastyState state,
        Team team,
        StaffMarketCandidate candidate)
    {
        var prestige =
            ProgramPrestigeService.GetCurrentPrestige(state, team);
        var profile = candidate.Profile;

        var roleFit = candidate.TargetRole switch
        {
            StaffRole.HeadCoach =>
                Average(
                    profile.Leadership,
                    profile.GameManagement,
                    profile.Recruiting,
                    profile.PlayerDevelopment),
            StaffRole.OffensiveCoordinator or
                StaffRole.DefensiveCoordinator =>
                Average(
                    profile.Scheme,
                    profile.PlayerDevelopment,
                    profile.Recruiting),
            StaffRole.SpecialTeamsCoordinator =>
                Average(
                    profile.SpecialTeams,
                    profile.GameManagement,
                    profile.PlayerDevelopment),
            StaffRole.MedicalTrainingDirector =>
                Average(profile.Medical, profile.Leadership),
            StaffRole.StrengthConditioningDirector =>
                Average(profile.Conditioning, profile.PlayerDevelopment),
            StaffRole.ChiefScout =>
                Average(
                    profile.TalentEvaluation,
                    profile.PotentialEvaluation,
                    profile.RegionalKnowledge,
                    profile.StaffManagement),
            _ => profile.OverallRating
        };

        var prestigeFit =
            Math.Clamp(prestige - candidate.MinimumProgramPrestige + 12, 0, 24);
        var growthBonus = candidate.GrowthPotential * 3;
        var internalBonus =
            candidate.SourceTeamName?.Equals(
                team.Name,
                StringComparison.OrdinalIgnoreCase) == true
                ? 7
                : 0;

        return Math.Clamp(
            (int)Math.Round(
                roleFit * .72 +
                profile.Reputation * .12 +
                prestigeFit * .35 +
                growthBonus +
                internalBonus),
            35,
            99);
    }

    public static DynastyState HireUserCandidate(
        DynastyState state,
        Team userTeam,
        Guid candidateId,
        IReadOnlyDictionary<string, Team> teamsByName)
    {
        state = EnsureMarket(state, teamsByName);

        var candidate = state.StaffMarketCandidates.FirstOrDefault(item =>
            item.CandidateId == candidateId &&
            item.IsAvailable);

        if (candidate is null ||
            candidate.TargetRole == StaffRole.HeadCoach)
        {
            return state;
        }

        var staff = state.Staff.ToList();
        var candidates = state.StaffMarketCandidates.ToList();
        var history = state.StaffMovementHistory.ToList();
        var vacancies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var current = staff.FirstOrDefault(item =>
            item.TeamName.Equals(
                userTeam.Name,
                StringComparison.OrdinalIgnoreCase) &&
            item.Role == candidate.TargetRole);

        if (current is not null &&
            current.StaffId != candidate.Profile.StaffId)
        {
            ReleaseToMarket(
                state,
                current,
                "User program replaced this staff member.",
                StaffMovementType.Fired,
                null,
                staff,
                candidates,
                history,
                teamsByName);
        }

        if (candidate.Origin == StaffCandidateOrigin.ActiveStaff &&
            !string.IsNullOrWhiteSpace(candidate.SourceTeamName) &&
            candidate.SourceRole is not null)
        {
            var sourceMember = staff.FirstOrDefault(item =>
                item.StaffId == candidate.Profile.StaffId &&
                item.TeamName.Equals(
                    candidate.SourceTeamName,
                    StringComparison.OrdinalIgnoreCase) &&
                item.Role == candidate.SourceRole.Value);

            if (sourceMember is not null)
            {
                staff.Remove(sourceMember);

                if (!candidate.SourceTeamName.Equals(
                        userTeam.Name,
                        StringComparison.OrdinalIgnoreCase))
                {
                    vacancies.Add(VacancyKey(
                        candidate.SourceTeamName,
                        candidate.SourceRole.Value));
                }
            }
        }

        var hired = candidate.Profile with
        {
            TeamName = userTeam.Name,
            Role = candidate.TargetRole,
            TenureYears = 0,
            ContractYearsRemaining =
                candidate.TargetRole == StaffRole.HeadCoach ? 5 : 3
        };

        staff.RemoveAll(item =>
            item.TeamName.Equals(
                userTeam.Name,
                StringComparison.OrdinalIgnoreCase) &&
            item.Role == candidate.TargetRole);
        staff.Add(hired);

        MarkCandidateUnavailable(candidates, candidate.Profile.StaffId);

        var movementType =
            candidate.SourceTeamName?.Equals(
                userTeam.Name,
                StringComparison.OrdinalIgnoreCase) == true &&
            (candidate.SourceRole is
                StaffRole.OffensiveCoordinator or
                StaffRole.DefensiveCoordinator) &&
            candidate.TargetRole == StaffRole.HeadCoach
                ? StaffMovementType.Promoted
                : candidate.Origin == StaffCandidateOrigin.ActiveStaff
                    ? StaffMovementType.Poached
                    : StaffMovementType.Hired;

        history.Add(new StaffMovementRecord
        {
            SeasonYear = state.SeasonYear,
            StaffId = hired.StaffId,
            FullName = hired.FullName,
            Role = hired.Role,
            MovementType = movementType,
            FromTeamName = candidate.SourceTeamName,
            ToTeamName = userTeam.Name,
            OverallRating = hired.OverallRating,
            Reason = movementType == StaffMovementType.Promoted
                ? "Promoted internally to head coach."
                : "Selected from the offseason staff market."
        });

        FillCpuVacancies(
            state,
            teamsByName,
            staff,
            candidates,
            history,
            vacancies);

        return state with
        {
            Staff = staff,
            StaffMarketCandidates = candidates,
            StaffMovementHistory = history
        };
    }

    public static DynastyState HandleUserHeadCoachMove(
        DynastyState state,
        string oldTeamName,
        string newTeamName,
        IReadOnlyDictionary<string, Team> teamsByName)
    {
        if (oldTeamName.Equals(
                newTeamName,
                StringComparison.OrdinalIgnoreCase))
        {
            return state;
        }

        state = EnsureMarket(state, teamsByName);

        var staff = state.Staff.ToList();
        var candidates =
            state.StaffMarketCandidates.ToList();
        var history =
            state.StaffMovementHistory.ToList();
        var vacancies =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        var displaced = staff.FirstOrDefault(item =>
            item.TeamName.Equals(
                newTeamName,
                StringComparison.OrdinalIgnoreCase) &&
            item.Role == StaffRole.HeadCoach);

        if (displaced is not null)
        {
            ReleaseToMarket(
                state,
                displaced,
                "Program hired the user as head coach.",
                StaffMovementType.Fired,
                null,
                staff,
                candidates,
                history,
                teamsByName);
        }

        if (teamsByName.ContainsKey(oldTeamName))
        {
            vacancies.Add(VacancyKey(
                oldTeamName,
                StaffRole.HeadCoach));
        }

        if (state.UserHeadCoach is not null)
        {
            history.Add(new StaffMovementRecord
            {
                SeasonYear = state.SeasonYear,
                StaffId =
                    state.UserHeadCoach.StaffId,
                FullName = "YOU",
                Role = StaffRole.HeadCoach,
                MovementType =
                    StaffMovementType.Hired,
                FromTeamName = oldTeamName,
                ToTeamName = newTeamName,
                OverallRating =
                    state.UserHeadCoach.OverallRating,
                Reason =
                    "User accepted a head-coaching job offer."
            });
        }

        FillCpuVacancies(
            state,
            teamsByName,
            staff,
            candidates,
            history,
            vacancies);

        return state with
        {
            Staff = staff,
            StaffMarketCandidates = candidates,
            StaffMovementHistory = history
        };
    }

    public static string GetHeadCoachStatus(
        DynastyState state,
        Team team)
    {
        if (team.Name.Equals(
                state.UserTeamName,
                StringComparison.OrdinalIgnoreCase))
        {
            if (state.UserCoachIsFired)
                return "Fired";

            return UserCoachCareerService.GetJobSecurityLabel(
                state.UserCoachJobSecurity);
        }

        var coach = StaffManagementService.GetStaff(
            state,
            team.Name,
            StaffRole.HeadCoach);
        if (coach is null)
            return "Vacant";

        var season = GetLatestSeason(state, team.Name);
        if (season is null)
            return "New Staff";

        var expected = GetExpectedWins(state, team);
        var delta = season.Wins - expected;

        if (delta <= -4)
            return "Hot Seat";
        if (delta <= -2 || coach.ContractYearsRemaining <= 1)
            return "Under Review";
        if (delta >= 3)
            return "Secure";
        return "Stable";
    }

    private static void FillCpuVacancies(
        DynastyState state,
        IReadOnlyDictionary<string, Team> teamsByName,
        List<StaffMember> staff,
        List<StaffMarketCandidate> candidates,
        List<StaffMovementRecord> history,
        HashSet<string> vacancies)
    {
        var safety = 0;

        while (vacancies.Count > 0 && safety++ < 1500)
        {
            var vacancy = vacancies
                .Select(ParseVacancy)
                .Where(item =>
                    teamsByName.ContainsKey(item.TeamName) &&
                    !item.TeamName.Equals(
                        state.UserTeamName,
                        StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item =>
                    ProgramPrestigeService.GetCurrentPrestige(
                        state,
                        teamsByName[item.TeamName]))
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(vacancy.TeamName))
                break;

            vacancies.Remove(VacancyKey(
                vacancy.TeamName,
                vacancy.Role));

            var team = teamsByName[vacancy.TeamName];
            var targetPrestige =
                ProgramPrestigeService.GetCurrentPrestige(state, team);

            var choices = candidates
                .Where(item =>
                    item.IsAvailable &&
                    item.TargetRole == vacancy.Role &&
                    item.SourceTeamName?.Equals(
                        state.UserTeamName,
                        StringComparison.OrdinalIgnoreCase) != true)
                .Where(item =>
                    item.MinimumProgramPrestige <= targetPrestige + 8)
                .Where(item =>
                    IsActiveMoveReasonable(
                        state,
                        teamsByName,
                        item,
                        team))
                .Select(item => new
                {
                    Candidate = item,
                    Score =
                        GetCandidateFitScore(state, team, item) * 10 +
                        SimulationSeed.Create(
                            state.DynastyId,
                            state.SeasonYear,
                            (int)vacancy.Role,
                            vacancy.TeamName,
                            $"cpu-market-{item.CandidateId:N}") % 31
                })
                .OrderByDescending(item => item.Score)
                .ToArray();

            var selected = choices.FirstOrDefault()?.Candidate;
            if (selected is null)
            {
                selected = CreateExternalCandidate(
                    state,
                    vacancy.Role,
                    1000 + safety,
                    teamsByName);
                candidates.Add(selected);
            }

            if (selected.Origin == StaffCandidateOrigin.ActiveStaff &&
                !string.IsNullOrWhiteSpace(selected.SourceTeamName) &&
                selected.SourceRole is not null)
            {
                var sourceMember = staff.FirstOrDefault(item =>
                    item.StaffId == selected.Profile.StaffId &&
                    item.TeamName.Equals(
                        selected.SourceTeamName,
                        StringComparison.OrdinalIgnoreCase) &&
                    item.Role == selected.SourceRole.Value);

                if (sourceMember is null)
                {
                    MarkCandidateUnavailable(
                        candidates,
                        selected.Profile.StaffId);
                    vacancies.Add(VacancyKey(
                        vacancy.TeamName,
                        vacancy.Role));
                    continue;
                }

                staff.Remove(sourceMember);

                if (!selected.SourceTeamName.Equals(
                        vacancy.TeamName,
                        StringComparison.OrdinalIgnoreCase) &&
                    !selected.SourceTeamName.Equals(
                        state.UserTeamName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    vacancies.Add(VacancyKey(
                        selected.SourceTeamName,
                        selected.SourceRole.Value));
                }
            }

            var hired = selected.Profile with
            {
                TeamName = vacancy.TeamName,
                Role = vacancy.Role,
                TenureYears = 0,
                ContractYearsRemaining =
                    vacancy.Role == StaffRole.HeadCoach ? 5 : 3
            };

            staff.RemoveAll(item =>
                item.TeamName.Equals(
                    vacancy.TeamName,
                    StringComparison.OrdinalIgnoreCase) &&
                item.Role == vacancy.Role);
            staff.Add(hired);
            MarkCandidateUnavailable(
                candidates,
                selected.Profile.StaffId);

            var movementType =
                selected.SourceTeamName?.Equals(
                    vacancy.TeamName,
                    StringComparison.OrdinalIgnoreCase) == true &&
                (selected.SourceRole is
                    StaffRole.OffensiveCoordinator or
                    StaffRole.DefensiveCoordinator) &&
                vacancy.Role == StaffRole.HeadCoach
                    ? StaffMovementType.Promoted
                    : selected.Origin == StaffCandidateOrigin.ActiveStaff
                        ? StaffMovementType.Poached
                        : StaffMovementType.Hired;

            history.Add(new StaffMovementRecord
            {
                SeasonYear = state.SeasonYear,
                StaffId = hired.StaffId,
                FullName = hired.FullName,
                Role = hired.Role,
                MovementType = movementType,
                FromTeamName = selected.SourceTeamName,
                ToTeamName = vacancy.TeamName,
                OverallRating = hired.OverallRating,
                Reason = movementType switch
                {
                    StaffMovementType.Promoted =>
                        "Promoted from coordinator to head coach.",
                    StaffMovementType.Poached =>
                        "Hired away from another program.",
                    _ => "Hired from the shared staff market."
                }
            });
        }
    }

    private static bool ShouldCpuFire(
        DynastyState state,
        StaffMember member,
        Team team,
        TeamSeasonHistoryRecord? season,
        int winDelta)
    {
        if (season is null)
            return false;

        if (member.OverallRating < 48)
            return true;

        if (member.Role == StaffRole.HeadCoach)
        {
            if (winDelta <= -4)
                return true;

            if (member.ContractYearsRemaining <= 0 &&
                winDelta <= 0)
            {
                return true;
            }

            if (winDelta <= -2 &&
                member.ContractYearsRemaining <= 1)
            {
                var roll = SimulationSeed.Create(
                    state.DynastyId,
                    state.SeasonYear,
                    member.OverallRating,
                    team.Name,
                    $"hc-fire-{member.StaffId:N}") % 100;
                return roll < 65;
            }

            return false;
        }

        var threshold = member.Role switch
        {
            StaffRole.OffensiveCoordinator or
                StaffRole.DefensiveCoordinator => 64,
            StaffRole.SpecialTeamsCoordinator => 60,
            StaffRole.MedicalTrainingDirector => 60,
            StaffRole.StrengthConditioningDirector => 60,
            StaffRole.ChiefScout => 62,
            _ => 60
        };

        if (member.ContractYearsRemaining <= 0 &&
            member.OverallRating < threshold + 7)
        {
            return true;
        }

        if (winDelta <= -3 &&
            member.OverallRating < threshold)
        {
            var roll = SimulationSeed.Create(
                state.DynastyId,
                state.SeasonYear,
                member.OverallRating,
                team.Name,
                $"staff-fire-{member.Role}-{member.StaffId:N}") % 100;
            return roll < 45;
        }

        return false;
    }

    private static void ReleaseToMarket(
        DynastyState state,
        StaffMember member,
        string reason,
        StaffMovementType movementType,
        string? destination,
        List<StaffMember> staff,
        List<StaffMarketCandidate> candidates,
        List<StaffMovementRecord> history,
        IReadOnlyDictionary<string, Team> teamsByName)
    {
        staff.Remove(member);
        candidates.RemoveAll(item =>
            item.Profile.StaffId == member.StaffId);

        var sourcePrestige =
            teamsByName.TryGetValue(member.TeamName, out var sourceTeam)
                ? ProgramPrestigeService.GetCurrentPrestige(
                    state,
                    sourceTeam)
                : 55;

        candidates.Add(new StaffMarketCandidate
        {
            CandidateId = CandidateId(
                state.DynastyId,
                state.SeasonYear,
                member.StaffId,
                member.Role),
            SeasonYear = state.SeasonYear,
            Profile = member with { TeamName = "Free Agent" },
            TargetRole = member.Role,
            Origin = StaffCandidateOrigin.FiredStaff,
            SourceTeamName = member.TeamName,
            SourceRole = member.Role,
            GrowthPotential = member.GrowthPotential,
            MinimumProgramPrestige =
                Math.Clamp(sourcePrestige - 12, 35, 85),
            StyleLabel = GetStyleLabel(member, member.Role)
        });

        if ((member.Role is
             StaffRole.OffensiveCoordinator or
             StaffRole.DefensiveCoordinator) &&
            member.OverallRating >= 68)
        {
            candidates.Add(new StaffMarketCandidate
            {
                CandidateId = CandidateId(
                    state.DynastyId,
                    state.SeasonYear,
                    member.StaffId,
                    StaffRole.HeadCoach),
                SeasonYear = state.SeasonYear,
                Profile = member with { TeamName = "Free Agent" },
                TargetRole = StaffRole.HeadCoach,
                Origin = StaffCandidateOrigin.FiredStaff,
                SourceTeamName = member.TeamName,
                SourceRole = member.Role,
                GrowthPotential = member.GrowthPotential,
                MinimumProgramPrestige =
                    Math.Clamp(sourcePrestige - 16, 35, 82),
                StyleLabel = "Coordinator HC Candidate"
            });
        }

        history.Add(new StaffMovementRecord
        {
            SeasonYear = state.SeasonYear,
            StaffId = member.StaffId,
            FullName = member.FullName,
            Role = member.Role,
            MovementType = movementType,
            FromTeamName = member.TeamName,
            ToTeamName = destination,
            OverallRating = member.OverallRating,
            Reason = reason
        });
    }

    private static bool IsActiveMoveReasonable(
        DynastyState state,
        IReadOnlyDictionary<string, Team> teamsByName,
        StaffMarketCandidate candidate,
        Team destination)
    {
        if (candidate.Origin != StaffCandidateOrigin.ActiveStaff ||
            string.IsNullOrWhiteSpace(candidate.SourceTeamName) ||
            !teamsByName.TryGetValue(
                candidate.SourceTeamName,
                out var source))
        {
            return true;
        }

        if (candidate.SourceTeamName.Equals(
                destination.Name,
                StringComparison.OrdinalIgnoreCase))
        {
            return candidate.TargetRole == StaffRole.HeadCoach &&
                   (candidate.SourceRole is
                       StaffRole.OffensiveCoordinator or
                       StaffRole.DefensiveCoordinator);
        }

        var sourcePrestige =
            ProgramPrestigeService.GetCurrentPrestige(state, source);
        var destinationPrestige =
            ProgramPrestigeService.GetCurrentPrestige(state, destination);

        if (candidate.TargetRole == StaffRole.HeadCoach &&
            (candidate.SourceRole is
                StaffRole.OffensiveCoordinator or
                StaffRole.DefensiveCoordinator))
        {
            return destinationPrestige >= sourcePrestige - 10;
        }

        return destinationPrestige >= sourcePrestige + 3;
    }

    private static StaffMarketCandidate CreateExternalCandidate(
        DynastyState state,
        StaffRole role,
        int index,
        IReadOnlyDictionary<string, Team> teamsByName)
    {
        var seed = SimulationSeed.Create(
            state.DynastyId,
            state.SeasonYear,
            index + (int)role * 100,
            role.ToString(),
            "staff-market");
        var quality = Math.Clamp(52 + seed % 40, 48, 94);
        var age = 31 + (seed / 7) % 35;
        var growthPotential = Math.Clamp(
            5 - Math.Max(0, age - 42) / 8 +
            (seed / 13) % 2,
            1,
            5);

        int Attr(string key, int adjustment = 0) =>
            Math.Clamp(
                quality + adjustment +
                SimulationSeed.Create(
                    state.DynastyId,
                    state.SeasonYear,
                    index,
                    role.ToString(),
                    $"market-{key}") % 15 - 7,
                35,
                99);

        var first = FirstNames[seed % FirstNames.Length];
        var last = LastNames[(seed / 19) % LastNames.Length];
        var staffId = StableId(
            state.DynastyId,
            $"market-{state.SeasonYear}-{role}-{index}");

        var profile = new StaffMember
        {
            StaffId = staffId,
            FullName = $"{first} {last}",
            TeamName = "Free Agent",
            Role = role,
            Reputation = Attr("rep"),
            Leadership = Attr(
                "lead",
                role == StaffRole.HeadCoach ? 6 : 0),
            Recruiting = Attr(
                "recruit",
                role is StaffRole.HeadCoach or
                    StaffRole.OffensiveCoordinator or
                    StaffRole.DefensiveCoordinator
                    ? 4
                    : 0),
            PlayerDevelopment = Attr(
                "develop",
                role is StaffRole.OffensiveCoordinator or
                    StaffRole.DefensiveCoordinator or
                    StaffRole.StrengthConditioningDirector
                    ? 5
                    : 0),
            GameManagement = Attr(
                "game",
                role == StaffRole.HeadCoach ? 5 : 0),
            Scheme = Attr(
                "scheme",
                role is StaffRole.OffensiveCoordinator or
                    StaffRole.DefensiveCoordinator
                    ? 7
                    : 0),
            SpecialTeams = Attr(
                "special",
                role == StaffRole.SpecialTeamsCoordinator ? 9 : -5),
            Medical = Attr(
                "medical",
                role == StaffRole.MedicalTrainingDirector ? 10 : -5),
            Conditioning = Attr(
                "conditioning",
                role == StaffRole.StrengthConditioningDirector ? 10 : -5),
            TalentEvaluation = Attr(
                "talent",
                role == StaffRole.ChiefScout ? 9 : -4),
            PotentialEvaluation = Attr(
                "potential",
                role == StaffRole.ChiefScout ? 9 : -4),
            RegionalKnowledge = Attr(
                "regional",
                role == StaffRole.ChiefScout ? 8 : -4),
            StaffManagement = Attr(
                "management",
                role == StaffRole.ChiefScout ? 8 : 0),
            Age = age,
            CareerYears = Math.Max(0, age - 29 - seed % 7),
            GrowthPotential = growthPotential,
            ContractYearsRemaining = role == StaffRole.HeadCoach ? 5 : 3
        };

        return new StaffMarketCandidate
        {
            CandidateId = CandidateId(
                state.DynastyId,
                state.SeasonYear,
                staffId,
                role),
            SeasonYear = state.SeasonYear,
            Profile = profile,
            TargetRole = role,
            Origin = StaffCandidateOrigin.FreeAgent,
            GrowthPotential = growthPotential,
            MinimumProgramPrestige =
                Math.Clamp(35 + Math.Max(0, quality - 55), 35, 82),
            StyleLabel = GetStyleLabel(profile, role)
        };
    }

    private static string GetStyleLabel(
        StaffMember staff,
        StaffRole role) =>
        role switch
        {
            StaffRole.HeadCoach when staff.Recruiting >=
                staff.PlayerDevelopment &&
                staff.Recruiting >= staff.GameManagement =>
                "Program Recruiter",
            StaffRole.HeadCoach when staff.PlayerDevelopment >=
                staff.GameManagement =>
                "Player Developer",
            StaffRole.HeadCoach => "Game Manager",
            StaffRole.OffensiveCoordinator =>
                staff.Recruiting >= staff.PlayerDevelopment
                    ? "Recruiting OC"
                    : "Offensive Developer",
            StaffRole.DefensiveCoordinator =>
                staff.Recruiting >= staff.PlayerDevelopment
                    ? "Recruiting DC"
                    : "Defensive Developer",
            StaffRole.SpecialTeamsCoordinator =>
                "Special Teams Specialist",
            StaffRole.MedicalTrainingDirector =>
                "Recovery Specialist",
            StaffRole.StrengthConditioningDirector =>
                "Performance Developer",
            StaffRole.ChiefScout when staff.StaffManagement >=
                staff.TalentEvaluation =>
                "Scouting Director",
            StaffRole.ChiefScout when staff.PotentialEvaluation >=
                staff.TalentEvaluation =>
                "Projection Specialist",
            StaffRole.ChiefScout => "Talent Evaluator",
            _ => "Balanced"
        };

    private static TeamSeasonHistoryRecord? GetLatestSeason(
        DynastyState state,
        string teamName) =>
        state.TeamSeasonHistory
            .Where(item => item.TeamName.Equals(
                teamName,
                StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.SeasonYear)
            .FirstOrDefault();

    private static int GetExpectedWins(
        DynastyState state,
        Team team)
    {
        var seasonExpectation = state.ProgramPrestigeHistory
            .Where(item =>
                item.SeasonYear == state.SeasonYear &&
                item.TeamName.Equals(
                    team.Name,
                    StringComparison.OrdinalIgnoreCase))
            .Select(item => (int?)item.StartingPrestige)
            .FirstOrDefault();

        var prestige = seasonExpectation ??
            ProgramPrestigeService.GetCurrentPrestige(state, team);

        return Math.Clamp(
            4 + (prestige - 50) / 7,
            3,
            11);
    }

    private static void ReplaceStaffInList(
        List<StaffMember> staff,
        StaffMember replacement)
    {
        var index = staff.FindIndex(item =>
            item.StaffId == replacement.StaffId);
        if (index >= 0)
            staff[index] = replacement;
    }

    private static void MarkCandidateUnavailable(
        List<StaffMarketCandidate> candidates,
        Guid staffId)
    {
        for (var index = 0; index < candidates.Count; index++)
        {
            if (candidates[index].Profile.StaffId == staffId)
            {
                candidates[index] =
                    candidates[index] with { IsAvailable = false };
            }
        }
    }

    private static int Average(params int[] values) =>
        values.Length == 0
            ? 0
            : (int)Math.Round(values.Average());

    private static string VacancyKey(
        string teamName,
        StaffRole role) =>
        $"{teamName}\u001f{role}";

    private static (string TeamName, StaffRole Role) ParseVacancy(
        string key)
    {
        var parts = key.Split('\u001f');
        return (
            parts[0],
            Enum.Parse<StaffRole>(parts[1]));
    }

    private static Guid CandidateId(
        Guid dynastyId,
        int seasonYear,
        Guid staffId,
        StaffRole role) =>
        StableId(
            dynastyId,
            $"candidate-{seasonYear}-{staffId:N}-{role}");

    private static Guid StableId(
        Guid dynastyId,
        string purpose) =>
        new(MD5.HashData(
            Encoding.UTF8.GetBytes(
                $"{dynastyId:N}|{purpose}")));
}
