using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

/// <summary>
/// Deterministic first-pass game simulation shell. It preserves two important
/// ideas from the legacy engine now: team-strength advantage and a home-field
/// bonus. Player/play-by-play logic will replace this scoring shell in later
/// Phase 5 slices.
/// </summary>
public static class DeterministicGameSimulator
{
    private const double HomeFieldPoints = 3.0;
    private const double PrestigePointFactor = 0.20;

    public static ScheduledGame Simulate(
        ScheduledGame game,
        Team homeTeam,
        Team awayTeam)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(homeTeam);
        ArgumentNullException.ThrowIfNull(awayTeam);

        if (game.HasPlayed)
            return game;

        if (!game.HomeTeamName.Equals(homeTeam.Name, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Home team does not match the scheduled game.", nameof(homeTeam));

        if (!game.AwayTeamName.Equals(awayTeam.Name, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Away team does not match the scheduled game.", nameof(awayTeam));

        var random = new Random(game.SimulationSeed);
        var prestigeDifference = homeTeam.Prestige - awayTeam.Prestige;

        var expectedHome =
            24.0 +
            prestigeDifference * PrestigePointFactor +
            HomeFieldPoints;

        var expectedAway =
            24.0 -
            prestigeDifference * PrestigePointFactor;

        var homeScore = ScoreFromExpectation(expectedHome, random);
        var awayScore = ScoreFromExpectation(expectedAway, random);

        if (homeScore == awayScore)
        {
            // College games cannot end tied. Keep the first implementation
            // simple but deterministic until the possession-based OT port.
            if (random.Next(2) == 0)
                homeScore += random.Next(2) == 0 ? 3 : 7;
            else
                awayScore += random.Next(2) == 0 ? 3 : 7;
        }

        return game with
        {
            HasPlayed = true,
            HomeScore = homeScore,
            AwayScore = awayScore
        };
    }

    private static int ScoreFromExpectation(double expectation, Random random)
    {
        // Sum several bounded samples for a football-like center-heavy spread
        // without relying on process-specific or non-deterministic randomness.
        var noise =
            random.Next(-7, 8) +
            random.Next(-5, 6) +
            random.Next(-3, 4);

        return Math.Clamp(
            (int)Math.Round(expectation + noise, MidpointRounding.AwayFromZero),
            0,
            70);
    }
}
