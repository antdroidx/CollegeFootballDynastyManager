using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

/// <summary>Selects an eligible eleven for each snap and maintains in-game energy.</summary>
public static class PersonnelRotationService
{
    public static SnapPersonnel SelectOffense(IReadOnlyList<DynastyPlayer> roster, string teamName,
        OffensivePersonnel package, GameFatigueState fatigue, Random random) =>
        Select(BuildDepthChart(roster, teamName), teamName, OffensiveNeeds(package), fatigue, random);

    public static SnapPersonnel SelectDefense(IReadOnlyList<DynastyPlayer> roster, string teamName,
        DefensivePersonnel package, GameFatigueState fatigue, Random random) =>
        Select(BuildDepthChart(roster, teamName), teamName, DefensiveNeeds(package), fatigue, random);

    public static SnapPersonnel SelectOffense(
        IReadOnlyDictionary<Position, IReadOnlyList<DynastyPlayer>> depthChart, string teamName,
        OffensivePersonnel package, GameFatigueState fatigue, Random random) =>
        Select(depthChart, teamName, OffensiveNeeds(package), fatigue, random);

    public static SnapPersonnel SelectDefense(
        IReadOnlyDictionary<Position, IReadOnlyList<DynastyPlayer>> depthChart, string teamName,
        DefensivePersonnel package, GameFatigueState fatigue, Random random) =>
        Select(depthChart, teamName, DefensiveNeeds(package), fatigue, random);

    public static IReadOnlyDictionary<Position, IReadOnlyList<DynastyPlayer>> BuildDepthChart(
        IReadOnlyList<DynastyPlayer> roster, string teamName) => roster
        .Where(player => player.TeamName.Equals(teamName, StringComparison.OrdinalIgnoreCase) &&
            !player.IsRedshirted && player.CurrentInjury is null)
        .GroupBy(player => player.Position)
        .ToDictionary(group => group.Key, group => (IReadOnlyList<DynastyPlayer>)group
            .OrderBy(player => player.DepthChartOrder <= 0 ? int.MaxValue : player.DepthChartOrder)
            .ThenByDescending(player => player.OverallRating).ToArray());

    public static void ApplySnap(GameFatigueState fatigue, IReadOnlyList<DynastyPlayer> roster,
        SnapPersonnel active, bool longPlay)
    {
        var activeIds = active.PlayerIds.ToHashSet();
        foreach (var player in roster)
        {
            var energy = fatigue.Energy(player.PlayerId);
            if (!activeIds.Contains(player.PlayerId))
            {
                fatigue.Set(player.PlayerId, energy + 2.5);
                continue;
            }

            var load = player.Position switch
            {
                Position.RB or Position.FB => 4.2, Position.WR or Position.TE => 3.5,
                Position.DE or Position.DT => 3.8, Position.OLB or Position.MLB => 2.7,
                Position.CB or Position.FS or Position.SS => 2.4,
                Position.OL => 1.25, Position.QB => 1.0, _ => 1.5
            };
            var stamina = player.InjuryRating;
            var durabilityFactor = 1.25 - Math.Clamp((stamina + player.InjuryRating) / 200d, .35, 1);
            fatigue.Set(player.PlayerId, energy - load * (1 + durabilityFactor) * (longPlay ? 1.3 : 1));
        }
    }

    private static SnapPersonnel Select(IReadOnlyDictionary<Position, IReadOnlyList<DynastyPlayer>> depthChart,
        string teamName,
        IReadOnlyList<(Position Position, int Count)> needs, GameFatigueState fatigue, Random random)
    {
        var selected = new List<Guid>(11);
        var substitutes = new List<Guid>();
        foreach (var (position, count) in needs)
        {
            var pool = depthChart.TryGetValue(position, out var players)
                ? players : Array.Empty<DynastyPlayer>();
            if (position == Position.FB && pool.Count == 0)
                pool = depthChart.Values.SelectMany(p => p).Where(p => p.Position is Position.TE or Position.RB && !selected.Contains(p.PlayerId))
                    .OrderBy(p => p.Position == Position.TE ? 0 : 1).ThenByDescending(p => DepthChartService.PositionalRating(p, 2)).ToArray();
            pool = pool.Where(p => !selected.Contains(p.PlayerId)).ToArray();
            if (position == Position.OL)
            {
                var starters = OffensiveLineService.Lineup(pool);
                foreach (var starter in starters)
                {
                    var choice = starter;
                    if (ShouldRotate(starter, fatigue.Energy(starter.PlayerId), random))
                        choice = pool.FirstOrDefault(p => starters.All(s => s.PlayerId != p.PlayerId) && !selected.Contains(p.PlayerId) &&
                            OffensiveLineService.Label(p) == OffensiveLineService.Label(starter)) ?? starter;
                    selected.Add(choice.PlayerId);
                    if (choice.PlayerId != starter.PlayerId) substitutes.Add(choice.PlayerId);
                }
                continue;
            }
            for (var slot = 0; slot < Math.Min(count, pool.Count); slot++)
            {
                var starter = pool[Math.Min(slot, pool.Count - 1)];
                var choice = starter;
                var backups = pool.Skip(count).Where(p => !selected.Contains(p.PlayerId)).ToArray();
                if (backups.Length > 0 && ShouldRotate(starter, fatigue.Energy(starter.PlayerId), random, backups.Max(p => p.OverallRating)))
                {
                    // Rotate the whole reserve group, favoring quality/depth and rested players.
                    var weighted = backups.Take(4).Select((p,index)=>(Player:p,Weight:Math.Max(.05,fatigue.Energy(p.PlayerId)/100d)/(1+index*.85)/(1+Math.Max(0,starter.OverallRating-p.OverallRating)/15d))).ToArray();
                    var roll=random.NextDouble()*weighted.Sum(x=>x.Weight);
                    choice=weighted[^1].Player;
                    foreach(var entry in weighted){roll-=entry.Weight;if(roll<=0){choice=entry.Player;break;}}
                }
                selected.Add(choice.PlayerId);
                if (choice.PlayerId != starter.PlayerId) substitutes.Add(choice.PlayerId);
            }
        }
        // Thin/injured depth must change the package, never put only nine or ten on the field.
        var offense=needs.Any(n=>n.Position==Position.QB);
        var unit=depthChart.Values.SelectMany(p=>p).Where(p=>offense
            ? p.Position is Position.QB or Position.RB or Position.FB or Position.WR or Position.TE or Position.OL
            : p.Position is Position.DE or Position.DT or Position.OLB or Position.MLB or Position.CB or Position.FS or Position.SS);
        foreach(var player in unit.Where(p=>!selected.Contains(p.PlayerId)).OrderBy(p=>p.Position==Position.QB?1:0).ThenByDescending(p=>p.OverallRating+fatigue.Energy(p.PlayerId)*.05))
        {
            if(selected.Count>=11)break;
            selected.Add(player.PlayerId);substitutes.Add(player.PlayerId);
        }
        return new SnapPersonnel { TeamName = teamName, PlayerIds = selected, SubstitutedPlayerIds = substitutes };
    }

