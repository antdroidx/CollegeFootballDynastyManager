using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Csv;

namespace DynastyManager.Data.Import;

/// <summary>NEXT26 decoded-name export. Attribute values are stored 0–59: add 40, once.</summary>
public static class Next26RosterCsvImporter
{
    public const string RosterName = "NCAA 06 NEXT26 · Aug 26, 2025";
    public static readonly Guid RosterId = Guid.Parse("2be66e42-ec81-49e9-8d1c-000000000026");
    private static readonly int[] DisplayedRatings = Enumerable.Range(40, 60).ToArray();
    private static readonly IReadOnlyDictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Florida Atlantic"] = "Florida Atl", ["Florida International"] = "Florida Intl", ["Hawai'i"] = "Hawaii",
        ["Louisiana"] = "LA Lafayette", ["Louisiana Tech"] = "LA Tech", ["Miami University"] = "Miami OH",
        ["Middle Tennessee"] = "Mid Tenn State", ["New Mexico State"] = "New Mexico St", ["SMU"] = "Southern Methodist",
        ["Southern Mississippi"] = "Southern Miss", ["Texas A&M"] = "Texas AM", ["UCF"] = "Central Florida",
        ["UL Monroe"] = "LA Monroe", ["USF"] = "South Florida", ["UTEP"] = "UT El Paso", ["UTSA"] = "UT San Antonio"
    };

    public static RosterImportPreview Preview(TextReader reader, IEnumerable<string> teamNames)
    {
        var teams = teamNames.Distinct(StringComparer.OrdinalIgnoreCase).ToDictionary(t => t, StringComparer.OrdinalIgnoreCase);
        var header = reader.ReadLine();
        if (header is null) return new([], ["The NEXT26 roster is empty."]);
        var columns = CsvLineParser.Parse(header.TrimStart('\uFEFF')).Select(c => c.ToUpperInvariant()).ToArray();
        if (columns.Distinct().Count() != columns.Length) return new([], ["Duplicate CSV column names."]);
        var required = Ncaa06RosterRatings.RatingFields.Concat(["FIRSTNAME", "LASTNAME", "COLLEGE", "PYER", "PRSD", "PPOS"]).ToArray();
        var missing = required.Except(columns).ToArray();
        if (missing.Length > 0) return new([], [$"Missing NEXT26 columns: {string.Join(", ", missing)}"]);
        if (!columns.Contains("PGID") && !columns.Contains("CFB26 PGID")) return new([], ["Missing player identity: PGID or CFB26 PGID."]);
        var players = new List<DynastyPlayer>(); var errors = new List<string>(); var ids = new HashSet<string>();
        string? line; var lineNumber = 1;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (lineNumber > 30001) { errors.Add("Import is limited to 30,000 players."); break; }
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                var values = CsvLineParser.Parse(line);
                if (values.Count != columns.Length) throw new FormatException("Column count does not match the header.");
                var row = columns.Select((key, index) => (key, values[index].Trim())).ToDictionary(p => p.key, p => p.Item2);
                int Number(string key) => int.TryParse(row[key], NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
                    ? number : throw new FormatException($"{key} must be an integer.");
                var name = $"{row["FIRSTNAME"]} {row["LASTNAME"]}".Trim();
                if (name.Length is < 2 or > 100) throw new FormatException("Player name is missing or too long.");
                var school = row["COLLEGE"];
                if (!teams.TryGetValue(school, out var team) &&
                    !(Aliases.TryGetValue(school, out var alias) && teams.TryGetValue(alias, out team)))
                    throw new FormatException($"Unknown NEXT26 school '{school}'.");
                var year = Number("PYER"); var redshirt = Number("PRSD");
                var idColumn = row.TryGetValue("PGID", out var pgid) && pgid.Length > 0 ? "PGID" : "CFB26 PGID";
                if (!row.ContainsKey(idColumn)) throw new FormatException("Missing player identity.");
                var id = idColumn + ":" + Number(idColumn);
                if (year is < 0 or > 4) throw new FormatException("PYER must be 0–4.");
                if (redshirt is < 0 or > 2) throw new FormatException("PRSD must be 0 (eligible), 1 (current), or 2 (previous).");
                if (!ids.Add(id)) throw new FormatException($"Duplicate player identity {id}.");
                var ratings = Ncaa06RosterRatings.RatingFields.Concat(["PPOS"]).ToDictionary(c => c, Number);
                var player = new DynastyPlayer
                {
                    PlayerId = new Guid(SHA256.HashData(Encoding.UTF8.GetBytes($"NEXT26-AUG26-2025:{id}"))[..16]),
                    FullName = name, TeamName = team!, Position = Position.QB, ClassYear = year + 1,
                    IsRedshirted = redshirt == 1, HasRedshirted = redshirt != 0, DepthChartOrder = 99,
                    PotentialRating = 60
                };
                player = NcaaPlayerBioImporter.Apply(Ncaa06RosterRatings.Apply(player, ratings, DisplayedRatings), row);
                players.Add(ImportedRecruitingStarsService.EstimateIfUnknown(player with { TalentLevel = player.OverallRating }));
            }
            catch (Exception ex) when (ex is ArgumentException or FormatException)
            { errors.Add($"Row {lineNumber}: {ex.Message}"); }
        }
        return new(players, errors);
    }

    public static StartingRosterPack LoadPack(TextReader reader, IEnumerable<string> teams)
    {
        var preview = Preview(reader, teams);
        if (!preview.CanApply) throw new FormatException("The packaged NEXT26 roster is invalid: " + string.Join("; ", preview.Errors.Take(5)));
        return new StartingRosterPack { Id = RosterId, Name = RosterName, Players = preview.Players };
    }
}
