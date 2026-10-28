# Current work - Football Sim Engine

Updated October27,2026. Canonical workspace D:/AppProjects/Codex/Football Simulation Engine, feature/pro-franchise-foundation. Preserve main fdd3d4898b5bd07eeec8dba346024d61f5289474, both app identities, College saves and negotiated Pro contracts. No subagents or continuation automation. Exchange and management batches are accepted; no active build/test remains.

## Latest accepted - College392 / Pro74 draft-class exchange

College runtime source0215ad4, build run40640733536:693 combined Core/College tests and Android packaging passed with0 warnings/errors. Exact tested APK12795552207:
https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40640733536/artifacts/12795552207

Pro runtime/core source0b60dbe, build run40640142353:279 scoped Pro cases,16 helpers/source validator and packaging passed with0 warnings/errors. Exact tested APK12795895212:
https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40640142353/artifacts/12795895212

Exact two-app verification40642639385 at harness9b56110 passed independent source-input guards, refreshed core fixtures and the complete Android handoff. Evidence12796481744 is downloaded in .artifacts/exchange77-ui. Every player-exchange-check.json flag is true: real College export, native document save, native share sheet, Android document picker, preview/apply, five source identities/appearances,315 generated filler, unchanged roster/contracts/picks/debt/career, reload and unsupported-format rejection. College saved-file, Pro preview and imported-class screenshots reviewed. The College snapshot is deliberately synthetic (five actual roster seniors placed in the new latest-class field); unit tests separately exercise real departure capture and SQLite round trips. Do not describe this as a full season played on-device. Failed invocations75/76 were harness setup (missing school selection, stopping before explicit save); both are superseded, with no app-source changes required.

Additional final test-only cases passed on unchanged runtime source: complete seven-round small-class draft and2500-player full-class draft (12s,224 selections/2276 remaining,0 filler). Three College export/SQLite cases pass; six initial affected lifecycle/export cases pass. No duplicate long NFL/College/GameCast runs: normal simulation and finances are unchanged; reuse accepted73 annual20/5 and prior field evidence. Both package IDs, save schemas, signing and stable main remain unchanged. No active build/test or continuation automation.

Exchange behavior:
- College retains the latest full graduate/early-declaration class before roster removal, excluding transfers/releases/redshirts. The optional save field uses existing schema26; old summary-only departed cohorts are not fabricated. Export in Season center or Player departures, prepare JSON, then Save JSON file through Android's document picker or share it. Save older class files externally before the next full snapshot replaces the latest one.
- Pro imports during offseason free agency or an unstarted draft. File size/depth/version/IDs/attributes are validated; preview shows source/target years and translated ratings. Explicit apply replaces only prospects/scouting/shortlist. Small files receive enough generated prospects to reach320; larger classes retain all players. Rosters, signed contracts, debt, career and traded pick ownership remain intact.
- All14 skills and physical/base attributes translate deterministically by player ID/year; shared canonical overall is recalculated. Names, IDs, size and illustrated appearances survive. Drafted players receive the existing four-year rookie deal/first-round option lifecycle; no direct roster injection. All coaching roles may configure this global class without changing their management authority. Duplicate/current-roster/historical players and imports after a first selection are rejected.
- Opening a draft with index0 no longer automatically selects players; its explicit Start draft action preserves the setup window. Normal offseason advancement still processes CPU decisions; import before advancing free agency into the draft. Coordinator device harness follows the explicit start action where applicable.

## Prior management acceptance - Pro73


Final application source a04ee62, financial/management core43635c0. All-green run40628330665:
https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40628330665

Exact tested APK12789619809:
https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40628330665/artifacts/12789619809

Device evidence12790617877 is downloaded in .artifacts/pro73-ui. Every pro-presentation-check.json flag passed: visible phone totals/comparisons, draft grade/action, retention demand/action, camp ratings, Fold layout and light/dark themes. Phone career totals/draft outcomes and Fold light analytics screenshots reviewed. CI271 fast Pro cases,16 helpers,32-pair cap validator and packaging passed. Final refinement changes only Pro view column order/widths, not core/shared/College inputs.

Management foundation app/core43635c0 was accepted as Pro71: APK12789078287/run40625932825, exact-APK verification40627341025 at harnessf4c1f61/evidence12789613796 (.artifacts/pro71-accepted-ui). All analytics categories, weekly news, career expectations, camp, standings, visible portrait/landscape draft/retention actions and Fold/dark checks passed. Landscape retention and Fold screenshots reviewed. Original settings-only emulator rotation timeout is superseded by explicit WindowManager rotation and independent orientation/scroll synchronization; it was not an app error. Reuse this unchanged behavior with73;73 specifically rechecks its altered presentation.

