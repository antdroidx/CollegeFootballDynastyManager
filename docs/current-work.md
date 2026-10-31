## Accepted Pro115 / College412 — guided offseason, compact overview and backfield corrections

User scope: Pro roster FB order/identity audit, compact College-style overview with weekly tasks, automatic dedicated offseason screens, dense post-draft training camp, draft board/results/order/prospect details, yearly offer/team-room projections, manual HC offseason over-budget extensions with regular-season compliance preserved, contrasting FA actions/visible rival bids and optional non-destructive GM bidding help. Both apps: Settings naming, confirmed save/exit moved into Settings, default FB order beside HB, offseason card near overview top. Save enum IDs and signed contracts are preserved; old Pro RosterCuts value3 is presented as Training Camp, directly after Draft.

Reviewed198 snapshot HB/FB identities with offline source roles and primary club evidence for discrepancies:12 additional FBs and Jack Westover to current-club TE; hybrids use latest explicit club roles, not weight. Stable-ID migrations preserve ownership, contracts, appearance and user depth assignments, and are idempotent. Audit: docs/data/pro-backfield-position-audit-2026-10-30.json.

Accepted Pro115 runtime401bd82: build41110120686, APK13003188026, native evidence13003108943; all13 guided-offseason assertions pass. Accepted College412 source09e6e19: run41112361113, APK13003019975, native evidence13005080737; all5 overview/roster/settings assertions pass. Both package with0 warnings/errors. CI64 targeted Pro cases and17 combined Core/College position/backfield cases pass, plus16 harness helpers. College411 packaged but its native check stopped at an offscreen saved-slot Load button;412 fixes that harness scroll and the remaining library Settings label. Screenshots reviewed. Pro runtime inputs are unchanged by the later College/harness commit. No new long-season or full GameCast run is claimed; prior unchanged evidence is reused.

A short64-game real-Pro backfield regression sample has63.14 plays/22.49 points and zero invalid/duplicate units. Scope: docs/pro-guided-offseason.md. An initial over-broad local filter was stopped after starting a long case; final filters explicitly exclude long cases. No completed new long-run evidence is claimed.

Permanent prerelease: https://github.com/antdroidx/Football-Sim-Engine/releases/tag/preview-2026-10-30-guided-offseason . Only exact tested APKs, checksums and a small manifest are published.

## Accepted Pro114 — playoff totals and tabbed player profiles

Runtime sourcee34056d, successful run41020845808. Exact tested APK https://github.com/antdroidx/Football-Sim-Engine/actions/runs/41020845808/artifacts/12962065061 . Two targeted Pro cases pass in1s,16 helper cases and32-team source validator pass; Android packaging0 warnings/errors. Native evidence12961925864 in.artifacts/profile-tabs-pro114-ui passes seven flags: league playoff selector, all five tabs, distinct playoff yardage, playoff career, retained filter state, return to overview and portrait identity bounds. Landscape playoff table and portrait overview screenshots reviewed. Earlier113 was canceled for the final narrow-screen header constraint; it is not an accepted build.

Pro statistics and player profiles now separate regular-season/playoff totals and current season/career. Queries deduplicate current/archive copies by season+game ID, exclude preseason/unplayed games and preserve maximum fields. Profiles have Overview, Attributes, Contract, Statistics and Analytics tabs; fixed compact header/tab bar and one independently scrolled section at a time. Statistics shows one selected category rather than every table stacked vertically. Existing save data/schema and contract actions preserved.

Testing is scoped: no new long simulations, broad Pro suite, shared Core/College suite, full GameCast device flow or College rebuild. Reuse accepted Pro112355-case and Core/College726-case/512-game evidence because simulation/shared/College inputs are unchanged. The new filter has a synthetic multi-year/archive/phase separation test; native fixtures use a completed production game plus an explicitly synthetic distinct playoff row to reveal accidental mixing. Exact-APK profile retry scopes also run only the two relevant cases. Existing long regression tests are retained for changes that need them, not rerun or deleted indiscriminately.

Permanent prerelease https://github.com/antdroidx/Football-Sim-Engine/releases/tag/preview-2026-10-30-profile-tabs contains the exact tested Pro114 APK/checksum/small manifest. College407 remains current and unchanged.

## Accepted expanded player statistics — Pro112 / College407

Runtime/app source1c6291b. Pro112 run41015659149 passes355 fast Pro cases,16 helpers,32-team validator and Android packaging0 warnings/errors. Exact APK https://github.com/antdroidx/Football-Sim-Engine/actions/runs/41015659149/artifacts/12959152937 ; native evidence12959323015 verifies production-game fixture, blocking, horizontal scrolling, playing time, receiving and career totals. Screenshots reviewed in.artifacts/expanded-stats-pro112-ui.

College407 build job in41015662290 passes726 combined Core/College cases,16 helpers and packaging0 warnings/errors. Exact APK https://github.com/antdroidx/Football-Sim-Engine/actions/runs/41015662290/artifacts/12959137915 . Exact-APK native retry41017184843 at harness9232bb6 passes actual new dynasty/completed week, blocking, playing time, receiving and career totals; evidence12959718338 downloaded/reviewed in.artifacts/expanded-stats-college410-ui. Source-input guard passes. Original device invocation canceled for harness update; retries408/409 failed only on picker scroll navigation and are superseded. No extra APK builds were needed for these harness repairs.

Adds event-derived OL/TE/FB blocking, all-position snap counts, receiving targets/catch rates, QB/defensive pressures, coverage production, longest plays, broken tackles, kicking distance splits, net punting and individual penalties. Shared dense columns and season/career aggregation preserve maxima and matched denominators. Old untracked values stay missing; no fabricated history.512 fixed-seed games across real/generated Pro/College modes have zero invalid/duplicate units and reconciled target/coverage totals. The audit uses32 Pro and136 roster-backed College teams; initial College rosterless-profile samples are superseded. Definitions, metrics and scope: docs/expanded-player-statistics.md. Existing annual/development and full GameCast layout evidence reused where unchanged; this batch does not claim a new multi-season or complete live-view device run.

Permanent prerelease: https://github.com/antdroidx/Football-Sim-Engine/releases/tag/preview-2026-10-30-statistics . Only the exact tested signed APKs, checksums and a small release manifest are published. Both package identities and save compatibility preserved.

## Accepted Pro109 / College406 — HB/FB and Pro practice-squad retirement

Runtime source f074974176a1210ff5170525ad87ba332fe21b82. Both current app source trees still match exactly. Pro109 build job in40990388530 passes354 fast Pro cases,16 helpers,32-team source validator and packaging0 warnings/errors. Exact APK: https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40990388530/artifacts/12948493646 . Exact-APK native retry40991908827 at harness42ca071 passes all12 flags; evidence12948959599 in.artifacts/backfield-pro111. Screenshots reviewed: HB roster, natural TE in FB depth, and migrated roster without PS. Guard confirms unchanged app inputs. Original109 device job canceled for harness update; retry110 reached all feature assertions but failed its final navigation on an undismissed Saved dialog, superseded by111. Earlier107/108/College405 builds canceled during final input fixes.

