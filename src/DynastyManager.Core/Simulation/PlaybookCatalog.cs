using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class PlaybookCatalog
{
    public static IReadOnlyList<OffensivePlayDefinition> OffensivePlays { get; } =
    [
        new("off-spread-zone", "Inside Zone", OffensiveFormation.ShotgunSpread, OffensivePersonnel.Eleven, OffensiveConcept.InsideRun, "Zone run attacking the interior with a spread box."),
        new("off-spread-bubble", "Bubble RPO", OffensiveFormation.ShotgunSpread, OffensivePersonnel.Eleven, OffensiveConcept.QuickPass, "Read the overhang defender and hand off or throw the bubble."),
        new("off-spread-verts", "Four Verticals", OffensiveFormation.ShotgunSpread, OffensivePersonnel.Ten, OffensiveConcept.DeepPass, "Stretch both safeties with four vertical routes."),
        new("off-spread-counter", "Counter Read", OffensiveFormation.ShotgunSpread, OffensivePersonnel.Eleven, OffensiveConcept.InsideRun, "Pull the guard while the quarterback reads the backside edge."),
        new("off-spread-mesh", "Mesh", OffensiveFormation.ShotgunSpread, OffensivePersonnel.Ten, OffensiveConcept.QuickPass, "Paired shallow crossers create natural traffic against man coverage."),
        new("off-spread-sail", "Sail", OffensiveFormation.ShotgunSpread, OffensivePersonnel.Eleven, OffensiveConcept.DeepPass, "A three-level sideline stretch attacks zone coverage."),
        new("off-spread-snag", "Snag", OffensiveFormation.ShotgunSpread, OffensivePersonnel.Eleven, OffensiveConcept.QuickPass, "Triangle read places the flat defender in conflict."),
        new("off-spread-split", "Split Zone", OffensiveFormation.ShotgunSpread, OffensivePersonnel.Twelve, OffensiveConcept.InsideRun, "A slicing tight end seals the backside of inside zone."),
        new("off-pistol-power", "Pistol Power", OffensiveFormation.Pistol, OffensivePersonnel.Eleven, OffensiveConcept.InsideRun, "Down blocks and a pulling guard create an interior gap."),
        new("off-pistol-read", "Read Option", OffensiveFormation.Pistol, OffensivePersonnel.Eleven, OffensiveConcept.OutsideRun, "Quarterback reads the edge before giving or keeping."),
        new("off-pistol-cross", "Play-Action Cross", OffensiveFormation.Pistol, OffensivePersonnel.Twelve, OffensiveConcept.PlayAction, "Run action sets up an intermediate crossing route."),
        new("off-pistol-zone", "Inside Zone", OffensiveFormation.Pistol, OffensivePersonnel.Eleven, OffensiveConcept.InsideRun, "Back presses the front side before choosing the open crease."),
        new("off-pistol-speed", "Speed Option", OffensiveFormation.Pistol, OffensivePersonnel.TwentyOne, OffensiveConcept.OutsideRun, "Quarterback attacks the edge with a pitch relationship."),
        new("off-pistol-smash", "Smash", OffensiveFormation.Pistol, OffensivePersonnel.Eleven, OffensiveConcept.QuickPass, "Hitch and corner combination high-lows the outside defender."),
        new("off-pistol-post", "Post-Dig", OffensiveFormation.Pistol, OffensivePersonnel.Eleven, OffensiveConcept.DeepPass, "Deep post clears space for an intermediate dig."),
        new("off-pistol-screen", "Halfback Screen", OffensiveFormation.Pistol, OffensivePersonnel.Eleven, OffensiveConcept.QuickPass, "Invite pressure before releasing blockers in front of the back."),
        new("off-single-zone", "Wide Zone", OffensiveFormation.Singleback, OffensivePersonnel.Twelve, OffensiveConcept.OutsideRun, "Stretch the front and give the back one decisive cut."),
        new("off-single-stick", "Stick", OffensiveFormation.Singleback, OffensivePersonnel.Eleven, OffensiveConcept.QuickPass, "Quick leverage read against underneath coverage."),
        new("off-single-duo", "Duo", OffensiveFormation.Singleback, OffensivePersonnel.Twelve, OffensiveConcept.InsideRun, "Double teams create vertical movement with the back reading the linebacker."),
        new("off-single-counter", "Counter", OffensiveFormation.Singleback, OffensivePersonnel.Eleven, OffensiveConcept.InsideRun, "Backside guard pulls into the designed gap."),
        new("off-single-toss", "Crack Toss", OffensiveFormation.Singleback, OffensivePersonnel.Eleven, OffensiveConcept.OutsideRun, "Receivers crack inside while the back races to the perimeter."),
        new("off-single-levels", "Levels", OffensiveFormation.Singleback, OffensivePersonnel.Eleven, OffensiveConcept.QuickPass, "In-breaking routes attack underneath zone windows."),
        new("off-single-shot", "Yankee Shot", OffensiveFormation.Singleback, OffensivePersonnel.Twelve, OffensiveConcept.PlayAction, "Max protection creates a deep post-over route combination."),
        new("off-single-flood", "Boot Flood", OffensiveFormation.Singleback, OffensivePersonnel.Twelve, OffensiveConcept.PlayAction, "Outside-zone action moves the pocket into a three-level read."),
        new("off-iform-lead", "Lead Iso", OffensiveFormation.IForm, OffensivePersonnel.TwentyOne, OffensiveConcept.InsideRun, "Fullback leads through the designed interior gap."),
        new("off-iform-boot", "Boot Flood", OffensiveFormation.IForm, OffensivePersonnel.TwentyOne, OffensiveConcept.PlayAction, "Hard run action with a three-level rollout read."),
        new("off-iform-power", "Power O", OffensiveFormation.IForm, OffensivePersonnel.TwentyOne, OffensiveConcept.InsideRun, "Fullback kicks out while the backside guard leads through."),
        new("off-iform-toss", "Toss Sweep", OffensiveFormation.IForm, OffensivePersonnel.TwentyOne, OffensiveConcept.OutsideRun, "Back takes a deep pitch behind lead blockers on the edge."),
        new("off-iform-trap", "Quick Trap", OffensiveFormation.IForm, OffensivePersonnel.TwentyOne, OffensiveConcept.InsideRun, "Let the defensive tackle penetrate before trapping him inside."),
        new("off-iform-slant", "Slant-Flat", OffensiveFormation.IForm, OffensivePersonnel.TwentyOne, OffensiveConcept.QuickPass, "A simple two-man leverage read to the outside."),
        new("off-iform-post", "Play-Action Post", OffensiveFormation.IForm, OffensivePersonnel.TwentyOne, OffensiveConcept.PlayAction, "Sell downhill run before attacking the deep middle."),
        new("off-iform-screen", "Fullback Screen", OffensiveFormation.IForm, OffensivePersonnel.TwentyOne, OffensiveConcept.QuickPass, "Hide the fullback behind the line before releasing him underneath."),
        new("off-empty-mesh", "Mesh", OffensiveFormation.Empty, OffensivePersonnel.Empty, OffensiveConcept.QuickPass, "Crossing routes create traffic against man coverage."),
        new("off-empty-seams", "Seam Choice", OffensiveFormation.Empty, OffensivePersonnel.Empty, OffensiveConcept.DeepPass, "Inside receivers adjust their seams to the safeties."),
        new("off-empty-stick", "Stick", OffensiveFormation.Empty, OffensivePersonnel.Empty, OffensiveConcept.QuickPass, "Read leverage quickly with five immediate receiving threats."),
        new("off-empty-spacing", "Spacing", OffensiveFormation.Empty, OffensivePersonnel.Empty, OffensiveConcept.QuickPass, "Settle receivers across underneath zone windows."),
        new("off-empty-verts", "Five Verticals", OffensiveFormation.Empty, OffensivePersonnel.Empty, OffensiveConcept.DeepPass, "Stress every deep defender with five vertical releases."),
        new("off-empty-dagger", "Dagger", OffensiveFormation.Empty, OffensivePersonnel.Empty, OffensiveConcept.DeepPass, "Clear the middle with a seam and follow it with a deep dig."),
        new("off-empty-qbdraw", "Quarterback Draw", OffensiveFormation.Empty, OffensivePersonnel.Empty, OffensiveConcept.InsideRun, "Spread the box before the quarterback follows interior blockers."),
        new("off-empty-screen", "Tunnel Screen", OffensiveFormation.Empty, OffensivePersonnel.Empty, OffensiveConcept.QuickPass, "Bring the receiver underneath with linemen releasing in front.")
    ];

    public static IReadOnlyList<DefensivePlayDefinition> DefensivePlays { get; } =
    [
        new("def-43-cover3", "Cover 3 Buzz", DefensiveFormation.FourThree, DefensivePersonnel.BaseFourThree, DefensiveApproach.Balanced, "Safety rotates down while three defenders protect the deep field."),
        new("def-43-fire", "Sam Fire Zone", DefensiveFormation.FourThree, DefensivePersonnel.BaseFourThree, DefensiveApproach.Pressure, "Strong-side linebacker pressure with three-deep coverage."),
        new("def-43-cover1", "Cover 1 Robber", DefensiveFormation.FourThree, DefensivePersonnel.BaseFourThree, DefensiveApproach.Balanced, "Man coverage with a safety robbing inside routes."),
        new("def-43-tampa", "Tampa 2", DefensiveFormation.FourThree, DefensivePersonnel.BaseFourThree, DefensiveApproach.ProtectPass, "Middle linebacker carries the deep seam between two safeties."),
        new("def-43-run", "Under Front", DefensiveFormation.FourThree, DefensivePersonnel.BaseFourThree, DefensiveApproach.StopRun, "Shift the front toward the tight end and close interior gaps."),
        new("def-43-double", "Double A-Gap", DefensiveFormation.FourThree, DefensivePersonnel.BaseFourThree, DefensiveApproach.Pressure, "Threaten both interior gaps before bringing or dropping linebackers."),
        new("def-34-cross", "Cross Dog", DefensiveFormation.ThreeFour, DefensivePersonnel.BaseThreeFour, DefensiveApproach.Pressure, "Inside linebackers exchange gaps to stress protection."),
        new("def-34-quarters", "Quarters", DefensiveFormation.ThreeFour, DefensivePersonnel.BaseThreeFour, DefensiveApproach.ProtectPass, "Four deep defenders match vertical releases."),
        new("def-34-cloud", "Cloud 3", DefensiveFormation.ThreeFour, DefensivePersonnel.BaseThreeFour, DefensiveApproach.Balanced, "Corner squats in the flat with three defenders deep."),
        new("def-34-edge", "Boundary Edge", DefensiveFormation.ThreeFour, DefensivePersonnel.BaseThreeFour, DefensiveApproach.Pressure, "Outside linebacker attacks from the short side."),
        new("def-34-bear", "Bear Front", DefensiveFormation.ThreeFour, DefensivePersonnel.BaseThreeFour, DefensiveApproach.StopRun, "Cover the interior linemen and force the run outside."),
        new("def-34-two", "Cover 2 Sink", DefensiveFormation.ThreeFour, DefensivePersonnel.BaseThreeFour, DefensiveApproach.ProtectPass, "Five underneath defenders sink beneath two deep safeties."),
        new("def-nickel-two", "Cover 2 Man", DefensiveFormation.Nickel, DefensivePersonnel.Nickel, DefensiveApproach.ProtectPass, "Man coverage underneath two deep safeties."),
        new("def-nickel-overload", "Boundary Overload", DefensiveFormation.Nickel, DefensivePersonnel.Nickel, DefensiveApproach.Pressure, "Pressure the short side with a rotating safety."),
        new("def-nickel-quarters", "Match Quarters", DefensiveFormation.Nickel, DefensivePersonnel.Nickel, DefensiveApproach.ProtectPass, "Pattern-match vertical routes from a four-deep shell."),
        new("def-nickel-robber", "Cover 1 Robber", DefensiveFormation.Nickel, DefensivePersonnel.Nickel, DefensiveApproach.Balanced, "Nickel man coverage with inside safety help."),
        new("def-nickel-mug", "Mug Double A", DefensiveFormation.Nickel, DefensivePersonnel.Nickel, DefensiveApproach.Pressure, "Walk linebackers into both A-gaps to stress protection calls."),
        new("def-nickel-run", "Big Nickel Fit", DefensiveFormation.Nickel, DefensivePersonnel.Nickel, DefensiveApproach.StopRun, "Safety inserts aggressively as an extra run defender."),
        new("def-dime-drop8", "Drop Eight", DefensiveFormation.Dime, DefensivePersonnel.Dime, DefensiveApproach.ProtectPass, "Rush three and crowd intermediate throwing lanes."),
        new("def-dime-two", "Two-Man", DefensiveFormation.Dime, DefensivePersonnel.Dime, DefensiveApproach.ProtectPass, "Trail-man technique underneath two deep safeties."),
        new("def-dime-one", "Dime Cover 1", DefensiveFormation.Dime, DefensivePersonnel.Dime, DefensiveApproach.Balanced, "Six defensive backs match receivers with a post safety."),
        new("def-dime-sim", "Simulated Pressure", DefensiveFormation.Dime, DefensivePersonnel.Dime, DefensiveApproach.Pressure, "Present pressure before rushing four from an unexpected side."),
        new("def-dime-bracket", "Double Bracket", DefensiveFormation.Dime, DefensivePersonnel.Dime, DefensiveApproach.ProtectPass, "Bracket the two primary receiving threats."),
        new("def-dime-spy", "Cover 3 Spy", DefensiveFormation.Dime, DefensivePersonnel.Dime, DefensiveApproach.Balanced, "Three deep zones with a second-level quarterback spy."),
        new("def-goal-stone", "Goal-Line Stone", DefensiveFormation.GoalLine, DefensivePersonnel.GoalLine, DefensiveApproach.StopRun, "Heavy front attacks every interior run gap."),
        new("def-goal-jump", "A-Gap Jump", DefensiveFormation.GoalLine, DefensivePersonnel.GoalLine, DefensiveApproach.Pressure, "Linebackers attack both interior gaps at the snap."),
        new("def-goal-edge", "Edge Crash", DefensiveFormation.GoalLine, DefensivePersonnel.GoalLine, DefensiveApproach.StopRun, "Ends squeeze down while linebackers contain the perimeter."),
        new("def-goal-man", "Goal-Line Man", DefensiveFormation.GoalLine, DefensivePersonnel.GoalLine, DefensiveApproach.Balanced, "Tight man coverage behind a heavy run front."),
        new("def-goal-pop", "Pop Pass Alert", DefensiveFormation.GoalLine, DefensivePersonnel.GoalLine, DefensiveApproach.ProtectPass, "Safeties key tight ends and backs releasing behind run action."),
        new("def-goal-wide", "Wide Goal Front", DefensiveFormation.GoalLine, DefensivePersonnel.GoalLine, DefensiveApproach.StopRun, "Widen the edge defenders against toss and option looks.")
    ];

    public static bool Contains(string id) => OffensivePlays.Any(play => play.Id == id) || DefensivePlays.Any(play => play.Id == id);
}
