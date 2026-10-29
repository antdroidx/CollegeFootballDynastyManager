## Accepted Pro95 — CPU roster movement audit

App/Core616c897, run40794034836:323 exact-source scoped Pro tests,16 helpers,32-pair validator and Android package0 warnings/errors pass. APK: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40794034836/artifacts/12862539615 . Device evidence12863940517 (.artifacts/pro-cpu-roster95) passes available reserve activation, injured action blocked, contract preservation and reload. CPU management itself is verified in Core whole-league tests, not claimed as on-device annual simulation. Local full324 pre-final-fallback candidate passed11m7 including20 generated/5 sourced seasons; final34 movement/option guards and12 corrected waiver/movement cases passed, and final CI323 includes both three-season scenarios. Pro94 failed waiver handling and is superseded. Main/College unchanged.

Next user steering: trim oversized practice squads by lowest rating, initialize opening reserve injuries to a four-game default, use verified season-ending public reports where possible, and implement timed recovery/CPU return. Keep unsigned rookie rights and non-medical restrictions distinct. Pro95 is accepted; new IR lifecycle work has not yet changed app inputs.

## Accepted Pro93 — future rights and compensatory allocation

App/Core source56cf7c3. Run40780183506 passed313 scoped Pro cases,16 helpers,32-pair source validator and Android packaging0 warnings/errors. Exact tested APK: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40780183506/artifacts/12857186828 . Device evidence12857057721 downloaded in .artifacts/pro-roster-update93. All ten acceptance flags pass:672 unique standard opening rights/selected multi-hop ownership, visible reserved rights, correct offseason2027 default, actual UI-created Seattle franchise, Nelson/Buckner contract/roster ownership and trade compensation, Nelson roster row, non-tradable projections, synthetic2027 awarded selections and save/exit. Reserved and awarded screenshots reviewed: Seattle has five opening standard rights (four allocated/one conditional), then ten total selections after five synthetic awards. Synthetic fixture does not claim a season played on-device.

All315 full local Pro cases pass7m35, including20 generated and5 sourced seasons (.artifacts/future-full-final.txt). CPU debt/roster failure is repaired without erasing liabilities. Reuse unchanged College399/398 and Pro44 GameCast evidence; no College/shared source changes. Existing saved careers retain ownership/contracts; historical opening snapshots apply only to new franchises. Private undisclosed conditions use documented published-round approximation, not invented clauses. Main remains fdd3d4898b5bd07eeec8dba346024d61f5289474. No active build/test or automation. This roster/pick/compensatory batch is accepted; broader cleanup/archive/rename remain later roadmap work.

## Accepted Pro92 — roster trades and compensatory allocation

App/Core source ef274e6. Run40774235744 passes305 fast Pro cases,16 helpers/source validator and Android packaging0 warnings/errors. Exact tested APK: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40774235744/artifacts/12853967833 . Device evidence12855070402 is downloaded in .artifacts/pro-roster-update92. All roster-update flags pass: actual UI-created Seattle franchise, Nelson/Buckner ownership/active contracts and exact three compensation picks in its save, visible Nelson roster row, non-tradable projections, synthetic valid32-team2027 awarded-pick display and save/exit. Nelson and award screenshots reviewed. The award fixture is explicitly synthetic; this does not claim a season played on-device or all real future ownership imported.

Local all305 full cases (before two final additional guards), including generated20 and sourced5 seasons, passed3m44; final11 targeted cases include the two old-save/seed-merge guards. CI's final fast count is305. Reuse unchanged College399/398 and Pro44 GameCast evidence. Main untouched; no automation. The wider real 2027-2029 pick ownership audit remains open; developer candidate JSON is not runtime data. Next reconcile/import all known allocations with conditional rights, preserving existing saved trade history. Pro92 is accepted and no build/test is active.

## Active roster/compensatory allocation batch — October28,2026

Canonical D:/AppProjects/Codex/Football Simulation Engine, feature/pro-franchise-foundation. Latest accepted remains Pro91/c55531f documentation, app5c65aae. No new APK accepted yet. New factory-only confirmed Nelson/Buckner trades and three compensation picks are implemented with existing salary-assignment accounting, preserving existing saved careers. A Pro-only compensatory system tracks eligible expired-contract signings, cancellations/early cuts, public APY bands, four/team and32/league quotas, separate sourced staff entitlement, yearly allocation and stable extra pick IDs. Opening 2027 estimates are projected separately; future simulated markets feed subsequent drafts. Draft numbering/completion and top51-vs-full-roster rookie budget planning are updated; a coordinator three-season regression exposed the latter and passes after the repair.

New Draft picks screen browses teams/years and separates allocated picks from non-tradable projections. Details/limits in pro-compensatory-draft.md. All305 full local Pro cases including generated20 and sourced5 annual scenarios passed3m44 (.artifacts/comp-full.txt), then11 final targeted cases passed including two newly added old-save/seed-merge guards. Final scoped CI should contain305 fast cases. Local Android compile passed0 warnings/errors before final Core/harness refinements;16 helpers and32-pair source validator pass. Dedicated roster-update device scope is prepared; CI/device acceptance pending. Reuse unchanged College399/398 and full GameCast evidence. Main untouched; no automation.

IMPORTANT remaining task: all-team real future ownership audit is still incomplete. docs/data/pro-future-pick-audit-2026-10-28.json is research only, not runtime data. It captures70 candidate rows and source review, but multi-hop identities and conditional rights remain unresolved. Do not claim the full real allocation task complete or apply guessed ownership. Only the two confirmed trades' three picks are imported so far. Next reconcile that ledger/conditional reservation and source conflicts after accepting this batch. Archive/rename remain last.

## Accepted Pro depth/schedule/coach batch — Pro91 / College399 compatibility

Final Pro app source5c65aae; core/shared/catalog source046a1d7. Run40747333613 passes294 fast Pro cases,16 UI helpers/source validator and Android packaging0 warnings/errors. Exact tested APK: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40747333613/artifacts/12842951177 . Device evidence12843181188 is downloaded in .artifacts/pro-staff-calendar91. All staff-calendar-check flags pass: numeric depth order, exactly three preseason weeks, phase-correct Current marker while browsing regular season, four named playoff rounds, Seattle staff illustrations, actual new Kansas City franchise and Andy Reid light/dark. Preseason picker and Andy Reid light screenshot reviewed. Calendar starts from an explicitly synthetic preseason2 save; Kansas City is created through normal UI. Pro90 source046 build was superseded by the one-line live-position-order follow-up; no further app inputs changed.

College399 source046a1d7 run40746521680 passes703 combined Core/College cases and packaging0 warnings/errors. Compatibility APK: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40746521680/artifacts/12842041575 . Shared portrait optional flags default to unchanged College/player rendering; reuse accepted College398 UI evidence, without claiming a new College399 device walkthrough. No engine rules/RNG/save schema/package/signing/financial changes. Main remains fdd3d4898b5bd07eeec8dba346024d61f5289474. No active build/test or automation.

Behavior: both Pro roster organizer and live substitution picker use the shared College numeric position sequence, with existing Pro grouped OT/OG/OC labels retained. Schedule choices use phase-qualified week identities: preseason3, regular18, playoffs4. Changing phase clears stale week selection; All phases supports explicitly qualified choices; Current appears only in the actual active phase/week, and offseason has none. Official-headshot-referenced illustrated appearances cover all127 named bundled coaches (one vacant slot excluded), include explicit eyewear/moustaches and stable normalized-name identity across team changes. Unknown/generated staff keep deterministic fallback. No photos are shipped. See pro-coach-portrait-review.md and nfl-coach-appearances.json for primary references. Eight new focused cases pass; reuse unchanged annual, finances, exchange and full GameCast acceptance.

## Pro schedule/depth/coaches implementation — completed — October28

User requests College-order depth positions, phase-aware schedule weeks and closer illustrated real-coach faces. Implementation uses shared College position numbering with Pro grouped OL labels; schedule choices have three preseason weeks,18 regular weeks and four named playoff rounds, phase-qualified selections/current markers and safe reset on phase changes. ProCoachPortraits embeds127 visually reviewed official-headshot-derived art profiles, stable by normalized identity; vacancy/generated fallback unchanged. No photos bundled, source names/ownership/coaching ratings/saves untouched. PersonPortrait gains optional explicit glasses/moustache flags; default College/player rendering stays unchanged.

