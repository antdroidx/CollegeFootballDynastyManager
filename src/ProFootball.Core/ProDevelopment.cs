using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
namespace ProFootball.Core;

/// <summary>Pro-specific development; overall still uses the shared attribute weights.</summary>
public static class ProDevelopment
{
    public static int PeakAge(Position position) => position switch
    {
        Position.RB => 25, Position.CB => 27,
        Position.WR or Position.DE or Position.MLB or Position.OLB or Position.SS => 28,
        Position.TE or Position.DT or Position.FS => 29,
        Position.OL => 30, Position.QB => 31, Position.K => 33, _ => 28
    };

    // A ceiling cannot imply lost present ability. Remaining upside narrows as the prime window closes.
    public static int Ceiling(ProPlayer player) => Math.Clamp(
        Math.Min(player.Football.PotentialRating,
            player.Football.OverallRating + Math.Max(0, PeakAge(player.Football.Position) - player.Age) * 3),
        player.Football.OverallRating, 99);
    public static int Headroom(ProPlayer player) => Ceiling(player) - player.Football.OverallRating;
    public static string Growth(ProPlayer player)
    {
        var gain = Headroom(player);
        if (gain == 0) return "At ceiling";
        var stars = gain <= 2 ? 1 : gain <= 5 ? 2 : gain <= 8 ? 3 : gain <= 12 ? 4 : 5;
        return new string('★', stars) + new string('☆', 5 - stars);
    }
    public static int? Delta(ProPlayer player) => player.PreviousSeasonOverall is int previous
        ? player.Football.OverallRating - previous
        : player.Football.SeasonStartOverall > 0 ? player.Football.OverallRating - player.Football.SeasonStartOverall : null;

    public static ProPlayer AgeOneYear(ProPlayer player, Random random)
    {
        var age = player.Age + 1;
        if (player.Retired) return player with { Age = age };
        var old = PlayerSkillService.EnsureInitialized(player.Football);
        var peak = PeakAge(old.Position);
        var pastPrime = Math.Max(0, age - peak);
        var growth = age <= peak ? Math.Min(Headroom(player), random.Next(0, 3)) : 0;
        var physicalLoss = pastPrime == 0 ? 0 : random.Next(1, 3) + (pastPrime >= 4 ? 1 : 0);
        var techniqueLoss = pastPrime <= 2 ? 0 : random.Next(1, 3);
        var mentalLoss = pastPrime <= 5 ? 0 : 1;
        int R(int value, int change) => Math.Clamp(value + change, 1, 99);
        DynastyPlayer Candidate(int gain)
        {
            var k = old.Skills!;
            return old with
            {
                SpeedRating = R(old.SpeedRating, gain - physicalLoss),
                AgilityRating = R(old.AgilityRating, gain - physicalLoss),
                StrengthRating = R(old.StrengthRating, gain - (pastPrime > 2 ? 1 : 0)),
                AwarenessRating = R(old.AwarenessRating, gain - mentalLoss),
                Skills = k with
                {
                    Acceleration = R(k.Acceleration, gain - physicalLoss), Jumping = R(k.Jumping, gain - physicalLoss),
                    PassingPower = R(k.PassingPower, gain - (pastPrime > 2 ? 1 : 0)),
                    PassingAccuracy = R(k.PassingAccuracy, gain - techniqueLoss), Catching = R(k.Catching, gain - techniqueLoss),
                    BreakTackle = R(k.BreakTackle, gain - physicalLoss), Tackling = R(k.Tackling, gain - techniqueLoss),
                    RunBlocking = R(k.RunBlocking, gain - techniqueLoss), PassBlocking = R(k.PassBlocking, gain - techniqueLoss),
                    BallCarrying = R(k.BallCarrying, gain - techniqueLoss),
                    Leadership = R(k.Leadership, gain - mentalLoss), Discipline = R(k.Discipline, -mentalLoss),
                    KickPower = R(k.KickPower, gain - (pastPrime > 2 ? 1 : 0)), KickAccuracy = R(k.KickAccuracy, gain - techniqueLoss)
                }, CurrentInjury = null
            };
        }
        var changed = Candidate(growth);
        while (growth > 0 && PlayerRatingService.CalculateOverall(changed) > Ceiling(player)) changed = Candidate(--growth);
        var overall = PlayerRatingService.CalculateOverall(changed);
        var aged = player with { Age = age, PreviousSeasonOverall = old.OverallRating,
            Football = changed with { OverallRating = overall, SeasonStartOverall = overall } };
        return aged with { Football = aged.Football with { PotentialRating = Math.Max(overall, Math.Min(Ceiling(player), Ceiling(aged))) } };
    }
}
