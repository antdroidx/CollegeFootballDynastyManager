using DynastyManager.Core.Models;

namespace DynastyManager.Data.Import;

/// <summary>2026 football membership; existing names remain stable for roster/save identity.</summary>
public static class ModernFbsUniverseFactory
{
    public static IReadOnlyList<Team> Create(IEnumerable<Team> legacyTeams)
    {
        var teams = legacyTeams.ToDictionary(team => team.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var (name, abbreviation, prestige, region) in new[]
        {
            ("James Madison", "JMU", 55, 3), ("Jacksonville State", "JVST", 45, 4),
            ("Kennesaw State", "KENN", 35, 4), ("Sam Houston", "SHSU", 40, 4),
            ("Delaware", "DEL", 40, 3), ("Missouri State", "MOST", 40, 2),
            ("North Dakota State", "NDSU", 55, 1), ("Sacramento State", "SAC", 40, 0)
        })
            teams[name] = new Team { Name = name, Abbreviation = abbreviation, Prestige = prestige,
                ConferenceName = "", LegacyRegionId = region };

        var membership = new Dictionary<string, string[]>
        {
            ["ACC"] = ["Boston College", "California", "Clemson", "Duke", "Florida State", "Georgia Tech", "Louisville", "Miami", "North Carolina", "NC State", "Pittsburgh", "Southern Methodist", "Stanford", "Syracuse", "Virginia", "Virginia Tech", "Wake Forest"],
            ["Big Ten"] = ["Illinois", "Indiana", "Iowa", "Maryland", "Michigan", "Michigan State", "Minnesota", "Nebraska", "Northwestern", "Ohio State", "Oregon", "Penn State", "Purdue", "Rutgers", "UCLA", "USC", "Washington", "Wisconsin"],
            ["Big 12"] = ["Arizona", "Arizona State", "Baylor", "BYU", "Central Florida", "Cincinnati", "Colorado", "Houston", "Iowa State", "Kansas", "Kansas State", "Oklahoma State", "TCU", "Texas Tech", "Utah", "West Virginia"],
            ["SEC"] = ["Alabama", "Arkansas", "Auburn", "Florida", "Georgia", "Kentucky", "LSU", "Mississippi State", "Missouri", "Oklahoma", "Ole Miss", "South Carolina", "Tennessee", "Texas", "Texas AM", "Vanderbilt"],
            ["American"] = ["Army", "Charlotte", "East Carolina", "Florida Atl", "Memphis", "Navy", "North Texas", "Rice", "South Florida", "Temple", "Tulane", "Tulsa", "UAB", "UT San Antonio"],
            ["Conf USA"] = ["Delaware", "Florida Intl", "Jacksonville State", "Kennesaw State", "Liberty", "Mid Tenn State", "Missouri State", "New Mexico St", "Sam Houston", "Western Kentucky"],
            ["MAC"] = ["Akron", "Ball State", "Bowling Green", "Buffalo", "Central Michigan", "Eastern Michigan", "Kent State", "Miami OH", "Ohio", "Sacramento State", "Toledo", "UMass", "Western Michigan"],
            ["Mt West"] = ["Air Force", "Hawaii", "Nevada", "New Mexico", "North Dakota State", "Northern Illinois", "San Jose State", "UNLV", "UT El Paso", "Wyoming"],
            ["Pac-12"] = ["Boise State", "Colorado State", "Fresno State", "Oregon State", "San Diego State", "Texas State", "Utah State", "Washington State"],
            ["Sun Belt"] = ["Appalachian State", "Arkansas State", "Coastal Carolina", "Georgia Southern", "Georgia State", "James Madison", "LA Lafayette", "LA Monroe", "LA Tech", "Marshall", "Old Dominion", "South Alabama", "Southern Miss", "Troy"],
            ["Independent"] = ["Notre Dame", "UConn"]
        };
        return membership.SelectMany(conference => conference.Value.Select(name =>
            teams[name] with { ConferenceName = conference.Key,
                ConferenceGamesPerSeason = conference.Key switch
                {
                    "Independent" => 0, "Pac-12" => 7,
                    "Big Ten" or "Big 12" or "SEC" => 9,
                    "ACC" => new[] { "Boston College", "Clemson", "Florida State", "Georgia Tech", "North Carolina" }.Contains(name) ? 8 : 9,
                    _ => 8
                },
                DivisionName = conference.Key == "Sun Belt"
                    ? (new[] { "Appalachian State", "Coastal Carolina", "Georgia Southern", "Georgia State", "James Madison", "Marshall", "Old Dominion" }.Contains(name) ? "East" : "West")
                    : null }))
            .OrderBy(team => team.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
