using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

/// <summary>
/// Deterministic roster-informed possession simulation. This is the first
/// gameplay port layer: team roster strengths drive passing, rushing,
/// turnovers, yards, and scoring while the later play-by-play port can replace
/// the drive model without changing schedule/save contracts.
/// </summary>
public static class DeterministicGameSimulator
{
    private const int PossessionsPerTeam = 12;
    private const double HomeFieldRatingBonus = 2.5;

    public static ScheduledGame Simulate(
        ScheduledGame game,
        Team homeTeam,
        Team awayTeam,
        TeamSimulationProfile? homeProfile = null,
        TeamSimulationProfile? awayProfile = null)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(homeTeam);
        ArgumentNullException.ThrowIfNull(awayTeam);

        if (game.HasPlayed)
            return game;

        if (!game.HomeTeamName.Equals(homeTeam.Name, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(
                "Home team does not match the scheduled game.",
                nameof(homeTeam));

        if (!game.AwayTeamName.Equals(awayTeam.Name, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(
                "Away team does not match the scheduled game.",
                nameof(awayTeam));

        homeProfile ??= FallbackProfile(homeTeam);
        awayProfile ??= FallbackProfile(awayTeam);

        var random = new Random(game.SimulationSeed);

        var home = SimulateSide(
            homeProfile,
            awayProfile,
            random,
            HomeFieldRatingBonus);

        var away = SimulateSide(
            awayProfile,
            homeProfile,
            random,
            0.0);

        var homeScore = home.Score;
        var awayScore = away.Score;

        if (homeScore == awayScore)
        {
            // Temporary deterministic OT resolution. The later possession-level
            // overtime port can replace this without altering saved game shape.
            if (random.Next(2) == 0)
                homeScore += random.Next(2) == 0 ? 3 : 7;
            else
                awayScore += random.Next(2) == 0 ? 3 : 7;
        }

        return game with
        {
            HasPlayed = true,
            HomeScore = homeScore,
            AwayScore = awayScore,
            HomeStats = home.Stats,
            AwayStats = away.Stats
        };
    }

    private static SimulatedSide SimulateSide(
        TeamSimulationProfile offense,
        TeamSimulationProfile defense,
        Random random,
        double homeFieldBonus)
    {
        var passAdvantage =
            offense.PassOffenseRating -
            defense.PassDefenseRating +
            homeFieldBonus;

        var rushAdvantage =
            offense.RushOffenseRating -
            defense.RushDefenseRating +
            homeFieldBonus;

        var overallAdvantage =
            passAdvantage * 0.55 +
            rushAdvantage * 0.45;

        var passShare = Math.Clamp(
            0.54 + (passAdvantage - rushAdvantage) * 0.006,
            0.38,
            0.68);

        var touchdownChance = Math.Clamp(
            0.22 + overallAdvantage * 0.010,
            0.08,
            0.48);

        var fieldGoalChance = Math.Clamp(
            0.15 +
            (offense.SpecialTeamsRating - defense.SpecialTeamsRating) * 0.003 +
            overallAdvantage * 0.002,
            0.08,
            0.25);

        var turnoverChance = Math.Clamp(
            0.105 - overallAdvantage * 0.003,
            0.045,
            0.19);

        var score = 0;
        var passAttempts = 0;
        var rushAttempts = 0;
        var passYards = 0;
        var rushYards = 0;
        var turnovers = 0;

        for (var possession = 0; possession < PossessionsPerTeam; possession++)
        {
            var plays = random.Next(5, 10);
            var drivePassAttempts = Math.Clamp(
                (int)Math.Round(plays * passShare) + random.Next(-1, 2),
                1,
                Math.Max(1, plays - 1));
            var driveRushAttempts = plays - drivePassAttempts;

            passAttempts += drivePassAttempts;
            rushAttempts += driveRushAttempts;

            var passPerAttempt =
                5.8 +
                passAdvantage * 0.075 +
                CenteredNoise(random, 1.8);

            var rushPerAttempt =
                4.2 +
                rushAdvantage * 0.055 +
                CenteredNoise(random, 1.1);

            passYards += Math.Max(
                0,
                (int)Math.Round(drivePassAttempts * passPerAttempt));

            rushYards += Math.Max(
                0,
                (int)Math.Round(driveRushAttempts * rushPerAttempt));

            if (random.NextDouble() < turnoverChance)
            {
                turnovers++;
                continue;
            }

            var scoringRoll = random.NextDouble();

            if (scoringRoll < touchdownChance)
                score += 7;
            else if (scoringRoll < touchdownChance + fieldGoalChance)
                score += 3;
        }

        return new SimulatedSide(
            score,
            new GameTeamStats
            {
                Possessions = PossessionsPerTeam,
                PassAttempts = passAttempts,
                RushAttempts = rushAttempts,
                PassYards = passYards,
                RushYards = rushYards,
                Turnovers = turnovers
            });
    }

    private static TeamSimulationProfile FallbackProfile(Team team)
    {
        var rating = Math.Clamp(
            60.0 + (team.Prestige - 50) * 0.5,
            55.0,
            95.0);

        return new TeamSimulationProfile
        {
            TeamName = team.Name,
            PassOffenseRating = rating,
            RushOffenseRating = rating,
            PassDefenseRating = rating,
            RushDefenseRating = rating,
            SpecialTeamsRating = rating,
            RosterSize = 0
        };
    }

    private static double CenteredNoise(Random random, double scale) =>
        (random.NextDouble() + random.NextDouble() - 1.0) * scale;

    private sealed record SimulatedSide(int Score, GameTeamStats Stats);
}