College406 run40990391150 passes721 combined Core/College cases,16 helpers, packaging0 warnings/errors and native generated-world/reload plus FB-identity/filter checks. Exact APK https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40990391150/artifacts/12949380974 ; native evidence12948549016 in.artifacts/backfield-college406. Air Force FB table screenshot reviewed (five FBs). Prior full GameCast/layout acceptance is reused; this batch's device scope is roster/depth/migration, not a new full GameCast run.

HB/FB roles, optional TE/HB substitutes, lead blocking, intended handoffs/FB screens and safe numeric-ID/old-save identity migrations are implemented. Pro PS UI/actions/CPU fills retired; default new games and every normal load release legacy PS players through existing obligations accounting, preserve IR and active negotiated contracts, and clear stale squad labels. Historical source-accounting fixtures explicitly retain their legacy basis; new default-path tests and native migration verify the new model independently. Three-season real/generated Pro audits each816 regular games show zero PS throughout and zero invalid sampled units; source-cap-constrained active counts are reported honestly in docs/backfield-roster-update.md. Scoped sampling remains~63 plays/team. No stored roster seed added.

Published permanent prerelease: https://github.com/antdroidx/Football-Sim-Engine/releases/tag/preview-2026-10-29-backfield . Exact signed APKs, checksums and manifest are uploaded; GitHub SHA-256 asset digests match the downloaded/tested bytes. No private evidence archive was published. Source, audit methods and actual scope: docs/backfield-roster-update.md.

## Accepted Pro106 / College404 — Pro pacing reduction

Pro source 4479e4e19cc76ccd90def0116e22ef9f97613f3c, successful run 40984224442. Exact regression/build-tested APK: https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40984224442/artifacts/12945439523 . 352 fast Pro cases, 16 helpers and 32-team source validator pass; Android packaging has 0 warnings/errors. College404 source c3465e2106da4e4fa00d02da5c214f75a16f38fb, successful run 40983805957, exact APK https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40983805957/artifacts/12945674373 . 714 combined Core/College cases and 16 helpers pass, packaging 0 warnings/errors. Later 4479e4e changes only Pro generation and docs/tests; College runtime inputs are identical. No new emulator flow is claimed; unchanged UI/device evidence from Pro104/College403 is reused. Published prerelease remains Pro104/College403.

Normal Pro pre-snap pacing is 36–39 seconds instead of 31–37; College keeps its prior values. Explicit tempo, stopped clock, 25-second administration and late-game rules are preserved. 160 focused combined clock/GameDay cases pass locally. 256-game before/after samples per real/generated Pro mode show offensive plays/team/game 66.32 -> 63.16 and 66.96 -> 62.82; new points 22.59/21.27 and yards 339.46/326.55. Raw reports and limits are in docs/pro-pacing-audit.md. No new multi-season audit claimed. A random generated-start cap defect exposed by the first CI attempt is fixed by reserving bonus headroom at fictional initialization; deterministic one-year worst-case test passes. Existing saves and real contracts are untouched. First Pro105 failure is superseded by successful Pro106.

## Main integration and prerelease complete — 2026-10-29

Merged PR #23 into main at 482cbac7b2963ffa2186191f89978c1d4be13779. Repository is now antdroidx/Football-Sim-Engine; local origin updated. Only main remains locally and remotely. Legacy Java source, Gradle files and historical packs are preserved in Archive/Java-game; tracked caches and the obsolete Java APK are removed. Both current Android workflows remain, with relevant-path main triggers and manual expensive simulation/device scopes.

Published prerelease: https://github.com/antdroidx/Football-Sim-Engine/releases/tag/preview-2026-10-29 . Permanent assets are the exact accepted Pro104 and College403 signed APKs, SHA256SUMS.txt and release-manifest.json. GitHub asset digests match the tested APK hashes. Current app/shared/data/signing inputs still exactly match tested a53e5f3; no rebuild was needed. Prior accepted evidence: 351 Pro cases, 707 combined Core/College cases and both native generated-world/reload flows. Cleanup validation: actionlint, 16 helper cases, packaged-resource validation and diff checks pass.

Deleted 150 superseded completed Actions runs and the unused latest calibration artifact. Retained successful runs 40918675174 and 40918680239 and their four APK/native-evidence artifacts; the older published prerelease is preserved. Historical links to deleted runs are retired; their recorded source/test summaries remain. A local evidence/inventory backup is retained under .artifacts/release-preview/publish/validation-evidence.zip; it was not published because automatic approval review rejected public disclosure of its repository metadata. Release APKs and manifests contain no such archive.

## Accepted Pro104 / College403 — rotations, statistical audit and unique fictional starts

App/shared/Core sourcea53e5f3. Pro run40918675174, exact APK https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40918675174/artifacts/12916863024 .351 scoped Pro cases pass inCI2m12;16 helpers and32-pair source validator pass; packaging0 warnings/errors. Device evidence12916494450 is downloaded in.artifacts/pro-generated104. All six checks pass: two actual new franchises, fresh identities/names/ratings, fictional-mode marker, no real opening-trade replay, exact saved-player/contract reload and53 active players. Creation/roster screenshots reviewed. Real-player starts and contracts remain default.

College run40918680239, exact APK https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40918680239/artifacts/12916449428 . Version0.20.403 / Android version code803.707 combined Core/College cases pass inCI18s and16 helpers pass; packaging0 warnings/errors. Device evidence12917568142 in.artifacts/college-generated403 verifies two actual new138-team dynasties with11,730 players each, different IDs/names, enabled generated option and exact roster preservation after reload. Creation screenshot reviewed. Generation follows the selected roster/program strengths; the Pro generator uses league positional baselines and tightly grouped team strength. Both persist the actual generated players only, with no extra roster seed and no regeneration on load. Existing saves are preserved. Fictional Pro starts use generated contracts and a fresh standard pick inventory rather than replaying real-player opening trades/allocations.

Offline three-season reports/method in docs/football-balance-audit.md and docs/data/football-balance-2026-10-29 cover6,600 regular games across real/generated Pro and College worlds, including draft/recruitment/development and actually participating newcomers. Generated Pro averages22.4–23.6 points/348–353 yards; generated College25.5–25.8 points/377–387 yards over2026–2028. All180 sampled full GameDay personnel sessions have11 unique eligible players per side,100% primary-QB snap share, meaningful RB3+ work and~37–38% reserve-DL participation. Rotation respects depth/quality/rest; healthy QB/OL stop arbitrary rest; thin packages use same-unit fallback; kneel/spike units are recorded. Pro sack losses now sit outside rushing in net offense/drive stats, and NFL-specific AI pass/run calibration leaves College rules separate.

Limits: the season statistics use normal production engines; College uses synthetic balanced all-FBS12-game schedules and does not claim actual conference/FCS or College postseason coverage. Personnel evidence is sampled180 sessions, not a census of every season snap. Broad real-world plausibility checks are not exact historical replication or an audit of every emergency/coaching archetype. The external NCAA national-trends PDF could not be fetched; no freshly imported exact NCAA mean is claimed. No extra new full visual GameCast/career run is claimed; unchanged viewer evidence is reused with707 engine/clock/coaching/animation guards and the new personnel/accounting checks. Stable main and both package identities preserved.

