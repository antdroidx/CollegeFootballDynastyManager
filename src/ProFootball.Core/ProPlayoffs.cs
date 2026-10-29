using DynastyManager.Core.Models;
namespace ProFootball.Core;

public static class ProPlayoffs
{
    static ProGame Pair(ProFranchise s, ProStanding home, ProStanding away, int week) => new(new ScheduledGame { GameId = $"{s.Year}-playoffs-{week}-{home.Team.Id}-{away.Team.Id}", SeasonYear = s.Year, Week = week, HomeTeamName = home.Team.Name, AwayTeamName = away.Team.Name, HomeSeed = ProStandings.Seeds(s, home.Team.Conference).FindIndex(r => r.Team.Id == home.Team.Id) + 1, AwaySeed = ProStandings.Seeds(s, away.Team.Conference).FindIndex(r => r.Team.Id == away.Team.Id) + 1, SimulationSeed = s.Year * 10000 + week * 100 + s.League.Teams.IndexOf(home.Team), IsNeutralSite = week == 4 }, ProPhase.Playoffs, week);
    public static ProFranchise Begin(ProFranchise state)
    {
        state = ProHonors.Publish(state);
        var games = state.Games.ToList(); foreach (var conference in state.League.Teams.Select(t => t.Conference).Distinct())
        { var seeds = ProStandings.Seeds(state, conference); foreach (var pair in new[] { (1, 6), (2, 5), (3, 4) }) games.Add(Pair(state, seeds[pair.Item1], seeds[pair.Item2], 1)); }
        return state with { Phase = ProPhase.Playoffs, Week = 1, Games = games };
    }
    public static ProFranchise Advance(ProFranchise state)
    {
        var games = state.Games.ToList();
        if (state.Week == 4)
        { var champion = games.Single(g => g.Phase == ProPhase.Playoffs && g.Football.Week == 4).Football.WinnerTeamName!; return state with { Phase = ProPhase.Offseason, Champion = champion, History = state.History.Append(new(state.Year, champion, games.ToList())).ToList() }; }
        if (state.Week == 3)
        { var winners = games.Where(g => g.Phase == ProPhase.Playoffs && g.Football.Week == 3).Select(g => g.Football.WinnerTeamName).ToArray(); var rows = ProStandings.Build(state); games.Add(Pair(state, rows.Single(r => r.Team.Name == winners[0]), rows.Single(r => r.Team.Name == winners[1]), 4)); }
        else foreach (var conference in state.League.Teams.Select(t => t.Conference).Distinct())
        { var seeds = ProStandings.Seeds(state, conference); var winners = games.Where(g => g.Phase == ProPhase.Playoffs && g.Football.Week == state.Week).Select(g => g.Football.WinnerTeamName).ToHashSet(); if (state.Week == 1) winners.Add(seeds[0].Team.Name); var remaining = seeds.Where(r => winners.Contains(r.Team.Name)).ToList(); while (remaining.Count > 1) { games.Add(Pair(state, remaining[0], remaining[^1], state.Week + 1)); remaining.RemoveAt(remaining.Count - 1); remaining.RemoveAt(0); } }
        return state with { Week = state.Week + 1, Games = games };
    }
}
