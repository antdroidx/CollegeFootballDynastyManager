# Current work - Football Sim Engine

Updated October27,2026. Work only in D:/AppProjects/Codex/Football Simulation Engine, feature/pro-franchise-foundation. Preserve stable main fdd3d4898b5bd07eeec8dba346024d61f5289474, College identity/saves and Pro update signing. The user removed automatic continuation; do not recreate it. No subagents authorized.

## Active user priorities - UI, rules, franchise depth, analytics/news

User deferred College-to-Pro draft-class exchange UI (#3). Work on #1 UI/UX, #2 NFL tiebreaks, #4 career/offseason and #5 analytics/news now. No subagents or continuation automation. Finance remains closed. Candidate adds primary first-column draft/retention actions, category-based analytics/development/draft outcomes/career records, automatic staff/combine scouting, once-per-year measured staff development, less-secure-club career prioritization, weekly summaries and late-season projected races. NFL postseason tiebreaks use official division/wild-card chains, restart after reductions, preserve division order between wild cards, aggregate opponent records and seeded final coin toss. Source: https://www.nfl.com/standings/tie-breaking-procedures .

Candidate is NOT accepted yet. Initial local Android compilation passed 0 warnings/errors in2m36 before final report-persistence change. Five initial standings and automatic-scouting cases passed. First broader local run found a real prior-season draft lookup regression (fixed by explicitly using archive year), plus fixture-export failures from a missing output directory (environment setup, not production). Final271 fast Pro cases and16 UI helpers now pass. Both annual cases passed in6m7 (.artifacts/roadmap-long-tests.txt):20 generated seasons and five sourced seasons. Pro71 app/core43635c0 packaged successfully in run40625932825 (APK12789078287); management device scope is running. Camp/depth-battle screen uses existing starter/roster guards. Latest accepted APK remains70 below. Both status docs will record exact final evidence. College/shared renderer unchanged; reuse their acceptance.

## Financial phase complete - practical model

Latest user direction: stop financial minutiae, use reasonable assumptions/estimates, keep the game simple and wrap finance promptly. This supersedes historical source-perfect/default-promotion gates. Follow docs/pro-practical-finance-policy.md. Missing/private/conflicting details remain game estimates in developer provenance; exact agreement with every public website is not a release gate. Keep the simulation and College independent of Pro finances.

## Latest accepted - Pro70

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

Broader remaining roadmap is retained in pro-completion-checklist.md/pro-parity-audit.md: actual College draft-class exchange UI, further career/offseason/analytics/news polish, NFL tiebreaks/mobile actions and audited test-category split. Preserve all completed work; no automatic continuation exists.
