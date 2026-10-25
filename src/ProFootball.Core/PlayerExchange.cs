using DynastyManager.Core.Models;
namespace ProFootball.Core;

public static class ProPlayerImport
{
    // Explicit professional translation; college OVR is never copied directly.
    public static List<ProPlayer> Convert(PlayerExport export, int seed)
    {
        if (export.FormatVersion != 1 || export.Players.Select(p => p.PlayerId).Distinct().Count() != export.Players.Count) throw new InvalidDataException("Unsupported or duplicate player export.");
        var random = new Random(seed);
        int Scale(int n) => Math.Clamp((int)Math.Round(35 + n * .45) + random.Next(-4, 5), 30, 95);
        return export.Players.Select(p => { var overall = Scale(p.OverallRating); return new ProPlayer(p with { TeamName = "Free agent", OverallRating = overall, SeasonStartOverall = overall, PotentialRating = Math.Clamp(Math.Max(overall, Scale(p.PotentialRating) + 5), 30, 99), SpeedRating = Scale(p.SpeedRating), StrengthRating = Scale(p.StrengthRating), AgilityRating = Scale(p.AgilityRating), AwarenessRating = Scale(p.AwarenessRating), DepthChartOrder = 99, IsRedshirted = false, HasRedshirted = false, Skills = p.Skills is null ? null : p.Skills with { PassingPower = Scale(p.Skills.PassingPower), PassingAccuracy = Scale(p.Skills.PassingAccuracy), Catching = Scale(p.Skills.Catching), Tackling = Scale(p.Skills.Tackling), RunBlocking = Scale(p.Skills.RunBlocking), PassBlocking = Scale(p.Skills.PassBlocking), BreakTackle = Scale(p.Skills.BreakTackle), BallCarrying = Scale(p.Skills.BallCarrying), Acceleration = Scale(p.Skills.Acceleration) } }, 22, 0, false); }).ToList();
    }
}
