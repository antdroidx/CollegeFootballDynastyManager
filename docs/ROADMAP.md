# Dynasty Manager roadmap

Working continuation of the roadmap discussed in the "Dynasty Manager Roadmap" chat. Build 202's geography and Week 19 fixes were confirmed working by the user. Subsequent work is published through the GitHub development branch and APK workflow.

| Priority | Area | Current status | Next exit criteria |
|---|---|---|---|
| 1 | Recruiting fixes | Shared multi-school HS/portal offer engine implemented; interest, effort, withdrawals and capacity drive decisions | Keep current save compatibility and single ownership across recruiting paths |
| 2 | Recruiting balance | Expanded: six seeded careers, including 130-team leagues, games and prestige movement; see build-205-career-validation.md | Full-league, multiple-seed comparisons with changing results/coach strength and CPU peers |
| 3 | Long-term dynasty health | Expanded: five/ten-year careers with weekly portal decisions, games, age/depth checks and annual save/load | Longer careers with games, deferred transfers, position age distributions and save recovery |
| 4 | Interface polish | Builds 209–211 replace the initial layout with a save-only Home, separate setup and career overview, compact controls and responsive recruiting; emulator flow/layout checks added | Continue refining deeper roster/staff screens and game-day presentation using device feedback |
| 5 | Game-day control | Started: saved playbooks, opening drive call sheets and persisted drive reports | Shared snap events with field position, downs and clock, then live watching and per-play calls |
| 6 | Broader modernization | Future | Remaining position detail, CSV workflows, faces and presentation |
| 7 | NIL and coaching budgets | Later; design backlog only | Separate NIL resources and coaching/staff budgets, sustainable multi-year commitments and recruiting/carousel integration |

## Current delivery

- Show an animated loading screen while recruit list calculations run off the UI thread; build visible rows in small batches.
- Cancel outdated loads on navigation or a replacement request and prevent stale results from overwriting the new view.
- Recognize CPU-signed recruits in visible interactions, assistance and manual actions, and prevent double-signing during resolution.
- Add repeatable five-cycle diagnostics rather than assuming that a single working season proves balance.

## Next work

1. Expand balance/health diagnostics to FBS scale and additional seeds, including simulated wins/losses and prestige movement.
2. Investigate low-prestige walk-on dependence and age/position bottlenecks from those measurements; preserve organic commitment timing and roster-based portal decisions.
3. Continue interface clarity once the recruiting and dynasty-health checks are dependable.

Competing offers are now a core engine feature (Build 204). CPU and user schools share persisted offers and one decision resolver. CPU direct signings and protected user targets have been removed. The next balance baseline must use this shared market; the original Build 203 report is historical.

Build 206 adds a selectable 138-team 2026 FBS structure with saved league configuration, modern conference game counts, original HS recruiting stars and a roster career-stats view. See build-206-modern-fbs-careers.md for limitations and validation.

Build 207 starts UI modernization and coaching controls. Drive-level playbooks and opening call sheets now affect the shared simulation. Full live per-play calling follows the snap-event layer; see build-207-coaching-ui.md.

## Later economy additions

NIL and coaching budgets are deferred while the UI, playcalling and career-history layers develop. Plan for separate NIL resources and school coaching/staff budgets, coach salaries/contracts/buyouts, multi-year affordability forecasts, and recruiting/transfer offer integration. Coaching preferences should influence spending. Avoid hard transfer/recruit signing caps or a money-only recruiting winner; preserve competition, player preferences and long-term roster health. This is a game-design backlog, not an implemented economy or a model of current NIL regulations.

Build 208 adds coach career histories and an archive: user/CPU head coaches and other staff retain season results by identity through moves and departures. Head-coaching records are separate from team results earned in other staff roles.

Builds 209–211 revise the initial UI after feedback: Home starts/loads careers, active dynasty navigation is separate, saves are grouped by dynasty, and short landscape windows keep recruiting controls beside the list. This remains an iterative UI, not a final visual design. See build-209-save-library-ui.md for scope and device validation.
