using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
namespace ProFootball.Core;

public sealed record ProRookieSeasonUsage(Guid PlayerId, int Year)
{
    public int OffensiveSnaps { get; init; }
    public int DefensiveSnaps { get; init; }
    public int TeamOffensiveSnaps { get; init; }
    public int TeamDefensiveSnaps { get; init; }
    public int UnknownOffenseGames { get; init; }
    public int UnknownDefenseGames { get; init; }
    public List<string> RecordedGames { get; init; } = [];
}

/// <summary>Observed regular-season participation for future rookie-option eligibility. Never invents missing past usage.</summary>
public static class ProRookieParticipation
{
    public static ProFranchise Record(ProFranchise state, ProGame game, IReadOnlyList<GameSnapEvent> events)
    {
        if (game.Phase != ProPhase.RegularSeason) return state;
        var rookies = state.Players.Where(p => !p.Retired && p.DraftOverall is >= 1 and <= 32
            && p.RookieYear is int rookie && state.Year >= rookie && state.Year <= rookie + 2
            && game.Football.InvolvesTeam(p.Football.TeamName)).ToArray();
        if (rookies.Length == 0) return state;
        var usage = state.RookieParticipation.ToDictionary(u => (u.PlayerId, u.Year));
        var snaps = events.Where(GameDayEngine.IsScrimmage).ToArray();
        foreach (var player in rookies)
        {
            var id = player.Football.PlayerId;
            var old = usage.GetValueOrDefault((id,state.Year)) ?? new(id,state.Year);
            if (old.RecordedGames.Contains(game.Football.GameId)) continue;
            var offense = snaps.Where(e => e.OffenseTeamName == player.Football.TeamName).ToArray();
            var defense = snaps.Where(e => e.DefenseTeamName == player.Football.TeamName).ToArray();
            usage[(id,state.Year)] = old with
            {
                OffensiveSnaps = old.OffensiveSnaps + offense.Count(e => e.OffensePersonnel.Contains(id)),
                DefensiveSnaps = old.DefensiveSnaps + defense.Count(e => e.DefensePersonnel.Contains(id)),
                TeamOffensiveSnaps = old.TeamOffensiveSnaps + offense.Length,
                TeamDefensiveSnaps = old.TeamDefensiveSnaps + defense.Length,
                UnknownOffenseGames = old.UnknownOffenseGames + (offense.Length == 0 || offense.Any(e => e.OffensePersonnel.Count == 0) ? 1 : 0),
                UnknownDefenseGames = old.UnknownDefenseGames + (defense.Length == 0 || defense.Any(e => e.DefensePersonnel.Count == 0) ? 1 : 0),
                RecordedGames = old.RecordedGames.Append(game.Football.GameId).ToList()
            };
        }
        return state with { RookieParticipation = usage.Values.OrderBy(u => u.Year).ThenBy(u => u.PlayerId).ToList() };
    }

    public static bool MeetsPlayingTimeTier(IEnumerable<ProRookieSeasonUsage> history, Guid id, int rookieYear, bool offense)
    {
        var seasons = history.Where(u => u.PlayerId == id && u.Year >= rookieYear && u.Year <= rookieYear + 2
            && u.RecordedGames.Distinct().Count() >= 17
            && (offense ? u.UnknownOffenseGames : u.UnknownDefenseGames) == 0
            && (offense ? u.OffensiveSnaps >= 0 && u.OffensiveSnaps <= u.TeamOffensiveSnaps : u.DefensiveSnaps >= 0 && u.DefensiveSnaps <= u.TeamDefensiveSnaps))
            .DistinctBy(u => u.Year).ToArray();
        long Played(ProRookieSeasonUsage u) => offense ? u.OffensiveSnaps : u.DefensiveSnaps;
        long Total(ProRookieSeasonUsage u) => offense ? u.TeamOffensiveSnaps : u.TeamDefensiveSnaps;
        bool Share(ProRookieSeasonUsage u, int percent) => Total(u) > 0 && Played(u) * 100 >= Total(u) * percent;
        if (seasons.Count(u => Share(u,75)) >= 2) return true;
        return seasons.Length == 3 && (seasons.All(u => Share(u,50))
            || seasons.Sum(Total) > 0 && seasons.Sum(Played) * 100 >= seasons.Sum(Total) * 75);
    }
}
