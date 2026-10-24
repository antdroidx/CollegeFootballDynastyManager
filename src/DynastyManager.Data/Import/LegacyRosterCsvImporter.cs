using DynastyManager.Data.Csv;

namespace DynastyManager.Data.Import;

public static class LegacyRosterCsvImporter
{
    public static LegacyRosterImportResult Import(string csv)
    {
        var players = new List<ImportedPlayerRow>();
        var warnings = new List<ImportWarning>();
        var lines = csv.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        for (var index = 0; index < lines.Length; index++)
        {
            if (string.IsNullOrWhiteSpace(lines[index]))
                continue;

            var values = CsvLineParser.Parse(lines[index]);
            if (values.Count < 5)
                continue;

            if (index == 0 && values[0].Equals("TEAM", StringComparison.OrdinalIgnoreCase))
                continue;

            var team = values[0];
            var name = values[1];
            var rawPosition = values[2];

            if (!int.TryParse(values[3], out var year))
                throw new FormatException($"Roster row {index + 1} has invalid YEAR '{values[3]}'.");

            if (!int.TryParse(values[4], out var talent))
                throw new FormatException($"Roster row {index + 1} has invalid STARS/TALENT '{values[4]}'.");

            var position = LegacyPositionConverter.Convert(
                rawPosition,
                $"{team}|{name}|{year}|{talent}",
                out var warning);

            if (warning is not null)
                warnings.Add(new ImportWarning(index + 1, $"{team} - {name}: {warning}"));

            players.Add(new ImportedPlayerRow
            {
                TeamName = team,
                FullName = name,
                Position = position,
                Year = year,
                LegacyTalentLevel = talent
            });
        }

        return new LegacyRosterImportResult(players, warnings);
    }
}
