using System.Security.Cryptography;
using System.Text;
using DynastyManager.Core.Models;
using DynastyManager.Data.Csv;
using DynastyManager.Core.Simulation;

namespace DynastyManager.Data.Import;

public sealed record RealCoachSeed(int Season, string Team, StaffRole Role, string FullName, string Title, string SourceUrl);

/// <summary>Identity data only. Generated game ratings and support staff are retained.</summary>
public static class RealCoachCsvImporter
{
    public static IReadOnlyList<RealCoachSeed> Read(TextReader reader)
    {
        var header = CsvLineParser.Parse((reader.ReadLine() ?? "").TrimStart('\uFEFF'));
        string[] required = ["Season", "Team", "Role", "Name", "Title", "SourceUrl"];
        if (required.Any(c => !header.Contains(c))) throw new FormatException("Coach CSV requires Season,Team,Role,Name,Title,SourceUrl.");
        if (header.Distinct(StringComparer.OrdinalIgnoreCase).Count() != header.Count) throw new FormatException("Duplicate coach columns.");
        var indices = required.ToDictionary(c => c, c => header.ToList().IndexOf(c));
        var result = new List<RealCoachSeed>();
        string? line; var row = 1;
        while ((line = reader.ReadLine()) is not null)
        {
            row++; if (string.IsNullOrWhiteSpace(line)) continue;
            var cells = CsvLineParser.Parse(line);
            string Get(string key) => indices[key] < cells.Count ? cells[indices[key]].Trim() : "";
            var role = Get("Role") switch { "HC" => StaffRole.HeadCoach, "OC" => StaffRole.OffensiveCoordinator,
                "DC" => StaffRole.DefensiveCoordinator, "ST" => StaffRole.SpecialTeamsCoordinator,
                _ => throw new FormatException($"Coach row {row}: role must be HC, OC, DC or ST.") };
            if (!int.TryParse(Get("Season"), out var year) || year < 1900 || year > 2200 ||
                string.IsNullOrWhiteSpace(Get("Team")) || string.IsNullOrWhiteSpace(Get("Name")) ||
                !Uri.TryCreate(Get("SourceUrl"), UriKind.Absolute, out var uri) || uri.Scheme != "https")
                throw new FormatException($"Coach row {row}: invalid season, identity or HTTPS source.");
            result.Add(new(year, Get("Team"), role, Get("Name"), Get("Title"), Get("SourceUrl")));
        }
        if (result.GroupBy(r => (r.Season, r.Team, r.Role, r.FullName)).Any(g => g.Count() > 1))
            throw new FormatException("Duplicate coach identity/role rows.");
        return result;
    }

    // Explicit new-dynasty operation: never call on load or in EnsureLeagueStaff.
    public static DynastyState ApplyToNewDynasty(DynastyState state, IReadOnlyList<RealCoachSeed> records)
    {
        if (state.Schedule.Any(g => g.HasPlayed) || state.CoachSeasonHistory.Count > 0)
            throw new InvalidOperationException("Real coach seeds can only initialize an unplayed dynasty.");
        var groups = records.GroupBy(r => (r.Team, r.Role)).ToDictionary(g => g.Key, g => g.ToArray());
        var seeded = state with { Staff = state.Staff.Select(staff =>
        {
            if (!groups.TryGetValue((staff.TeamName, staff.Role), out var group)) return staff;
            var primary = group[0];
            var id = new Guid(SHA256.HashData(Encoding.UTF8.GetBytes($"coach:{primary.Season}:{primary.Team}:{primary.Role}:{primary.FullName}"))[..16]);
            return staff with { StaffId = id, FullName = primary.FullName, DataSeason = primary.Season,
                PublishedTitle = primary.Title, DataSourceUrl = primary.SourceUrl,
                IsHeadCoachRoleAssignment = primary.Title.Contains("game OC assignment", StringComparison.Ordinal),
                SharedRoleCoaches = group.Skip(1).Select(r => new SharedCoachRole(r.FullName, r.Title, r.SourceUrl)).ToArray() };
        }).ToArray() };
        return seeded with { Staff = seeded.Staff.Select(member => member.IsHeadCoachRoleAssignment
            ? StaffManagementService.GetStaff(seeded, member.TeamName, member.Role)! : member).ToArray() };
    }
}
