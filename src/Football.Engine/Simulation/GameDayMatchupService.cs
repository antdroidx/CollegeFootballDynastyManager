using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class GameDayMatchupService
{
    public static double FieldGoalProbability(int distance, double accuracy, double power)
    {
        var range = 48 + (power - 60) * .35;
        return Math.Clamp(.985 - Math.Max(0, distance - 25) * .008 -
            Math.Max(0, distance - 40) * .014 + (accuracy - 75) * .0025 -
            Math.Max(0, distance - range) * .04, .01, .99);
    }

    public static double FormationAdvantage(OffensivePlayDefinition offense, DefensivePlayDefinition defense, bool pass)
    {
        var defensiveBacks = defense.Personnel switch
        { DefensivePersonnel.Dime => 6, DefensivePersonnel.Nickel => 5, DefensivePersonnel.GoalLine => 2, _ => 4 };
        var receivers = offense.Personnel switch
        { OffensivePersonnel.Empty => 5, OffensivePersonnel.Ten => 4, OffensivePersonnel.Eleven => 3, _ => 2 };
        // Lighter boxes trade run support for coverage. Heavy sets trade receiving options for blocking.
        var shift = pass ? (4 - defensiveBacks) * 1.5 + (receivers - 3) * .5
            : (defensiveBacks - 4) * 2 + (3 - receivers) * .75;
        if (pass && offense.Formation == OffensiveFormation.Empty && defense.Approach == DefensiveApproach.Pressure) shift -= 2;
        return Math.Clamp(shift, -6, 6);
    }

    public static double PersonnelAdvantage(IReadOnlyList<DynastyPlayer> offense, IReadOnlyList<DynastyPlayer> defense, bool pass)
    {
        double Average(IEnumerable<DynastyPlayer> players, Func<DynastyPlayer, double> rating) =>
            players.Select(rating).DefaultIfEmpty(75).Average();
        var qb = Average(offense.Where(p => p.Position == Position.QB), p =>
            ((p.Skills?.PassingAccuracy ?? p.AwarenessRating) * .65 + (p.Skills?.PassingPower ?? p.StrengthRating) * .20 + p.AwarenessRating * .15));
        var blocking = Average(offense.Where(p => p.Position is Position.OL or Position.TE), p =>
            pass ? p.Skills?.PassBlocking ?? p.StrengthRating : p.Skills?.RunBlocking ?? p.StrengthRating);
        var ballSkills = Average(offense.Where(p => pass ? p.Position is Position.WR or Position.TE : p.Position == Position.RB), p =>
            pass ? (p.Skills?.Catching ?? p.AwarenessRating) * .6 + p.SpeedRating * .25 + p.AgilityRating * .15
                : (p.Skills?.BreakTackle ?? p.StrengthRating) * .4 + (p.Skills?.BallCarrying ?? p.AwarenessRating) * .3 + p.SpeedRating * .3);
        var defending = Average(defense.Where(p => pass ? p.Position is Position.CB or Position.FS or Position.SS or Position.OLB
            : p.Position is Position.DE or Position.DT or Position.MLB or Position.OLB), p =>
            pass ? p.AwarenessRating * .6 + p.SpeedRating * .25 + (p.Skills?.Jumping ?? p.AgilityRating) * .15
                : (p.Skills?.Tackling ?? p.StrengthRating) * .6 + p.StrengthRating * .25 + p.AwarenessRating * .15);
        var attacking = pass ? qb * .45 + blocking * .25 + ballSkills * .30 : blocking * .55 + ballSkills * .45;
        // Team profiles already contain roster/coaching quality; this modest adjustment reflects the actual package/subs.
        return Math.Clamp((attacking - defending) * .25, -10, 10);
    }
}
