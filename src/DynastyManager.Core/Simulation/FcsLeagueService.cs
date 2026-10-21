using System.Security.Cryptography;
using System.Text;
using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class FcsLeagueService
{
    // Historical opponent library for the bundled 2025-era roster. Membership is a dynasty snapshot.
    private static readonly (string Conference, string Names)[] Schools =
    [
        ("Big Sky", "Cal Poly|Eastern Washington|Idaho|Idaho State|Montana|Montana State|Northern Arizona|Northern Colorado|Portland State|Sacramento State|UC Davis|Weber State"),
        ("Big South–OVC", "Charleston Southern|Eastern Illinois|Gardner-Webb|Lindenwood|Southeast Missouri State|Tennessee State|Tennessee Tech|UT Martin|Western Illinois"),
        ("CAA", "Albany|Bryant|Campbell|Elon|Hampton|Maine|Monmouth|New Hampshire|North Carolina A&T|Rhode Island|Stony Brook|Towson|Villanova|William & Mary"),
        ("FCS Independent", "Merrimack|Sacred Heart"),
        ("Ivy", "Brown|Columbia|Cornell|Dartmouth|Harvard|Penn|Princeton|Yale"),
        ("MEAC", "Delaware State|Howard|Morgan State|Norfolk State|North Carolina Central|South Carolina State"),
        ("Missouri Valley", "Illinois State|Indiana State|Murray State|North Dakota|North Dakota State|Northern Iowa|South Dakota|South Dakota State|Southern Illinois|Youngstown State"),
        ("NEC", "Central Connecticut|Duquesne|LIU|Mercyhurst|New Haven|Robert Morris|Saint Francis|Stonehill|Wagner"),
        ("Patriot", "Bucknell|Colgate|Fordham|Georgetown|Holy Cross|Lafayette|Lehigh|Richmond"),
        ("Pioneer", "Butler|Davidson|Dayton|Drake|Marist|Morehead State|Presbyterian|San Diego|St. Thomas|Stetson|Valparaiso"),
        ("Southern", "Chattanooga|East Tennessee State|Furman|Mercer|Samford|The Citadel|VMI|Western Carolina|Wofford"),
        ("Southland", "East Texas A&M|Houston Christian|Incarnate Word|Lamar|McNeese|Nicholls|Northwestern State|Southeastern Louisiana|Stephen F. Austin|UTRGV"),
        ("SWAC", "Alabama A&M|Alabama State|Alcorn State|Arkansas–Pine Bluff|Bethune-Cookman|Florida A&M|Grambling State|Jackson State|Mississippi Valley State|Prairie View A&M|Southern|Texas Southern"),
        ("UAC", "Abilene Christian|Austin Peay|Central Arkansas|Eastern Kentucky|North Alabama|Southern Utah|Tarleton State|Utah Tech|West Georgia")
    ];
    public static IReadOnlyList<Team> Catalog { get; } = Schools.SelectMany(group => group.Names.Split('|').Select(name => new Team
    {
        Id = StableId(name), Name = name, Abbreviation = new string(name.Where(char.IsLetter).Take(4).ToArray()).ToUpperInvariant(),
        ConferenceName = group.Conference, Prestige = name is "North Dakota State" or "South Dakota State" or "Montana" or "Montana State" ? 42 : 25,
        ConferenceGamesPerSeason = null
    })).ToArray();

    public static string Identity(string name)
    {
        var value = new string(name.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        return value switch { "ndsu" => "northdakotastate", "sdst" or "sdsu" => "southdakotastate", "ucd" => "ucdavis", "northernariz" => "northernarizona", "williamandmary" => "williammary", "uialbany" or "ualbany" => "albany", _ => value };
    }
    public static IReadOnlyList<Team> Available(IEnumerable<Team> league)
    {
        var names = league.Select(t => Identity(t.Name)).ToHashSet();
        return Catalog.Where(t => !names.Contains(Identity(t.Name))).ToArray();
    }
    public static Team? Find(string name) => Catalog.FirstOrDefault(t => Identity(t.Name) == Identity(name));
    private static Guid StableId(string name) => new(MD5.HashData(Encoding.UTF8.GetBytes("fcs|" + name)));

    public static IReadOnlyList<DynastyPlayer> CreateRoster(DynastyState state, Team team)
    {
        var generated = WalkOnRosterService.FillAllTeams(state with { ActiveRoster = [] }, new Dictionary<string, Team> { [team.Name] = team }).ActiveRoster;
        // Real schools; fictional athletes. Only scheduled FCS opponents allocate a roster.
        return PlayerRatingService.EnsureProfiles(state with { ActiveRoster = generated }).ActiveRoster;
    }

    public static IReadOnlyList<ScheduledGame> BuildSchedule(IReadOnlyList<Team> teams, Guid dynastyId, int year, LeagueRules rules)
    {
        if (!rules.FcsOpponents) return SeasonScheduleBuilder.BuildRegularSeason(teams, dynastyId, year, gamesPerTeam: rules.GamesPerTeam, allowAutomaticConferenceFill: rules.AllowAutomaticConferenceFill);
        var available = Available(teams).OrderBy(t => SimulationSeed.Create(dynastyId, year, 0, t.Name, "fcs-order")).ToArray();
        if (available.Length == 0) return SeasonScheduleBuilder.BuildRegularSeason(teams, dynastyId, year, gamesPerTeam: rules.GamesPerTeam, allowAutomaticConferenceFill: rules.AllowAutomaticConferenceFill);
        var odd = teams.Count * rules.GamesPerTeam % 2 != 0;
        var schedulingTeams = odd ? teams.Append(available[0] with { ConferenceName = "FCS scheduling", ConferenceGamesPerSeason = 0 }).ToArray() : teams.ToArray();
        var schedule = SeasonScheduleBuilder.BuildRegularSeason(schedulingTeams, dynastyId, year, gamesPerTeam: rules.GamesPerTeam, allowAutomaticConferenceFill: true).ToList();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var occupied = new HashSet<(string, int)>();
        if (odd)
        {
            for (var i = 0; i < schedule.Count; i++)
                if (schedule[i].InvolvesTeam(available[0].Name))
                {
                    var g = schedule[i];
                    var fbs = g.HomeTeamName == available[0].Name ? g.AwayTeamName : g.HomeTeamName;
                    schedule[i] = g with { HomeTeamName = fbs, AwayTeamName = available[0].Name, IsFcsGame = true };
                    used.Add(fbs); occupied.Add((available[0].Name, g.Week));
                }
        }
        var additions = new List<ScheduledGame>();
        for (var i = 0; i < schedule.Count; i++)
        {
            var g = schedule[i];
            if (g.IsFcsGame || g.GameType != ScheduledGameType.NonConference || used.Contains(g.HomeTeamName) || used.Contains(g.AwayTeamName)) continue;
            if (SimulationSeed.Create(dynastyId, year, g.Week, g.HomeTeamName, "fcs-chance") % 5 != 0) continue;
            var opponents = available.Where(t => !occupied.Contains((t.Name, g.Week))).Take(2).ToArray();
            if (opponents.Length < 2) continue;
            ScheduledGame Match(string host, Team visitor, string suffix) => g with { GameId = g.GameId + suffix, HomeTeamName = host, AwayTeamName = visitor.Name, IsFcsGame = true,
                SimulationSeed = SimulationSeed.Create(dynastyId, year, g.Week, host, visitor.Name) };
            schedule[i] = Match(g.HomeTeamName, opponents[0], "-F1");
            additions.Add(Match(g.AwayTeamName, opponents[1], "-F2"));
            used.Add(g.HomeTeamName); used.Add(g.AwayTeamName);
            occupied.Add((opponents[0].Name, g.Week)); occupied.Add((opponents[1].Name, g.Week));
        }
        return schedule.Concat(additions).OrderBy(g => g.Week).ThenBy(g => g.GameId).ToArray();
    }
}
