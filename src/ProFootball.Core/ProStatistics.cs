using DynastyManager.Core.Models;
namespace ProFootball.Core;

public static class ProStatistics
{
    public static double PasserRating(PlayerSeasonStatLine line)
    {
        if (line.PassAttempts <= 0) return 0;
        double Part(double value) => Math.Clamp(value, 0, 2.375);
        var attempts = (double)line.PassAttempts;
        return (Part((line.PassCompletions / attempts - .3) * 5) + Part((line.PassYards / attempts - 3) * .25)
            + Part(line.PassingTouchdowns / attempts * 20) + Part(2.375 - line.InterceptionsThrown / attempts * 25)) / 6 * 100;
    }
}
