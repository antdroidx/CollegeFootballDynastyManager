using System.Security.Cryptography;
using System.Text;
using DynastyManager.Core.Models;

namespace DynastyManager.Data.Import;

public static class LegacyDynastyRosterFactory
{
    public const int LegacyRosterSeasonYear = 2026;

    private static readonly string[] FallbackFirstNames =
    {
        "Alex", "Jordan", "Chris", "Cameron", "Drew", "Taylor",
        "Marcus", "Jalen", "Ryan", "Tyler", "Devin", "Malik"
    };

    private static readonly string[] FallbackLastNames =
    {
        "Smith", "Johnson", "Williams", "Brown", "Davis", "Wilson",
        "Moore", "Taylor", "Anderson", "Thomas", "Jackson", "White"
    };

    public static IReadOnlyList<DynastyPlayer> Create(
        IEnumerable<ImportedPlayerRow> rows,
        Guid dynastyId,
        int seasonYear = LegacyRosterSeasonYear) =>
        Create(
            rows,
            Array.Empty<Team>(),
            dynastyId,
            seasonYear);

    public static IReadOnlyList<DynastyPlayer> Create(
        IEnumerable<ImportedPlayerRow> rows,
        IEnumerable<Team> teams,
        Guid dynastyId,
        int seasonYear = LegacyRosterSeasonYear)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(teams);

        var rowArray = rows.ToArray();
        var teamArray = teams.ToArray();

        var seasonOffset = Math.Max(
            0,
            seasonYear - LegacyRosterSeasonYear);

        var namePool = BuildNamePool(rowArray);
        var playersByTeam = new Dictionary<string, List<DynastyPlayer>>(
            StringComparer.OrdinalIgnoreCase);

        var index = 0;

        foreach (var row in rowArray)
        {
            var classYear = row.Year + seasonOffset;
            if (classYear > 4)
            {
                index++;
                continue;
            }

            if (!playersByTeam.TryGetValue(row.TeamName, out var teamPlayers))
            {
                teamPlayers = new List<DynastyPlayer>();
                playersByTeam[row.TeamName] = teamPlayers;
            }

            teamPlayers.Add(CreateImportedPlayer(
                dynastyId,
                row,
                index,
                classYear));

            index++;
        }

        foreach (var team in teamArray)
        {
            if (!playersByTeam.TryGetValue(team.Name, out var teamPlayers))
            {
                teamPlayers = new List<DynastyPlayer>();
                playersByTeam[team.Name] = teamPlayers;
            }

            if (teamPlayers.Count > DynastyRosterRules.MaximumRosterSize)
            {
                teamPlayers = teamPlayers
                    .OrderByDescending(player => player.OverallRating)
                    .ThenBy(player => player.ClassYear)
                    .ThenBy(player => player.FullName, StringComparer.OrdinalIgnoreCase)
                    .Take(DynastyRosterRules.MaximumRosterSize)
                    .ToList();

                playersByTeam[team.Name] = teamPlayers;
            }

            FillToTargetRoster(
                teamPlayers,
                team,
                dynastyId,
                seasonYear,
                namePool);
        }

        if (teamArray.Length == 0)
        {
            return playersByTeam.Values
                .SelectMany(teamPlayers => teamPlayers)
                .ToArray();
        }