## Candidate balance acceptance evidence

Final offline reports in docs/data/football-balance-2026-10-29 and methodology in docs/football-balance-audit.md cover6,600 regular games/three seasons per real/generated Pro/College world. All180 sampled personnel sessions have0 invalid units and100% primary-QB share; actual generated newcomers play in2027/2028. Final generated attributes include valid physical measurements and positional archetypes; Pro practice starters stay young, fresh-world veteran remaining terms are staggered.707 combined Core/College and351 Pro pre-final broad guards pass, plus final focused generated/rotation/clock guards. Both APKs and native two-world uniqueness/reload checks pending. No extra roster seed field is stored.

## Rotation/stat audit and unique generated starts — candidate

User direction: generate a fresh fictional roster once at new-save creation when enabled, persist the actual players normally; do not add a stored roster seed or regenerate on load. College follows selected-roster/program strength; Pro uses tight team parity and position-specific athletic/skill archetypes. UI choices implemented; Android acceptance pending. Old saves/real-player defaults preserved.

Shared audit fixes: reserve rotation samples beyond RB2/first DL backup pair; healthy QB/OL remain stable unless exhausted; thin packages fill with healthy same-unit alternatives to11; kneel/spike events now carry actual personnel. Pro has its own calibrated pass bias/run baseline and excludes sacks from QB rushing while retaining sack yards in net team offense; College sack conventions unchanged. Initial128-game samples exposed the old zero third-back usage and short personnel packages. Three-season audits cover32-team Pro real/generated and138-team College real/generated, including draft/recruitment/development; final generated runs are being refreshed after archetype/physical-metadata refinements. All finished personnel samples have0 invalid/duplicate units and100% primary-QB share; backup DL share~37–38%, third-and-later backs~13–19%.707 combined Core/College regressions pass after replacing a brittle timeout yardage-duration assertion with explicit zero-runoff/live-seconds assertions.349 pre-final Pro guards and two generated-mode guards pass; final Pro run in progress. No APK from this phase is accepted yet.

## Accepted Pro103 — dense retention table and contrasting actions

App sourcebdff738, workflow fixture repair7b7f03d, run40907837678. Exact tested APK: https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40907837678/artifacts/12910789994 . Five scoped retention guards pass (CI4s/local1s),16 helpers and32-pair validator pass; Android packaging0 warnings/errors. Device evidence12912083142 is downloaded in.artifacts/pro-retention103. All six presentation flags pass: dense table, multiple visible rows, compact action heights, light/dark alternating stripes and opening negotiation. Reviewed both theme screenshots: eight actionable rows visible, filled primary/outlined secondary controls legible on both stripes, plain linked names/headers. Synthetic $2B-cap fixture isolates presentation; its inflated demands are not production market evidence. Failed102 fixture staging is superseded; no app bug was found there.

User UI direction supersedes the Pro101 retention card design: prefer dense sortable tables for comparable lists throughout both games, reserve larger cards for summaries/narrative content. Retention now uses one row per player with adjacent actions and aligned sortable statistics; eligibility reasons can be tapped for details. No Core/College behavior changed. Reuse Pro101349-case financial/phase evidence and College40241-case/device evidence; no extra College APK or annual run for this presentation-only batch.

## Retention presentation packaging repair

Pro102/run40907056054 compiled with0 package warnings/errors and passed the five scoped retention guards, helpers and validator, but artifact staging expected unrelated trade/coordinator fixtures omitted by the deliberately narrow test filter. Workflow now stages only the required retention fixture for this scope. App sourcebdff738 is unchanged. New packaging/device run required because the first job failed before APK upload.

## UI density preference and retention table candidate

User preference: use dense sortable tables for comparable lists in both games; reserve cards/large blocks for summaries and narrative content. Player names and headers remain plain contextual links, not outlined boxes. Restoring the expiring-contracts table with a single compact row per player, separate Actions column, filled primary action and contrasting surfaced/outlined secondary action on either stripe in light/dark. Eligibility explanations remain accessible by tapping the reason. Contract rules and simulation systems unchanged; reuse Pro101349-case and College402 evidence. New retention-table scope runs only five affected retention guards plus device light/dark density/actions; pending APK acceptance.

## Accepted Pro101 / College402 — retention usability, phase shortcuts and Simmons trade

Pro app/Core086ab52, run40902693409, exact APK https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40902693409/artifacts/12908759960 .349 scoped Pro cases pass inCI1m49 (local1m44),16 helpers and32-pair source validator pass; packaging0 warnings/errors. Device evidence12911241025 is downloaded in.artifacts/pro-inseason101. All seven acceptance flags pass: eligible in-season negotiation opens, contract actions in light/dark, preseason->regular, regular->playoffs, playoffs->offseason and persisted boundaries. Light/dark card screenshots reviewed. Fixture uses synthetic $2B cap for isolation, so illustrated demands are deliberately inflated and are not production market-calibration evidence. Core cases exercise actual veteran extension preserving current salary, staff negotiation/let-expire choice, drafted/undrafted rookie windows, cap refusal and source trade compensation. Device phases use completed-game synthetic32-team fixtures, not whole seasons played on device.

College sourceb55a234 (Pro inputs unchanged), run40903197220, exact APK https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40903197220/artifacts/12909769395 . Version0.20.402 / Android version code802.41 scoped combined Core/College season/save cases and16 helpers pass; packaging0 warnings/errors. Device evidence12909649673 in.artifacts/college-phase402 shows real Air Force creation, preseason stopping at regular Week0, interrupting the regular skip after opening Week0 atWeek1, automatic save and successful reload. Both screenshots reviewed. The script completed every DB/reload assertion; its JSON summary was not uploaded because the initial workflow listed it only on failure. Future uploads now include the summary/log in the always-uploaded UI evidence. Do not rerun the APK just to recover that metadata. Earlier College401 packaging failure (missing namespace) is superseded. Reuse unchanged full GameCast, career and annual evidence; no claim of a new complete device postseason/season.

Retention is now a readable card list with filled high-contrast Negotiate and outlined Let expire controls. Eligible extensions and optional staff delegation work during playing phases; current salaries stay intact and new years follow the deal. Rookie restrictions and same-year change restrictions are explained per player. Tags retain the offseason window. NFL rule reference: https://operations.nfl.com/calendar-events/nfl-free-agency/contract-language . Pro and College simulation shortcuts appear for the current relevant phase, process normal weekly decisions, save completed weeks and allow stopping after a week. Pro retains roster/cap gates; College postseason includes conference championships/bowls/playoffs and stops before the portal.

New-franchise opening snapshot2026-10-29 adds reported Jeffery Simmons TEN->CHI, CHI2027 first/2028 first/2027 fourth toTEN and TEN2028 fifth toCHI, matching https://www.nfl.com/news/jeffery-simmons-bears-titans-trade . All672 standard pick identities retained; old saves preserve prior ownership/contracts. Chicago's54 active camp players are legal; regular-season CPU repair now trims oversized active lists via existing release/PS accounting, preserving obligations. Main and both app/package identities remain unchanged.

