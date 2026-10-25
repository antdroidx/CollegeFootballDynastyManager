using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class LeagueEvolutionService
{
    public static IReadOnlyDictionary<string, double> ConferencePower(DynastyState state) => state.LeagueTeams
        .GroupBy(t => t.ConferenceName).ToDictionary(g => g.Key, g => g.Average(t => (double)ProgramPrestigeService.GetCurrentPrestige(state, t)));

    public static DynastyState AdvanceOffseason(DynastyState state)
    {
        if (state.Phase != SeasonPhase.Preseason || state.LeagueMovementProcessedYear == state.SeasonYear) return state;
        var teams = state.LeagueTeams.ToList();
        var history = state.LeagueMovementHistory.ToList();
        var random = new Random(SimulationSeed.Create(state.DynastyId, state.SeasonYear, 0, "league", "evolution"));
        // Periodic moves, rather than annual wholesale churn. Exchange members to preserve scheduling capacity.
        if (state.LeagueRules.ConferenceRealignment && random.NextDouble() < .28)
        {
            var groups = teams.GroupBy(t => t.ConferenceName).Where(g => g.Key != "Independent" && g.Count() >= 4).ToArray();
            if (groups.Length >= 2)
            {
                var first = groups[random.Next(groups.Length)];
                var second = groups.Where(g => g.Key != first.Key).OrderBy(g => Math.Abs(g.Average(t => t.Prestige) - first.Average(t => t.Prestige))).First();
                var a = first.OrderByDescending(t => ProgramPrestigeService.GetCurrentPrestige(state, t)).First();
                var b = second.OrderBy(t => ProgramPrestigeService.GetCurrentPrestige(state, t)).First();
                teams[teams.IndexOf(a)] = a with { ConferenceName = b.ConferenceName, DivisionName = b.DivisionName, ConferenceGamesPerSeason = b.ConferenceGamesPerSeason };
                teams[teams.IndexOf(b)] = b with { ConferenceName = a.ConferenceName, DivisionName = a.DivisionName, ConferenceGamesPerSeason = a.ConferenceGamesPerSeason };
                history.Add(new(state.SeasonYear, a.Name, a.ConferenceName, b.ConferenceName, false));
                history.Add(new(state.SeasonYear, b.Name, b.ConferenceName, a.ConferenceName, false));
            }
        }
        var roster = state.ActiveRoster;
        if (state.LeagueRules.FcsPromotion && state.LeagueRules.FcsOpponents && random.NextDouble() < .16)
        {
            // Invite a successful external opponent; no school already in the playable league can be invited again.
            var available = FcsLeagueService.Available(teams);
            var invite = available.OrderByDescending(t => state.Schedule.Count(g => g.SeasonYear == state.SeasonYear - 1 && g.IsFcsGame && g.WinnerTeamName == t.Name))
                .ThenByDescending(t => t.Prestige).ThenBy(t => SimulationSeed.Create(state.DynastyId, state.SeasonYear, 0, t.Name, "promotion")).FirstOrDefault();
            if (invite is not null)
            {
                var conference = teams.GroupBy(t => t.ConferenceName).Where(g => g.Key != "Independent").OrderBy(g => g.Count()).ThenBy(g => g.Average(t => t.Prestige)).FirstOrDefault();
                var destination = conference?.Key ?? "Independent";
                int? conferenceGames = null;
                if (conference is not null && conference.All(t => t.ConferenceGamesPerSeason.HasValue))
                {
                    var maximum = Math.Min(state.LeagueRules.GamesPerTeam,conference.Count());
                    var requested = Math.Clamp((int)Math.Round(conference.Average(t => t.ConferenceGamesPerSeason!.Value)),0,maximum);
                    if ((conference.Sum(t => t.ConferenceGamesPerSeason!.Value) + requested) % 2 != 0)
                        requested = requested > 0 ? requested - 1 : Math.Min(1,maximum);
                    conferenceGames = requested;
                }
                teams.Add(invite with { ConferenceName = destination, ConferenceGamesPerSeason = conferenceGames });
                roster = roster.Concat(FcsLeagueService.CreateRoster(state, invite)).ToArray();
                history.Add(new(state.SeasonYear, invite.Name, invite.ConferenceName, destination, true));
            }
        }
        return state with { LeagueTeams = teams, ActiveRoster = roster, LeagueMovementHistory = history, LeagueMovementProcessedYear = state.SeasonYear };
    }
}
