using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class GamePlanService
{
    public static DynastyState SetPlan(DynastyState state, TeamGamePlan plan)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plan.TeamName);
        if (!Enum.IsDefined(plan.Offense) || !Enum.IsDefined(plan.Defense) || !Enum.IsDefined(plan.Approach) ||
            plan.OpeningScript.Count > 12 || plan.OpeningScript.Any(call => !Enum.IsDefined(call)))
            throw new ArgumentException("Invalid game plan.", nameof(plan));
        return state with { TeamGamePlans = state.TeamGamePlans.Where(item =>
            !item.TeamName.Equals(plan.TeamName, StringComparison.OrdinalIgnoreCase))
            .Append(plan with { OpeningScript = plan.OpeningScript.ToArray() }).ToArray() };
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
        OpeningScript = plan.OpeningScript.Take(12).Select(call => Enum.IsDefined(call) ? call : OffensiveConcept.Automatic).ToArray()
    };

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
    public static string Describe(OffensivePlaybook book) => book switch
    {
        OffensivePlaybook.Spread => "Space the defense with quick throws and outside runs. Best with strong passing personnel.",
        OffensivePlaybook.AirRaid => "Throw often. Deep concepts offer explosive gains with more turnover risk.",
        OffensivePlaybook.PowerRun => "Lean on the backs and line. Mix in play action to challenge coverage.",
        OffensivePlaybook.Option => "Run often and attack the perimeter. Strong rushing personnel matter.",
        _ => "Let passing and rushing matchups set the mix. No fixed tendency bonus."
    };
}