## College phase build repair

College401/run40902699603 passed scoped engine/save checks but failed packaging on a missing Models namespace import in FoundationPage.Library.cs (six SeasonPhase references). Import added; no Pro source changes.349 final local fast Pro cases pass in1m44. Pro101/run40902693409 remains in progress. College rebuild required before accepting this batch.

## Candidate: retention layout, in-season extensions, phase simulation and Simmons trade

Implementing clearer unstriped contract cards with high-contrast negotiation actions and explicit eligibility reasons. Eligible veterans can negotiate during playing phases, including delegated staff negotiation; drafted rookies wait through their third regular season, undrafted through their second. Pro and College fast-forward shortcuts reuse normal weekly advancement, save each completed week, expose a stop-after-week control and stop at phase boundaries (College postseason includes conference title games). Pro still enforces cap/roster entry requirements. New-franchise October29 roster rights include reported Jeffery Simmons TEN->CHI for CHI2027/2028 firsts and2027 fourth; TEN2028 fifth goes toCHI. NFL source: https://www.nfl.com/news/jeffery-simmons-bears-titans-trade . Existing saves are not overwritten. Chicago's camp roster may legally exceed53; CPU regular-season oversized-roster repair uses existing cut/PS accounting.39 focused cases pass; final fast Pro run and both APK/phase UI scopes pending. No new APK is accepted yet.

## Accepted Pro100 — drag-and-drop depth chart and out-of-position roles

App/Core sourcefa1de8f, build/device run40839037565. Exact tested APK: https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40839037565/artifacts/12882102178 . All340 scoped Pro cases pass inCI1m43 (local1m59),16 UI helpers and32-pair cap validator pass; Android packaging0 warnings/errors. Evidence12882501969 is downloaded in.artifacts/pro-depth100. All six depth acceptance flags pass: actual held native drag moves QB1 to3, arbitrary rank menu returns him to1, WR assignment toTE, unchanged natural player/contract records, and reload preservation. Native-drag and reloaded-TE screenshots reviewed. The fixture is synthetic32-team through the production save loader, not a device-played season. Core tests verify target positional rating/role in GameDay, inactive exclusion, coordinator permissions and old-save defaults. Shared Core/College source unchanged; reuse accepted College, annual finance and unchanged full GameCast evidence. Push-triggered duplicate full run40839037284 was cancelled in favor of scoped100.

Use Organize depth chart: hold the ≡ handle and drop onto the desired slot, or use Move to for any rank. Select a playing role and search/Assign another active player to play out of position. Natural position/attributes/contracts stay intact; target-role rating is shown. Persistent assignments drive the next game, while ongoing games retain their live substitution controls. Training camp follows assigned role/numeric position order. GM trade acquisitions retain CPU promotion authority independently of manual coaching permissions.

## Pro depth editor implementation — pending Android acceptance

The Pro editor now uses the College shared depth assignment/projection service: native handle drag/drop, arbitrary rank moves, all 21 playing roles and searchable active-player candidates. Natural positions, ratings and contracts remain unchanged. Assignments persist separately and feed Pro profiles/GameDay; camp battles follow playing role order. Coordinator restrictions cover both source/target units; GM/CPU trade acquisition retains its separate promotion authority. Seven local focused cases pass; broader rerun is in progress after repairing the trade-helper interaction. Android depth-editor scope is added with a synthetic production-loader fixture; no APK is accepted yet. Reuse unchanged College and annual finance evidence.

## Accepted Pro97 — single season-cap display

App/Coreab0e53b, packaged APK12880312505 in run40835230475: https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40835230475/artifacts/12880312505 . All335 scoped Pro cases,16 helpers,32-pair validator and Android packaging0 warnings/errors pass. Initial device invocation failed a harness partial-vs-full breakdown label lookup, not app behavior. Harness-onlyc214496 retry40836275698 passes exact-APK source guard and all seven cap-display flags; evidence12880703404 downloaded in .artifacts/pro-cap-display98. Front Office/hub/inbox agreement, Overview season balance, absence of temporary balance, light/dark, unchanged saved contracts/counting and positive balance resolving the alert pass. Light cap card and dark inbox screenshots reviewed. Explicit synthetic32-team fixture demonstrates temporary positive camp room versus a negative full-season balance; it is not a device-played season.

One read-only full-season budget now supplies Front Office, expanded breakdown, Financial overview/forecasts, Overview/team cards and trade/contract previews. Inbox has one matching season-cap alert and correct Front Office link. Removed temporary preseason room/exclusion rows and misleading250k rounding of cap balances; contract offer increments are unchanged. Actual transaction cap counting, contracts, saves, simulation, College and stable main are unchanged. Local14 focused cases passed3s; reuse unchanged annual/College/GameCast evidence. No active build/test or automation.

Follow-up visual observation outside cap scope: some retry captures show the unchanged team banner without child logo/name content after repeated navigation/theme changes; header team identity and cap cards remain readable. Do not claim this scope retested/fixed all banner behavior.

## Accepted Pro96 — opening IR and lowest-rated practice cleanup

App/Core6741d4c, run40830670108. Exact tested APK: https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40830670108/artifacts/12879200586 . CI passes329 exact-source scoped Pro tests (including both three-season scenarios),16 helpers,32-pair cap validator and Android packaging0 warnings/errors. Device evidence12878214098 is downloaded in .artifacts/pro-ir-policy96. All reserve flags pass: four-game minimum blocked, season-ending blocked, explicit two-week injury blocked, cleared activation, contract preservation and reload. Four-game/season-ending screenshots reviewed. This is an explicitly synthetic32-team UI fixture, not a device-played season. Local328 pre-final fast cases pass1m4; final20 policy/reserve/inbox cases pass1s, including live/sim Finish retry recovery. Reuse unchanged College/GameCast and prior long financial evidence; no new20-season IR claim.

New/current franchises trim excess ordinary practice depth by lowest OVR, preserving legal international exemption and normal owed/dead-cap accounting; existing squads clean up before calendar advancement. Opening medical reserves default to four completed regular-season team games. Eight public season-ending overrides remain blocked for2026; unknown body areas are not invented. Unsigned rookie rights and non-medical EXE/SUSP restrictions remain separate. Timed eligibility derives from saved completed games, so preseason/byes/reload do not advance/reset it. Existing2026 source reserves get a season-start baseline without a new four-game wait after four saved games. CPU, GameCast, player cards and inbox share availability; actual activation still checks cap/roster/contracts. Details and approximation limits: docs/pro-reserve-policy.md. Main remains fdd3d4898b5bd07eeec8dba346024d61f5289474; College/shared source and app identities unchanged. No active build/test or automation. This CPU roster/IR/cleanup batch is accepted.

## Accepted Pro95 — CPU roster movement audit