Eight affected schedule/depth/portrait tests and all294 fast Pro cases pass; local Pro Android build0 warnings/errors before final data/harness refinements. Both final Android packages/device acceptance pending. Dedicated staff-calendar device scope checks preseason2 vs regular2 markers, depth order, playoff options, Seattle staff and real Kansas City new-franchise Andy Reid light/dark portraits. Prior College398/Pro88 remain accepted until this batch passes. Reuse unchanged financial/annual/GameCast evidence; no main or cleanup changes.

## Accepted shared news/roster UI — College398 / Pro88

Final app/shared source9cc34c5. College398 run40739215922/APK12839440855 passes703 combined Core/College cases and packaging0 warnings/errors. Pro88 run40739211829/APK12837753565 passes286 fast Pro cases, helpers/source validator and packaging0 warnings/errors. Exact tested APKs:
- Pro: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40739211829/artifacts/12837753565
- College: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40739215922/artifacts/12839440855

Exact two-app verification40740101064/evidence12839197534 (.artifacts/roster-sync-exchange89) passes source equality guards, actual wide filter bounds (all four College controls share a row), opponent roster selection, no duplicate profile button, restored own-team controls, contextual Pro team navigation, direct native save/share/import, reload and preserved roster/contracts/picks/debt/career. College wide roster screenshot reviewed and confirms one row. Test class/display fixture remains explicitly synthetic, not a season played on-device.

Pro84 presentation40732039464/evidence12837400520 (.artifacts/roster-sync-pro84) supplies unchanged news/card, light/dark, management and landscape evidence; Pro landscape roster screenshot reviewed. Only final compact-filter geometry differs; final89 rechecks that component in both hosts. Earlier functional exchange85/87 and flex-row candidates are superseded for final geometry. No unrelated annual/cap/full GameCast scenarios repeated. No active build/test or automation.

Both apps now use FootballNewsCard for left-aligned thumbnail/metadata/headline/preview/story links and a shared responsive card grid. CompactFilters uses explicit responsive Grid columns to avoid native flex rounding. College team links and legacy Teams navigation route to the unified roster with team picker; redundant Profile column removed. Opponent injuries follow the selected roster, own depth-chart management stays hidden for opponents and returns on own selection. Pro roster filters occupy one row on wide screens; phones use available columns. No simulation/finance rules, save schemas, package IDs, signing or stable main changes. Source workspace remains D:/AppProjects/Codex/Football Simulation Engine; branch feature/pro-franchise-foundation.

## Accepted contextual UI — October28,2026

College394 source f2ec0ad/run40724719708/APK12832202972:703 combined Core/College cases, packaging0 warnings/errors. Pro81 app f345f90/core f2ec0ad/run40725321084/APK12833235834:286 fast Pro cases (five new calendar-label cases), helpers/source validator and packaging0 warnings/errors; presentation device acceptance passed. Evidence12833821291 in .artifacts/contextual-pro81. Original Pro80 build is superseded by phone-width headline refinement.

Exact two-app verification40727973325 at harness59127ee/evidence12833838921 (.artifacts/contextual-exchange82) passes all flags, including actual header-to-team-page navigation, native College save/share, Pro import/reload and preserved contracts/roster/career. Team-page screenshot reviewed: unboxed linked names and headers. Existing synthetic class/display fixture scope remains explicit. No unrelated annual/GameCast tests repeated. No active build/test remains.

Changes: removed default button outlines, restoring flat player links, College headers/sidebar while preserving text contrast; Pro team cells and main team banner open a roster/schedule team page, College school banners use existing OpenSchool navigation. Pro game cards use linked team cells. Pro overview contains three actual headline/body previews plus News/Statistics links. Structured transaction phase displays P1/W1/PO1/offseason, with unknown legacy stages labeled honestly. No save schema, financial rules, simulation, package or main changes.

Exact APKs:
- Pro: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40725321084/artifacts/12833235834
- College: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40724719708/artifacts/12832202972

# Pro completion checklist

## Active October28 UI/efficiency batch

- [x] Shared light/dark contrast and new Pro navy/teal palette.
- [x] Table/navigation/disabled-state polish; direct export; simpler trades/contracts.
- [x] Shared documented game-derived efficiency and league comparisons in both apps.
- [x] Scoped tests, both Android packages and affected device acceptance. Pro78 run40648950218/APK12798821913; College393 run40648953014/APK12798946403; two-app verification40651057545/evidence12800585172 passed. See current-work.md for exact scope.

See ui-efficiency-plan.md. Prior College392/Pro74 remain latest accepted. New source ceeeb34 packages College393/APK12798946403 (703 combined tests) and Pro78/APK12798821913; device acceptance is pending in40648950218.


## Draft-class exchange - accepted College392 / Pro74

- [x] #3 latest full College departure snapshot, versioned JSON export/preview and native document save/sharing.
- [x] Pro file picker/preview/apply, deterministic full-attribute translation, appearances/IDs, source-year mapping and small-class filler.
- [x] Started/duplicate/malformed imports rejected; roster/contracts/debt/picks/career and saves preserved.
- [x] Normal rookie signing and complete small/2500-player seven-round drafts verified.

College392 APK12795552207/run40640733536 (source0215ad4),693 combined Core/College tests. Pro74 APK12795895212/run40640142353 (source0b60dbe),279 scoped Pro cases. Both package with0 warnings/errors. Two-app exact verification40642639385 at harness9b56110/evidence12796481744 passes every native save/share/picker/import/reload/identity/conservation/unsupported-rejection flag; screenshots reviewed in .artifacts/exchange77-ui. Additional test-only full-draft cases pass locally without runtime changes. Current-work.md has exact links and actual synthetic-fixture scope. No active work/build remains; main unchanged. Earlier #3 deferral and setup-failure records are superseded.

## Current requested batch - accepted Pro73

- [x] #1 UI finishing batch: phone-visible primary actions/numbers, focused analytics, camp and portrait/landscape/Fold/theme checks.
- [x] #2 NFL postseason division/wild-card chains, reductions/restarts and correct archived-season draft lookup.
- [x] #4 Staff/combine scouting, measured annual staff development, camp/depth battles and career/ownership/job-market improvements.
- [x] #5 Development/draft comparisons, recorded career leaders, completed weekly reviews and projected late-season playoff news.
- [x] #3 College-to-Pro draft-class exchange screens: reactivated and accepted in College392/Pro74 above.

Final appa04ee62/core43635c0. All-green run40628330665; exact tested APK12789619809:
https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40628330665/artifacts/12789619809

271 fast Pro cases/16 helpers/source validator/packaging and presentation-device checks passed. Evidence12790617877 in .artifacts/pro73-ui; phone metrics/draft/retention/camp, Fold and light/dark flags all true; screenshots reviewed. Core's additional generated20/sourced5 cases passed6m7 (273 local Pro cases combined). Pro71 management-device verification40627341025/evidence12789613796 supplies unchanged category/news/career/camp/standings and portrait/landscape action evidence. Earlier rotation harness timeout is superseded. No College/shared inputs changed; reuse College390/full GameCast44 and690 legacy combined Core/College cases. Main unchanged. No active build/test or continuation automation.

Current-work.md is authoritative. Financial phase remains complete under practical estimates. Remaining later roadmap: deferred exchange UI, audited test split/release validation, then agreed archival/rename/release cleanup. Historical unchecked items below are not a fresh task list.

## Current result - financial phase complete (Pro70)

User-approved practical-estimate policy: use best available real contracts with reasonable game estimates, simplify the visible finances and stop source-perfect auditing as a release gate.

- [x] Reserve activation with roster/cap/role checks and saved status (63; unchanged evidence reused).
- [x] Practical contract accounting and guarded source corrections; preserve negotiated contracts/booked debt (66/69).
- [x]32-team adjusted budgets/opening assessments, real-roster default, sensible fallback estimates and optional cap breakdown (68/70).
- [x] Original-contract waiver assumptions, assignment accrual, timed termination rules and preserved obligations (66/67/68).