Local271 fast Pro cases pass1m27 (.artifacts/roadmap-fast-tests-final.txt). Both additional long cases passed6m7 (.artifacts/roadmap-long-tests.txt):20 generated-contract and five sourced-contract seasons. All273 Pro cases therefore passed on final core; do not repeat long simulations for view/harness/docs-only changes. New source includes8 standings and7 franchise/scouting/analytics/news/fixture cases. Existing690 combined Core/College cases and College390/full GameCast44 acceptance remain reusable; no College/shared engine/renderer change. Do not relabel690 as exclusively Core or College.

## Completed user-selected batch (#1, #2, #4, #5)

- UI: first-column draft/retention actions, primary numerical columns visible on phones, focused analytics category screens, responsive camp/depth views, phone/landscape/Fold/light/dark checks. Secondary table details still scroll horizontally as in College.
- NFL standings: division and wild-card head-to-head/common/conference chains, aggregate strength of victory/schedule, tied scoring ranks, common/conference net points, net touchdowns and save-stable final coin toss. Multi-club reductions restart; wild-card selection preserves original division order. Rules source: https://www.nfl.com/standings/tie-breaking-procedures . Archived-season draft lookup explicitly uses the archive year, preventing the regression caught during this batch.
- Franchise depth: automatic staff-quality scouting and draft-stage combine confidence upgrades, bounded once-per-season measured staff growth, less-secure-club job-market priority, ownership/career context and history chart. Camp uses existing depth, availability, roster/cap and coaching-authority rules; no duplicate roster model.
- Analytics/news: position development comparisons, same-class/same-round draft outcomes, team/league recorded career leaders, shorter performance/roster/cap categories, completed-week factual reviews and explicitly projected late-season playoff pictures. Regular-season scope/deduplicated history, stable news IDs, no tied-game win streak and no premature weekly review are tested. No fabricated clinching claims.

## Deferred / next roadmap

#3 College-to-Pro draft-class export/import is now implemented and accepted above. Later work remains the audited Core/College/Pro test split, release validation and eventual Java archival/repository rename/branch-artifact cleanup. Do not merge unfinished work into main or perform cleanup ahead of the agreed release review. Further career/scouting/analytics enhancements can build on this accepted batch; source-perfect finance remains closed under the practical-estimate policy.

## Financial phase complete - practical model

Latest user direction: stop financial minutiae, use reasonable assumptions/estimates, keep the game simple and wrap finance promptly. This supersedes historical source-perfect/default-promotion gates. Follow docs/pro-practical-finance-policy.md. Missing/private/conflicting details remain game estimates in developer provenance; exact agreement with every public website is not a release gate. Keep the simulation and College independent of Pro finances.

## Financial-phase acceptance - Pro70 (reused where unchanged)

App/core fabf464, run40613034696 (opening device scope):
https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40613034696

All build/device jobs passed. Exact tested APK12783469883:
https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40613034696/artifacts/12783469883

Device evidence12784307093 is downloaded in .artifacts/pro70-ui. Real-roster default, compact/expanded cap panel, three rookie imports,32 allocations/735 opening assessments, contract/void views, light/dark appearance, reinstall/save and original-source/negotiated-save migration all passed. New source allocation, unchanged-source migration, negotiated-deal preservation and booked debt/career preservation proof flags are all true. Compact cap and expanded light-theme screenshots reviewed. No active build/test remains. This financial phase is complete under the user's practical-estimate policy.

Local257 Pro cases excluding unchanged twenty-generated-season case passed1m40, including five sourced/three imported-cap seasons;17 final source/hash guard cases passed1s;16 UI helpers pass. CI passed256 fast cases,16 helpers, the source validator and Android packaging. Reuse unchanged generated20 evidence from66, College390 and GameCast44. No duplicate local Android build for the final UI/resource batch; CI packages it.

## Final batch behavior

- New app franchises default to Real roster contracts; Generated contracts remains optional. The factory's existing generated default is retained for backward-compatible internal/test calls; the UI passes the selected mode explicitly.
- Cap panel shows rounded available room, budget, dead money, pending offers and season-start room. Exact salary/bonus/rollover/adjustment fields are behind Show cap breakdown. Contract accounting stays optional; offers retain250k controls/market defaults.
- Three previously unmatched fully guaranteed first-round deals now reconcile published annual cash/cap/total values: Sadiq, Cooper and Bain. Four playing years; fifth-year decision separate. All prior packed records remain equal. Pack2485 records, SHA447f32fb327df9ca3cf20a712b314bc882f8329067dd1164a48a80f7c17b153a; reviewed metadata hash updated. Existing saved deals are not overwritten with new imports.
- Final device scope verifies the source default, collapsed/expanded cap display, all three persisted rookie deals,32 allowances/735 opening assessments, contract/void UI, themes/reinstall and guarded old-source/negotiated-save handling.