App/Core616c897, run40794034836:323 exact-source scoped Pro tests,16 helpers,32-pair validator and Android package0 warnings/errors pass. APK: https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40794034836/artifacts/12862539615 . Device evidence12863940517 (.artifacts/pro-cpu-roster95) passes available reserve activation, injured action blocked, contract preservation and reload. CPU management itself is verified in Core whole-league tests, not claimed as on-device annual simulation. Local full324 pre-final-fallback candidate passed11m7 including20 generated/5 sourced seasons; final34 movement/option guards and12 corrected waiver/movement cases passed, and final CI323 includes both three-season scenarios. Pro94 failed waiver handling and is superseded. Main/College unchanged.

Next user steering: trim oversized practice squads by lowest rating, initialize opening reserve injuries to a four-game default, use verified season-ending public reports where possible, and implement timed recovery/CPU return. Keep unsigned rookie rights and non-medical restrictions distinct. Pro95 is accepted; new IR lifecycle work has not yet changed app inputs.

## Accepted Pro93 — future rights and compensatory allocation

App/Core source56cf7c3. Run40780183506 passed313 scoped Pro cases,16 helpers,32-pair source validator and Android packaging0 warnings/errors. Exact tested APK: https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40780183506/artifacts/12857186828 . Device evidence12857057721 downloaded in .artifacts/pro-roster-update93. All ten acceptance flags pass:672 unique standard opening rights/selected multi-hop ownership, visible reserved rights, correct offseason2027 default, actual UI-created Seattle franchise, Nelson/Buckner contract/roster ownership and trade compensation, Nelson roster row, non-tradable projections, synthetic2027 awarded selections and save/exit. Reserved and awarded screenshots reviewed: Seattle has five opening standard rights (four allocated/one conditional), then ten total selections after five synthetic awards. Synthetic fixture does not claim a season played on-device.

All315 full local Pro cases pass7m35, including20 generated and5 sourced seasons (.artifacts/future-full-final.txt). CPU debt/roster failure is repaired without erasing liabilities. Reuse unchanged College399/398 and Pro44 GameCast evidence; no College/shared source changes. Existing saved careers retain ownership/contracts; historical opening snapshots apply only to new franchises. Private undisclosed conditions use documented published-round approximation, not invented clauses. Main remains fdd3d4898b5bd07eeec8dba346024d61f5289474. No active build/test or automation. This roster/pick/compensatory batch is accepted; broader cleanup/archive/rename remain later roadmap work.

## Accepted Pro92 — roster trades and compensatory allocation

App/Core source ef274e6. Run40774235744 passes305 fast Pro cases,16 helpers/source validator and Android packaging0 warnings/errors. Exact tested APK: https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40774235744/artifacts/12853967833 . Device evidence12855070402 is downloaded in .artifacts/pro-roster-update92. All roster-update flags pass: actual UI-created Seattle franchise, Nelson/Buckner ownership/active contracts and exact three compensation picks in its save, visible Nelson roster row, non-tradable projections, synthetic valid32-team2027 awarded-pick display and save/exit. Nelson and award screenshots reviewed. The award fixture is explicitly synthetic; this does not claim a season played on-device or all real future ownership imported.

Local all305 full cases (before two final additional guards), including generated20 and sourced5 seasons, passed3m44; final11 targeted cases include the two old-save/seed-merge guards. CI's final fast count is305. Reuse unchanged College399/398 and Pro44 GameCast evidence. Main untouched; no automation. The wider real 2027-2029 pick ownership audit remains open; developer candidate JSON is not runtime data. Next reconcile/import all known allocations with conditional rights, preserving existing saved trade history. Pro92 is accepted and no build/test is active.

## Active roster/compensatory allocation batch — October28,2026

Canonical D:/AppProjects/Codex/Football Simulation Engine, feature/pro-franchise-foundation. Latest accepted remains Pro91/c55531f documentation, app5c65aae. No new APK accepted yet. New factory-only confirmed Nelson/Buckner trades and three compensation picks are implemented with existing salary-assignment accounting, preserving existing saved careers. A Pro-only compensatory system tracks eligible expired-contract signings, cancellations/early cuts, public APY bands, four/team and32/league quotas, separate sourced staff entitlement, yearly allocation and stable extra pick IDs. Opening 2027 estimates are projected separately; future simulated markets feed subsequent drafts. Draft numbering/completion and top51-vs-full-roster rookie budget planning are updated; a coordinator three-season regression exposed the latter and passes after the repair.

New Draft picks screen browses teams/years and separates allocated picks from non-tradable projections. Details/limits in pro-compensatory-draft.md. All305 full local Pro cases including generated20 and sourced5 annual scenarios passed3m44 (.artifacts/comp-full.txt), then11 final targeted cases passed including two newly added old-save/seed-merge guards. Final scoped CI should contain305 fast cases. Local Android compile passed0 warnings/errors before final Core/harness refinements;16 helpers and32-pair source validator pass. Dedicated roster-update device scope is prepared; CI/device acceptance pending. Reuse unchanged College399/398 and full GameCast evidence. Main untouched; no automation.

IMPORTANT remaining task: all-team real future ownership audit is still incomplete. docs/data/pro-future-pick-audit-2026-10-28.json is research only, not runtime data. It captures70 candidate rows and source review, but multi-hop identities and conditional rights remain unresolved. Do not claim the full real allocation task complete or apply guessed ownership. Only the two confirmed trades' three picks are imported so far. Next reconcile that ledger/conditional reservation and source conflicts after accepting this batch. Archive/rename remain last.

## Accepted Pro depth/schedule/coach batch — Pro91 / College399 compatibility

Final Pro app source5c65aae; core/shared/catalog source046a1d7. Run40747333613 passes294 fast Pro cases,16 UI helpers/source validator and Android packaging0 warnings/errors. Exact tested APK: https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40747333613/artifacts/12842951177 . Device evidence12843181188 is downloaded in .artifacts/pro-staff-calendar91. All staff-calendar-check flags pass: numeric depth order, exactly three preseason weeks, phase-correct Current marker while browsing regular season, four named playoff rounds, Seattle staff illustrations, actual new Kansas City franchise and Andy Reid light/dark. Preseason picker and Andy Reid light screenshot reviewed. Calendar starts from an explicitly synthetic preseason2 save; Kansas City is created through normal UI. Pro90 source046 build was superseded by the one-line live-position-order follow-up; no further app inputs changed.

College399 source046a1d7 run40746521680 passes703 combined Core/College cases and packaging0 warnings/errors. Compatibility APK: https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40746521680/artifacts/12842041575 . Shared portrait optional flags default to unchanged College/player rendering; reuse accepted College398 UI evidence, without claiming a new College399 device walkthrough. No engine rules/RNG/save schema/package/signing/financial changes. Main remains fdd3d4898b5bd07eeec8dba346024d61f5289474. No active build/test or automation.

Behavior: both Pro roster organizer and live substitution picker use the shared College numeric position sequence, with existing Pro grouped OT/OG/OC labels retained. Schedule choices use phase-qualified week identities: preseason3, regular18, playoffs4. Changing phase clears stale week selection; All phases supports explicitly qualified choices; Current appears only in the actual active phase/week, and offseason has none. Official-headshot-referenced illustrated appearances cover all127 named bundled coaches (one vacant slot excluded), include explicit eyewear/moustaches and stable normalized-name identity across team changes. Unknown/generated staff keep deterministic fallback. No photos are shipped. See pro-coach-portrait-review.md and nfl-coach-appearances.json for primary references. Eight new focused cases pass; reuse unchanged annual, finances, exchange and full GameCast acceptance.

