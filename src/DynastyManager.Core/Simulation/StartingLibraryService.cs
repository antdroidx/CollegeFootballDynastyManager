using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class StartingLibraryService
{
    public static void Validate(StartingLeagueSetup setup)
    {
        if (string.IsNullOrWhiteSpace(setup.Name) || setup.Name.Length > 80) throw new ArgumentException("Name the league setup (1–80 characters).");
        if (setup.Teams.Count < 2) throw new ArgumentException("A league needs at least two teams.");
        if (setup.Teams.Any(t => string.IsNullOrWhiteSpace(t.Name) || string.IsNullOrWhiteSpace(t.Abbreviation) || string.IsNullOrWhiteSpace(t.ConferenceName)))
            throw new ArgumentException("Every team needs a name, abbreviation and conference (or Independent).");
        if (setup.Teams.Select(t => t.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != setup.Teams.Count)
            throw new ArgumentException("Team names must be unique.");
        if (setup.Rules.GamesPerTeam is < 1 or > 12 || setup.Rules.GamesPerTeam >= setup.Teams.Count)
            throw new ArgumentException("Choose 1–12 games per team, with enough unique opponents.");
        if ((setup.Teams.Count * setup.Rules.GamesPerTeam) % 2 != 0)
            throw new ArgumentException("An odd number of teams needs an even game count so every matchup has two teams.");
        if (setup.Rules.PlayoffTeams is not (2 or 4 or 8 or 12) || setup.Rules.PlayoffTeams > setup.Teams.Count)
            throw new ArgumentException("Choose a 2-, 4-, 8- or 12-team playoff that fits this league.");
        foreach (var group in setup.Teams.GroupBy(t => t.ConferenceName, StringComparer.OrdinalIgnoreCase))
            if (group.Any(t => t.ConferenceGamesPerSeason is < 0 || t.ConferenceGamesPerSeason > Math.Min(setup.Rules.GamesPerTeam, group.Count() - 1)))
                throw new ArgumentException($"Conference game counts do not fit {group.Key}.");
    }

    public static bool IsCompatible(StartingRosterPack roster, IEnumerable<Team> teams)
    {
        var names = teams.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return roster.Players.Any(p => names.Contains(p.TeamName));
    }

    public static IReadOnlyList<DynastyPlayer> CreateRoster(StartingLeagueSetup setup, StartingRosterPack? roster,
        IReadOnlyList<DynastyPlayer> generated)
    {
        Validate(setup);
        if (roster is null) return generated;
        if (!IsCompatible(roster, setup.Teams)) throw new ArgumentException("The roster has no schools in this league setup.");
        var leagueNames = setup.Teams.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var selected = roster.Players.Where(p => leagueNames.Contains(p.TeamName)).ToArray();
        var names = selected.Select(p => p.TeamName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return generated.Where(p => !names.Contains(p.TeamName)).Concat(selected.Select(p => p with
        { PlayerId = Guid.NewGuid(), CurrentInjury = null, DepthChartOrder = 99 })).ToArray();
    }

    public static StartingLibrary RemoveRoster(StartingLibrary library, Guid id) => library with
    {
        Rosters = library.Rosters.Where(r => r.Id != id).ToArray(),
        DefaultRosterBySetup = library.DefaultRosterBySetup.Where(p => p.Value != id).ToDictionary()
    };

    public static StartingLibrary RemoveSetup(StartingLibrary library, StartingLeagueSetup setup) => library with
    {
        Setups = library.Setups.Where(s => s.Id != setup.Id).ToArray(),
        HiddenBuiltIns = setup.IsBuiltIn ? library.HiddenBuiltIns.Append(setup.Id).Distinct().ToArray() : library.HiddenBuiltIns,
        DefaultRosterBySetup = library.DefaultRosterBySetup.Where(p => p.Key != setup.Id).ToDictionary()
    };
}