## Four requested priorities - practical implementation

1. Reserve activation accepted63: healthy signed RES/INA/DEV players can activate with roster/cap/role checks and saved ACT state. Explicit unknown medical/league restrictions remain protected; no inferred clearance dates.
2. Contract accounting accepted66/69: cash/bonus/void/guarantee separation, realistic rookie terms/options, signed-deal preservation and guarded corrections. Roquan roster cash no longer becomes duplicate buyer-guaranteed salary. Unknown details use practical estimates under current policy.
3. Financial budgets accepted68, final default/UI in70: all32 adjusted allowances and735 independently reported opening cap assessments initialize new sourced franchises. No invented historical cash or forced website-room balances. Existing saves are not backfilled. Remaining publication differences are ordinary data maintenance.
4. Waiver/termination behavior accepted66/67/68: inherited original contracts, tagged release assessments, claim/award budget/role/roster rechecks, assignment-start salary accrual, June1 holds/splits and final-CBA-year handling. Current scope includes practice release/trade guards and explicit international exemption behavior; broader calendar/elevation details stay game approximations.

Financial acceptance is complete. Do not extend this phase with another bespoke player-by-player audit. Java Initial Base Upload archival and repository rename Football-Sim-Engine remain at the end of the broader project; main is untouched.

## Accepted APK/evidence

-69 app/core d71782a, all-green run40590454472, APK12774172307/evidence12773632366 (.artifacts/pro69-ui).253 fast CI/16 helpers/source validator/packaging and expanded opening device flow passed.254 local cases excluding generated20 passed1m52. Original source migrates; negotiated deal, booked debt and career persist. https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40590454472/artifacts/12774172307
-68 app/core afe8b73, run40587763626, APK12772514300/evidence12773465745 (.artifacts/pro68-ui),249 fast CI/16 helpers/source validator/packaging and opening device checks passed.250 local cases incl five sourced/three imported-cap seasons passed1m55.32 clubs/735 source keys/no historical-cash fabrication, Seattle liabilities/contract/void/themes/reinstall/save verified; compact-old budget screenshot reviewed. https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40587763626/artifacts/12772514300
-67 app faa55a1, run40584307166, APK12770853072/evidence12771961687.243 fast/16 helpers and termination device scope passed; refreshed Free agent card and disabled own/duplicate claims reviewed.
-66 app/core db88f28, run40579419877, APK12768613293/evidence12768544312.245 full cases10m6 incl20 generated/five sourced,243 fast and midseason waiver/assignment release device scope passed;333333 earned buyer cash after one week.
-64 source8b06fa7 APK12754790693/run40547793852; exact verification40573852505 at harness00944a7/evidence12766593020: finance/options/trades/six coordinator/June1/waiver/reload accepted. Initial fixture-glob failure superseded.
-63 reserve sourcea8ad14b, run40530334315, APK12745318137/evidence12745348663;21 focused reserve/camp/depth,220 fast and activation/disabled injury/contracts/ACT reload device checks.
-44 full GameCast/aging/field source4f6bb2c, run40438801865, APK12705098072/evidence12705962978.
-College390 source2acfcc1, run40438515279, APK12705660863/evidence12705718044+12705752478:690 legacy combined Core/College cases and both device flows. Do not relabel690 exclusively Core or College before auditing.

## Developer evidence / later maintenance

Raw source cache stays in data/pro/source-snapshots (32 team pages,507 opening pages,95 bonus pages,10 status-conflict pages and primary roster reviews). Do not refetch completed cohorts or delete this cache as disposable artifacts. Huge .artifacts franchise JSON can be136MB on one line; parse selected fields instead of dumping it.

Source dossiers/proofs are in docs/data, including opening debt, actual runtime comparison, guarantee semantic review, Roquan classification conservation and reviewed rookie cash/cap normalization. Source-perfect all32 room matching is explicitly deferred by user direction. Staged payment candidates remain review aids, not automatic imports. Four primary roster-review findings remain ordinary roster maintenance; they are not part of this financial completion gate.

Remaining requested roadmap: deferred draft-class exchange UI, audited test-category split, final release validation and end-of-project archival/repository rename/release cleanup. Historical parity notes are not current completion status. No automatic continuation exists.
