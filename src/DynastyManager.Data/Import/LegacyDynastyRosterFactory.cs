using System.Security.Cryptography;
using System.Text;
using DynastyManager.Core.Models;

namespace DynastyManager.Data.Import;

public static class LegacyDynastyRosterFactory
{
    public const int LegacyRosterSeasonYear = 2026;

    public static IReadOnlyList<DynastyPlayer> Create(
        IEnumerable<ImportedPlayerRow> rows,
        Guid dynastyId,
        int seasonYear = LegacyRosterSeasonYear)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var seasonOffset = Math.Max(
            0,
            seasonYear - LegacyRosterSeasonYear);

        var players = new List<DynastyPlayer>();
        var index = 0;

        foreach (var row in rows)
        {
            var classYear = row.Year + seasonOffset;
            if (classYear > 4)
            {
                index++;
                continue;
            }

            players.Add(new DynastyPlayer
            {
                PlayerId = CreatePlayerId(
                    dynastyId,
                    row,
                    index),
                FullName = row.FullName,
                TeamName = row.TeamName,
                Position = row.Position,
                ClassYear = Math.Clamp(classYear, 1, 4),
                TalentLevel = row.LegacyTalentLevel,
                OverallRating = Math.Clamp(
                    (int)Math.Round(
                        50.0 + row.LegacyTalentLevel * 4.5,
                        MidpointRounding.AwayFromZero),
                    50,
                    99)
            });

            index++;
        }

        return players;
    }

    private static Guid CreatePlayerId(
        Guid dynastyId,
        ImportedPlayerRow row,
        int index)
    {
        var source =
            $"{dynastyId:N}|{index}|{row.TeamName}|{row.FullName}|" +
            $"{row.Position}|{row.Year}|{row.LegacyTalentLevel}";

        var bytes = MD5.HashData(
            Encoding.UTF8.GetBytes(source));

        return new Guid(bytes);
    }
}
