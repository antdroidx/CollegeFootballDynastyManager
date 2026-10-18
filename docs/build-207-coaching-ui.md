# Build 207: dashboard and coaching foundation

## Interface

A shared dark theme introduces navy surfaces, teal primary actions, rounded cards, consistent text/control colors and larger touch targets. The dashboard promotes the current program, a full-width Advance Week action, two save actions and a Coaching Hub shortcut. Five fixed navigation tabs replace the horizontally scrolling module bar: Home, Recruit, Coach, Roster and More. Schedule, teams, rankings and staff remain available through More, which stays highlighted while viewing those modules.

The Coaching Hub shows roster passing/rushing/defensive strength, the next opponent's defensive matchups, editable playbooks, an opening call sheet and the last game's drive report.

## Playbooks and decisions

- Offense: Balanced, Spread, Air Raid, Power Run and Option.
- Defensive front: Balanced, 4-3, 3-4 and Nickel.
- Defensive approach: Balanced, Stop the run, Protect the pass and Pressure.
- Opening sheet: three possession priorities, each using the playbook, inside run, outside run, quick pass, play action or deep pass.

These choices guide entire drives, not individual snaps. They change pass/run mix, yardage, turnover risk and scoring opportunity using the existing roster-informed possession simulator. Run/pass defensive emphasis trades strength between matchups; pressure increases both takeaways and explosive-play exposure. CPU teams use the same simulation rules and choose default offensive identities from their passing/rushing personnel. Formation and concept effects are an initial tuning model, not a full assignment-based football simulation.

A saved plan belongs to its school, survives save/load and applies in regular-season, conference championship, bowl and CFP games. Changing jobs does not copy the old school's plan onto the new school. Each completed game stores the plans used and its drive summaries, including the opening choices, yards, turnover/scoring outcomes and a separate simplified overtime scoring entry. Reopening or editing a plan cannot reroll an already completed game. Save schema is 20; older saves default to an empty plan book and historical games without drive logs remain readable.

## Next playable layers

1. Replace drive abstractions with shared snap events: field position, down/distance, clock, possession changes and full overtime.
2. Build a watch/call screen using that event stream, with pause, offensive/defensive calls and fourth-down decisions. Quick simulation must consume the same engine.
3. Add personnel packages, formations/routes, quarterback reads, substitutions and fatigue.
4. Expand individual receiving/defensive/kicking statistics from events and show tactical feedback.
5. Tune across roster strengths and multiple seasons, then extend the visual refresh to the deeper recruiting, roster and staff views.

## Validation

Focused tests cover deterministic drive reports, exact reconciliation of drive scores/yards/attempts/turnovers to box scores (including overtime), playbook tendency differences, deep-pass risk/reward, defensive tradeoffs, copied call-sheet ownership, save/load and completed-game idempotency, and all postseason entry points. Determinism assertions now compare nested result values because games contain report collections. One pre-existing recruiting fixture now uses fixed prospect IDs rather than random IDs that changed its school-affinity outcome.

All 202 Release tests passed, including six careers spanning 45 combined seasons, in 11 minutes 41 seconds. Android Debug APK packaging and packaged-resource checks passed; five existing obsolete MAUI alert API warnings remain. The plan-save action captures its dynasty snapshot before awaiting persistence so navigation cannot redirect a save to a different career.

Local visual QA could not complete because the test emulator remained unauthorized to adb, including after a cold boot with its documented test option. No local screenshot/interactive validation is claimed. The GitHub emulator job now uploads its launch screenshot and UI hierarchy as `android-launch-ui-<build>` for follow-up review. Deeper Coaching Hub interaction and device layout checks remain outstanding.