App/core fabf464, all-green run40613034696. Exact tested APK12783469883:
https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40613034696/artifacts/12783469883

257 relevant local Pro cases passed1m40 including five sourced/three imported-cap seasons;17 final source/hash checks passed; CI passed256 fast cases/16 helpers/source validator/packaging. Device evidence12784307093 in .artifacts/pro70-ui verifies default source selection, compact/expanded cap UI, three rookie imports,735 opening source assessments, contract/void views, themes/reinstall and guarded source migration/negotiated-deal/debt/career preservation. Compact and expanded screenshots reviewed. Main/College unchanged; generated20/College390/GameCast44 evidence reused. Source inconsistencies and four staged roster-review findings are ordinary later maintenance, not financial blockers.

## Historical implementation records

The earlier source-perfect/default-promotion gates below are superseded by the user direction and current practical policy. They are retained as history, not active financial tasks.

## User scope update - practical financial completion

October27: stop detailed source-perfect accounting work, make reasonable estimates and wrap finance promptly. Historical strict source/default gates below are superseded by docs/pro-practical-finance-policy.md. Current finish: real-roster default, compact cap summary with optional details, already-reviewed three rookie imports.257 relevant local cases passed including five sourced seasons; final APK/device verification pending. No additional private-guarantee/source-room audit gate. Preserve saves/main/College.

## Latest accepted - Pro69

App/core d71782a, successful run40590454472, APK12774172307/evidence12773632366.253 fast CI/16 helpers/source validation/packaging and expanded opening device checks passed. Original-source migration/negotiated deal/debt/career preservation verified.254 final local cases excluding unchanged generated20 passed1m52. APK: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40590454472/artifacts/12774172307

## Latest accepted - Pro68

App/core afe8b73, successful run40587763626, APK12772514300/evidence12773465745.249 fast CI Pro/16 helpers/source validator/packaging and focused opening device flow passed. All735 dated opening assessments/32 clubs/no invented historical cash/source keys/exact Seattle liabilities/contract display/themes/reinstall/save verified; screenshot reviewed. Final250 local cases (including five sourced/three imported-cap seasons) passed1m55; unchanged generated20/College390/GameCast44 evidence reused. APK: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40587763626/artifacts/12772514300

Next guarded Roquan guarantee/cash classification correction is in progress.34 focused cases and all254 final local cases excluding unchanged twenty-generated-season case passed1m52. APK/device checks remain pending. Source default and broader32-team current-contract reconciliation remain open.

## Latest accepted - Pro67

App faa55a1, all-green run40584307166, APK12770853072/evidence12771961687.243 fast Pro cases/16 helpers/source validator and packaging passed. Exact termination device scope verifies held June1 savings, inherited waiver terms/reload,333333 one-week buyer salary, refreshed Free agent card and disabled own/duplicate claims; screenshot reviewed. Core/data are66; reuse its245 full/annual cases. APK: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40584307166/artifacts/12770853072

Active follow-up: source-only new-franchise initialization for735 observed prior-contract assessments across32 teams, verified Seattle roster/current-club changes and typed international practice exemption.Final250 local cases excluding unchanged twenty-generated-season case passed1m55, including five sourced and three imported-cap seasons. Local Android build passed0 warnings/errors in2m15. Pro68 app/core afe8b73 in run40587763626 passed249 fast CI/16 helpers/source validation/packaging; focused opening device acceptance remains active. Existing saves and generated mode remain unchanged; sourced default stays gated. No College/shared engine inputs changed.

## Latest accepted — Pro66

App/core db88f28: run40579419877, APK12768613293, device evidence12768544312. All245 full local cases passed10m6,243 fast CI cases,16 helpers, source validator and packaging passed. Midseason claim/award/reload/release verified333333 cash after one elapsed week, preserving inherited terms and June1 held savings. Exact APK: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40579419877/artifacts/12768613293

A small UI follow-up refreshes cards after contract/roster actions and disables own-release/duplicate claim actions. Android local build passes0 warnings/errors; core/data remain66 and their long tests are reused. Further source reconciliation/default promotion remains open. Read-only Seattle proof matches observed charges exactly without a balancing entry but is not yet app initialization. All four tasks are not claimed complete.

## Current acceptance and follow-up

Pro64 app/core8b06fa7 is accepted. APK12754790693/run40547793852; exact-APK device verification40573852505 at harness00944a7 passed all scoped flows, evidence12766593020.239 full local Pro tests (5m22),237 fast CI cases,16 helpers and source validation passed. Finance/options/trades/coordinator/June1/waiver/reload checks passed; screenshots reviewed. Original fixture-glob failure is superseded. Exact APK: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40547793852/artifacts/12754790693

Post64 candidate repairs retroactive buyer salary accrual and unpaid guaranteed option transfer on trades, preserves original signing/lock data, and adds23 verified records (2482 optional sourced contracts total). Six accrual cases and31 combined contract/waiver/termination cases pass; all245 full cases passed in10m6s, no next APK yet. New device scope checks midseason claim/award/reload and release after one week. See current-work.md for exact source/evidence and actual gaps. Real-source default remains gated, and all four requested tasks are not yet complete.

## Accepted reserve activation — Pro63

App/core source `a8ad14b`. [Exact APK63](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40530334315/artifacts/12745318137), [all-green run40530334315](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40530334315), [device evidence12745348663](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40530334315/artifacts/12745348663). Local Android build passed with zero warnings/errors.21 focused reserve/camp/depth cases and16 helpers passed; CI passed220 scoped Pro cases, helpers, source validator and packaging. Exact device flow verified available activation, disabled injured action/reason, unchanged contracts, retained blocked player, saved ACT state and reopening. Screenshots reviewed in .artifacts/pro63-ui. A stronger real-loader contract-equality assertion also passed locally after the build source; it changes tests only, not the APK.

Open a reserve player's profile and use Activate to roster near the header, or Contract & roster actions / Activate from reserve. Activation checks team ownership, retirement, unsigned rookie rights, injury, known clearance restrictions,90/53 roster limits, active salary minimum, actual cap and pending offers. It preserves existing terms/negotiation locks except a required current minimum. Coordinator authority is enforced. Generic imported RES/INA/DEV codes do not establish a dated IR event; healthy signed players can be activated as game roster decisions. Explicit IR/PUP/suspension/exempt statuses with unverified clearance remain blocked. Full dated medical-return lifecycle is not claimed complete.

Reserve task is accepted in this scope. Next three priorities remain real-contract semantics,32-team financial reconciliation, and full waiver/legally timed termination rules. Reuse62 financial/annual evidence and unchanged College390/GameCast44; no new College test/build required. Main and both app identities unchanged. The user removed automatic continuation; do not recreate it.

## Current four-task priority (user requested)

1. Reserve-to-active roster activation first: available imported reserve players, actionable/disabled reasons near the profile header, roster/cap and career authority checks, saved status and device acceptance. Known medical/league restrictions and unsigned draft rights must not be bypassed. Do not infer an IR start date from generic RES status.
2. Complete real-contract bonus/guarantee/vesting/void/release accounting, using a dated offline raw-source dataset before corrections.
3. Reconcile all32 dated team ledgers, player identity/status, departed debt and adjustments; enable real contracts by default only once reconciled.
4. Complete waiver original-contract assumption and legally timed release/trade treatment, with consistent previews and conserved obligations.

The user removed automatic continuation. Do not recreate it. Work resumes when the user continues this chat. Pro63 is accepted for reserve activation; Pro62 financial evidence remains reusable.

Current verified Pro application/core source: `a8ad14b`, APK Build63 (reserve activation); Pro62 financial/trade/coordinator evidence, full unchanged GameCast Pro44 and College390 remain reusable. See current-work.md for exact artifacts and scope.

## October 26 batch — Pro APK 33 financially verified; College 389 preserved

