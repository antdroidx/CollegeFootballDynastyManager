using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class GamePlanService
{
    public static DynastyState SetPlan(DynastyState state, TeamGamePlan plan)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plan.TeamName);
        if (!Enum.IsDefined(plan.Offense) || !Enum.IsDefined(plan.Defense) || !Enum.IsDefined(plan.Approach) ||
            !Enum.IsDefined(plan.OffensiveFormation) || !Enum.IsDefined(plan.DefensiveFormation) ||
            plan.OpeningScript.Count > 12 || plan.OpeningScript.Any(call => !Enum.IsDefined(call)) ||
            plan.OpeningPlayIds.Count > 12 || plan.OpeningPlayIds.Any(id => PlaybookCatalog.OffensivePlays.All(play => play.Id != id)) ||
            plan.CustomOffensivePlayIds.Count > 17 || plan.CustomDefensivePlayIds.Count > 14 ||
            plan.CustomOffensivePlayIds.Any(id => PlaybookCatalog.OffensivePlays.All(p => p.Id != id)) ||
            plan.CustomDefensivePlayIds.Any(id => PlaybookCatalog.DefensivePlays.All(p => p.Id != id)) ||
            plan.Offense == OffensivePlaybook.Custom && plan.CustomOffensivePlayIds.Count == 0 ||
            plan.Defense == DefensivePlaybook.Custom && plan.CustomDefensivePlayIds.Count == 0)
            throw new ArgumentException("Invalid game plan.", nameof(plan));
        return state with { TeamGamePlans = state.TeamGamePlans.Where(item =>
            !item.TeamName.Equals(plan.TeamName, StringComparison.OrdinalIgnoreCase))
            .Append(plan with { OpeningScript = plan.OpeningScript.ToArray(), OpeningPlayIds = plan.OpeningPlayIds.ToArray(), CustomOffensivePlayIds = plan.CustomOffensivePlayIds.Distinct().ToArray(), CustomDefensivePlayIds = plan.CustomDefensivePlayIds.Distinct().ToArray() }).ToArray() };
    }

    public static TeamGamePlan GetPlan(DynastyState state, TeamSimulationProfile profile)
    {
        var saved = state.TeamGamePlans.LastOrDefault(plan =>
            plan.TeamName.Equals(profile.TeamName, StringComparison.OrdinalIgnoreCase));
        return saved is not null ? Normalize(saved) : new TeamGamePlan
        {
            TeamName = profile.TeamName,
            Offense = profile.PassOffenseRating - profile.RushOffenseRating > 4 ? OffensivePlaybook.AirRaid
                : profile.RushOffenseRating - profile.PassOffenseRating > 4 ? OffensivePlaybook.PowerRun
                : OffensivePlaybook.Balanced
        };
    }

    public static TeamSimulationProfile Attach(DynastyState state, Team team, TeamSimulationProfile? profile)
    {
        profile ??= DynastyRosterSimulationProfileBuilder.Build(state, [team])[team.Name];
        return state.TeamGamePlans.Any(plan => plan.TeamName.Equals(team.Name, StringComparison.OrdinalIgnoreCase))
            ? profile with { GamePlan = GetPlan(state, profile) }
            : profile.GamePlan is not null ? profile : profile with { GamePlan = GetPlan(state, profile) };
    }

    public static TeamGamePlan Normalize(TeamGamePlan plan) => plan with
    {
        Offense = Enum.IsDefined(plan.Offense) ? plan.Offense : OffensivePlaybook.Balanced,
        Defense = Enum.IsDefined(plan.Defense) ? plan.Defense : DefensivePlaybook.Balanced,
        Approach = Enum.IsDefined(plan.Approach) ? plan.Approach : DefensiveApproach.Balanced,
        OffensiveFormation = Enum.IsDefined(plan.OffensiveFormation) ? plan.OffensiveFormation : OffensiveFormation.ShotgunSpread,
        DefensiveFormation = Enum.IsDefined(plan.DefensiveFormation) ? plan.DefensiveFormation : DefensiveFormation.FourThree,
        CustomOffensivePlayIds = plan.CustomOffensivePlayIds.Where(id => PlaybookCatalog.OffensivePlays.Any(p => p.Id == id)).Distinct().Take(17).ToArray(),
        CustomDefensivePlayIds = plan.CustomDefensivePlayIds.Where(id => PlaybookCatalog.DefensivePlays.Any(p => p.Id == id)).Distinct().Take(14).ToArray(),
        OpeningPlayIds = plan.OpeningPlayIds.Where(id => PlaybookCatalog.OffensivePlays.Any(play => play.Id == id)).Take(12).ToArray(),
        OpeningScript = plan.OpeningScript.Take(12).Select(call => Enum.IsDefined(call) ? call : OffensiveConcept.Automatic).ToArray()
    };

    public static ResolvedPlayCall ResolvePlayCall(TeamGamePlan offense, TeamGamePlan defense,
        int possession, Random random, int offensiveGameManagement = 60, int defensiveGameManagement = 60)
    {
        var scriptedId = possession < offense.OpeningPlayIds.Count ? offense.OpeningPlayIds[possession] : null;
        if (offense.Offense == OffensivePlaybook.Custom && !offense.CustomOffensivePlayIds.Contains(scriptedId ?? string.Empty)) scriptedId = null;
        var scriptedConcept = possession < offense.OpeningScript.Count &&
            offense.OpeningScript[possession] != OffensiveConcept.Automatic;
        var source = scriptedId is null ? PlayCallSource.Cpu : PlayCallSource.User;
        var desiredConcept = scriptedId is null ? GetConcept(offense, possession) : OffensiveConcept.Automatic;
        var deviationChance = Math.Clamp((70 - offensiveGameManagement) / 150d, .02, .30);
        if (source == PlayCallSource.Cpu && !scriptedConcept && random.NextDouble() < deviationChance)
            desiredConcept = Enum.GetValues<OffensiveConcept>()[random.Next(1, Enum.GetValues<OffensiveConcept>().Length)];
        var offensivePool = AvailableOffensivePlays(offense).ToArray();
        var conceptFallback = desiredConcept == OffensiveConcept.Automatic
            ? offensivePool
            : offensivePool.Where(play => play.Concept == desiredConcept).ToArray();
        if (conceptFallback.Length == 0) conceptFallback = offensivePool;
        var calledOffense = scriptedId is not null
            ? PlaybookCatalog.OffensivePlays.Single(play => play.Id == scriptedId)
            : Pick(offensivePool.Where(play => play.Formation == offense.OffensiveFormation &&
                (desiredConcept == OffensiveConcept.Automatic || play.Concept == desiredConcept)).ToArray(), conceptFallback, random);

        var defensivePool = AvailableDefensivePlays(defense).ToArray();
        var anticipatedApproach = random.Next(100) < Math.Clamp(defensiveGameManagement, 25, 95)
            ? defense.Approach : DefensiveApproach.Balanced;
        var preferredDefense = defensivePool.Where(play => play.Formation == defense.DefensiveFormation &&
            play.Approach == anticipatedApproach).ToArray();
        var calledDefense = Pick(preferredDefense, defensivePool, random);
        return new ResolvedPlayCall(calledOffense, calledDefense, source);
    }

    public static IReadOnlyList<OffensivePlayDefinition> AvailableOffensivePlays(TeamGamePlan plan)
    {
        if (plan.Offense == OffensivePlaybook.Custom && plan.CustomOffensivePlayIds.Count > 0)
            return PlaybookCatalog.OffensivePlays.Where(p => plan.CustomOffensivePlayIds.Contains(p.Id)).Take(17).ToArray();
        var formations = plan.Offense switch
        {
            OffensivePlaybook.Spread => new[] { OffensiveFormation.ShotgunSpread, OffensiveFormation.Pistol, OffensiveFormation.Empty },
            OffensivePlaybook.AirRaid => new[] { OffensiveFormation.Empty, OffensiveFormation.ShotgunSpread, OffensiveFormation.Pistol },
            OffensivePlaybook.PowerRun => new[] { OffensiveFormation.IForm, OffensiveFormation.Singleback, OffensiveFormation.Pistol },
            OffensivePlaybook.Flexbone => new[] { OffensiveFormation.Flexbone, OffensiveFormation.IForm, OffensiveFormation.Pistol },
            OffensivePlaybook.Option => new[] { OffensiveFormation.Pistol, OffensiveFormation.ShotgunSpread, OffensiveFormation.IForm },
            _ => new[] { plan.OffensiveFormation, OffensiveFormation.ShotgunSpread, OffensiveFormation.Singleback, OffensiveFormation.Pistol }
        };
        var orderedFormations = new[] { plan.OffensiveFormation }.Concat(formations).Distinct().Take(3).ToArray();
        var preferred = PreferredConcepts(plan.Offense);
        IEnumerable<OffensivePlayDefinition> Curated(OffensiveFormation formation, int limit)
        {
            var formationPlays = PlaybookCatalog.OffensivePlays.Where(play => play.Formation == formation).ToArray();
            var conceptMix = formationPlays.GroupBy(play => play.Concept).Select(group => group.OrderBy(play => play.Id).First());
            var preferredOrder = formationPlays.OrderBy(play =>
                    Array.IndexOf(preferred, play.Concept) is var rank && rank >= 0 ? rank : preferred.Length)
                .ThenBy(play => play.Id);
            return conceptMix.Concat(preferredOrder).DistinctBy(play => play.Id).Take(limit);
        }
        var selected = orderedFormations.SelectMany((formation, formationIndex) =>
            Curated(formation, formationIndex == 0 ? 7 : 5));
        return selected.DistinctBy(play => play.Id).ToArray();
    }

    public static IReadOnlyList<DefensivePlayDefinition> AvailableDefensivePlays(TeamGamePlan plan)
    {
        if (plan.Defense == DefensivePlaybook.Custom && plan.CustomDefensivePlayIds.Count > 0)
            return PlaybookCatalog.DefensivePlays.Where(p => plan.CustomDefensivePlayIds.Contains(p.Id)).Take(14).ToArray();
        var formations = plan.Defense switch
        {
            DefensivePlaybook.FourThree => new[] { DefensiveFormation.FourThree, DefensiveFormation.Nickel, DefensiveFormation.GoalLine },
            DefensivePlaybook.ThreeFour => new[] { DefensiveFormation.ThreeFour, DefensiveFormation.Nickel, DefensiveFormation.Dime },
            DefensivePlaybook.Nickel => new[] { DefensiveFormation.Nickel, DefensiveFormation.Dime, DefensiveFormation.FourThree },
            _ => new[] { plan.DefensiveFormation, DefensiveFormation.Nickel, DefensiveFormation.Dime, DefensiveFormation.FourThree }
        };
        var orderedFormations = new[] { plan.DefensiveFormation }.Concat(formations).Distinct().Take(3).ToArray();
        var selected = orderedFormations.SelectMany((formation, formationIndex) => PlaybookCatalog.DefensivePlays
            .Where(play => play.Formation == formation)
            .OrderBy(play => play.Approach == plan.Approach ? 0 : 1)
            .ThenBy(play => play.Id)
            .Take(formationIndex == 0 ? 6 : 4));
        return selected.DistinctBy(play => play.Id).ToArray();
    }

    public static OffensivePlayDefinition RecommendOffensivePlay(TeamGamePlan plan, int down, int distance, int yardLine)
    {
        var available = AvailableOffensivePlays(plan);
        var preferred = distance <= 2 ? new[] { OffensiveConcept.InsideRun, OffensiveConcept.OutsideRun, OffensiveConcept.PlayAction }
            : down >= 3 && distance >= 7 ? new[] { OffensiveConcept.QuickPass, OffensiveConcept.DeepPass, OffensiveConcept.PlayAction }
            : yardLine >= 80 ? new[] { OffensiveConcept.InsideRun, OffensiveConcept.QuickPass, OffensiveConcept.PlayAction }
            : PreferredConcepts(plan.Offense);
        return available.OrderBy(play => Array.IndexOf(preferred, play.Concept) is var rank && rank >= 0 ? rank : preferred.Length)
            .ThenBy(play => play.Formation == plan.OffensiveFormation ? 0 : 1).ThenBy(play => play.Id).First();
    }

    public static DefensivePlayDefinition RecommendDefensivePlay(TeamGamePlan plan, int down, int distance)
    {
        var desired = distance <= 2 ? DefensiveApproach.StopRun
            : down >= 3 && distance >= 7 ? DefensiveApproach.ProtectPass : plan.Approach;
        return AvailableDefensivePlays(plan).OrderBy(play => play.Approach == desired ? 0 : 1)
            .ThenBy(play => play.Formation == plan.DefensiveFormation ? 0 : 1).ThenBy(play => play.Id).First();
    }

    private static OffensiveConcept[] PreferredConcepts(OffensivePlaybook playbook) => playbook switch
    {
        OffensivePlaybook.Spread => [OffensiveConcept.QuickPass, OffensiveConcept.OutsideRun, OffensiveConcept.DeepPass, OffensiveConcept.InsideRun],
        OffensivePlaybook.AirRaid => [OffensiveConcept.QuickPass, OffensiveConcept.DeepPass, OffensiveConcept.PlayAction, OffensiveConcept.InsideRun],
        OffensivePlaybook.PowerRun => [OffensiveConcept.InsideRun, OffensiveConcept.OutsideRun, OffensiveConcept.PlayAction, OffensiveConcept.QuickPass],
        OffensivePlaybook.Flexbone => [OffensiveConcept.OutsideRun, OffensiveConcept.InsideRun, OffensiveConcept.PlayAction, OffensiveConcept.QuickPass],
        OffensivePlaybook.Option => [OffensiveConcept.OutsideRun, OffensiveConcept.InsideRun, OffensiveConcept.PlayAction, OffensiveConcept.QuickPass],
        _ => [OffensiveConcept.InsideRun, OffensiveConcept.QuickPass, OffensiveConcept.OutsideRun, OffensiveConcept.PlayAction, OffensiveConcept.DeepPass]
    };

    private static T Pick<T>(T[] preferred, T[] fallback, Random random) =>
        (preferred.Length > 0 ? preferred : fallback)[random.Next(preferred.Length > 0 ? preferred.Length : fallback.Length)];

    public static OffensiveConcept GetConcept(TeamGamePlan plan, int possession)
    {
        if (possession < plan.OpeningScript.Count && plan.OpeningScript[possession] != OffensiveConcept.Automatic)
            return plan.OpeningScript[possession];
        OffensiveConcept[] pattern = plan.Offense switch
        {
            OffensivePlaybook.Spread => [OffensiveConcept.QuickPass, OffensiveConcept.OutsideRun, OffensiveConcept.QuickPass, OffensiveConcept.DeepPass, OffensiveConcept.InsideRun, OffensiveConcept.PlayAction],
            OffensivePlaybook.AirRaid => [OffensiveConcept.QuickPass, OffensiveConcept.QuickPass, OffensiveConcept.DeepPass, OffensiveConcept.QuickPass, OffensiveConcept.InsideRun, OffensiveConcept.DeepPass],
            OffensivePlaybook.PowerRun => [OffensiveConcept.InsideRun, OffensiveConcept.InsideRun, OffensiveConcept.PlayAction, OffensiveConcept.OutsideRun, OffensiveConcept.InsideRun, OffensiveConcept.QuickPass],
            OffensivePlaybook.Flexbone => [OffensiveConcept.OutsideRun, OffensiveConcept.InsideRun, OffensiveConcept.OutsideRun, OffensiveConcept.PlayAction, OffensiveConcept.InsideRun, OffensiveConcept.QuickPass],
            OffensivePlaybook.Option => [OffensiveConcept.OutsideRun, OffensiveConcept.InsideRun, OffensiveConcept.OutsideRun, OffensiveConcept.QuickPass, OffensiveConcept.PlayAction, OffensiveConcept.InsideRun],
            _ => [OffensiveConcept.Automatic]
        };
        return pattern[possession % pattern.Length];
    }

    public static string Label(OffensivePlaybook book) => book switch
    { OffensivePlaybook.AirRaid => "Air Raid", OffensivePlaybook.PowerRun => "Power Run", _ => book.ToString() };
    public static string Label(DefensivePlaybook book) => book switch
    { DefensivePlaybook.FourThree => "4-3", DefensivePlaybook.ThreeFour => "3-4", _ => book.ToString() };
    public static string Label(DefensiveApproach approach) => approach switch
    { DefensiveApproach.StopRun => "Stop the run", DefensiveApproach.ProtectPass => "Protect the pass", _ => approach.ToString() };
    public static string Label(OffensiveConcept concept) => concept switch
    { OffensiveConcept.InsideRun => "Inside run", OffensiveConcept.OutsideRun => "Outside run", OffensiveConcept.QuickPass => "Quick pass", OffensiveConcept.PlayAction => "Play action", OffensiveConcept.DeepPass => "Deep pass", _ => "Use playbook" };
    public static string Label(OffensiveFormation formation) => formation switch
    { OffensiveFormation.ShotgunSpread => "Shotgun Spread", OffensiveFormation.IForm => "I-Formation", _ => formation.ToString() };
    public static string Label(DefensiveFormation formation) => formation switch
    { DefensiveFormation.FourThree => "4-3", DefensiveFormation.ThreeFour => "3-4", DefensiveFormation.GoalLine => "Goal Line", _ => formation.ToString() };
    public static string Label(OffensivePersonnel personnel) => personnel switch
    { OffensivePersonnel.Ten => "10 personnel", OffensivePersonnel.Eleven => "11 personnel", OffensivePersonnel.Twelve => "12 personnel", OffensivePersonnel.TwentyOne => "21 personnel", OffensivePersonnel.TripleBack => "30 personnel · 3 backs", _ => "Empty" };
    public static string Label(DefensivePersonnel personnel) => personnel switch
    { DefensivePersonnel.BaseFourThree => "4-3 base", DefensivePersonnel.BaseThreeFour => "3-4 base", DefensivePersonnel.GoalLine => "Goal line", _ => personnel.ToString() };
    public static string Describe(OffensivePlaybook book) => book switch
    {
        OffensivePlaybook.Spread => "Space the defense with quick throws and outside runs. Best with strong passing personnel.",
        OffensivePlaybook.AirRaid => "Throw often. Deep concepts offer explosive gains with more turnover risk.",
        OffensivePlaybook.PowerRun => "Lean on the backs and line. Mix in play action to challenge coverage.",
        OffensivePlaybook.Custom => "Use exactly the calls selected in Playbook Editor. Custom selections replace the standard playbook.",
        OffensivePlaybook.Flexbone => "Under-center triple-option football with a fullback and two slotbacks, supported by play-action passes.",
        OffensivePlaybook.Option => "Run often and attack the perimeter. Strong rushing personnel matter.",
        _ => "Let passing and rushing matchups set the mix. No fixed tendency bonus."
    };
}
