using DynastyManager.Core.Models;
using DynastyManager.Data.Csv;

namespace DynastyManager.Data.Import;

public static class LegacyCoachCsvImporter
{
    public static IReadOnlyList<Coach> Import(string csv)
    {
        var coaches = new List<Coach>();
        var active = false;

        foreach (var line in csv.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var values = CsvLineParser.Parse(line);
            var first = values[0];

            if (first.Equals("[START_COACHES]", StringComparison.OrdinalIgnoreCase))
            {
                active = true;
                continue;
            }

            if (first.Equals("[END_COACHES]", StringComparison.OrdinalIgnoreCase))
                break;

            if (!active || first.StartsWith("[", StringComparison.Ordinal))
                continue;

            if (values.Count >= 4 && TryRole(values[2], out var role) && int.TryParse(values[3], out var modernRating))
            {
                coaches.Add(new Coach
                {
                    TeamName = values[0],
                    Name = values[1],
                    Role = role,
                    Rating = modernRating
                });
            }
            else if (values.Count >= 3 && int.TryParse(values[2], out var legacyRating))
            {
                coaches.Add(new Coach
                {
                    TeamName = values[0],
                    Name = values[1],
                    Role = CoachRole.HeadCoach,
                    Rating = legacyRating
                });
            }
        }

        return coaches;
    }

    private static bool TryRole(string value, out CoachRole role)
    {
        role = value.Trim().ToUpperInvariant() switch
        {
            "HC" => CoachRole.HeadCoach,
            "OC" => CoachRole.OffensiveCoordinator,
            "DC" => CoachRole.DefensiveCoordinator,
            _ => CoachRole.HeadCoach
        };

        return value.Trim().Equals("HC", StringComparison.OrdinalIgnoreCase)
            || value.Trim().Equals("OC", StringComparison.OrdinalIgnoreCase)
            || value.Trim().Equals("DC", StringComparison.OrdinalIgnoreCase);
    }
}