        return teamArray
            .SelectMany(team => playersByTeam[team.Name])
            .ToArray();
    }

    private static DynastyPlayer CreateImportedPlayer(
        Guid dynastyId,
        ImportedPlayerRow row,
        int index,
        int classYear) =>
        new()
        {
            PlayerId = CreatePlayerId(
                dynastyId,
                $"imported|{index}|{row.TeamName}|{row.FullName}|" +
                $"{row.Position}|{row.Year}|{row.LegacyTalentLevel}"),
            FullName = row.FullName,
            TeamName = row.TeamName,
            Position = row.Position,
            ClassYear = Math.Clamp(classYear, 1, 4),
            TalentLevel = row.LegacyTalentLevel,
            OverallRating = RatingFromTalent(row.LegacyTalentLevel)
        };

    private static void FillToTargetRoster(
        List<DynastyPlayer> players,
        Team team,
        Guid dynastyId,
        int seasonYear,
        NamePool namePool)
    {
        var generatedIndex = 0;

        while (players.Count < DynastyRosterRules.MaximumRosterSize)
        {
            var position = SelectPositionToFill(
                players,
                dynastyId,
                seasonYear,
                team.Name,
                generatedIndex);

            var talent = GenerateTalentLevel(
                team.Prestige,
                dynastyId,
                seasonYear,
                team.Name,
                position,
                generatedIndex);

            var classYear = 1 + DeterministicValue(
                dynastyId,
                seasonYear,
                team.Name,
                position.ToString(),
                generatedIndex,
                "class") % 4;

            var fullName = GenerateName(
                namePool,
                dynastyId,
                seasonYear,
                team.Name,
                position,
                generatedIndex);

            players.Add(new DynastyPlayer
            {
                PlayerId = CreatePlayerId(
                    dynastyId,
                    $"generated|{seasonYear}|{team.Name}|{position}|" +
                    $"{generatedIndex}|{fullName}"),
                FullName = fullName,
                TeamName = team.Name,
                Position = position,
                ClassYear = classYear,
                TalentLevel = talent,
                OverallRating = RatingFromTalent(talent)
            });

            generatedIndex++;
        }
    }

    private static Position SelectPositionToFill(
        IReadOnlyCollection<DynastyPlayer> players,
        Guid dynastyId,
        int seasonYear,
        string teamName,
        int generatedIndex)
    {
        var counts = players
            .GroupBy(player => player.Position)
            .ToDictionary(
                group => group.Key,
                group => group.Count());

        var candidates = DynastyRosterRules.TargetPositionCounts
            .Select(pair => new
            {
                Position = pair.Key,
                Deficit =
                    pair.Value -
                    (counts.TryGetValue(pair.Key, out var count) ? count : 0),
                Tie = DeterministicValue(
                    dynastyId,
                    seasonYear,
                    teamName,
                    pair.Key.ToString(),
                    generatedIndex,
                    "position")
            })
            .OrderByDescending(item => item.Deficit)
            .ThenBy(item => item.Tie)
            .ToArray();

        var positive = candidates.FirstOrDefault(item => item.Deficit > 0);

        return positive?.Position ??
               candidates[generatedIndex % candidates.Length].Position;
    }

    private static int GenerateTalentLevel(
        int prestige,
        Guid dynastyId,
        int seasonYear,
        string teamName,
        Position position,
        int generatedIndex)
    {
        var baseline = Math.Clamp(
            (int)Math.Round(
                prestige / 10.0,
                MidpointRounding.AwayFromZero),
            1,
            10);

        var roll = DeterministicValue(
            dynastyId,
            seasonYear,
            teamName,
            position.ToString(),
            generatedIndex,
            "talent") % 100;

        var talent = roll switch
        {
            < 15 => baseline - 2,
            >= 85 => baseline + 2,
            _ => baseline
        };

        return Math.Clamp(talent, 1, 10);
    }

    private static string GenerateName(
        NamePool pool,
        Guid dynastyId,
        int seasonYear,
        string teamName,
        Position position,
        int generatedIndex)
    {
        var firstIndex = DeterministicValue(
            dynastyId,
            seasonYear,
            teamName,
            position.ToString(),
            generatedIndex,
            "first-name") % pool.FirstNames.Count;

        var lastIndex = DeterministicValue(
            dynastyId,
            seasonYear,
            teamName,
            position.ToString(),
            generatedIndex,
            "last-name") % pool.LastNames.Count;

        return $"{pool.FirstNames[firstIndex]} {pool.LastNames[lastIndex]}";
    }

    private static NamePool BuildNamePool(
        IEnumerable<ImportedPlayerRow> rows)
    {
        var firstNames = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);
        var lastNames = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var parts = row.FullName
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries);

            if (parts.Length == 0)
                continue;

            firstNames.Add(parts[0]);

            if (parts.Length > 1)
                lastNames.Add(parts[^1]);
        }

        return new NamePool(
            firstNames.Count > 0
                ? firstNames.OrderBy(name => name).ToArray()
                : FallbackFirstNames,
            lastNames.Count > 0
                ? lastNames.OrderBy(name => name).ToArray()
                : FallbackLastNames);
    }

    private static int RatingFromTalent(int talent) =>
        Math.Clamp(
            (int)Math.Round(
                50.0 + talent * 4.5,
                MidpointRounding.AwayFromZero),
            50,
            99);

    private static int DeterministicValue(
        Guid dynastyId,
        int seasonYear,
        string teamName,
        string position,
        int generatedIndex,
        string purpose)
    {
        var source =
            $"{dynastyId:N}|{seasonYear}|{teamName}|{position}|" +
            $"{generatedIndex}|{purpose}";

        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes(source));

        return BitConverter.ToInt32(hash, 0) &
               int.MaxValue;
    }

    private static Guid CreatePlayerId(
        Guid dynastyId,
        string source)
    {
        var bytes = MD5.HashData(
            Encoding.UTF8.GetBytes(
                $"{dynastyId:N}|{source}"));

        return new Guid(bytes);
    }

    private sealed record NamePool(
        IReadOnlyList<string> FirstNames,
        IReadOnlyList<string> LastNames);
}