    private static bool ShouldRotate(DynastyPlayer player, double energy, Random random, int? backupOverall = null)
    {
        if (energy < EmergencyThreshold(player.Position)) return true;
        if(player.Position is Position.QB or Position.OL)return false;
        var baseChance = player.Position switch
        {
            Position.FB => .15, Position.RB => .34, Position.WR => .22, Position.TE => .15,
            Position.DE or Position.DT => .32,
            Position.OLB or Position.MLB => .07,
            Position.CB or Position.FS or Position.SS => .05,
            Position.QB => .01, Position.OL => 0, _ => .02
        };
        var fatigueChance = Math.Max(0, 75 - energy) / 90;
        if(backupOverall is int backup && player.Position is Position.RB or Position.FB or Position.WR or Position.TE)baseChance=Math.Max(.08,baseChance-Math.Max(0,player.OverallRating-backup-3)*.008);
        return random.NextDouble() < Math.Clamp(baseChance + fatigueChance, 0, .9);
    }

    private static double EmergencyThreshold(Position position) => position switch
    {
        Position.OL => 12, Position.QB => 15,
        Position.OLB or Position.MLB or Position.CB or Position.FS or Position.SS => 24,
        Position.DE or Position.DT => 36,
        _ => 32
    };

    private static (Position, int)[] OffensiveNeeds(OffensivePersonnel package) => package switch
    {
        OffensivePersonnel.TripleBack => [(Position.QB, 1), (Position.FB, 1), (Position.RB, 2), (Position.WR, 2), (Position.OL, 5)],
        OffensivePersonnel.Ten => [(Position.QB, 1), (Position.RB, 1), (Position.WR, 4), (Position.OL, 5)],
        OffensivePersonnel.Twelve => [(Position.QB, 1), (Position.RB, 1), (Position.WR, 2), (Position.TE, 2), (Position.OL, 5)],
        OffensivePersonnel.TwentyOne => [(Position.QB, 1), (Position.FB, 1), (Position.RB, 1), (Position.WR, 2), (Position.TE, 1), (Position.OL, 5)],
        OffensivePersonnel.Empty => [(Position.QB, 1), (Position.WR, 5), (Position.OL, 5)],
        _ => [(Position.QB, 1), (Position.RB, 1), (Position.WR, 3), (Position.TE, 1), (Position.OL, 5)]
    };

    public static IReadOnlyList<Position> DefensivePositions(DefensivePersonnel package) => DefensiveNeeds(package).SelectMany(p => Enumerable.Repeat(p.Item1, p.Item2)).ToArray();

    private static (Position, int)[] DefensiveNeeds(DefensivePersonnel package) => package switch
    {
        DefensivePersonnel.BaseThreeFour => [(Position.DE, 2), (Position.DT, 1), (Position.OLB, 2), (Position.MLB, 2), (Position.CB, 2), (Position.FS, 1), (Position.SS, 1)],
        DefensivePersonnel.Nickel => [(Position.DE, 2), (Position.DT, 2), (Position.OLB, 1), (Position.MLB, 1), (Position.CB, 3), (Position.FS, 1), (Position.SS, 1)],
        DefensivePersonnel.Dime => [(Position.DE, 2), (Position.DT, 1), (Position.MLB, 1), (Position.CB, 4), (Position.FS, 2), (Position.SS, 1)],
        DefensivePersonnel.GoalLine => [(Position.DE, 2), (Position.DT, 3), (Position.OLB, 2), (Position.MLB, 2), (Position.CB, 2)],
        _ => [(Position.DE, 2), (Position.DT, 2), (Position.OLB, 2), (Position.MLB, 1), (Position.CB, 2), (Position.FS, 1), (Position.SS, 1)]
    };
}