- [x] Contract concern protection/grace and legacy migration.
- [x] Current/offer/market tabs, annual salary/bonus/cap/guarantees and release schedule.
- [x] Rollover, tracked cash floor, minimum salary schedules, bonus cash and seeded cap inflation.
- [x] Strength/performance-based future picks; annual transaction history; late-season Overview bracket.
- [x] In-season coordinator staff changes; All-Pros/Pro Bowl naming; Front Office capitalization.
- [x] Ordinary development disclaimers removed; editable photo-reference complexion overlay.
- [x] 98 local Pro cases including twenty seasons; Pro Android packaging.
- [x] Pro 31 and College 389: all Android build/device jobs passed; exact links in current-work.md.
- [x] CPU restructure/depth/rookie budget follow-up included in Pro 33; 108 local Pro cases and 106 scoped CI cases passed. Clipped-tap/helper and workflow-selector errors fixed; 12 helper regressions and exact-APK financial device checks passed.

## Verified in Pro Build 30

- [x] Shared College simulation/GameCast, canonical 19 attributes/overall, animation/field, kickoff/coin toss, quick calls, playbooks, replay and controlled-side/role restrictions.
- [x] Separate Pro package/saves/workflow, real-team roster/staff pack, city abbreviation badges, distinct palette, College-style responsive sidebar/themes/profiles/grids and visible save deletion.
- [x] Compact FA/position filters, sortable contract/stat/draft grids, full-team/weekly schedule navigation and automatic scout reports.
- [x] Two-team trade workspace, future-pick filters/ownership, trading block, value assessment, suggestions and trade history.
- [x] Negotiations/counters, $250K controls and market defaults, extensions/restructures and once-per-season completed-change lock.
- [x] Coordinator future unit/team commitment preflight, including pending bids and atomic rejection; repairs allowed.
- [x] Weekly positive/negative morale talks, gray cooldown, signing reactions, contract concern/trade requests and inbox alerts/read status.
- [x] 90-player camp / 53-player final roster, cap/roster regular-season gate and no calendar advance on blocked attempts.
- [x] Draft selection cannot stall on CPU cap; unsigned reserved rookies sign when affordable and remain unavailable until then.
- [x] Starting/legacy rookie term repairs preserve current cap charges; future financial projections remain recorded as estimates.
- [x] League-relative analytics and preseason empty state; current/historical playoff bracket with byes/results/reseeding.
- [x] Expiring-contract desk, explicit let-expire decisions and optional staff negotiations.
- [x] Illustrated appearance editor and matching portrait/live seven-tone skin palette; saved choices verified.
- [x] Background library loading, visible progress, migration indexes/cache and protected controls.
- [x] Saved awards/all-stars, isolated exhibition, factual thumbnail-only news and efficiency/box-score grids.
- [x] 91 Pro cases including twenty-season estimated-contract stability; full Pro Android acceptance passed.

## College / Core preservation

- [x] Main remains unchanged; College package identity and save schema retained.
- [x] College coach naming, non-user team choice and obvious deletion implemented.
- [x] 682 fast cases passed in the legacy combined Core/College suite; individual categories remain unaudited.
- [x] College Build 388 packages and career Android checks pass.
- [x] College Build 388 broad Android acceptance passed, including management/themes and shared live-view/replay/advance regression checks.

## Contract accounting batch — core 6faa77f, final UI 7687443, Build 36 verified

- [x] Ten targeted cash/void/term/migration/trade/expiry regressions; all 118 Pro cases passed locally, including 20 generated + 5 sourced + 3 imported-cap seasons.
- [x] Full source pack refreshed to October 26: 2,012 matched contracts; developer audit records 519 generated roster fallbacks and unresolved official accounting basis.
- [x] Financial CI tests and Android packaging passed in run 40405152646.
- [x] Exact Build 35 financial/contract device acceptance passed: APK artifact 12690317621, evidence 12689689721, run 40405152646.
- [x] Final UI follow-up: readable Void column/compact pay-table screenshot. Build 36 all jobs passed: run 40406332938, APK artifact 12690204175, evidence 12691288745; 116 scoped tests and exact-APK financial/contract/theme/reinstall acceptance.
- [ ] Finish broader source reconciliation and option rules before enabling the sourced default.

## Seasonal cap counting — verified in Build 39

- [x] New-save v2 seasonal counting, excluded salary, full opening-day gate, conservative CPU planning and inbox warning. Build 39 passed 124 scoped cases; exact-APK financial acceptance succeeded in 40416144714. Annual and final-cut evidence is detailed in current-work.md.
- [x] Final-cut accounting uses opening week 1; targeted regression passed. Build 38 was superseded by verified Build 39.
- [x] v0/v1 compatibility, v2 reinstall persistence and 32-team arithmetic checks verified; official opening commitments remain unreconciled.

## Next authorized financial phase

- [ ] Correct sourced employment/void/cash/guarantee semantics and dependent expiration/release calculations. Initial accounting implementation is verified in Build 36; full option/vesting/source liability reconciliation remains pending.
- [x] Replace GUID-random fallback terms with source playing dates/rookie metadata and explicit estimates; preserve known later released/re-signed deals.
- [ ] Real contracts default for new franchises after reconciliation; preserve existing saves and explicit generated mode.
- [x] Simplified main contract display with optional detailed accounting, verified in Build 36.
- [ ] Source-dated team ledger reconciliation and affected financial/device verification.

See [phase details](pro-contract-reconciliation.md). Checked items are included in verified Build 36. Unchecked source/option/default items remain pending.

## Newly reported live-view layout

- [x] Pro landscape quick-play controls fit without scrolling: compact header/pinned controls, flexible field height, three horizontal choices, collapse after selection, expand field, restore choices after the snap and More for secondary actions. Source `a2c74e8`, Build 37 run `40408864223` all green. Device assertions and before/after screenshots reviewed. APK artifact `12691773553`; evidence `12693410970`. Broad Pro GameCast, watch/replay, saved final/reload/advance and coordinator checks also passed; College/shared source unchanged.

## Newly requested field presentation — after current financial batch

- [x] Pro uppercase city end zones, abbreviated badges retained; LOS ANGELES and cameras visually/device verified in Pro 44.
- [x] Both games: idle referee/ball aligned with LOS and clear of huddles; live/replay spotting separated. Geometry and Pro 44/College 390 device/screenshot acceptance passed.

## Newly requested Pro development / aging

- [x] Pro age-aware ceiling and remaining-growth outlook, separated from elite current ability. Never imply a ceiling below current overall; College attributes/behavior preserved. Verified in Pro 44.
- [ ] Calibrate position-specific aging/regression (including athletic vs mental/technical attributes) and verify annual decline/retirement over several seasons; avoid a uniform age cutoff for every position.
- [x] Pro roster/player rating delta, saved prior-season baseline, neutral/up/down styling and legacy fallback; generated prospects cannot inherit fictional prior-pro history. Verified in Pro 44.

## Outstanding acceptance

- [x] CPU autonomous trade offers/counteroffers, accepted Pro56 and reused management acceptance.
- [x] Coordinator shared-pick value review, verified in Pro 45; rejects unjustified future draft-capital spending atomically.
- [x] Supplied 2026 base/rollover/adjustment allowances: 32 teams, exact reconciliation, separate budget components and new-save initialization.
- [x] Measured/reserved annual rollover and next-year contract changes; 108 local Pro tests including three imported-cap seasons.
- [ ] Complete opening source commitments/departed-player dead money and historical cash reconciliation. **Source accounting remains experimental.**
- [x] Finance-scoped exact Pro 33 APK verified in run 40366831239; source guard permitted reuse and avoided a rebuild.
- [x] First-round fifth-year exercise/decline decisions, accepted Pro46/64 finance scope.
- [ ] Finish deeper source guarantee/vesting and waiver assignment-accrual follow-up; initial original-contract waiver flow accepted64.
- [ ] Richer scouting/combine/camp, CPU roster/contract calibration, automatic coordinator opposite-unit management and deeper career/job-market behavior.
- [ ] Further rating/morale/statistical balancing and growth/scatter/draft-outcome analytics.
- [ ] Additional factual records/playoff-race/weekly-roundup stories.
- [ ] Test audit and a genuine Core/College/Pro split without misleading relabeling.

See [testing-scope.md](testing-scope.md): use finance-scoped Pro checks here and reuse unchanged College 389 evidence. Keep the continuation active while roadmap items remain. Do not merge unfinished Pro work or delete branches/artifacts as a substitute for acceptance.