## Pro schedule/depth/coaches implementation — completed — October28

User requests College-order depth positions, phase-aware schedule weeks and closer illustrated real-coach faces. Implementation uses shared College position numbering with Pro grouped OL labels; schedule choices have three preseason weeks,18 regular weeks and four named playoff rounds, phase-qualified selections/current markers and safe reset on phase changes. ProCoachPortraits embeds127 visually reviewed official-headshot-derived art profiles, stable by normalized identity; vacancy/generated fallback unchanged. No photos bundled, source names/ownership/coaching ratings/saves untouched. PersonPortrait gains optional explicit glasses/moustache flags; default College/player rendering stays unchanged.

Eight affected schedule/depth/portrait tests and all294 fast Pro cases pass; local Pro Android build0 warnings/errors before final data/harness refinements. Both final Android packages/device acceptance pending. Dedicated staff-calendar device scope checks preseason2 vs regular2 markers, depth order, playoff options, Seattle staff and real Kansas City new-franchise Andy Reid light/dark portraits. Prior College398/Pro88 remain accepted until this batch passes. Reuse unchanged financial/annual/GameCast evidence; no main or cleanup changes.

## Accepted shared news/roster UI — College398 / Pro88

Final app/shared source9cc34c5. College398 run40739215922/APK12839440855 passes703 combined Core/College cases and packaging0 warnings/errors. Pro88 run40739211829/APK12837753565 passes286 fast Pro cases, helpers/source validator and packaging0 warnings/errors. Exact tested APKs:
- Pro: https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40739211829/artifacts/12837753565
- College: https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40739215922/artifacts/12839440855

Exact two-app verification40740101064/evidence12839197534 (.artifacts/roster-sync-exchange89) passes source equality guards, actual wide filter bounds (all four College controls share a row), opponent roster selection, no duplicate profile button, restored own-team controls, contextual Pro team navigation, direct native save/share/import, reload and preserved roster/contracts/picks/debt/career. College wide roster screenshot reviewed and confirms one row. Test class/display fixture remains explicitly synthetic, not a season played on-device.

Pro84 presentation40732039464/evidence12837400520 (.artifacts/roster-sync-pro84) supplies unchanged news/card, light/dark, management and landscape evidence; Pro landscape roster screenshot reviewed. Only final compact-filter geometry differs; final89 rechecks that component in both hosts. Earlier functional exchange85/87 and flex-row candidates are superseded for final geometry. No unrelated annual/cap/full GameCast scenarios repeated. No active build/test or automation.

Both apps now use FootballNewsCard for left-aligned thumbnail/metadata/headline/preview/story links and a shared responsive card grid. CompactFilters uses explicit responsive Grid columns to avoid native flex rounding. College team links and legacy Teams navigation route to the unified roster with team picker; redundant Profile column removed. Opponent injuries follow the selected roster, own depth-chart management stays hidden for opponents and returns on own selection. Pro roster filters occupy one row on wide screens; phones use available columns. No simulation/finance rules, save schemas, package IDs, signing or stable main changes. Source workspace remains D:/AppProjects/Codex/Football Simulation Engine; branch feature/pro-franchise-foundation.

## Shared news/roster alignment implementation — completed

User requests identical College/Pro news cards, compact one-row roster filters and College team browsing through the roster instead of redundant Teams screen. Implementation in progress; prior College394/Pro81 remain accepted. Shared FootballNewsCard and CompactFilters added; College contextual team links now choose roster team, with user management hidden for opponents. Verify compilation, team-picker persistence/ownership, news alignment and phone/Fold layouts in both apps. No new build accepted yet.

## Accepted contextual UI — October28,2026

College394 source f2ec0ad/run40724719708/APK12832202972:703 combined Core/College cases, packaging0 warnings/errors. Pro81 app f345f90/core f2ec0ad/run40725321084/APK12833235834:286 fast Pro cases (five new calendar-label cases), helpers/source validator and packaging0 warnings/errors; presentation device acceptance passed. Evidence12833821291 in .artifacts/contextual-pro81. Original Pro80 build is superseded by phone-width headline refinement.

Exact two-app verification40727973325 at harness59127ee/evidence12833838921 (.artifacts/contextual-exchange82) passes all flags, including actual header-to-team-page navigation, native College save/share, Pro import/reload and preserved contracts/roster/career. Team-page screenshot reviewed: unboxed linked names and headers. Existing synthetic class/display fixture scope remains explicit. No unrelated annual/GameCast tests repeated. No active build/test remains.

Changes: removed default button outlines, restoring flat player links, College headers/sidebar while preserving text contrast; Pro team cells and main team banner open a roster/schedule team page, College school banners use existing OpenSchool navigation. Pro game cards use linked team cells. Pro overview contains three actual headline/body previews plus News/Statistics links. Structured transaction phase displays P1/W1/PO1/offseason, with unknown legacy stages labeled honestly. No save schema, financial rules, simulation, package or main changes.

Exact APKs:
- Pro: https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40725321084/artifacts/12833235834
- College: https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40724719708/artifacts/12832202972

# Active October28 contextual UI correction

User requests unboxed names/headers/sidebar, more team links, actual Pro overview headlines, and P1 preseason transaction labels. Implementation started on feature/pro-franchise-foundation; prior College393/Pro78 remain accepted. Changes are not yet built/device accepted. Global button outline removed to restore prior appearance while retaining contrast colors. College school banners and Pro team cells gain navigation; Pro team page provides linked roster/schedule. Structured transaction phase is used; unknown legacy stages are not invented. Finish compile/tests, contextual-link and overview/device checks, then package both apps before claiming acceptance.

# Current work - Football Sim Engine

Updated October28,2026. Canonical workspace D:/AppProjects/Codex/Football Simulation Engine, feature/pro-franchise-foundation. Preserve main fdd3d4898b5bd07eeec8dba346024d61f5289474, both app identities, College saves and negotiated Pro contracts. No subagents or continuation automation. Exchange and management batches are accepted. New UI/efficiency batch is in implementation; see ui-efficiency-plan.md. No new APK is accepted yet.

## Accepted October28 UI/efficiency — College393 / Pro78

Application/shared source ceeeb34. Pro78 run40648950218 passed281 fast Pro cases,16 helpers/source validator, packaging0 warnings/errors and complete presentation-device acceptance. Exact APK: https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40648950218/artifacts/12798821913 . Evidence12799118166 (.artifacts/ui-efficiency-pro78) passes light efficiency/definitions, trade selection/removal, landscape trade, contract presets/optional yearly details, existing dense grids, Fold and dark theme. Contract screenshot reviewed.

