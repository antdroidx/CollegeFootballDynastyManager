using System.Security.Cryptography;
using System.Text;
using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

/// <summary>
/// Final roster safety net. Mirrors the legacy recruitWalkOns behavior:
/// after recruiting, any remaining roster/position shortages are filled by
/// low-rated freshmen walk-ons so every program can enter the next season
/// with a complete 85-player roster.
/// </summary>
public static class WalkOnRosterService
{
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

    public static DynastyState FillAllTeams(
        DynastyState state,
        IReadOnlyDictionary<string, Team> teamsByName)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(teamsByName);

        var roster = state.ActiveRoster.ToList();
        var namePool = BuildNamePool(roster);

        foreach (var team in teamsByName.Values
                     .OrderBy(team => team.Name, StringComparer.OrdinalIgnoreCase))
        {
            var generatedIndex = 0;
            void AddWalkOn(Position position)
            {
                var talentLevel =
                    1 + DeterministicValue(
                        state,
                        team.Name,
                        position,
                        generatedIndex,
                        "talent") % 2;

                var overall =
                    53 +
                    talentLevel * 4 +
                    team.Prestige / 25 +
                    DeterministicValue(
                        state,
                        team.Name,
                        position,
                        generatedIndex,
                        "overall") % 4;

                var firstName = namePool.FirstNames[
                    DeterministicValue(
                        state,
                        team.Name,
                        position,
                        generatedIndex,
                        "first") %
                    namePool.FirstNames.Count];

                var lastName = namePool.LastNames[
                    DeterministicValue(
                        state,
                        team.Name,
                        position,
                        generatedIndex,
                        "last") %
                    namePool.LastNames.Count];

                var fullName = $"{firstName} {lastName}";

                roster.Add(new DynastyPlayer
                {
                    PlayerId = CreatePlayerId(
                        state,
                        team.Name,
                        position,
                        generatedIndex,
                        fullName),
                    FullName = fullName,
                    TeamName = team.Name,
                    Position = position,
                    ClassYear = 1,
                    TalentLevel = talentLevel,
                    OverallRating = Math.Clamp(overall, 50, 67),
                    PotentialRating = Math.Clamp(overall + 4, 54, 72),
                    IsWalkOn = true
                });

                generatedIndex++;
            }

            foreach (var need in DynastyRosterRules.StarterPositionCounts)
            {
                while (CountActivePosition(roster, team.Name, need.Key) < need.Value)
                {
                    if (CountTeamRoster(roster, team.Name) >= DynastyRosterRules.MaximumRosterSize)
                    {
                        var activeCounts = roster.Where(player => player.TeamName.Equals(team.Name,
                                StringComparison.OrdinalIgnoreCase) && !player.IsRedshirted)
                            .GroupBy(player => player.Position).ToDictionary(group => group.Key, group => group.Count());
                        var expendable = roster.Where(player => player.TeamName.Equals(team.Name,
                                StringComparison.OrdinalIgnoreCase) && player.Position != need.Key &&
                            activeCounts.GetValueOrDefault(player.Position) >
                                DynastyRosterRules.StarterPositionCounts.GetValueOrDefault(player.Position, 1))
                            .OrderBy(player => player.IsWalkOn ? 0 : 1)
                            .ThenBy(player => player.OverallRating).ThenBy(player => player.PotentialRating).FirstOrDefault();
                        if (expendable is null) break;
                        roster.Remove(expendable);
                    }
                    AddWalkOn(need.Key);
                }
            }

            while (CountTeamRoster(roster, team.Name) < DynastyRosterRules.MaximumRosterSize)
                AddWalkOn(SelectMostNeededPosition(roster, team.Name));
        }

        var retainedIds = roster.Select(p => p.PlayerId).ToHashSet();
        var released = state.ActiveRoster.Where(p => !retainedIds.Contains(p.PlayerId)).Select(p => new PlayerDepartureRecord
        {
            SeasonYear = state.SeasonYear, PlayerId = p.PlayerId, FullName = p.FullName,
            TeamName = p.TeamName, Position = p.Position, OverallRating = p.OverallRating, Reason = PlayerDepartureReason.Released
        }).ToArray();
        return PlayerRatingService.EnsureProfiles(state with
        {
            ActiveRoster = roster,
            RecentPlayerDepartures = state.RecentPlayerDepartures.Concat(released).DistinctBy(p => (p.SeasonYear, p.PlayerId, p.Reason)).ToArray(),
            PlayerDepartureHistory = state.PlayerDepartureHistory.Concat(released).DistinctBy(p => (p.SeasonYear, p.PlayerId, p.Reason)).ToArray()
        });
    }

    private static Position SelectMostNeededPosition(
        IReadOnlyCollection<DynastyPlayer> roster,
        string teamName)
    {
        var counts = roster
            .Where(player =>
                player.TeamName.Equals(
                    teamName,
                    StringComparison.OrdinalIgnoreCase) &&
                !player.IsRedshirted)
            .GroupBy(player => player.Position)
            .ToDictionary(
                group => group.Key,
                group => group.Count());

        return DynastyRosterRules.TargetPositionCounts
            .Select(pair => new
            {
                Position = pair.Key,
                Deficit = pair.Value -
                    (counts.TryGetValue(
                        pair.Key,
                        out var count)
                        ? count
                        : 0)
            })
            .OrderByDescending(item => item.Deficit)
            .ThenBy(item => item.Position)
            .First()
            .Position;
    }

    private static int CountTeamRoster(
        IEnumerable<DynastyPlayer> roster,
        string teamName) =>
        roster.Count(player => player.TeamName.Equals(
            teamName,
            StringComparison.OrdinalIgnoreCase));

    private static int CountActivePosition(IEnumerable<DynastyPlayer> roster, string teamName, Position position) =>
        roster.Count(player => player.TeamName.Equals(teamName, StringComparison.OrdinalIgnoreCase) &&
            player.Position == position && !player.IsRedshirted);

    private static NamePool BuildNamePool(
        IEnumerable<DynastyPlayer> players)
    {
        var firstNames = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);
        var lastNames = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var player in players)
        {
            var parts = player.FullName.Split(
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

    private static int DeterministicValue(
        DynastyState state,
        string teamName,
        Position position,
        int generatedIndex,
        string purpose) =>
        SimulationSeed.Create(
            state.DynastyId,
            state.SeasonYear,
            generatedIndex,
            $"{teamName}|{position}",
            $"walkon-{purpose}");

    private static Guid CreatePlayerId(
        DynastyState state,
        string teamName,
        Position position,
        int generatedIndex,
        string fullName)
    {
        var bytes = MD5.HashData(
            Encoding.UTF8.GetBytes(
                $"{state.DynastyId:N}|{state.SeasonYear}|{teamName}|" +
                $"{position}|{generatedIndex}|{fullName}|walkon"));

        return new Guid(bytes);
    }

    private sealed record NamePool(
        IReadOnlyList<string> FirstNames,
        IReadOnlyList<string> LastNames);
}