Latest verified Pro: [Build 37](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40408864223/artifacts/12691773553), full Pro walkthrough plus landscape assertions passed. Earlier downloads: [Pro 33](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40365289405/artifacts/12669723503), tested in successful [verification run 40366831239](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40366831239) · [College 389](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40352915636/artifacts/12665525718), all College jobs passed. Pro 33 uses financial-scoped device acceptance; latest Pro GameCast walkthrough is Build 37; unchanged College remains verified at 389.

## Final repository cleanup — after current game tasks

User authorized October 26; perform last, after the current implementation and verification batches.

- [ ] Identify files introduced by the original `Initial Base Upload` commit and audit current references/build/data dependencies.
- [ ] Archive obsolete Java/Android project files under top-level `Archive/` (preferred reversible choice), or remove if clearly unnecessary. Preserve any assets/data still used by C# projects and update their references instead of breaking builds. Do not rewrite history or delete unrelated branches/releases as part of this request.
- [ ] Rename GitHub repository from `antdroidx/CollegeFootballDynastyManager` to `antdroidx/Football-Sim-Engine`.
- [ ] Update Git remotes, README/docs, workflow references, badges and source/download links as applicable; verify the renamed repository and both app build paths. Preserve application package IDs, saves and signing/update compatibility.


Financial QA retry 40415642843 passed the exact-APK source guard and all 13 helper cases, then stopped because the contract term was visible at the bottom while the adjacent APY remained below the viewport. Evidence confirmed correct profile/term; the harness now explicitly scrolls to the APY before asserting it. This does not change application inputs. Reuse Build 39 again; it is not accepted until the complete financial flow passes.


## Financial Build 39 accepted

The exact [Build 39 APK](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40414202682/artifacts/12694921409), application source `561dc4f`, passed financial device acceptance in [QA-only run 40416144714](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40416144714). [Evidence 12695832869](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40416144714/artifacts/12695832869). Source/build-input equality passed, 13 helper regressions passed, 124 scoped Pro cases and packaging passed in the original run. The complete financial flow checked imported terms/cash, compact/void details, all 32 cap allocations, signed adjustments, reinstall/save and light theme. Screenshots reviewed. Prior 125-case annual run and eight final-cut regressions remain as recorded above; College/shared code unchanged, reuse College 389. Seattle modeled camp room is $5,672,598 vs full-season room -$9,422,775, not a claim of official $13.442M reconciliation. No balancing credits invented; sourced default remains experimental.

## Active Pro aging batch — not in Build 39

Added Pro-only position-specific peak ages, earlier athletic than technical/mental decline, shrinking future headroom, and persistent previous-season overall. The canonical College 19-attribute overall formula remains unchanged. Growth outlook is separate from present ability/projected ceiling; elite veterans no longer carry misleading young-prospect growth stars. Roster delta and profile annual change expose offseason decline; old saves without previous-season history use an existing baseline or unknown, never fabricated history. Trade/pick forecasts use the same age-aware headroom. Fifteen targeted development/shared-rating cases passed; broad annual/Android validation remains pending. Do not call this batch accepted until its build/device checks pass.

Aging batch: local Android compilation passed with zero warnings/errors; 15 targeted tests passed. Full annual regression started; Android acceptance pending.


## Active shared field batch

Pro end zones now use uppercase home city, retaining abbreviated scoreboard/center badges. Both hosts mark idle scrimmage huddles for ball spotting: central official uses projected LOS, offset across the field to clear both huddles, and a ball is drawn at the spot. Shared animation disables idle spotting before live/replay movement; kickoff huddles and coin ceremony remain separate. Eight goal-line/midfield/both-direction geometry cases were added; 36 affected shared presentation/live cases passed in Release. App device/visual acceptance remains pending. Pro aging APK source stays `56e7d1b`; this field batch is later and must be verified in both apps. Do not claim College 389 covers these new shared changes.


Aging validation at `56e7d1b`: all **136 Pro cases passed locally in 4m03s**, including twenty generated-contract seasons, five sourced-contract seasons and imported-cap lifecycle coverage. Local Android build passed, zero warnings/errors. Run 40438135849 reached APK packaging; supersede it with the combined aging/shared-field build at `2acfcc1` to avoid a redundant interim APK/device walkthrough. The next Pro `live` scope reuses the completed annual evidence, reruns affected fast cases and performs the full Pro UI/GameCast flow. College run 40438515279 verifies the shared field batch with both device flows, without repeating unrelated long career/calibration simulations.

Draft-generation review found template players could carry their previous-season OVR into newly generated prospects. Clear that additive history field at creation; generated draft players have no prior pro season. Fifteen affected tests passed, including the new assertion. Supersede 40438635362 before packaging to include this correction in the final combined APK; College source/verification is unaffected.


## Next Pro-only batch — coordinator draft-capital review

ProManagement now reviews actual pick/player ownership changes after a coordinator trade recommendation, in addition to existing current/future unit/team salary checks. Spending shared picks requires incoming package value to justify outgoing players/picks (10% valuation tolerance); projected pick strength/year discount uses existing ProTrade valuation. Rejection keeps rosters/contracts/picks/transactions unchanged and records a GM reason. Pick-only coordinator proposals remain GM-controlled; ordinary draft selections are not mistaken for traded picks. Twenty-six relevant career/trade/GM cases passed locally, including two new asset-review regressions. This is **not in Build 44** (source `4f6bb2c`); do not cancel its active device acceptance. Scope/package the next Pro-only batch after Build 44 is accepted. Autonomous CPU trade proposals/counteroffers remain outstanding.


## Accepted combined aging / field batch — Pro 44 and College 390

- Pro application source `4f6bb2c`: [APK 44](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40438801865/artifacts/12705098072), [all-green run](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40438801865), [evidence](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40438801865/artifacts/12705962978). 134 scoped Pro cases, 13 helpers, packaging and full device walkthrough passed. Local 136-case annual evidence at `56e7d1b` plus 15 targeted cases after the prospect-history fix is retained. Device checks cover growth/delta display, play/watch/team control, kickoff/quick calls, collapse/field expansion, replay, themes/Fold, saved final, reinstall/reload/advance, appearance and career restrictions.
- College/shared application source `2acfcc1`: [APK 390](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40438515279/artifacts/12705660863), [all-green run](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40438515279), [broad device evidence](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40438515279/artifacts/12705718044), [career evidence](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40438515279/artifacts/12705752478). 690 legacy combined Core/College cases, packaging and both Android flows passed. Do not relabel 690 as exclusively Core or College.
- Screenshots visually reviewed: Pro projected ceiling/growth/delta, landscape cards before/after collapse, LOS official/ball, readable LOS ANGELES end zones; College landscape LOS spotting and normal quick calls. Both cameras/animated flows passed their existing device checks. Eight new geometry cases cover both directions and goal-line/midfield locations. No simulation rules/College attributes changed.
- Stable main independently reconfirmed unchanged at `fdd3d4898b5bd07eeec8dba346024d61f5289474`.
- Later Pro-only coordinator pick-value guard `c9cddbf` has 26 targeted tests passed but is not in APK 44. Its scoped financial/management build follows; no redundant College/GameCast rerun required.

Coordinator draft-capital review is packaging/verification run 40440713067 (finance scope), application source c9cddbf. Accepted full GameCast remains Pro 44; accepted College remains 390. Hourly follow-up was updated with these completed batches and current queue; it remains ACTIVE.


## Latest accepted Pro — Build 45, coordinator draft-capital guard

Accepted application source `c9cddbf`; subsequent participation-foundation code is not in APK 45. [Exact APK 45](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40440713067/artifacts/12705638936), [all-green run](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40440713067), [financial device evidence](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40440713067/artifacts/12706705306). All 136 scoped Pro cases, 13 helper cases, packaging and exact-APK financial device acceptance passed. Twenty-six relevant GM/trade/career cases also passed locally. This Pro-only guard changes no College/GameCast behavior: reuse full Pro 44 and College 390 evidence above, plus the 136-case annual aging run and post-prospect-fix targeted checks. No redundant College rebuild or full live walkthrough for this small management change.

