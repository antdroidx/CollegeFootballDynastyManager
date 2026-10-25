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

        homeProfile = homeProfile with { GamePlan = GamePlanService.Normalize(homeProfile.GamePlan ?? new TeamGamePlan { TeamName = homeTeam.Name }) };
        awayProfile = awayProfile with { GamePlan = GamePlanService.Normalize(awayProfile.GamePlan ?? new TeamGamePlan { TeamName = awayTeam.Name }) };
        var random = new Random(game.SimulationSeed);

        var home = SimulateSide(
            homeProfile,
            awayProfile,
            random,
            game.IsNeutralSite ? 0.0 : HomeFieldRatingBonus);

        var away = SimulateSide(
            awayProfile,
            homeProfile,
            random,
            0.0);

        home = home with { Stats = AddReturns(home.Stats, away.Stats, homeProfile.SpecialTeamsRating, random) };
        away = away with { Stats = AddReturns(away.Stats, home.Stats, awayProfile.SpecialTeamsRating, random) };

        var homeScore = home.Score;
        var awayScore = away.Score;

        var drives = home.Drives.Concat(away.Drives).ToList();
        if (homeScore == awayScore)
        {
            // Temporary deterministic OT scoring; full overtime possessions are a later layer.
            var homeWins = random.Next(2) == 0;
            var points = random.Next(2) == 0 ? 3 : 7;
            if (homeWins) homeScore += points; else awayScore += points;
            drives.Add(new GameDriveSummary
            {
                TeamName = homeWins ? homeTeam.Name : awayTeam.Name, PossessionNumber = 13,
                IsOvertime = true, Points = points,
                Outcome = points == 7 ? DriveOutcome.Touchdown : DriveOutcome.FieldGoal
            });
        }

        return game with
        {
            HasPlayed = true,
            HomePregameRating = (homeProfile.OffenseRating + homeProfile.DefenseRating) / 2,
            AwayPregameRating = (awayProfile.OffenseRating + awayProfile.DefenseRating) / 2,
            HomeGamePlan = homeProfile.GamePlan, AwayGamePlan = awayProfile.GamePlan, Drives = drives,
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
        var plan = offense.GamePlan!;
        var defensePlan = defense.GamePlan!;
        var (passDefenseShift, rushDefenseShift) = defensePlan.Defense switch
        {
            DefensivePlaybook.FourThree => (-2.0, 3.0),
            DefensivePlaybook.ThreeFour => (-1.0, 1.0),
            DefensivePlaybook.Nickel => (3.0, -2.0),
            _ => (0.0, 0.0)
        };
        var (passFocus, rushFocus) = defensePlan.Approach switch
        {
            DefensiveApproach.StopRun => (-3.0, 5.0),
            DefensiveApproach.ProtectPass => (5.0, -3.0),
            DefensiveApproach.Pressure => (2.0, -1.0),
            _ => (0.0, 0.0)
        };
        var passAdvantage = offense.PassOffenseRating - defense.PassDefenseRating - passDefenseShift - passFocus + homeFieldBonus;
        var rushAdvantage = offense.RushOffenseRating - defense.RushDefenseRating - rushDefenseShift - rushFocus + homeFieldBonus;

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
        var passCompletions = 0;
        var rushAttempts = 0;
        var passYards = 0;
        var rushYards = 0;
        var turnovers = 0;
        var passingTouchdowns = 0;
        var rushingTouchdowns = 0;
        var interceptions = 0;
        var fumbles = 0;
        var explosivePasses = 0;
        var explosiveRuns = 0;
        var drops = 0;
        var yardsAfterCatch = 0;
        var sacksAllowed = 0;
        var fieldGoalsMade = 0;
        var fieldGoalsAttempted = 0;
        var extraPointsMade = 0;
        var extraPointsAttempted = 0;
        var punts = 0;
        var puntYards = 0;
        var longPunt = 0;
        var puntsInside20 = 0;
        var puntsInside10 = 0;

        var drives = new List<GameDriveSummary>();
        for (var possession = 0; possession < PossessionsPerTeam; possession++)
        {
            var playCall = GamePlanService.ResolvePlayCall(plan, defensePlan, possession, random,
                (offense.HeadCoachGameManagement + offense.OffensiveCoordinatorGameManagement) / 2,
                (defense.HeadCoachGameManagement + defense.DefensiveCoordinatorGameManagement) / 2);
            var concept = playCall.Offense.Concept;
            var (shareShift, passGain, rushGain, turnoverShift, scoringShift) = concept switch
            {
                OffensiveConcept.InsideRun => (-0.30, 0.0, 0.25, -0.020, 0.0),
                OffensiveConcept.OutsideRun => (-0.26, 0.0, 0.45, -0.010, 0.0),
                OffensiveConcept.QuickPass => (0.22, -0.5, 0.0, -0.018, -0.010),
                OffensiveConcept.PlayAction => (0.12, 0.8, 0.0, 0.012, 0.010),
                OffensiveConcept.DeepPass => (0.28, 2.2, 0.0, 0.045, 0.025),
                _ => (0.0, 0.0, 0.0, 0.0, 0.0)
            };
            var driveShare = Math.Clamp(passShare + shareShift, 0.12, 0.90);
            var pressure = defensePlan.Approach == DefensiveApproach.Pressure;
            var driveTurnoverChance = Math.Clamp(turnoverChance + turnoverShift + (pressure ? 0.025 : 0), 0.02, 0.30);
            var driveTouchdownChance = Math.Clamp(touchdownChance + scoringShift +
                (driveShare - passShare) * (passAdvantage - rushAdvantage) * .010 + (pressure ? .01 : 0), .04, .55);
            var plays = random.Next(5, 10);
            var drivePassAttempts = Math.Clamp(
                (int)Math.Round(plays * driveShare) + random.Next(-1, 2),
                0,
                plays);
            var driveRushAttempts = plays - drivePassAttempts;

            passAttempts += drivePassAttempts;
            rushAttempts += driveRushAttempts;

            var completionRate = Math.Clamp(.61 + passAdvantage * .0035 - (pressure ? .035 : 0) +
                (concept == OffensiveConcept.QuickPass ? .055 : concept == OffensiveConcept.DeepPass ? -.09 : 0), .42, .78);
            var driveCompletions = Math.Clamp((int)Math.Round(drivePassAttempts * completionRate + CenteredNoise(random, .7)), 0, drivePassAttempts);
            passCompletions += driveCompletions;
            drops += Math.Max(0, Math.Min(drivePassAttempts - driveCompletions, random.Next(0, 2)));
            if (pressure && random.NextDouble() < .32) sacksAllowed++;

            var passPerAttempt =
                5.8 +
                passAdvantage * 0.075 + passGain + (pressure ? 1.2 : 0) +
                CenteredNoise(random, 1.8);

            var rushPerAttempt =
                4.2 +
                rushAdvantage * 0.055 + rushGain +
                CenteredNoise(random, 1.1);

            var drivePassYards = Math.Max(0, (int)Math.Round(drivePassAttempts * passPerAttempt));
            var driveRushYards = Math.Max(0, (int)Math.Round(driveRushAttempts * rushPerAttempt));
            passYards += drivePassYards;
            rushYards += driveRushYards;
            explosivePasses += Math.Max(0, drivePassYards / 70 + (concept == OffensiveConcept.DeepPass && drivePassYards >= 40 ? 1 : 0));
            explosiveRuns += Math.Max(0, driveRushYards / 65);
            yardsAfterCatch += Math.Max(0, (int)Math.Round(drivePassYards * (concept == OffensiveConcept.QuickPass ? .48 : .30)));
            var outcome = DriveOutcome.Punt;
            var points = 0;
            if (random.NextDouble() < driveTurnoverChance)
            {
                turnovers++;
                if (drivePassAttempts > 0 && random.NextDouble() < driveShare * .78) interceptions++;
                else fumbles++;
                outcome = DriveOutcome.Turnover;
            }
            else
            {
                var scoringRoll = random.NextDouble();
                if (scoringRoll < driveTouchdownChance)
                {
                    extraPointsAttempted++;
                    var madeExtraPoint = random.NextDouble() < Math.Clamp(.94 + offense.SpecialTeamsRating * .0005, .93, .99);
                    if (madeExtraPoint) extraPointsMade++;
                    points = 6 + (madeExtraPoint ? 1 : 0);
                    if (random.NextDouble() < driveShare) passingTouchdowns++; else rushingTouchdowns++;
                    outcome = DriveOutcome.Touchdown;
                }
                else if (scoringRoll < driveTouchdownChance + fieldGoalChance)
                {
                    fieldGoalsAttempted++; fieldGoalsMade++; points = 3; outcome = DriveOutcome.FieldGoal;
                }
                else if (scoringRoll < driveTouchdownChance + fieldGoalChance + .045)
                    fieldGoalsAttempted++;
                else
                {
                    punts++;
                    var punt = Math.Clamp((int)Math.Round(39 + offense.SpecialTeamsRating * .07 + CenteredNoise(random, 7)), 25, 68);
                    puntYards += punt; longPunt = Math.Max(longPunt, punt);
                    if (random.NextDouble() < .34) puntsInside20++;
                    if (random.NextDouble() < .14) puntsInside10++;
                }
            }
            score += points;
            drives.Add(new GameDriveSummary
            {
                TeamName = offense.TeamName, PossessionNumber = possession + 1, Concept = concept,
                OffensivePlayId = playCall.Offense.Id, DefensivePlayId = playCall.Defense.Id,
                PlayCallSource = playCall.Source,
                DefenseFaced = defensePlan.Approach, PassAttempts = drivePassAttempts, RushAttempts = driveRushAttempts,
                PassYards = drivePassYards, RushYards = driveRushYards, Outcome = outcome, Points = points
            });
        }

        return new SimulatedSide(
            score,
            new GameTeamStats
            {
                Possessions = PossessionsPerTeam,
                PassAttempts = passAttempts,
                PassCompletions = passCompletions,
                RushAttempts = rushAttempts,
                PassYards = passYards,
                RushYards = rushYards,
                PassingTouchdowns = passingTouchdowns,
                RushingTouchdowns = rushingTouchdowns,
                InterceptionsThrown = interceptions,
                FumblesLost = fumbles,
                ExplosivePasses20 = explosivePasses,
                ExplosiveRuns20 = explosiveRuns,
                Drops = drops,
                YardsAfterCatch = yardsAfterCatch,
                SacksAllowed = sacksAllowed,
                FieldGoalsMade = fieldGoalsMade,
                FieldGoalsAttempted = fieldGoalsAttempted,
                ExtraPointsMade = extraPointsMade,
                ExtraPointsAttempted = extraPointsAttempted,
                Punts = punts,
                PuntYards = puntYards,
                LongPunt = longPunt,
                PuntsInside20 = puntsInside20,
                PuntsInside10 = puntsInside10,
                Turnovers = turnovers
            }, drives);
    }

    private static GameTeamStats AddReturns(GameTeamStats own, GameTeamStats opponent, double specialTeams, Random random)
    {
        var kickReturns = Math.Max(0, opponent.PassingTouchdowns + opponent.RushingTouchdowns + opponent.FieldGoalsMade - 1);
        var kickAverage = Math.Clamp((int)Math.Round(18 + specialTeams * .07 + CenteredNoise(random, 3)), 15, 31);
        var puntReturns = Math.Max(0, opponent.Punts - opponent.PuntsInside20 / 2);
        var puntAverage = Math.Clamp((int)Math.Round(5 + specialTeams * .055 + CenteredNoise(random, 2.5)), 2, 18);
        return own with
        {
            KickReturns = kickReturns,
            KickReturnYards = kickReturns * kickAverage,
            KickReturnTouchdowns = kickReturns >= 4 && random.NextDouble() < .025 ? 1 : 0,
            PuntReturns = puntReturns,
            PuntReturnYards = puntReturns * puntAverage,
            PuntReturnTouchdowns = puntReturns >= 3 && random.NextDouble() < .018 ? 1 : 0
        };
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

    private sealed record SimulatedSide(int Score, GameTeamStats Stats, IReadOnlyList<GameDriveSummary> Drives);
}
