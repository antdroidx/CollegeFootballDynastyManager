# Dynasty Manager roadmap

Working continuation of the roadmap discussed in the "Dynasty Manager Roadmap" chat. Build 202's geography and Week 19 fixes were confirmed working by the user. Subsequent work is published through the GitHub development branch and APK workflow.

| Priority | Area | Current status | Next exit criteria |
|---|---|---|---|
| 1 | Recruiting fixes | Shared multi-school HS/portal offer engine implemented; interest, effort, withdrawals and capacity drive decisions | Keep current save compatibility and single ownership across recruiting paths |
| 2 | Recruiting balance | Expanded: six seeded careers, including 130-team leagues, games and prestige movement; see build-205-career-validation.md | Full-league, multiple-seed comparisons with changing results/coach strength and CPU peers |
| 3 | Long-term dynasty health | Expanded: five/ten-year careers with weekly portal decisions, games, age/depth checks and annual save/load | Longer careers with games, deferred transfers, position age distributions and save recovery |
| 4 | Interface polish | Recruit loading overlay and background list preparation added | Device check; clearer targeting reasons, comparisons, development summaries and consistent layouts |
| 5 | Game-day control | Future | Live watching and play calling built on shared simulation events |
| 6 | Broader modernization | Future | Remaining position detail, CSV workflows, faces and presentation |

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