Coordinator future pick/value review is implemented and verified. Autonomous CPU proposals/counteroffers remain unfinished. First-round fifth-year choices, full sourced-contract/departed-debt/historical-cash reconciliation and default promotion remain outstanding. The indexed NFLPA cross-check supports the supplied rounded allowances but does not reconcile official opening balances. Retain experimental source accounting. Continue the roadmap, with Java archive/repository rename last. Main remains unchanged; no merge/release cleanup performed.


## Active fifth-year-option foundation — observed participation (not in APK 45)

Added Pro-only additive `RookieParticipation` rows for first-round players' first three seasons. Regular-season finals record observed offensive/defensive personnel and team-play denominators, including zero participation for injured/reserve players still rostered. Kickoffs, special teams, penalties, stoppages, preseason and postseason do not count. Per-game IDs prevent retry/replay double-counting; the franchise final guard remains in place. Missing personnel/partial seasons stay unknown; old saves are not backfilled with invented usage. Tier evaluation handles two observed 75% seasons, weighted three-year 75%, and 50% in every fully observed year. Financial obligations, player ratings and shared/College code are unchanged.

Five new cases plus thirteen foundation cases passed locally (18 total), covering actual simulated final/retry, save serialization/defaults, per-unit coverage, zero participation, weighted thresholds and incomplete legacy data. All **143 Pro cases passed locally in 3m58s**, including twenty generated-contract seasons, five sourced-contract seasons and imported-cap lifecycle coverage. The Pro core builds with zero warnings/errors. This is a building block, **not completed fifth-year exercise/decline functionality**. Still implement original rookie-contract origin, statutory decision/guarantees, positional tender history/pricing, CPU/GM decisions, post-draft deadline and Front Office/inbox UI. Defer another APK until the option feature is reviewable; reuse Pro 45/College 390 acceptance for unchanged released behavior and run appropriate option-specific tests/device checks on the completed batch.

Participation foundation checkpoint: committed after the verified Pro 45 source; no new APK was dispatched for this internal building block. Continue original-rookie-deal identity, tender history/prices, actual exercise/decline guarantees/deadline/CPU/GM and UI, then package and verify the completed option batch. No active build or local test remains. Main and College code are untouched.


## Active complete rookie-option batch — Android acceptance pending

First-round original rookie contracts now have an explicit exercise/decline desk in Front Office and an inbox link. The window lasts from the end of season three through the following draft; unresolved manual decisions block leaving that draft stage. Exercise fully guarantees year-four salary and adds a fully guaranteed fifth-year salary while preserving existing bonus accounting. Decline keeps the original deal, final decisions cannot be repeated, later extensions remain distinct from the statutory option, and trade rebasing preserves original rookie identity. Existing exercised/source-extended five-year deals are not offered again. Fixed option-year salaries retain rookie contract-concern protection.

Annual positional salary/cap samples feed versioned, frozen game-generated tender prices using available cap-percentage history and the four usage/Pro Bowl tiers. Missing pre-franchise usage and five-year salary history remain unknown/estimated in developer metadata. These simulated prices are not a claim of announced real NFL option prices or complete source reconciliation. No missing historical records were fabricated. CPU decisions use the same cap checks; user delegation is optional, opposite coordinator units are staff-managed and own-unit exercises retain GM future-budget review. The desk shows current/future salary guarantees, added cap charge, retained bonus charge and future room with themed confirmations.

Local validation: all **150 Pro cases passed in 3m28s** before four final edge-case additions and the fixed-option concern guard; all **26 affected option/usage/GM/rookie-concern cases passed afterward**. Long runs include twenty generated-contract seasons, five sourced seasons and imported-cap lifecycle coverage. Automated continuity scenarios explicitly delegate user options rather than bypassing the new deadline. Thirteen UI-helper cases passed. Android compiled locally with zero warnings/errors before the final concern-only follow-up. The scoped financial device script now includes isolated, clearly synthetic exercise/decline saves and checks persisted fourth/fifth guarantees; it never changes the source-cap QA franchise. Package/verify this batch with finance scope; reuse unchanged College 390 and full Pro 44 GameCast evidence. Do not claim a new accepted APK until device checks succeed. Latest accepted remains Pro 45.

Rookie option build 46 run 40445569937, app source 26cb625, packaged artifact 12708396236 / pro-football-apk-46. Device-script review caught a fixture assertion using Advance week on an offseason screen (correct label Advance offseason). Correct the harness only and reuse this exact APK with the source/build-input guard; supersede the active old test invocation rather than wait for its known wrong-label timeout. This is not accepted device evidence yet.

Next trade-AI design/acceptance is in docs/pro-trade-ai-plan.md. A further code-audit gap is explicit: ProStandings still documents foundation ordering rather than the full NFL multi-team/common-opponent tiebreak chain. Keep that NFL rules/conformance work on the unfinished roadmap; do not claim complete league-rule parity. User-requested College-style visual hash spacing remains an intentional Pro presentation preference.


## Latest acceptance — Pro 46 rookie options

Pro 46 application source `26cb625` passed 152 scoped Pro tests and packaging. The exact APK is https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40445569937/artifacts/12708396236 . Harness-only retry `acac104` corrected the synthetic offseason label; run https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40446386891 passed source-equality guard, helper tests and exact-APK financial plus exercise/decline device checks. Evidence: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40446386891/artifacts/12707954405 . Persisted fourth/fifth-year guarantees and unchanged negotiation lock were checked. Reuse full Pro 44 GameCast and College 390 evidence. Earlier pending-option paragraphs below are historical and superseded by this acceptance.

## Active correction — 2026 allowance source interpretation

The browser-rendered NFLPA public report observed October 26 shows two spending/room series per club. Both independently produce the same allowance from reported Team Cap + Cap Room. The supplied CSV's adjusted-cap column matches rounded spending, not that allowance. Preserve the supplied bytes as `nfl_2026_salary_caps.user-original.csv`; corrected exact-dollar allowances and separate carryover/derived adjustments are in the normalized CSV. Raw observed pairs, observation metadata and original hash are in docs/data. All 32 pairs reconcile; four derived adjustments are negative. Displayed report subtotals have opposite $2 rounding differences, with identical combined totals; no balancing liabilities are added. Seattle corrected allowance is $315,053,262, rollover $15,029,706, other adjustment -$1,176,444. This supersedes earlier claims that the original rounded allowances were validated.

New franchises use cap-rules version 3 (same seasonal counting). Version-2 saves retain their existing budgets unless the user explicitly selects Update 2026 cap budgets. Eligibility requires all original 32 allocations unchanged, base unchanged, 2026 not closed; custom, legacy and later-year budgets are excluded. Correction changes allowances only and preserves all contracts, transactions, debt and elections. Reported room is never imported as opening game cash/room. Source roster/guarantee/departed-debt reconciliation remains unfinished and real-source default remains off.

Current verification: 32-pair source validator and 13 UI helpers passed. One stale rollover expected value was corrected; all 157 Pro cases passed in 4m20s, including twenty generated seasons, five sourced seasons and imported-cap lifecycle. No corrected-budget APK is accepted yet. This batch also makes option history compact and inactive staff decisions clearly unavailable. Main/College/shared engine unchanged.


## Accepted corrected-budget batch — Pro 50

Application/data source `61022d9` (core change `61ec66e`). Exact [APK 50](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40449248836/artifacts/12710150676). Build run 40449248836 passed 155 scoped Pro cases, source-data verification, helpers and packaging. Harness-only correction `9f4938c` waits for completed save-library layout. [Verification run 40451229268](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40451229268) passed application-input equality guard, 14 helpers and exact-APK financial device flow: new budgets/all 32 equations, signed adjustments/source cash, themes, reinstall/save survival, explicit old-budget correction with contracts retained, rookie-option exercise/decline and persisted guarantees. [Evidence](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40451229268/artifacts/12709764334), downloaded `.artifacts/pro51-ui`. Corrected old-save budget and compact/disabled option-history screens were reviewed. All 157 full Pro cases passed locally in 4m20s, including annual simulations. No College/shared application changes; reuse full Pro 44 and College 390 evidence. Source obligations/default promotion and remaining roadmap remain unfinished.

