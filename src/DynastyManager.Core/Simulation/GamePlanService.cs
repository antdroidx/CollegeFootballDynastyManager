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
            plan.FavoritePlayIds.Count > 20 || plan.FavoritePlayIds.Any(id => !PlaybookCatalog.Contains(id)))
            throw new ArgumentException("Invalid game plan.", nameof(plan));
        return state with { TeamGamePlans = state.TeamGamePlans.Where(item =>
            !item.TeamName.Equals(plan.TeamName, StringComparison.OrdinalIgnoreCase))
            .Append(plan with { OpeningScript = plan.OpeningScript.ToArray(), OpeningPlayIds = plan.OpeningPlayIds.ToArray(), FavoritePlayIds = plan.FavoritePlayIds.Distinct().ToArray() }).ToArray() };
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
        FavoritePlayIds = plan.FavoritePlayIds.Where(PlaybookCatalog.Contains).Distinct().Take(20).ToArray(),
        OpeningPlayIds = plan.OpeningPlayIds.Where(id => PlaybookCatalog.OffensivePlays.Any(play => play.Id == id)).Take(12).ToArray(),
        OpeningScript = plan.OpeningScript.Take(12).Select(call => Enum.IsDefined(call) ? call : OffensiveConcept.Automatic).ToArray()
    };

    public static ResolvedPlayCall ResolvePlayCall(TeamGamePlan offense, TeamGamePlan defense,
        int possession, Random random, int offensiveGameManagement = 60, int defensiveGameManagement = 60)
    {
        var scriptedId = possession < offense.OpeningPlayIds.Count ? offense.OpeningPlayIds[possession] : null;
        var scriptedConcept = possession < offense.OpeningScript.Count &&
            offense.OpeningScript[possession] != OffensiveConcept.Automatic;
        var source = scriptedId is null ? PlayCallSource.Cpu : PlayCallSource.User;
        var desiredConcept = scriptedId is null ? GetConcept(offense, possession) : OffensiveConcept.Automatic;
        var deviationChance = Math.Clamp((70 - offensiveGameManagement) / 150d, .02, .30);
        if (source == PlayCallSource.Cpu && !scriptedConcept && random.NextDouble() < deviationChance)
            desiredConcept = Enum.GetValues<OffensiveConcept>()[random.Next(1, Enum.GetValues<OffensiveConcept>().Length)];
        var offensivePool = PlaybookCatalog.OffensivePlays.Where(play =>
            play.Formation == offense.OffensiveFormation || offense.FavoritePlayIds.Contains(play.Id)).ToArray();
        if (offensivePool.Length == 0) offensivePool = PlaybookCatalog.OffensivePlays.ToArray();
        var conceptFallback = desiredConcept == OffensiveConcept.Automatic
            ? offensivePool
            : PlaybookCatalog.OffensivePlays.Where(play => play.Concept == desiredConcept).ToArray();
        var calledOffense = scriptedId is not null
            ? PlaybookCatalog.OffensivePlays.Single(play => play.Id == scriptedId)
            : Pick(offensivePool.Where(play => desiredConcept == OffensiveConcept.Automatic || play.Concept == desiredConcept).ToArray(), conceptFallback, random);

        var defensivePool = PlaybookCatalog.DefensivePlays.Where(play =>
            play.Formation == defense.DefensiveFormation || defense.FavoritePlayIds.Contains(play.Id)).ToArray();
        if (defensivePool.Length == 0) defensivePool = PlaybookCatalog.DefensivePlays.ToArray();
        var anticipatedApproach = random.Next(100) < Math.Clamp(defensiveGameManagement, 25, 95)
            ? defense.Approach : DefensiveApproach.Balanced;
        var preferredDefense = defensivePool.Where(play => play.Approach == anticipatedApproach).ToArray();
        var calledDefense = Pick(preferredDefense, defensivePool, random);
        return new ResolvedPlayCall(calledOffense, calledDefense, source);
    }

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
    { OffensivePersonnel.Ten => "10 personnel", OffensivePersonnel.Eleven => "11 personnel", OffensivePersonnel.Twelve => "12 personnel", OffensivePersonnel.TwentyOne => "21 personnel", _ => "Empty" };
    public static string Label(DefensivePersonnel personnel) => personnel switch
    { DefensivePersonnel.BaseFourThree => "4-3 base", DefensivePersonnel.BaseThreeFour => "3-4 base", DefensivePersonnel.GoalLine => "Goal line", _ => personnel.ToString() };
    public static string Describe(OffensivePlaybook book) => book switch
    {
        OffensivePlaybook.Spread => "Space the defense with quick throws and outside runs. Best with strong passing personnel.",
        OffensivePlaybook.AirRaid => "Throw often. Deep concepts offer explosive gains with more turnover risk.",
        OffensivePlaybook.PowerRun => "Lean on the backs and line. Mix in play action to challenge coverage.",
        OffensivePlaybook.Option => "Run often and attack the perimeter. Strong rushing personnel matter.",
        _ => "Let passing and rushing matchups set the mix. No fixed tendency bonus."
    };
}
