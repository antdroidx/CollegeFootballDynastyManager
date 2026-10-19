# NCAA 06 NEXT player ratings

The game stores 17 NCAA core ratings plus Leadership and Discipline. The Injury rating is positive: higher means more resistance to injury. Potential remains development metadata, not another on-field attribute. Stamina, separate coverage ratings and generic Technique have been removed. Fatigue remains in-game state rather than a permanent player rating.

`Ncaa06RatingService` uses the supplied **POCI-NEXT26X.xlsx**, sheet **POCI-NEXT26X**, **A1:T22**. The lower comparison table is not the active weight set. These values are embedded in code so Android does not need the workbook at runtime.

Calculation follows [NCAA DB Editor Conversions.cs at 2408f00](https://github.com/antdroidx/NCAA-DB-Editor/blob/2408f00ad471f40951030eaaf39cd5ddf02a0719/NCAA_XDBE/Conversions.cs#L1346):

`OVR = 50 + sum((displayed rating - (PLDH + PLDL)/2) * attribute weight / total weight * 98/(PLDH-PLDL))`

Round to nearest integer using .NET midpoint-to-even, then clamp displayed OVR to 40–99. Leadership and Discipline do not enter the NCAA overall formula. The supplied position codes 0–20 include fullback, individual line positions, and punter. `NcaaPositionCode` preserves these even when the game groups them as RB, OL, DE, OLB, or K. Generated grouped OL players default to center weights; generated DE/OLB/K players default to LE/LOLB/K respectively.

Generated roster and recruit talent targets are fitted to the position-weighted scale when their attributes are first created. This keeps a prospect's recruiting OVR consistent with their playing OVR. Imported and already initialized attributes are not recalibrated; Leadership and Discipline retain their generated values.

`Ncaa06RosterRatings.Apply` and `Export` map all 17 PLAY rating fields and PPOS. Displayed values are the default. When importing compressed database values, supply the roster's actual PRLU lookup; the adapter does not guess the encoding. Export returns displayed values. This is a rating-field adapter, not a binary roster-file reader or an Android file-import screen.

The player schema is version 25. Start a new test dynasty for this format; older saves are not migrated.

## Playbook behavior

Standard coach books expose up to 17 offensive and 14 defensive plays. The Playbook Editor browses the complete catalog by side and formation, then saves explicit Custom books under the same limits. A Custom book replaces the corresponding standard book; it does not append extra plays. Both CPU and user game-day calls use the selected membership. Saving a custom book clears the old opening script. Route assignment editing is deferred to a later Play Editor.

## Viewer and simulation

Formation alignments and route paths are shared between play art and live presentation. Route movement uses distance along the path; play duration accounts for speed ratings, pursuit distance and the returner's catch delay. The gold outline follows possession through QB, handoff, pass flight and catch. Read/RPO/option calls can become QB keepers; boot calls move the quarterback laterally. Three timeouts per half stop the next clock runoff; the CPU uses them late while trailing on defense. The existing probabilistic snap resolver still determines outcomes before playback; this update does not implement a spatial coverage/read progression engine.