## Active autonomous-trade batch (after accepted Pro 50)

The next Pro-only batch adds persisted CPU offers/counters/expiry and limited CPU-to-CPU purchases through ProTrade.Propose. User players never move automatically; accept revalidates ownership, cap, roster and coordinator authority. CPU departures preserve specific OL/kicking roles and depth; full buyers release only legal surplus in the immutable transaction, so failed future-budget review discards the cut/debt/logs too. Current/full-opening/future obligations and pending bids are checked, future picks are limited to the next three classes, recent trades cannot immediately flip players, and acquired upgrades start. Pick suggestions share a bounded, least-cost package search instead of paying a first-round pick when smaller assets fit.

Twenty-four affected local trade/GM cases pass; Android local build passed with zero warnings/errors before the latest core-only follow-ups. Full Pro annual regressions are running; an added saved-offer test and conditional fixture exporter need verification afterward. New offer desk/Front Office/inbox integration is not yet in an accepted APK. Latest accepted remains Pro 50. Finance-scope device acceptance now also counter/accepts and declines isolated synthetic offers generated by the core regression test, validates persisted player/pick ownership and depth/history, and leaves the source-cap QA franchise untouched. The compressed fixture travels beside the APK as test evidence, not inside either application. No College/shared app change; reuse College390/full GameCast Pro44. Finish all failures and exact-APK evidence before marking this batch accepted.


Trade batch validation checkpoint: all **168 Pro cases passed in 6m47s**, including twenty generated-contract and five sourced seasons. After the non-semantic pick-projection index optimization, new save-store case and conditional fixture export, all **40 affected trade/GM/cap/comparison cases passed in 25s**, including the three-season sourced-cap lifecycle replay. The replay recorded 11/1/4 CPU trades in 2026/2027/2028, no silent user trades, 53 minimum active players and $2,283,589 minimum cap room at its final checkpoint; future first-round holdings across the next three classes range 1–5. Full developer metrics are in docs/data/pro-trade-three-season-calibration.json. Fourteen helpers and the 32-pair cap validator pass. Generated synthetic offer fixtures are under 1MB compressed and verified; test-output environment paths must be absolute because testhost's working directory is bin/. Android local build passed before core-only follow-ups; next CI must package/verify final source. No new trade APK is accepted yet. Reuse Pro50 finance baseline, College390/full Pro44 unchanged evidence.


## Accepted autonomous-trade batch — Pro 56

Application source `0e72ba4`, core `e899094`. [Exact APK56](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40456067627/artifacts/12712506988), [all-green run40456067627](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40456067627), [trade device evidence12713315926](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40456067627/artifacts/12713315926). Build passed167 scoped Pro cases,15 helpers, source validator and packaging. Exact-APK focused trade flow passed counter/accept/decline with persisted player/pick ownership, history and acquired starter depth. Offer/history screenshots in .artifacts/pro56-ui were reviewed; phone result column is visible. Fixtures are explicitly synthetic (altered age/ratings/budget), generated by the core tests and carried outside the installed APK; not real roster/cap facts.

All168 full Pro cases including twenty generated and five sourced seasons passed before the projection-index optimization/new save case;40 affected cases including three sourced-cap seasons passed afterward. Calibration11/1/4 CPU trades by year, no automatic user trades, final minimum active roster53, cap room$2,283,589 and next-three-class first-round holdings1–5. Reuse unchanged College390/full Pro44 and finance source/option assertions completed in Build52 before the fixture-selection harness failure, plus full accepted Pro50 finance evidence. Pro56 is trade-device scope, not a fresh complete financial/GameCast walkthrough. Earlier failed/cancelled runs are historical; no active build/test remains. Continue coordinator staff work per pro-coordinator-staff-plan.md; source financial reconciliation and remaining roadmap remain unfinished.

## Active batch — coordinator retention (after accepted Pro56)

Uncommitted Pro-only retention fix: ProManagement shares its future/team/unit budget evaluator with an internal staff-unit path, preserving the user's coach identity and authority. ProRetention automatically negotiates the coordinator's other unit (including specialists), while own-unit choices remain manual/delegated. Retention advancement includes a fired user's former team as CPU-managed. Head-coach manual/delegated behavior and LetExpire remain unchanged; no contract-change-lock policy was altered. The screen explains own-unit delegation and gray availability for jobless/unavailable actions.

Forty targeted career/GM/retention cases passed, including OC/DC symmetry, both head-coach delegation settings, own-unit manual expiry, unemployment, future-unit atomic rejection and three coordinator seasons with explicit GM roster/draft delegation. That annual scenario does not claim automatic opposite-unit roster/draft work is complete. New isolated OC/DC device fixtures and a retention-only flow are prepared; local fast suite and final APK/device acceptance still required. Auto FA/waiver/cut/draft handling for the uncontrolled unit remains the next coordinator task, per pro-coordinator-staff-plan.md. Latest accepted remains Pro56. No College/shared app change; reuse accepted evidence and long HC annual runs where unaffected.

Coordinator retention checkpoint: all **179 scoped Pro cases passed locally in 51s**, including three coordinator seasons and imported-cap lifecycle, plus **40 targeted cases** in22s and15 UI helpers. Source-only staff-unit authority reuses the existing GM evaluator; no global market/contract-lock/HC CPU policy change. OC/DC synthetic fixture exports are valid (about376KB each), isolated from production. New `retention` device scope validates the delegation explanation, advances the stage with own-unit auto negotiation off, and checks saved opposite-unit renewal, manual own-unit expiry, job identity and GM decision. Finance scope invokes this same flow after its existing checks; helpers can select saved slots below the fold. Package and verify this batch; do not report a new accepted APK yet.

## Accepted coordinator-retention batch — Pro57

Application/core source `4294f8e`. [Exact APK57](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40458818959/artifacts/12713836374), [all-green run40458818959](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40458818959), [device evidence12713851967](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40458818959/artifacts/12713851967).179 scoped Pro cases,15 synchronization helpers, source validator, packaging and OC/DC retention device flow passed. Both roles' UI explanations, own-unit delegation off, next-year advancement, opposite-unit renewal, manual-own expiry and persisted job/contract/GM outcomes were checked. Screenshots in .artifacts/pro57-ui reviewed. Synthetic fixture numbers/ages are not real roster facts.

Forty targeted career/GM cases including three coordinator seasons with explicit GM roster/draft delegation also passed locally. No College/shared/market/trade mechanism change; reuse College390/full Pro44 and accepted Pro56 trade evidence. Head-coach long-simulation evidence is reused for unchanged branches. Retention is complete; automatic opposite-unit FA/waiver/cut/draft handling remains next, alongside documented financial/source/market/pending-bid and broader roadmap. No active build/test remains. Preserve existing contract-change-lock behavior unless a concrete failing case justifies a policy change.


Additional architecture audit: PlayerExport v1 and ProPlayerImport.Convert exist and have a foundation test, but no app caller exports a college draft class or imports one into Pro scouting/draft. Complete the actual College export / Pro import UI and safe draft-stage integration later; do not claim the unused conversion helper delivers this user feature. Preserve all19 canonical attributes and version/duplicate validation. The current converter scales only part of PlayerSkills and returns free agents; audit skill completeness, age/eligibility, deterministic identity and scouting uncertainty before integrating.

## Active coordinator roster/draft completion (not yet APK accepted)

- [x] Implement automatic other-unit FA bids using existing CPU manager and shared GM review.
- [x] Implement other-unit waiver claims with budget preflight and signing-time recheck.
- [x] Implement staff-only cap repair/final cuts preserving controlled contracts and specific OL/kicking coverage.
- [x] Implement automatic other-unit draft selection, own-unit recommendation stop, unsigned rights on budget rejection and signing-time GM review.
- [x] Quote pending offers using actual future minimums and exact bonus allocation.
- [x] Final regressions/annual evidence and exact-APK58 coordinator device acceptance; see current-work.md.

Waiver financial mechanics still use the existing simplified release/new-deal model; source reconciliation, full waiver assumption rules, market calibration, draft-class UI integration and remaining roadmap remain open.

