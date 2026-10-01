using System.Security.Cryptography;
using System.Text;
using DynastyManager.Core.Models;

namespace DynastyManager.Data.Import;

public static class HighSchoolRecruitingPoolFactory
{
    public static IReadOnlyList<HighSchoolRecruit> Create(
        Guid dynastyId,
        int seasonYear,
        int teamCount,
        IEnumerable<ImportedPlayerRow> nameSource)
    {
        ArgumentNullException.ThrowIfNull(nameSource);

        var target = RecruitingPoolRules.GetTargetPoolSize(teamCount);
        var names = BuildNamePool(nameSource);
        var positions = BuildPositionWheel();
        var recruits = new HighSchoolRecruit[target];

        for (var index = 0; index < target; index++)
        {
            var starRoll = Value(
                dynastyId,
                seasonYear,
                index,
                "stars") % 1000;

            var stars = starRoll switch
            {
                < 20 => 5,
                < 120 => 4,
                < 500 => 3,
                < 850 => 2,
                _ => 1
            };

            var position = positions[
                Value(dynastyId, seasonYear, index, "position") %
                positions.Count];

            var overall = GenerateOverall(
                dynastyId,
                seasonYear,
                index,
                stars);

            var first = names.FirstNames[
                Value(dynastyId, seasonYear, index, "first") %
                names.FirstNames.Count];

            var last = names.LastNames[
                Value(dynastyId, seasonYear, index, "last") %
                names.LastNames.Count];

            recruits[index] = new HighSchoolRecruit
            {
                RecruitId = CreateId(
                    dynastyId,
                    seasonYear,
                    index,
                    $"{first}|{last}|{position}"),
                FullName = $"{first} {last}",
                Position = position,
                StarRating = stars,
                TrueOverallRating = overall,
                PotentialRating = Math.Clamp(
                    overall +
                    Value(dynastyId, seasonYear, index, "potential") % 11,
                    overall,
                    99),
                HomeRegion =
                    Value(dynastyId, seasonYear, index, "region") % 50
            };
        }

        return recruits;
    }

    private static int GenerateOverall(
        Guid dynastyId,
        int seasonYear,
        int index,
        int stars)
    {
        var (minimum, maximum) = stars switch
        {
            5 => (82, 91),
            4 => (75, 85),
            3 => (68, 79),
            2 => (59, 73),
            _ => (50, 65)
        };

        return minimum +
               Value(dynastyId, seasonYear, index, "overall") %
               (maximum - minimum + 1);
    }

    private static IReadOnlyList<Position> BuildPositionWheel()
    {
        var positions = new List<Position>();

        foreach (var pair in DynastyRosterRules.TargetPositionCounts)
        {
            for (var index = 0; index < pair.Value; index++)
                positions.Add(pair.Key);
        }

        return positions;
    }

    private static NamePool BuildNamePool(
        IEnumerable<ImportedPlayerRow> rows)
    {
        var first = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);
        var last = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var parts = row.FullName.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

            if (parts.Length == 0)
                continue;

            first.Add(parts[0]);
            if (parts.Length > 1)
                last.Add(parts[^1]);
        }

        if (first.Count == 0)
        {
            first.UnionWith(new[]
            {
                "Alex", "Jordan", "Chris", "Cameron",
                "Jalen", "Marcus", "Devin", "Malik"
            });
        }

        if (last.Count == 0)
        {
            last.UnionWith(new[]
            {
                "Smith", "Johnson", "Williams", "Brown",
                "Davis", "Wilson", "Moore", "Taylor"
            });
        }

        return new NamePool(
            first.OrderBy(value => value).ToArray(),
            last.OrderBy(value => value).ToArray());
    }

    private static int Value(
        Guid dynastyId,
        int seasonYear,
        int index,
        string purpose)
    {
        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(
                $"{dynastyId:N}|{seasonYear}|{index}|{purpose}"));

        return BitConverter.ToInt32(bytes, 0) &
               int.MaxValue;
    }

    private static Guid CreateId(
        Guid dynastyId,
        int seasonYear,
        int index,
        string source)
    {
        var bytes = MD5.HashData(
            Encoding.UTF8.GetBytes(
                $"{dynastyId:N}|{seasonYear}|{index}|{source}"));

        return new Guid(bytes);
    }

    private sealed record NamePool(
        IReadOnlyList<string> FirstNames,
        IReadOnlyList<string> LastNames);
}
