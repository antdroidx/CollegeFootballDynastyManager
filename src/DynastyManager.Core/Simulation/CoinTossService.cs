using DynastyManager.Core.Models;
namespace DynastyManager.Core.Simulation;

public static class CoinTossService
{
    // Separate RNG: presenting or changing the toss does not consume play simulation randomness.
    public static CoinTossResult Toss(ScheduledGame game, bool awayCallsHeads = true)
    {
        var heads = new Random(unchecked(game.SimulationSeed ^ 0x43F26)).Next(2) == 0;
        var winner = heads == awayCallsHeads ? game.AwayTeamName : game.HomeTeamName;
        var other = winner == game.HomeTeamName ? game.AwayTeamName : game.HomeTeamName;
        return new(heads, winner, other, true); // CPU winner defers to the second half.
    }
    public static void Apply(GameDaySession session, CoinTossResult toss)
    {
        if (session.Events.Count != 0) throw new InvalidOperationException("The toss must precede the opening kickoff.");
        if (toss.ReceivingTeam != session.Game.HomeTeamName && toss.ReceivingTeam != session.Game.AwayTeamName)
            throw new ArgumentException("Receiving team must be in this game.");
        session.CoinToss = toss;
        session.OpeningReceivingTeam = session.PossessionTeamName = toss.ReceivingTeam;
        session.KickoffTeamName = toss.ReceivingTeam == session.Game.HomeTeamName ? session.Game.AwayTeamName : session.Game.HomeTeamName;
        session.KickoffPending = true;
    }
}