Pro58 verification follow-up: packaging and196 fast cases passed. Original device run verified retention but refused the invalid one-team synthetic draft save; corrected32-team fixtures and a real-loader check are being verified through an exact-APK retry. Do not mark roster/draft device acceptance complete until the retry passes.

## Accepted coordinator roster/draft batch — Pro58

Application/core source `aad411d`. [Exact APK58](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40462840012/artifacts/12714304881). Build run40462840012 passed196 fast Pro cases,15 UI helpers,32-pair source validation and packaging. Harness-only source6cf9b3b [verification run40485812027](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40485812027) passed app-input equality,16 fixture/loader cases and all six OC/DC retention/roster/draft device scenarios. [Device evidence12725815166](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40485812027/artifacts/12725815166) is downloaded in .artifacts/pro60-ui; draft and roster screenshots reviewed. Persisted staff FA acquisitions, other-unit GM draft selections/own-unit stops, controlled contract/ownership preservation, coach identity, retention/expiry and advancement all passed. Synthetic fixtures are outside the APK and do not represent real roster/cap facts.

All196 full Pro cases passed locally in6m41s before two extra rollback cases;18 final targeted cases passed afterward including four real-loader fixture round trips. Long generated/sourced seasons and the existing three coordinator seasons with explicit broad GM delegation are included in the earlier full run; do not describe that annual test as an automatic-only coordinator career. Core cases additionally cover future-unit rejection, reserved unsigned rookie rights/recheck, waiver signing-time review, safe cuts and rollback. The exact-APK retries corrected invalid one-team fixtures and offscreen-control synchronization; no production save-validation change, app rebuild or duplicate long run was needed. Earlier failed runs are superseded for acceptance.

Pending offers now share exact bonus remainder and future service-minimum quoting across GM/trade review. Staff use existing CPU transactions and shared budgets without role impersonation. Main, College/shared engine and GameCast remain unchanged: reuse College390/full Pro44 and prior trade/finance evidence. Waiver original-contract assumption and financial source reconciliation/default promotion remain unfinished. Mobile draft/retention tables still need more accessible primary actions without horizontal hunting; this acceptance proves the scoped decisions, not completion of all UI/roadmap work.

## Positional financial-market calibration evidence

- [x] Developer audit uses canonical ratings and distinguishes veteran source APY from original rookie/PS/generated terms;1,113 samples and all2,531 contracts reconcile. Both existing32-team ledger and new market audit modes run.
- [x] Bounded positional/rating pricing, specialist fallback and multi-season balance verified in Pro62; signed contracts remain unchanged. The original market audit is developer evidence, with implementation/acceptance detailed below.

Market implementation checkpoint bd0ada7 (not APK accepted):15 frozen bounded monotone curves and specialist fallback are implemented with cached quotes; five market/save/depth tests added. Initial fast run had191/197 pass; updated accounting/role fixtures preserve existing assertions and use actual legal/market offers. A genuine PHI2029 roster/debt failure is being fixed via conservative future planning, with no debt forgiveness. Do not mark pricing complete or rebuild/promote default until scoped and long financial validation pass. See current-work.md for active test session.

Current pricing checkpoint:15-curve candidate and six new market/comparable/save/depth cases are implemented but unaccepted. Last broad run excluding known sourced lifecycle failure:201/202 pass; the remaining unrealistic low-cap restructure fixture was corrected and11 affected cases pass. Generated TwentySeasons is running (session23820/log in current-work.md). Sourced PHI2029 void-debt/roster failure remains; all debt conservation/roster assertions remain active. No new APK/real-source default promotion. Read current-work.md before continuing to avoid duplicate tests or treating the source failure as a mere fixture change.

Cash/rollover repair checkpoint: generated TwentySeasons passed8m5s before new cash policy. The formerly failing sourced three-season PHI2029 scenario now passes with all liabilities/assertions retained, together with three cash-window cases (4/4 in46s). CPU now protects known future deficits and uses fully tracked multi-year floor cash, not an annual90%-of-adjusted-cap spending target; missing2024/25 history does not create a made-up floor bill. Whole-suite/long rerun and APK finance acceptance remain required; no new APK yet.

Full financial candidate checkpoint: all209 Pro cases pass in9m55s, including20 generated/five sourced seasons and the exact previously failing imported-cap lifecycle; four cash/future-buffer cases pass.15 helpers,32-team source validator and byte-identical curve regeneration pass. Next build/verify finance APK. Source/guarantee/departed-debt reconciliation and real-source default promotion remain separate, unfinished gates; no new accepted APK yet.

Pro61 packaging/207 scoped cases succeeded. Finance/source/options UI assertions completed; trade helper then waited for a hidden control below a retained save-library scroll offset. A shared readiness regression now covers scrolled/busy overviews (16 helpers). Opening a save also explicitly resets Overview to the top for users, requiring a new APK; reuse unchanged209-case/core/long/College/GameCast evidence. Device acceptance is still pending; no new APK is accepted yet.

## Accepted financial calibration — Pro62

Application source `2ac5992`, financial core `94d77bc`. [Exact APK62](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40523114687/artifacts/12741879655), [successful run40523114687](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40523114687), [device evidence12742934656](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40523114687/artifacts/12742934656). Packaging,207 scoped Pro regressions,16 UI helpers and the32-pair source validator passed. Device acceptance passed32 persisted budgets/negative adjustments/reconciliation, financial themes, sourced annual cash and compact/expanded void displays, reinstall/save survival, explicit old-budget correction preserving contracts, rookie-option exercise/decline, trade counter/accept/decline with persisted ownership, and all six OC/DC retention/roster/draft scenarios. Evidence downloaded to .artifacts/pro62-ui; compact contract and light-budget screenshots reviewed. The displayed QA budget is a synthetic/test franchise, not a verified real-world opening balance.

All209 full local Pro cases passed in9m55s on unchanged financial core, including20 generated seasons, five sourced seasons and the formerly failing imported-cap lifecycle.15 bounded positional market curves, multi-year tracked cash-floor policy, future-deficit spending protection and saved-franchise scroll reset are accepted in this scope. Reuse unchanged Pro44 GameCast and College390 evidence; this is not a fresh shared-renderer acceptance. Main/College/package identities remain unchanged. Build61's partial flow is superseded by62. No build/test remains active.

Next: source-dated paid/future bonus events, vesting and departed-debt reconciliation. Real-source default remains off; no source liabilities or historical cash have been invented. Existing209-case evidence does not establish complete real-source accuracy. Remaining roadmap remains authorized.

Reserve activation checkpoint:13 focused reserve cases plus8 camp/depth regressions pass (21 total). New activation preserves contract terms/locks except a required current active minimum, updates roster status/depth and rechecks pending bids/cap. Profile action appears before long attributes, with disabled reasons; contract/roster menu also supports it. Unknown EXE/suspension/IR/PUP clearances remain blocked rather than inventing eligibility. Imported generic RES/INA/DEV availability is treated as a game decision only when healthy and signed. Separate reserve device scope uses core-generated32-team fixture and avoids repeating unchanged finance/GameCast flows. Android packaging/device acceptance pending.

Offline financial preparation: complete raw Parquet +32 team pages are consolidated in data/pro/source-snapshots/2026-10-26 (ignored local evidence; preserve outside build cleanup). Manifest records53,507 raw/2,540 current rows,2,532 unique OTC IDs and8 duplicate identities. New row-level comparison accounts for all2,539 current financial rows:1,997 matching,490 missing source contracts,32 identity/status reviews and20 differences. Reported debt residuals remain separate. Source defaults/accounting are unchanged; notes capture completed its bounded pass under separately dated2026-10-27:25 cached notes,70 request timeouts, all explicit in the report. Offline regeneration works; no missing guarantees are guessed. No new app build is needed for developer-only source preparation.

Active post63 batch is unaccepted. Original waiver terms/debt tagging, source event/vesting metadata, immediate guarantee-cap/separate-cash treatment and June1 timing/designations are implemented;13 affected transaction cases and11 cash/planning cases including three sourced seasons pass after CPU repairs. Full long/Android acceptance and32-team source/default gates remain open. See current-work.md for active source acquisition; do not stop after a partial milestone.
