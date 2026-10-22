using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class GameWeatherService
{
    // Fictional, repeatable football conditions, not forecasts or a meteorological database.
    public static GameWeather ForGame(ScheduledGame game)
    {
        if (game.Weather is not null) return Normalize(game.Weather);
        var site = game.IsNeutralSite ? game.BowlName ?? "Neutral site" : game.HomeTeamName;
        var indoors = new[] { "Syracuse", "UNLV", "Texas San Antonio", "UTSA", "Idaho State", "Sugar Bowl" }
            .Any(n => site.Equals(n, StringComparison.OrdinalIgnoreCase));
        if (indoors) return new() { Indoors = true, TemperatureFahrenheit = 72 };
        var hash = site.Aggregate(17, (h,c) => unchecked(h * 31 + c));
        var random = new Random(unchecked(hash ^ game.SimulationSeed ^ game.SeasonYear * 397 ^ game.Week * 7919));
        var warm = new[] { "Florida", "Miami", "Hawaii", "Arizona", "Texas", "Fiesta", "Orange", "Rose" }.Any(n => site.Contains(n, StringComparison.OrdinalIgnoreCase));
        var temperature = Math.Clamp((warm ? 86 : 78) - Math.Max(0, game.Week) * (warm ? 1 : 2) + random.Next(-12, 13), 12, 100);
        var wet = random.NextDouble() < (warm ? .15 : .22);
        return new() { TemperatureFahrenheit = temperature, WindMph = random.Next(0, 23),
            WindFieldDirection = random.Next(-1, 2), Precipitation = !wet ? GamePrecipitation.Clear : temperature <= 32 ? GamePrecipitation.Snow : GamePrecipitation.Rain };
    }
    public static GameWeather Normalize(GameWeather w) => w.Indoors ? new() { Indoors = true, TemperatureFahrenheit = 72 } :
        w with { TemperatureFahrenheit = Math.Clamp(w.TemperatureFahrenheit, -10, 120), WindMph = Math.Clamp(w.WindMph, 0, 45), WindFieldDirection = Math.Clamp(w.WindFieldDirection,-1,1) };
    public static int AttackDirection(GameDaySession s, string team) => (team == s.Game.HomeTeamName ? 1 : -1) * (s.Quarter % 2 == 0 ? -1 : 1);
    public static double Tailwind(GameDaySession s, string team) => s.Weather.Indoors ? 0 : s.Weather.WindMph * s.Weather.WindFieldDirection * AttackDirection(s,team);
    public static double CatchPenalty(GameWeather w, bool deep) => w.Indoors ? 0 :
        Math.Min(.045, w.WindMph * (deep ? .0015 : .0004)) + (w.Precipitation == GamePrecipitation.Rain ? .025 : w.Precipitation == GamePrecipitation.Snow ? .045 : 0);
    public static double FumblePenalty(GameWeather w) => w.Indoors ? 0 : w.Precipitation == GamePrecipitation.Rain ? .007 : w.Precipitation == GamePrecipitation.Snow ? .012 : 0;
    public static double KickProbability(GameDaySession s, double baseProbability)
    {
        var w = s.Weather;
        if (w.Indoors) return baseProbability;
        var distance = 117 - s.YardLine;
        var adjustment = Tailwind(s,s.PossessionTeamName) * .0008 - w.WindMph * .0007 -
            (w.Precipitation == GamePrecipitation.Clear ? 0 : .015) - (w.TemperatureFahrenheit < 32 && distance > 40 ? .015 : 0);
        return Math.Clamp(baseProbability + adjustment, .01, .995);
    }
}
