using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class PlaybookCatalog
{
    public static IReadOnlyList<OffensivePlayDefinition> OffensivePlays { get; } =
    [
        new("off-spread-zone", "Inside Zone", OffensiveFormation.ShotgunSpread, OffensivePersonnel.Eleven, OffensiveConcept.InsideRun, "Zone run attacking the interior with a spread box."),
        new("off-spread-bubble", "Bubble RPO", OffensiveFormation.ShotgunSpread, OffensivePersonnel.Eleven, OffensiveConcept.QuickPass, "Read the overhang defender and hand off or throw the bubble."),
        new("off-spread-verts", "Four Verticals", OffensiveFormation.ShotgunSpread, OffensivePersonnel.Ten, OffensiveConcept.DeepPass, "Stretch both safeties with four vertical routes."),
        new("off-pistol-power", "Pistol Power", OffensiveFormation.Pistol, OffensivePersonnel.Eleven, OffensiveConcept.InsideRun, "Down blocks and a pulling guard create an interior gap."),
        new("off-pistol-read", "Read Option", OffensiveFormation.Pistol, OffensivePersonnel.Eleven, OffensiveConcept.OutsideRun, "Quarterback reads the edge before giving or keeping."),
        new("off-pistol-cross", "Play-Action Cross", OffensiveFormation.Pistol, OffensivePersonnel.Twelve, OffensiveConcept.PlayAction, "Run action sets up an intermediate crossing route."),
        new("off-single-zone", "Wide Zone", OffensiveFormation.Singleback, OffensivePersonnel.Twelve, OffensiveConcept.OutsideRun, "Stretch the front and give the back one decisive cut."),
        new("off-single-stick", "Stick", OffensiveFormation.Singleback, OffensivePersonnel.Eleven, OffensiveConcept.QuickPass, "Quick leverage read against underneath coverage."),
        new("off-iform-lead", "Lead Iso", OffensiveFormation.IForm, OffensivePersonnel.TwentyOne, OffensiveConcept.InsideRun, "Fullback leads through the designed interior gap."),
        new("off-iform-boot", "Boot Flood", OffensiveFormation.IForm, OffensivePersonnel.TwentyOne, OffensiveConcept.PlayAction, "Hard run action with a three-level rollout read."),
        new("off-empty-mesh", "Mesh", OffensiveFormation.Empty, OffensivePersonnel.Empty, OffensiveConcept.QuickPass, "Crossing routes create traffic against man coverage."),
        new("off-empty-seams", "Seam Choice", OffensiveFormation.Empty, OffensivePersonnel.Empty, OffensiveConcept.DeepPass, "Inside receivers adjust their seams to the safeties.")
    ];

    public static IReadOnlyList<DefensivePlayDefinition> DefensivePlays { get; } =
    [
        new("def-43-cover3", "Cover 3 Buzz", DefensiveFormation.FourThree, DefensivePersonnel.BaseFourThree, DefensiveApproach.Balanced, "Safety rotates down while three defenders protect the deep field."),
        new("def-43-fire", "Sam Fire Zone", DefensiveFormation.FourThree, DefensivePersonnel.BaseFourThree, DefensiveApproach.Pressure, "Strong-side linebacker pressure with three-deep coverage."),
        new("def-34-cross", "Cross Dog", DefensiveFormation.ThreeFour, DefensivePersonnel.BaseThreeFour, DefensiveApproach.Pressure, "Inside linebackers exchange gaps to stress protection."),
        new("def-34-quarters", "Quarters", DefensiveFormation.ThreeFour, DefensivePersonnel.BaseThreeFour, DefensiveApproach.ProtectPass, "Four deep defenders match vertical releases."),
        new("def-nickel-two", "Cover 2 Man", DefensiveFormation.Nickel, DefensivePersonnel.Nickel, DefensiveApproach.ProtectPass, "Man coverage underneath two deep safeties."),
        new("def-nickel-overload", "Boundary Overload", DefensiveFormation.Nickel, DefensivePersonnel.Nickel, DefensiveApproach.Pressure, "Pressure the short side with a rotating safety."),
        new("def-dime-drop8", "Drop Eight", DefensiveFormation.Dime, DefensivePersonnel.Dime, DefensiveApproach.ProtectPass, "Rush three and crowd intermediate throwing lanes."),
        new("def-goal-stone", "Goal-Line Stone", DefensiveFormation.GoalLine, DefensivePersonnel.GoalLine, DefensiveApproach.StopRun, "Heavy front attacks every interior run gap.")
    ];

    public static bool Contains(string id) => OffensivePlays.Any(play => play.Id == id) || DefensivePlays.Any(play => play.Id == id);
}
