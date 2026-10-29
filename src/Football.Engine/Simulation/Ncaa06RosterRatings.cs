using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

/// <summary>Explicit NCAA PLAY-field adapter. Input defaults to displayed ratings;
/// pass the roster's PRLU lookup to decode compressed DB values.</summary>
public static class Ncaa06RosterRatings
{
    public static readonly IReadOnlyList<string> RatingFields = new[]
    { "PCAR", "PKAC", "PTHA", "PPBK", "PRBK", "PACC", "PAGI", "PTAK", "PINJ", "PKPR", "PSPD", "PTHP", "PBTK", "PCTH", "PSTR", "PJMP", "PAWR" };

    public static DynastyPlayer Apply(DynastyPlayer player, IReadOnlyDictionary<string, int> fields,
        IReadOnlyList<int>? ratingLookup = null)
    {
        int Read(string name)
        {
            if (!fields.TryGetValue(name, out var value)) throw new ArgumentException($"Missing NCAA rating {name}.", nameof(fields));
            if (ratingLookup is not null)
            {
                if (value < 0 || value >= ratingLookup.Count) throw new ArgumentOutOfRangeException(name);
                value = ratingLookup[value];
            }
            if (value is < 1 or > 99) throw new ArgumentOutOfRangeException(name);
            return value;
        }
        var positionCode = fields.TryGetValue("PPOS", out var code) ? code : Ncaa06RatingService.PositionCode(player);
        var position = positionCode switch
        {
            0 => Position.QB, 1 or 2 => Position.RB, 3 => Position.WR, 4 => Position.TE,
            >= 5 and <= 9 => Position.OL, 10 or 11 => Position.DE, 12 => Position.DT,
            13 or 15 => Position.OLB, 14 => Position.MLB, 16 => Position.CB,
            17 => Position.FS, 18 => Position.SS, 19 or 20 => Position.K,
            _ => throw new ArgumentOutOfRangeException("PPOS")
        };
        var skills = player.Skills ?? new PlayerSkillRatings { Leadership = 60, Discipline = 60 };
        var imported = player with
        {
            Position = position, NcaaPositionCode = positionCode,
            SpeedRating = Read("PSPD"), StrengthRating = Read("PSTR"), AgilityRating = Read("PAGI"),
            AwarenessRating = Read("PAWR"), InjuryRating = Read("PINJ"),
            Skills = skills with
            {
                BallCarrying = Read("PCAR"), KickAccuracy = Read("PKAC"), PassingAccuracy = Read("PTHA"),
                PassBlocking = Read("PPBK"), RunBlocking = Read("PRBK"), Acceleration = Read("PACC"),
                Tackling = Read("PTAK"), KickPower = Read("PKPR"), PassingPower = Read("PTHP"),
                BreakTackle = Read("PBTK"), Catching = Read("PCTH"), Jumping = Read("PJMP")
            }
        };
        imported = PlayerRatingService.RecalculateOverall(imported);
        return PlayerPhysicalService.EnsureMeasurements(imported with { PotentialRating = Math.Max(imported.PotentialRating, imported.OverallRating) });
    }

    public static IReadOnlyDictionary<string, int> Export(DynastyPlayer player)
    {
        var skills = PlayerSkillService.EnsureInitialized(player).Skills!;
        return new Dictionary<string, int>
        {
            ["PPOS"] = Ncaa06RatingService.PositionCode(player), ["POVR"] = Ncaa06RatingService.CalculateOverall(player),
            ["PCAR"] = skills.BallCarrying, ["PKAC"] = skills.KickAccuracy, ["PTHA"] = skills.PassingAccuracy,
            ["PPBK"] = skills.PassBlocking, ["PRBK"] = skills.RunBlocking, ["PACC"] = skills.Acceleration,
            ["PAGI"] = player.AgilityRating, ["PTAK"] = skills.Tackling, ["PINJ"] = player.InjuryRating,
            ["PKPR"] = skills.KickPower, ["PSPD"] = player.SpeedRating, ["PTHP"] = skills.PassingPower,
            ["PBTK"] = skills.BreakTackle, ["PCTH"] = skills.Catching, ["PSTR"] = player.StrengthRating,
            ["PJMP"] = skills.Jumping, ["PAWR"] = player.AwarenessRating
        };
    }
}
