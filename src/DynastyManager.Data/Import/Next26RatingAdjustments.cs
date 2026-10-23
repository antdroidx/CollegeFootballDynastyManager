namespace DynastyManager.Data.Import;

/// <summary>Import-only displayed-rating deltas from ratingdrops.xlsx, Sheet1 (2026-10-22).</summary>
internal static class Next26RatingAdjustments
{
    public static Dictionary<string, int> Decode(IReadOnlyDictionary<string, int> source)
    {
        var position = source["PPOS"];
        var group = position switch
        {
            0 => "QB", 1 => "HB", 2 => "FB", 3 => "WR", 4 => "TE",
            >= 5 and <= 9 => "OL", 10 or 11 => "DE", 12 => "DT",
            >= 13 and <= 15 => "LB", 16 => "CB", 17 => "FS", 18 => "SS",
            19 => "K", 20 => "P", _ => throw new FormatException("Invalid PPOS.")
        };
        var decoded = new Dictionary<string, int> { ["PPOS"] = position };
        foreach (var (field, raw) in source.Where(p => p.Key != "PPOS"))
        {
            if (raw is < 0 or > 59) throw new FormatException($"{field} must be 0–59.");
            decoded[field] = Math.Clamp(raw + 40 + Delta(group, field), 1, 99);
        }
        return decoded;
    }

    private static int Delta(string group, string field) => (group, field) switch
    {
        ("QB", "PACC" or "PAGI" or "PSPD") => -3,
        ("QB", "PSTR") => -6,
        ("QB", "PAWR" or "PTHA" or "PTHP") => -2,
        ("HB" or "FB" or "WR" or "TE" or "CB" or "K" or "P", "PAWR") => -2,
        ("DT" or "DE" or "LB" or "FS", "PAWR") => -3,
        ("SS", "PAWR") => -4,
        ("TE" or "LB" or "FS" or "SS", "PACC") => -1,
        ("TE", "PAGI") => -2,
        ("LB" or "FS" or "SS", "PAGI") => -1,
        ("LB" or "CB" or "FS" or "SS", "PCTH") => -1,
        ("CB" or "FS" or "SS", "PJMP") => -2,
        ("K" or "P", "PKAC" or "PKPR") => -1,
        ("TE" or "DE" or "LB" or "FS" or "SS", "PSPD") => -1,
        ("DT" or "DE", "PSTR") => -1,
        ("DT" or "DE" or "LB" or "CB" or "FS" or "SS", "PTAK") => -1,
        _ => 0
    };
}
