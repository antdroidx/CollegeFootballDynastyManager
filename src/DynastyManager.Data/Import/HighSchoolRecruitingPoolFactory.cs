using System.Security.Cryptography;
using System.Text;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;

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

            var homeState =
                Value(dynastyId, seasonYear, index, "state") % 50;

            recruits[index] = new HighSchoolRecruit
            {
                RecruitId = CreateId(
                    dynastyId,
                    seasonYear,
                    index,
                    $"{first}|{last}|{position}"),
                SeasonYear = seasonYear,
                FullName = $"{first} {last}",
                Position = position,
                StarRating = stars,
                TrueOverallRating = overall,
                PotentialRating = Math.Clamp(
                    Math.Max(overall + 2, 58 + stars * 6 +
                    Value(dynastyId, seasonYear, index, "potential") % 17 - 8),
                    overall,
                    99),
                HomeState = homeState,
                HomeRegion = RecruitGeography.GetRegionForState(homeState)
            };
            var measurements = PlayerPhysicalService.Generate(recruits[index].RecruitId, position);
            recruits[index] = recruits[index] with { HeightInches = measurements.Height, WeightPounds = measurements.Weight };
            var recruit = recruits[index];
            var profile = PlayerRatingService.EnsureInitialized(new DynastyState
                { DynastyId = dynastyId, DynastyName = "Recruit generation", SeasonYear = seasonYear, UserTeamName = "" },
                new DynastyPlayer
                {
                    PlayerId = recruit.RecruitId, FullName = recruit.FullName, TeamName = "", Position = position,
                    ClassYear = 1, OverallRating = overall, PotentialRating = recruit.PotentialRating,
                    OriginalRecruitingStars = stars, TalentLevel = stars * 2,
                    HeightInches = measurements.Height, WeightPounds = measurements.Weight,
                    NcaaPositionCode = GeneratedPositionCode(position, index)
                });
            recruits[index] = recruit with { AttributeProfile = profile, TrueOverallRating = profile.OverallRating, PotentialRating = profile.PotentialRating };
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
            5 => (76, 90),
            4 => (75, 85),
            3 => (68, 79),
            2 => (59, 73),
            _ => (50, 65)
        };

        return minimum +
               Value(dynastyId, seasonYear, index, "overall") %
               (maximum - minimum + 1);
    }

    private static int GeneratedPositionCode(Position position, int index) => position switch
    {
        Position.RB => index % 8 == 0 ? 2 : 1,
        Position.OL => 5 + index % 5,
        Position.DE => 10 + index % 2,
        Position.OLB => index % 2 == 0 ? 13 : 15,
        Position.K => 19 + index % 2,
        _ => Ncaa06RatingService.PositionCode(new DynastyPlayer { FullName = "", TeamName = "", Position = position })
    };

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