College393 run40648953014 passed703 combined Core/College cases and packaging0 warnings/errors. Exact APK: https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40648953014/artifacts/12798946403 . Exact two-app verification40651057545/evidence12800585172 (.artifacts/ui-efficiency-exchange79) passed source guards, College light settings/efficiency, direct native Save/Share, Pro picker/preview/apply, preserved identities/finances/career, reload and unsupported-format rejection. Display box scores/class snapshot are deliberately synthetic; real telemetry/formulas/lifecycle have separate Core tests.36 affected local Core/GameDay/contrast cases pass. No unrelated annual/cap/full GameCast runs were repeated.

Both hosts now share measured contrast, visible control boundaries and disabled states; Pro uses navy/teal. Trades have filters/removable assets; offers have current-deal context and Low/Agent/Strong presets. Efficiency uses documented own-game formulas, not proprietary PFF grades/official DVOA. Old context is not fabricated. Save schemas/identities, signed contracts, RNG/rules and main remain preserved. No active build/test remains; no automation recreated. Archive/rename/release review remain later work.

## Active UI/efficiency batch — source ceeeb34

Implementation is committed/pushed on feature/pro-franchise-foundation. Shared contrast uses WCAG relative sRGB luminance with 4.5:1 text correction, visible button edges and disabled states; Pro now uses navy/teal. Trade workspace has per-team name/position filters and removable selected assets. Offers show current deal/agent demand, Low/Agent/Strong presets and optional yearly accounting. College Save/Share directly prepares its file. Existing responsive navigation and sortable grids remain shared.

Both apps expose game-derived opponent-adjusted efficiency, league charts/ranks, success/explosive/sack/turnover/third-down rates and same-position player rate comparisons. New optional full-play aggregates do not consume RNG or change rules; old save context remains unavailable. Formula/scope in efficiency-metrics.md. No proprietary PFF grades or official DVOA claims.

Local36 affected Core/GameDay/contrast cases,281 fast Pro cases and16 helpers pass. Local College Android packaging passes0 warnings/errors. College393 run40648953014 passes703 combined Core/College cases and packaging0 warnings/errors; packaged APK12798946403. Pro78 build job in40648950218 passes and packages APK12798821913; emulator presentation acceptance is in progress. These packaged artifacts are NOT yet accepted device builds. Next finish Pro light/dark/trade/contract/efficiency screens, then exact two-app College light/export checks without rebuilding app inputs. Reuse unchanged annual/cap/full GameCast evidence. No merge/rename/archive actions.

## Latest accepted - College392 / Pro74 draft-class exchange

College runtime source0215ad4, build run40640733536:693 combined Core/College tests and Android packaging passed with0 warnings/errors. Exact tested APK12795552207:
https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40640733536/artifacts/12795552207

Pro runtime/core source0b60dbe, build run40640142353:279 scoped Pro cases,16 helpers/source validator and packaging passed with0 warnings/errors. Exact tested APK12795895212:
https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40640142353/artifacts/12795895212

Exact two-app verification40642639385 at harness9b56110 passed independent source-input guards, refreshed core fixtures and the complete Android handoff. Evidence12796481744 is downloaded in .artifacts/exchange77-ui. Every player-exchange-check.json flag is true: real College export, native document save, native share sheet, Android document picker, preview/apply, five source identities/appearances,315 generated filler, unchanged roster/contracts/picks/debt/career, reload and unsupported-format rejection. College saved-file, Pro preview and imported-class screenshots reviewed. The College snapshot is deliberately synthetic (five actual roster seniors placed in the new latest-class field); unit tests separately exercise real departure capture and SQLite round trips. Do not describe this as a full season played on-device. Failed invocations75/76 were harness setup (missing school selection, stopping before explicit save); both are superseded, with no app-source changes required.

Additional final test-only cases passed on unchanged runtime source: complete seven-round small-class draft and2500-player full-class draft (12s,224 selections/2276 remaining,0 filler). Three College export/SQLite cases pass; six initial affected lifecycle/export cases pass. No duplicate long NFL/College/GameCast runs: normal simulation and finances are unchanged; reuse accepted73 annual20/5 and prior field evidence. Both package IDs, save schemas, signing and stable main remain unchanged. No active build/test or continuation automation.

Exchange behavior:
- College retains the latest full graduate/early-declaration class before roster removal, excluding transfers/releases/redshirts. The optional save field uses existing schema26; old summary-only departed cohorts are not fabricated. Export in Season center or Player departures, prepare JSON, then Save JSON file through Android's document picker or share it. Save older class files externally before the next full snapshot replaces the latest one.
- Pro imports during offseason free agency or an unstarted draft. File size/depth/version/IDs/attributes are validated; preview shows source/target years and translated ratings. Explicit apply replaces only prospects/scouting/shortlist. Small files receive enough generated prospects to reach320; larger classes retain all players. Rosters, signed contracts, debt, career and traded pick ownership remain intact.
- All14 skills and physical/base attributes translate deterministically by player ID/year; shared canonical overall is recalculated. Names, IDs, size and illustrated appearances survive. Drafted players receive the existing four-year rookie deal/first-round option lifecycle; no direct roster injection. All coaching roles may configure this global class without changing their management authority. Duplicate/current-roster/historical players and imports after a first selection are rejected.
- Opening a draft with index0 no longer automatically selects players; its explicit Start draft action preserves the setup window. Normal offseason advancement still processes CPU decisions; import before advancing free agency into the draft. Coordinator device harness follows the explicit start action where applicable.

## Prior management acceptance - Pro73


Final application source a04ee62, financial/management core43635c0. All-green run40628330665:
https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40628330665

Exact tested APK12789619809:
https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40628330665/artifacts/12789619809

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
https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40613034696

All build/device jobs passed. Exact tested APK12783469883:
https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40613034696/artifacts/12783469883

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

-69 app/core d71782a, all-green run40590454472, APK12774172307/evidence12773632366 (.artifacts/pro69-ui).253 fast CI/16 helpers/source validator/packaging and expanded opening device flow passed.254 local cases excluding generated20 passed1m52. Original source migrates; negotiated deal, booked debt and career persist. https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40590454472/artifacts/12774172307
-68 app/core afe8b73, run40587763626, APK12772514300/evidence12773465745 (.artifacts/pro68-ui),249 fast CI/16 helpers/source validator/packaging and opening device checks passed.250 local cases incl five sourced/three imported-cap seasons passed1m55.32 clubs/735 source keys/no historical-cash fabrication, Seattle liabilities/contract/void/themes/reinstall/save verified; compact-old budget screenshot reviewed. https://github.com/antdroidx/Football-Sim-Engine/actions/runs/40587763626/artifacts/12772514300
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

CI Pro first attempt 40983806143: 350/351 passed; fresh random fictional team could exceed budget because salary-only scaling omitted its half-salary signing bonus on short deals. New fictional initialization now reserves that bonus headroom; existing saves and real contracts unchanged. Deterministic worst-case one-year veteran fixture plus two generated-world/save/accounting cases pass locally (3/3). Packaging retry pending.

## Pro scouting depth + UI/UX improvements (codex branch)

Implementation in codex/pro-scouting-depth branch: 3-tier scouting model (basic <50% / priority 50-75% / visit 75-95% confidence), 60-priority/30-visit nested limits, deterministic CPU fill, save-compatible migration, draft-results OVR column, table row/button contrast fixes, offseason stage timeline chips.

