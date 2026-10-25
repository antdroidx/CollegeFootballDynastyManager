using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class PlayerClassYearService
{
    public static string Format(DynastyPlayer player) =>
        Format(player.ClassYear, player.IsRedshirted || player.HasRedshirted);

    public static string Format(int classYear, bool hasRedshirted = false)
    {
        var label = classYear switch { <= 1 => "FR", 2 => "SO", 3 => "JR", 4 => "SR", _ => "GR" };
        return hasRedshirted ? label + "-RS" : label;
    }
}
