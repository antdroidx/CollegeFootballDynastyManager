using DynastyManager.Core.Models;
namespace DynastyManager.Core.Simulation;

public sealed record PlayerAttributeGroup(string Title, string Subtitle, (string Name, int Rating)[] Attributes);

/// <summary>One attribute catalog for college and professional player cards.</summary>
public static class PlayerAttributeCatalog
{
    public static string PotentialStars(int potential) => potential switch { >= 92 => "5★", >= 84 => "4★", >= 76 => "3★", >= 68 => "2★", _ => "1★" };

    public static IReadOnlyList<PlayerAttributeGroup> Groups(DynastyPlayer player)
    {
        var skills = PlayerSkillService.EnsureInitialized(player).Skills!;
        return [
            new("Character traits", "Decision-making and team influence", [("Awareness", player.AwarenessRating), ("Leadership", skills.Leadership), ("Discipline", skills.Discipline)]),
            new("Physical traits", "Athletic ability and availability", [("Speed", player.SpeedRating), ("Acceleration", skills.Acceleration), ("Agility", player.AgilityRating), ("Strength", player.StrengthRating), ("Jumping", skills.Jumping), ("Injury", player.InjuryRating)]),
            new("Skill traits", "Position technique and football execution", [("Throw power", skills.PassingPower), ("Throw accuracy", skills.PassingAccuracy), ("Catching", skills.Catching), ("Break tackle", skills.BreakTackle), ("Ball carrying", skills.BallCarrying), ("Tackling", skills.Tackling), ("Run blocking", skills.RunBlocking), ("Pass blocking", skills.PassBlocking), ("Kicking power", skills.KickPower), ("Kicking accuracy", skills.KickAccuracy)])
        ];
    }
}