Focused checks run locally:
- Built ProFootball.Core+ProFootball.App with MSBuild (net10.0-android): 0 errors, 0 warnings.
- dotnet test filter ScoutingDepthTests|FranchiseDepthAnalyticsTests|ManagementRefinementTests: 13/13 passed, 0 failed, ~5 s.
- APK produced: src/ProFootball.App/bin/Debug/net10.0-android/antdroid.profootball.franchise-Signed.apk (~87.6 MB; signed).

dotnet test requires escalated sandbox access in this environment for testhost IPC (see docs/pro-local-validation-runbook.md).

## ProSaveStore load-chain rating migration fix

Added ProPlayerRatingService.Migrate to the Pro save-load chain so legacy saves with RatingSystemVersion=0 migrate to v1 and re-derive OverallRating from the shared rating system (SharedRatingsTests.OldSaveAdoptsSharedRatingsWithoutReplacingContracts now passes). Chain order: Pron.Migrate -> ProScouting.Migrate -> ProCareer -> ProEconomy -> ProContractData -> ProMorale -> ProReserve -> ProPracticeRetirement.

Scoped verification: 27/27 passed across SharedRatingsTests + ScoutingDepthTests + FranchiseDepthAnalyticsTests + ManagementRefinementTests (~10 s). Pro app MSBuild net10.0-android: 0 errors.

Accepted CI run 41150835916 on codex/pro-scouting-depth (commit 5fd00c9): pro-build passed 5m33s; artifact pro-football-apk-118 (id 13023655512, ~103.7 MB). Local focused run before push: 27/27 passed (SharedRatingsTests + ScoutingDepthTests + FranchiseDepthAnalyticsTests + ManagementRefinementTests, ~10 s); MSBuild Pro app (net10.0-android) 0 errors.

## FM Touch-style Pro UI polish (codex branch)

Shared UI primitives in src/ProFootball.App/FranchisePage.Ui.cs (PrimaryAction, PosPill, StatTile, SectionCard, TaskTile). Reworked screens: Overview (big record card, cap/roster/coach tiles, 2x2 task grid, news+transactions cards), Roster (position-group cards with colored pills, plain-text player names), Draft (prospect/selections/depth chip row), Inbox (SectionCard), Standings (per-division sections), Schedule (SectionCard). Draft results OVR column retained. Transparent player-name links in all data-grid tables no longer get the boxed button styling.

Verification: 27/27 focused tests pass (ScoutingDepthTests | FranchiseDepthAnalyticsTests | ManagementRefinementTests | SharedRatingsTests); Pro app MSBuild net10.0-android 0 errors; APK built and installed on emulator; Overview and Roster screenshots in dark theme reviewed (visual only, no automated device checks).

Verified the FM Touch-style Pro UI on a Pixel 9 API 36 emulator in both portrait and landscape (phone aspect): Overview dashboard collapses to stacked cards on narrow and switches to summary + task-grid side-by-side on wide; sidebar navigation appears in landscape; Front Office stretch table, Roster position pills and cards all render correctly. Tablet (tablet-size emulator) not yet verified in this batch.

Front Office reorganized into FM-Touch-style card sections (Money / Trading / Draft / Roster tools) — flat 10-button wall replaced with grouped tile grids. Visual verification from emulator screenshot; build only (no test scope change).

## FM Touch restoration + coach profiles (b543bb5)

Cause of the missing UI: the FM Touch overhaul commits (acce54e, a10b47f, bfaa44e) were on codex/pro-scouting-depth, but the last APK artifact (pro-football-apk-118) was built at 5fd00c9, before them. Fresh artifact built here.

Verified present at HEAD: shared UI primitives (PrimaryAction, PosPill, StatTile, SectionCard, TaskTile, CityLabel), Overview card dashboard with Advance/Open stage, Roster position cards with the Columns toggle removed, Schedule "Season" label, dense inbox with week/year stamp, centered loading overlay, Draft class tile in Front Office for the whole offseason, and Playoffs/Awards news stories in the All filter and Overview feed.

New in b543bb5: ProStaff.Profile derives deterministic coaching abilities and preferred schemes (save-compatible, defaults untouched in the record), hires now store those abilities, coach profile rebuilt with hero header, six stat tiles, five ability meters, scheme pair, and role card. Offseason guide now has an "Open <stage>" shortcut next to Continue.

Checks: Pro app MSBuild net10.0-android 0 errors; 57 focused tests passed; CI run 41175520041 completed success; artifact pro-football-apk-121 (13032906388).

## Pro FM Touch consistency, kicker/punter, and College coach polish (4df29b6..5dd1df3)

Published the previously uncommitted kicker/punter split (Position.P, market/trade/depth integration) with the Reserve-roster compact table.

Pro consistency pass: shared PageHeader and StatRow primitives; Player overview now opens with OVR / ceiling / growth / morale tiles; Staff renders each coordinator as a SectionCard with Overall/Offense/Defense/Leadership tiles and preferred schemes; page headers applied to Standings, Staff, Schedule, Statistics, Analytics, Inbox, Transactions, Honors, Career, Playoffs, Draft, News.

College: coach profile gains a KEY RATINGS card (Overall, Leadership, Development, Recruiting) and the team staff table gains Lead and Scheme columns. College behavior otherwise unchanged.

Checks: Pro app MSBuild 0 errors; 64 focused Pro tests passed; College app MSBuild 0 errors; 34 focused College coach tests passed.

## Final branch state and CI (487c878..9bd88f2)

Pushed everything outstanding to codex/pro-scouting-depth, including the kicker/punter split, Reserve roster table, Pro FM Touch page headers, College coach ratings card, and staff Leadership/Scheme columns.

Two regressions from the kicker/punter split were found by College CI and fixed: College position-need lookup needed a Position.P entry (kept at 0 so College recruiting behavior is unchanged), and GameDayEngine now prefers P for punts and K for field goals.

CI at the final commit: College Android 41178645152 success (artifact dynasty-manager-build-416); Pro Football Android 41178646670 success (artifact pro-football-apk-124).

## Coach ratings wired to gameplay (44c9097)

Audit found the Pro coach ability fields were display-only. Now wired:
- ProLeagueService.Profile blends Pass/Rush offense with OC Offense ability and Pass/Rush defense with DC Defense ability; SpecialTeams blends roster K/P with ST coordinator; HeadCoachLeadership and coordinator scheme fields read the ability profile.
- ProStaff.DevelopmentRating gives position-scoped staff development; ProDevelopment.AgeOneYear accepts it and adds growth chance above 80/90 development.
- Pro scouting already used coach Development for report confidence.

College: fixed KEY RATINGS tile overlap by assigning explicit grid columns (2x2 on phones, 4 across on wide screens). College coaching ratings already drove recruiting (CompetingRecruitingService uses head coach Recruiting) and scheme fit.

Checks: Pro app MSBuild 0 errors; 46 focused Pro tests including new CoachAbilityImpactTests (2 passed); College app MSBuild 0 errors; CI runs 41180456977 (College success) and 41180458621 (Pro success, pro-football-apk-125).
