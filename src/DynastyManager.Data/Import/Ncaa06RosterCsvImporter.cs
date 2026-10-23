using System.Globalization;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using DynastyManager.Data.Csv;

namespace DynastyManager.Data.Import;

public sealed record RosterImportPreview(IReadOnlyList<DynastyPlayer> Players, IReadOnlyList<string> Errors)
{
    public bool CanApply => Players.Count > 0 && Errors.Count == 0;
}

/// <summary>PLAY table CSV with displayed 1–99 ratings. Numeric TGID values require an explicit team mapping.</summary>
public static class Ncaa06RosterCsvImporter
{
    public static RosterImportPreview Preview(TextReader reader, IEnumerable<string> teamNames,
        IReadOnlyDictionary<string, string>? teamMapping = null, IReadOnlyList<int>? ratingLookup = null)
    {
        var errors = new List<string>(); var players = new List<DynastyPlayer>();
        var known = teamNames.ToDictionary(t => t, StringComparer.OrdinalIgnoreCase);
        var header = reader.ReadLine();
        if (header is null) return new([], ["The CSV file is empty."]);
        var columns = CsvLineParser.Parse(header.TrimStart('\uFEFF')).Select(c => c.ToUpperInvariant()).Select(c => c == "PYEA" ? "PYER" : c).ToArray();
        if (columns.Distinct().Count() != columns.Length) return new([], ["Duplicate CSV column names."]);
        var required = Ncaa06RosterRatings.RatingFields.Concat(["PPOS", "PFNA", "PLNA", "PYER", "TGID"]).ToArray();
        var missing = required.Where(c => !columns.Contains(c)).ToArray();
        if (missing.Length > 0) return new([], [$"Missing columns: {string.Join(", ", missing)}"]);
        var lineNumber = 1; var ids = new HashSet<string>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (lineNumber > 30001) { errors.Add("Import is limited to 30,000 players."); break; }
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                var values = CsvLineParser.Parse(line);
                if (values.Count != columns.Length) throw new FormatException("Column count does not match the header.");
                var row = columns.Select((key, index) => (key, values[index])).ToDictionary(p => p.key, p => p.Item2);
                var teamId = row["TGID"];
                var mapped = teamMapping is not null && teamMapping.TryGetValue(teamId, out var name) ? name : teamId;
                if (!known.TryGetValue(mapped, out var team)) throw new FormatException($"Unmapped team TGID '{teamId}'. Map it to a league team name.");
                int Number(string key) => int.TryParse(row[key], NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
                    ? n : throw new FormatException($"{key} must be an integer.");
                var year = Number("PYER");
                if (year is < 0 or > 4) throw new FormatException("PYER must be NCAA class year 0–4 (FR through GR).");
                var fullName = $"{row["PFNA"]} {row["PLNA"]}".Trim();
                if (fullName.Length is < 2 or > 100) throw new FormatException("Player name is missing or too long.");
                if (row.TryGetValue("PGID", out var id) && !ids.Add(id)) throw new FormatException($"Duplicate PGID {id}.");
                var redshirt = row.TryGetValue("PRSD", out var rs) && !string.IsNullOrWhiteSpace(rs) ? Number("PRSD") : 0;
                if (redshirt is < 0 or > 2) throw new FormatException("PRSD must be 0 (eligible), 1 (current), or 2 (previous).");
                var player = new DynastyPlayer { PlayerId = Guid.NewGuid(), FullName = fullName,
                    TeamName = team, Position = Position.QB, ClassYear = year + 1, PotentialRating = 60, TalentLevel = 60,
                    HasRedshirted = redshirt != 0, IsRedshirted = redshirt == 1, DepthChartOrder = 99 };
                var ratings = Ncaa06RosterRatings.RatingFields.Concat(["PPOS"]).ToDictionary(c => c, Number);
                player = NcaaPlayerBioImporter.Apply(Ncaa06RosterRatings.Apply(player, ratings, ratingLookup), row);
                players.Add(player with { TalentLevel = player.OverallRating });
            }
            catch (Exception ex) when (ex is ArgumentException or FormatException)
            { errors.Add($"Row {lineNumber}: {ex.Message}"); }
        }
        return new(players, errors);
    }

    public static DynastyState Apply(DynastyState state, RosterImportPreview preview)
    {
        if (state.Phase != SeasonPhase.Preseason || state.Week != 0 || state.Schedule.Any(g => g.HasPlayed))
            throw new InvalidOperationException("Roster imports are available only before the first season starts.");
        if (!preview.CanApply) throw new InvalidOperationException("Resolve all preview errors before importing.");
        var replacedTeams = preview.Players.Select(p => p.TeamName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (state.ActiveRoster.Where(p => replacedTeams.Contains(p.TeamName)).Any(p => state.PlayerSeasonStats.Any(s => s.PlayerId == p.PlayerId)))
            throw new InvalidOperationException("A roster with recorded player statistics cannot be replaced.");
        return state with { ActiveRoster = state.ActiveRoster.Where(p => !replacedTeams.Contains(p.TeamName))
            .Concat(preview.Players).ToArray() };
    }
}
