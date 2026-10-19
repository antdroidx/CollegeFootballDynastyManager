using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class PlayEditorService
{
    public static IEnumerable<OffensivePlayDefinition> OffensiveLibrary(TeamGamePlan plan) =>
        PlaybookCatalog.OffensivePlays.Concat(plan.CreatedOffensivePlays);
    public static IEnumerable<DefensivePlayDefinition> DefensiveLibrary(TeamGamePlan plan) =>
        PlaybookCatalog.DefensivePlays.Concat(plan.CreatedDefensivePlays);

    public static void Validate(TeamGamePlan plan)
    {
        if (plan.CreatedOffensivePlays.Count > 100 || plan.CreatedDefensivePlays.Count > 100)
            throw new ArgumentException("The play library supports up to 100 created plays per side.");
        var ids = new HashSet<string>(PlaybookCatalog.OffensivePlays.Select(p => p.Id).Concat(PlaybookCatalog.DefensivePlays.Select(p => p.Id)));
        foreach (var play in plan.CreatedOffensivePlays)
        {
            Check(play.Id, play.Name, play.Assignments);
            if (!Enum.IsDefined(play.Formation) || !Enum.IsDefined(play.Personnel) || !Enum.IsDefined(play.Concept)) throw new ArgumentException("Invalid offensive play.");
            if (play.Assignments.Any(a => a.Task is not (AssignmentTask.Route or AssignmentTask.Block))) throw new ArgumentException("Offensive assignments must be routes or blocks.");
        }
        foreach (var play in plan.CreatedDefensivePlays)
        {
            Check(play.Id, play.Name, play.Assignments);
            if (!Enum.IsDefined(play.Formation) || !Enum.IsDefined(play.Personnel) || !Enum.IsDefined(play.Approach)) throw new ArgumentException("Invalid defensive play.");
            if (play.Assignments.Any(a => a.Task is not (AssignmentTask.Rush or AssignmentTask.Man or AssignmentTask.Zone))) throw new ArgumentException("Defensive assignments must be rush, man or zone.");
        }
        void Check(string id, string name, IReadOnlyList<PlayAssignment> assignments)
        {
            if (!id.StartsWith("custom-", StringComparison.Ordinal) || !ids.Add(id) || string.IsNullOrWhiteSpace(name) || name.Length > 60)
                throw new ArgumentException("Created plays need a unique custom ID and a name of 1–60 characters.");
            if (assignments.Count > 11 || assignments.DistinctBy(a => (a.Position, a.RoleIndex)).Count() != assignments.Count)
                throw new ArgumentException("Each player slot can have only one assignment.");
            foreach (var a in assignments)
                if (!Enum.IsDefined(a.Position) || !Enum.IsDefined(a.Task) || a.RoleIndex is < 0 or > 4 ||
                    !double.IsFinite(a.Depth) || a.Depth is < -12 or > 40 || !double.IsFinite(a.Lane) || a.Lane is < .05 or > .95 ||
                    !Enum.TryParse<GameDayRouteFamily>(a.Route, out var route) || !Enum.IsDefined(route)) throw new ArgumentException("Invalid player assignment.");
        }
    }
}
