using System.Security.Cryptography;
using System.Text;
using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class UserCoachCareerService
{
    public static DynastyState EvaluateCompletedSeason(
        DynastyState state,
        IReadOnlyDictionary<string, Team> teamsByName)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(teamsByName);

        state = StaffManagementService.EnsureUserHeadCoach(
            state,
            teamsByName.Values);

        if (state.UserHeadCoach is null ||
            !teamsByName.TryGetValue(state.UserTeamName, out var team))
        {
            return state;
        }

        if (state.UserCoachLastEvaluatedSeasonYear >= state.SeasonYear)
        {
            if (state.UserCoachIsFired &&
                !state.UserCoachJobOffers.Any(item =>
                    item.Status ==
                        UserCoachJobOfferStatus.Pending))
            {
                return GenerateJobOffers(
                    state,
                    teamsByName);
            }

            return state;
        }

        var season = state.TeamSeasonHistory
            .FirstOrDefault(item =>
                item.SeasonYear == state.SeasonYear &&
                item.TeamName.Equals(
                    state.UserTeamName,
                    StringComparison.OrdinalIgnoreCase));

        if (season is null)
            return state;

        var coach = state.UserHeadCoach;
        var startingPrestige = state.ProgramPrestigeHistory
            .Where(item =>
                item.SeasonYear == state.SeasonYear &&
                item.TeamName.Equals(
                    state.UserTeamName,
                    StringComparison.OrdinalIgnoreCase))
            .Select(item => (int?)item.StartingPrestige)
            .FirstOrDefault() ??
            ProgramPrestigeService.GetCurrentPrestige(state, team);

        var expectedWins = ExpectedWins(startingPrestige);
        var winDelta = season.Wins - expectedWins;
        var developmentAverage = state.PlayerDevelopmentHistory
            .Where(item =>
                item.SeasonYear == state.SeasonYear &&
                item.TeamName.Equals(
                    state.UserTeamName,
                    StringComparison.OrdinalIgnoreCase))
            .Select(item => (double?)item.OverallChange)
            .Average() ?? 0.0;

        var leadershipChange = Math.Clamp(
            (int)Math.Round(winDelta / 2.5) +
            (season.ConferenceChampion ? 1 : 0) +
            (season.NationalChampion ? 1 : 0),
            -2,
            2);

        var gameManagementChange = Math.Clamp(
            winDelta switch
            {
                >= 4 => 2,
                >= 2 => 1,
                <= -4 => -2,
                <= -2 => -1,
                _ => 0
            } +
            (season.PlayoffParticipant ? 1 : 0),
            -2,
            2);

        var recruitingChange = season.RecruitingAverageRating switch
        {
            >= 84 => 2,
            >= 77 => 1,
            > 0 and < 68 => -1,
            _ => 0
        };

        var playerDevelopmentChange = developmentAverage switch
        {
            >= 1.75 => 2,
            >= .50 => 1,
            < -.25 => -1,
            _ => 0
        };

        var reputationChange = Math.Clamp(
            (winDelta >= 3 ? 1 : 0) -
            (winDelta <= -3 ? 1 : 0) +
            (season.ConferenceChampion ? 1 : 0) +
            (season.PlayoffParticipant ? 1 : 0) +
            (season.NationalChampion ? 2 : 0),
            -2,
            3);

        var securityBefore = Math.Clamp(
            state.UserCoachJobSecurity <= 0
                ? 70
                : state.UserCoachJobSecurity,
            0,
            100);

        var securityChange =
            winDelta * 7 +
            (season.ConferenceChampion ? 8 : 0) +
            (season.PlayoffParticipant ? 10 : 0) +
            (season.NationalChampion ? 20 : 0) +
            (season.FinalRanking is > 0 and <= 25 ? 5 : 0) +
            (season.Wins <= 4 ? -8 : 0) +
            (season.Losses >= 9 ? -8 : 0);

        var securityAfter = Math.Clamp(
            securityBefore + securityChange,
            0,
            100);

        var nextContractYears = Math.Max(
            0,
            coach.ContractYearsRemaining - 1);

        var updatedCoach = coach with
        {
            Reputation = Clamp(coach.Reputation + reputationChange),
            Leadership = Clamp(coach.Leadership + leadershipChange),
            Recruiting = Clamp(coach.Recruiting + recruitingChange),
            PlayerDevelopment = Clamp(
                coach.PlayerDevelopment +
                playerDevelopmentChange),
            GameManagement = Clamp(
                coach.GameManagement +
                gameManagementChange),
            Age = coach.Age + 1,
            CareerYears = coach.CareerYears + 1,
            TenureYears = coach.TenureYears + 1,
            ContractYearsRemaining = nextContractYears
        };

        var fired =
            !season.NationalChampion &&
            (securityAfter <= 20 ||
             (nextContractYears == 0 &&
              securityAfter < 50));

        if (!fired &&
            updatedCoach.ContractYearsRemaining <= 1 &&
            securityAfter >= 58)
        {
            updatedCoach = updatedCoach with
            {
                ContractYearsRemaining =
                    securityAfter >= 80 ? 5 : 4
            };
        }

        var summary = BuildSummary(
            season,
            expectedWins,
            fired,
            securityAfter);

        var career = state.UserCoachCareerHistory
            .Append(new UserCoachCareerRecord
            {
                SeasonYear = state.SeasonYear,
                TeamName = state.UserTeamName,
                Wins = season.Wins,
                Losses = season.Losses,
                ExpectedWins = expectedWins,
                OverallBefore = coach.OverallRating,
                OverallAfter = updatedCoach.OverallRating,
                ReputationBefore = coach.Reputation,
                ReputationAfter = updatedCoach.Reputation,
                LeadershipChange = leadershipChange,
                RecruitingChange = recruitingChange,
                PlayerDevelopmentChange =
                    playerDevelopmentChange,
                GameManagementChange =
                    gameManagementChange,
                JobSecurityBefore = securityBefore,
                JobSecurityAfter = securityAfter,
                ConferenceChampion =
                    season.ConferenceChampion,
                PlayoffParticipant =
                    season.PlayoffParticipant,
                NationalChampion =
                    season.NationalChampion,
                WasFired = fired,
                Summary = summary
            })
            .ToArray();

        var evaluated = state with
        {
            UserHeadCoach = updatedCoach,
            UserCoachJobSecurity = securityAfter,
            UserCoachIsFired = fired,
            UserCoachFiredFromTeamName =
                fired ? state.UserTeamName : null,
            UserCoachLastEvaluatedSeasonYear =
                state.SeasonYear,
            UserCoachCareerHistory = career
        };

        return GenerateJobOffers(
            evaluated,
            teamsByName,
            season,
            expectedWins);
    }

    public static DynastyState GenerateJobOffers(
        DynastyState state,
        IReadOnlyDictionary<string, Team> teamsByName,
        TeamSeasonHistoryRecord? completedSeason = null,
        int? expectedWins = null)
    {
        if (state.UserHeadCoach is null ||
            !teamsByName.TryGetValue(
                state.UserTeamName,
                out var currentTeam))
        {
            return state;
        }

        var coach = state.UserHeadCoach;
        var season = completedSeason ??
            state.TeamSeasonHistory
                .Where(item =>
                    item.TeamName.Equals(
                        state.UserTeamName,
                        StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item => item.SeasonYear)
                .FirstOrDefault();

        var expected = expectedWins ??
            ExpectedWins(
                ProgramPrestigeService.GetCurrentPrestige(
                    state,
                    currentTeam));
        var winDelta =
            (season?.Wins ?? expected) - expected;

        var currentPrestige =
            ProgramPrestigeService.GetCurrentPrestige(
                state,
                currentTeam);

        var performanceScore =
            winDelta * 7 +
            (season?.ConferenceChampion == true ? 10 : 0) +
            (season?.PlayoffParticipant == true ? 12 : 0) +
            (season?.NationalChampion == true ? 22 : 0) +
            (season?.FinalRanking is > 0 and <= 25 ? 5 : 0);

        var deservesOffers =
            state.UserCoachIsFired ||
            performanceScore >= 10 ||
            coach.Reputation >= 70 ||
            coach.OverallRating >= 70;

        var existing = state.UserCoachJobOffers
            .Select(item =>
                item.Status == UserCoachJobOfferStatus.Pending
                    ? item with
                    {
                        Status =
                            UserCoachJobOfferStatus.Expired
                    }
                    : item)
            .ToList();

        if (!deservesOffers)
        {
            return state with
            {
                UserCoachJobOffers = existing
            };
        }

        var candidates = teamsByName.Values
            .Where(team =>
                !team.Name.Equals(
                    state.UserTeamName,
                    StringComparison.OrdinalIgnoreCase))
            .Select(team =>
            {
                var prestige =
                    ProgramPrestigeService.GetCurrentPrestige(
                        state,
                        team);
                var teamSeason = state.TeamSeasonHistory
                    .FirstOrDefault(item =>
                        item.SeasonYear == state.SeasonYear &&
                        item.TeamName.Equals(
                            team.Name,
                            StringComparison.OrdinalIgnoreCase));
                var teamExpected = ExpectedWins(prestige);
                var opportunity =
                    teamSeason is null
                        ? 0
                        : Math.Max(
                            0,
                            teamExpected - teamSeason.Wins) * 4;

                var reputationGap =
                    coach.Reputation - prestige;
                var reachPenalty =
                    Math.Max(0, prestige -
                        (coach.Reputation + 12)) * 4;
                var prestigeMovement =
                    state.UserCoachIsFired
                        ? -Math.Max(
                            0,
                            prestige - currentPrestige) * 2
                        : Math.Min(
                            12,
                            Math.Max(
                                -8,
                                prestige - currentPrestige));

                var deterministic =
                    SimulationSeed.Create(
                        state.DynastyId,
                        state.SeasonYear,
                        prestige,
                        team.Name,
                        "user-coach-job-offer") % 13;

                var fit =
                    coach.OverallRating +
                    coach.Reputation / 2 +
                    performanceScore +
                    opportunity +
                    prestigeMovement +
                    reputationGap / 4 +
                    deterministic -
                    reachPenalty;

                return new
                {
                    Team = team,
                    Prestige = prestige,
                    Fit = fit,
                    Opportunity = opportunity
                };
            })
            .Where(item =>
                state.UserCoachIsFired
                    ? item.Prestige <=
                      Math.Max(
                          currentPrestige + 5,
                          coach.Reputation + 8)
                    : item.Prestige <=
                      coach.Reputation + 18)
            .OrderByDescending(item => item.Fit)
            .ThenByDescending(item => item.Prestige)
            .ThenBy(item => item.Team.Name,
                StringComparer.OrdinalIgnoreCase)
            .Take(state.UserCoachIsFired ? 5 : 4)
            .ToList();

        if (state.UserCoachIsFired &&
            candidates.Count < 3)
        {
            foreach (var fallback in teamsByName.Values
                         .Where(team =>
                             !team.Name.Equals(
                                 state.UserTeamName,
                                 StringComparison.OrdinalIgnoreCase))
                         .Select(team => new
                         {
                             Team = team,
                             Prestige =
                                 ProgramPrestigeService
                                     .GetCurrentPrestige(
                                         state,
                                         team),
                             Fit = 50,
                             Opportunity = 0
                         })
                         .OrderBy(item =>
                             Math.Abs(
                                 item.Prestige -
                                 currentPrestige))
                         .ThenBy(item => item.Team.Name,
                             StringComparer.OrdinalIgnoreCase))
            {
                if (candidates.Any(item =>
                        item.Team.Name.Equals(
                            fallback.Team.Name,
                            StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                candidates.Add(fallback);
                if (candidates.Count >= 3)
                    break;
            }
        }

        foreach (var candidate in candidates)
        {
            var direction =
                candidate.Prestige >= currentPrestige + 6
                    ? "A step up in program prestige."
                    : candidate.Prestige <=
                      currentPrestige - 8
                        ? "A rebuilding opportunity."
                        : "A comparable program opportunity.";

            existing.Add(new UserCoachJobOffer
            {
                OfferId = StableId(
                    state.DynastyId,
                    $"{state.SeasonYear}|{candidate.Team.Name}|job-offer"),
                SeasonYear = state.SeasonYear,
                TeamName = candidate.Team.Name,
                ProgramPrestige =
                    candidate.Prestige,
                ContractYears =
                    candidate.Fit >= 120 ? 5 :
                    candidate.Fit >= 95 ? 4 : 3,
                FitScore = Math.Clamp(
                    candidate.Fit,
                    0,
                    150),
                Reason = state.UserCoachIsFired
                    ? $"{direction} The program is willing to hire you after your dismissal."
                    : $"{direction} Your recent results raised your coaching profile."
            });
        }

        return state with
        {
            UserCoachJobOffers = existing
        };
    }

    public static DynastyState AcceptJobOffer(
        DynastyState state,
        Guid offerId,
        IReadOnlyDictionary<string, Team> teamsByName)
    {
        var offer = state.UserCoachJobOffers
            .FirstOrDefault(item =>
                item.OfferId == offerId &&
                item.Status ==
                    UserCoachJobOfferStatus.Pending);

        if (offer is null ||
            state.UserHeadCoach is null ||
            !teamsByName.ContainsKey(offer.TeamName))
        {
            return state;
        }

        var oldTeamName = state.UserTeamName;
        var offers = state.UserCoachJobOffers
            .Select(item =>
                item.OfferId == offerId
                    ? item with
                    {
                        Status =
                            UserCoachJobOfferStatus.Accepted
                    }
                    : item.Status ==
                      UserCoachJobOfferStatus.Pending
                        ? item with
                        {
                            Status =
                                UserCoachJobOfferStatus.Declined
                        }
                        : item)
            .ToArray();

        var moved = state with
        {
            UserTeamName = offer.TeamName,
            UserHeadCoach = state.UserHeadCoach with
            {
                TeamName = offer.TeamName,
                TenureYears = 0,
                ContractYearsRemaining =
                    offer.ContractYears
            },
            UserCoachJobSecurity = 65,
            UserCoachIsFired = false,
            UserCoachFiredFromTeamName = null,
            UserCoachJobOffers = offers,
            ScoutingStaff = Array.Empty<ScoutStaff>(),
            ScoutAssignments =
                Array.Empty<ScoutAssignment>(),
            ScoutingRecommendationReport = null,
            RecruitingInteractions =
                state.RecruitingInteractions
                    .Where(item =>
                        item.SeasonYear != state.SeasonYear)
                    .ToArray(),
            RecruitingPointsRemaining = 0,
            RecruitingPointsPhase = null,
            RecruitingPointsWeek = -1,
            RecruitingAssistanceSeasonYear = 0,
            RecruitingAssistanceWeek = -1
        };

        moved = StaffMarketService
            .HandleUserHeadCoachMove(
                moved,
                oldTeamName,
                offer.TeamName,
                teamsByName);

        moved = StaffManagementService.EnsureLeagueStaff(
            moved,
            teamsByName.Values,
            moved.StaffMarketSeasonYear ==
                moved.SeasonYear
                ? moved.UserTeamName
                : null);

        if (teamsByName.TryGetValue(
                moved.UserTeamName,
                out var newTeam))
        {
            moved = ScoutingDepartmentService
                .RebuildDepartment(
                    moved,
                    newTeam);
            moved = InteractiveRecruitingService
                .EnsurePhaseInitialized(
                    moved,
                    newTeam);
        }

        return moved;
    }

    public static DynastyState DeclineJobOffer(
        DynastyState state,
        Guid offerId)
    {
        var pending = state.UserCoachJobOffers
            .Where(item =>
                item.Status ==
                    UserCoachJobOfferStatus.Pending)
            .ToArray();

        if (state.UserCoachIsFired &&
            pending.Length <= 1 &&
            pending.Any(item => item.OfferId == offerId))
        {
            return state;
        }

        return state with
        {
            UserCoachJobOffers =
                state.UserCoachJobOffers
                    .Select(item =>
                        item.OfferId == offerId &&
                        item.Status ==
                            UserCoachJobOfferStatus.Pending
                            ? item with
                            {
                                Status =
                                    UserCoachJobOfferStatus.Declined
                            }
                            : item)
                    .ToArray()
        };
    }

    public static string GetJobSecurityLabel(int security) =>
        security switch
        {
            >= 85 => "Very Secure",
            >= 70 => "Secure",
            >= 50 => "Stable",
            >= 35 => "Under Review",
            >= 21 => "Hot Seat",
            _ => "Fired"
        };

    private static int ExpectedWins(int prestige) =>
        Math.Clamp(
            4 + (prestige - 50) / 7,
            3,
            11);

    private static string BuildSummary(
        TeamSeasonHistoryRecord season,
        int expectedWins,
        bool fired,
        int security) =>
        fired
            ? $"{season.Wins}-{season.Losses} against {expectedWins} expected wins. The program dismissed you."
            : $"{season.Wins}-{season.Losses} against {expectedWins} expected wins. Job security: {GetJobSecurityLabel(security)}.";

    private static int Clamp(int value) =>
        Math.Clamp(value, 40, 99);

    private static Guid StableId(
        Guid dynastyId,
        string purpose) =>
        new(MD5.HashData(
            Encoding.UTF8.GetBytes(
                $"{dynastyId:N}|{purpose}")));
}
