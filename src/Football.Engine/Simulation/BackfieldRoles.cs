using DynastyManager.Core.Models;
namespace DynastyManager.Core.Simulation;

/// <summary>Per-play jobs; a TE/HB filling the FB role retains his roster identity.</summary>
public static class BackfieldRoles
{
    public static DynastyPlayer? MarkerRole(DynastyPlayer? player, Guid? fullbackId) => player is not null && player.PlayerId==fullbackId ? player with {Position=Position.FB} : player;
    public static bool UsesFullback(OffensivePlayDefinition play) => play.Personnel is OffensivePersonnel.TwentyOne or OffensivePersonnel.TripleBack;
    public static DynastyPlayer? Fullback(OffensivePlayDefinition play, IReadOnlyList<DynastyPlayer> players) => !UsesFullback(play) ? null :
        players.FirstOrDefault(p=>p.Position==Position.FB) ?? players.FirstOrDefault(p=>p.Position==Position.TE) ?? players.FirstOrDefault(p=>p.Position==Position.RB);
    public static IReadOnlyList<DynastyPlayer> Assign(OffensivePlayDefinition play, IReadOnlyList<DynastyPlayer> players)
    {
        var fb=Fullback(play,players);
        return players.Select(p=>p.PlayerId==fb?.PlayerId?p with {Position=Position.FB}:p).ToArray();
    }
    public static DynastyPlayer? Runner(OffensivePlayDefinition play, IReadOnlyList<DynastyPlayer> players, Random random)
    {
        if(play.Name.StartsWith("FB ",StringComparison.Ordinal))return players.FirstOrDefault(p=>p.Position==Position.FB);
        var backs=players.Where(p=>p.Position==Position.RB).ToArray();
        return backs.Length>0?backs[random.Next(backs.Length)]:players.FirstOrDefault(p=>p.Position==Position.FB);
    }
}
