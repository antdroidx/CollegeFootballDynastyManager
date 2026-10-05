using DynastyManager.Core.Models;
using DynastyManager.Data.Csv;

namespace DynastyManager.Data.Import;

public static class LegacyUniverseCsvImporter
{
    private enum Section
    {
        None,
        Conferences,
        Teams
    }

    public static LegacyUniverseImportResult Import(string csv)
    {
        var conferences = new List<Conference>();
        var teams = new List<Team>();
        var section = Section.None;

        foreach (var line in csv.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var values = CsvLineParser.Parse(line);
            var first = values[0];

            if (first.Equals("[START_CONFERENCES]", StringComparison.OrdinalIgnoreCase))
            {
                section = Section.Conferences;
                continue;
            }

            if (first.Equals("[END_CONFERENCES]", StringComparison.OrdinalIgnoreCase))
            {
                section = Section.None;
                continue;
            }

            if (first.Equals("[START_TEAMS]", StringComparison.OrdinalIgnoreCase))
            {
                section = Section.Teams;
                continue;
            }

            if (first.Equals("[END_TEAMS]", StringComparison.OrdinalIgnoreCase))
            {
                section = Section.None;
                continue;
            }

            if (first.StartsWith("[", StringComparison.Ordinal))
                continue;

            if (section == Section.Conferences && values.Count >= 3)
            {
                conferences.Add(new Conference
                {
                    Name = values[0],
                    DivisionOneName = BlankToNull(values[1]),
                    DivisionTwoName = BlankToNull(values[2])
                });
                continue;
            }

            if (section == Section.Teams && values.Count >= 6)
            {
                if (!int.TryParse(values[3], out var prestige))
                    throw new FormatException($"Invalid prestige '{values[3]}' for team '{values[0]}'.");

                if (!int.TryParse(values[5], out var region))
                    region = 0;

                teams.Add(new Team
                {
                    Name = values[0],
                    Abbreviation = values[1],
                    ConferenceName = values[2],
                    Prestige = prestige,
                    DivisionName = BlankToNull(values[4]),
                    LegacyRegionId = region
                });
            }
        }

        return new LegacyUniverseImportResult(conferences, teams);
    }

    private static string? BlankToNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
