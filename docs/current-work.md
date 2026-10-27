# Current work — shared Core, College and Pro

Updated October 26, 2026 (Pacific). This is the current handoff; Git history retains prior build narratives.

## Workspace and safeguards

- Work only in `D:/AppProjects/Codex/Football Simulation Engine`, branch `feature/pro-franchise-foundation`.
- Stable main remains `fdd3d4898b5bd07eeec8dba346024d61f5289474`. Do not merge unfinished Pro work.
- Preserve College package identity/save compatibility, independent Pro package/saves and update signing.
- Do not duplicate active implementation/builds. Use scoped tests per [testing-scope.md](testing-scope.md).
- Continue all authorized roadmap work autonomously. Hourly continuation `resume-pro-football-development` is active again; do not disable until everything is complete.

## Active batch — coordinator roster / draft and precise pending offers

Pro57 retention is accepted below; the prior pending-retention checkpoint is superseded. The Pro-only batch committed as aad411d extends existing CPU management to the coordinator's other unit: free-agent bids, rechecked waiver claims/signings, legal cap repairs/final cuts and automatic other-unit draft choices. Controlled players/contracts and coach identity are preserved. Final cuts retain minimum position depth and specific OL/kicking coverage; staff targets leave roster space for the user's unit. Shared current/future GM budgets review every staff commitment; rejected rookie deals retain unsigned draft rights rather than blocking the draft or bypassing future budgets. Opening the draft resumes staff/CPU decisions for existing saves. The underlying waiver contract mechanics remain simplified (not original-contract assumption), so this is not a claim of complete NFL waivers.

Pending bids now quote exact bonus remainders and each future year's service minimum, shared by GM/trade preflight. No real-source financial liabilities/default promotion or College/shared changes. All 196 full Pro cases passed in6m41s, including twenty generated and five sourced seasons plus the existing three coordinator seasons with explicit GM delegation. Seventeen final targeted cases passed afterward, including two added rejected-restructure rollback cases and all four synthetic fixture exports. That makes196 fast cases for the final source (the two long cases are excluded in scoped CI). Fifteen UI helpers and the32-pair source validator pass. The new automatic role behavior is covered by mirrored core scenarios; the prior three-season test still explicitly delegates GM roster/draft decisions and must not be described as proof of automatic-only coordinator seasons. Expanded retention device scope will check OC/DC automatic FA acquisitions and GM other-unit draft/own-unit stop alongside accepted retention flows using synthetic isolated saves. No new APK is accepted yet; latest remains Pro57. Finish tests/package/device checks and record exact artifacts before acceptance.

Packaging source `aad411d` in run40462840012 (coordinator retention/roster/draft device scope). Do not duplicate this build or alter app inputs while acceptance is pending. New developer-only primary roster/event review of all ten source identity-team conflicts is in docs/data/nfl-contract-status-review-2026-10-26.json; it explains historical spending versus actual team ownership without changing game data or declaring financial reconciliation complete.

Pro58 packaged successfully (artifact12714304881) and passed196 scoped Pro cases,15 helpers and source validation in run40462840012. Its device run completed both retention flows, then rejected a synthetic roster/draft save because the test fixture used a one-team league, which the production save loader correctly disallows. This is a fixture failure, not accepted roster/draft device evidence. Corrected fixtures retain32 teams and legally promote opposing practice players to full53 rosters to isolate the intended market. Eighteen affected cases pass on these corrected fixtures, including all four real-save-loader round trips. A harness-only exact-APK retry regenerates these fixtures from current core tests, preserving app inputs and the APK58 source guard. No APK rebuild or repeated long simulations are needed.

Harness-only source c94b7fa queued exact-APK58 verification in run40484403327. Eighteen targeted cases and15 helpers passed; app source remains aad411d. Follow this retry, not the superseded failed device invocation.

## Latest verified APKs

## Accepted coordinator-retention batch — Pro57

Application/core source `4294f8e`. [Exact APK57](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40458818959/artifacts/12713836374), [all-green run40458818959](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40458818959), [device evidence12713851967](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40458818959/artifacts/12713851967).179 scoped Pro cases,15 synchronization helpers, source validator, packaging and OC/DC retention device flow passed. Both roles' UI explanations, own-unit delegation off, next-year advancement, opposite-unit renewal, manual-own expiry and persisted job/contract/GM outcomes were checked. Screenshots in .artifacts/pro57-ui reviewed. Synthetic fixture numbers/ages are not real roster facts.

Forty targeted career/GM cases including three coordinator seasons with explicit GM roster/draft delegation also passed locally. No College/shared/market/trade mechanism change; reuse College390/full Pro44 and accepted Pro56 trade evidence. Head-coach long-simulation evidence is reused for unchanged branches. Retention is complete; automatic opposite-unit FA/waiver/cut/draft handling remains next, alongside documented financial/source/market/pending-bid and broader roadmap. No active build/test remains. Preserve existing contract-change-lock behavior unless a concrete failing case justifies a policy change.



## Accepted autonomous-trade batch — Pro 56

Application source `0e72ba4`, core `e899094`. [Exact APK56](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40456067627/artifacts/12712506988), [all-green run40456067627](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40456067627), [trade device evidence12713315926](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40456067627/artifacts/12713315926). Build passed167 scoped Pro cases,15 helpers, source validator and packaging. Exact-APK focused trade flow passed counter/accept/decline with persisted player/pick ownership, history and acquired starter depth. Offer/history screenshots in .artifacts/pro56-ui were reviewed; phone result column is visible. Fixtures are explicitly synthetic (altered age/ratings/budget), generated by the core tests and carried outside the installed APK; not real roster/cap facts.

All168 full Pro cases including twenty generated and five sourced seasons passed before the projection-index optimization/new save case;40 affected cases including three sourced-cap seasons passed afterward. Calibration11/1/4 CPU trades by year, no automatic user trades, final minimum active roster53, cap room$2,283,589 and next-three-class first-round holdings1–5. Reuse unchanged College390/full Pro44 and finance source/option assertions completed in Build52 before the fixture-selection harness failure, plus full accepted Pro50 finance evidence. Pro56 is trade-device scope, not a fresh complete financial/GameCast walkthrough. Earlier failed/cancelled runs are historical; no active build/test remains. Continue coordinator staff work per pro-coordinator-staff-plan.md; source financial reconciliation and remaining roadmap remain unfinished.



## Accepted corrected-budget batch — Pro 50

Application/data source `61022d9` (core change `61ec66e`). Exact [APK 50](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40449248836/artifacts/12710150676). Build run 40449248836 passed 155 scoped Pro cases, source-data verification, helpers and packaging. Harness-only correction `9f4938c` waits for completed save-library layout. [Verification run 40451229268](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40451229268) passed application-input equality guard, 14 helpers and exact-APK financial device flow: new budgets/all 32 equations, signed adjustments/source cash, themes, reinstall/save survival, explicit old-budget correction with contracts retained, rookie-option exercise/decline and persisted guarantees. [Evidence](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40451229268/artifacts/12709764334), downloaded `.artifacts/pro51-ui`. Corrected old-save budget and compact/disabled option-history screens were reviewed. All 157 full Pro cases passed locally in 4m20s, including annual simulations. No College/shared application changes; reuse full Pro 44 and College 390 evidence. Source obligations/default promotion and remaining roadmap remain unfinished.



### Pro 46 — fifth-year options / financial device scope

Application source `26cb625`. [Exact APK](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40445569937/artifacts/12708396236), original build run `40445569937`. 152 scoped Pro cases and packaging passed. The original device invocation was superseded for a synthetic fixture using the wrong offseason label.

Harness-only source `acac104` corrected that label. [Verification run 40446386891](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40446386891) passed the source/build-input equality guard, helper tests, exact-APK financial UI and exercise/decline device checks. [Evidence](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40446386891/artifacts/12707954405), downloaded `.artifacts/pro47-ui`. Reviewed exercise/decline screenshots; guarantees and unchanged negotiation lock persisted. This is not a new full GameCast walkthrough.

### Pro 44 — full GameCast / aging / field

Source `4f6bb2c`. [APK](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40438801865/artifacts/12705098072), [all-green run](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40438801865), [evidence](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40438801865/artifacts/12705962978). 134 scoped cases and 13 helper cases plus full Pro device flow passed. Reviewed landscape call fit/collapse/field expansion, city end zones, referee LOS spotting, age-aware growth/potential/deltas. Evidence `.artifacts/pro44-ui`.

### College 390 — preserved shared renderer / both device flows

Shared/College source `2acfcc1`. [APK](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40438515279/artifacts/12705660863), [all-green run](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40438515279), [broad evidence](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40438515279/artifacts/12705718044), [career evidence](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40438515279/artifacts/12705752478). 690 legacy combined Core/College cases and both Android flows passed. LOS referee screenshot reviewed. No College/shared changes since; reuse evidence. Do not label all 690 exclusively Core or College until audited.

Pro 45 coordinator future-pick GM value guard remains included: source `c9cddbf`, [APK](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40440713067/artifacts/12705638936), verified run `40440713067` (136 scoped cases, 13 helpers, finance device). Twenty-six affected local GM/trade/career cases passed.

## Active batch — corrected 2026 team allowances

Application/data correction `61ec66e`; evidence preservation follow-ups `544a49f`, `61022d9`. Source audit refresh `22c0800`.

**Critical source correction:** the supplied CSV adjusted-cap column matched rounded reported spending, not actual allowance. The browser-rendered public NFLPA report observed October 26 contains two spending/room rows per club. Both independently yield the same inferred allowance from Team Cap + Cap Room. Raw pairs and observation metadata are in `docs/data/nflpa-cap-observation-2026-10-26.*`. The report does not print a dataset effective timestamp; record observation date only.

- Preserve original supplied CSV bytes as `src/ProFootball.Core/nfl_2026_salary_caps.user-original.csv`. SHA-256 `fa83ced04c16e36415ab69c78a666b92bd062c40ff283e9880e16a35aa4e6c52`. Explicit Git `-text` preserves CRLF across platforms.
- Normalized CSV holds 32 exact-dollar allowances, carryover and derived adjustments; all paired equations reconcile. Four adjustments are negative. Row/header subtotals differ by opposing $2 rounding, while combined totals agree; no balancing charge is added.
- Seattle allowance $315,053,262 = base $301,200,000 + rollover $15,029,706 - other adjustment $1,176,444. This supersedes older rounded allowance claims.
- New franchises use cap-rules version 3, retaining existing seasonal Top-51 counting.
- Version-2 saves retain old budgets until explicit Update 2026 cap budgets action. Requires all original allocations/base unchanged, year 2026 not closed; custom, legacy and later-year budgets excluded. Only allocations/version change; contracts, transactions, debt, rollover elections and negotiated terms remain intact.
- Reported source room is never imported as game room. Current modeled Seattle camp room $18,625,860; full-roster room $3,530,487. These are not asserted real-world balances. OTC reports another date/basis; unresolved coverage/status/departed debt stays visible in developer audit.
- Option desk follow-up makes history compact and unavailable staff decisions visibly disabled.

Validation: all **157 Pro cases passed locally in 4m20s**, including twenty generated seasons, five sourced seasons and imported-cap lifecycle; 13 UI helpers passed; 32-pair data validator passed. One stale rollover expected value was corrected. Current CI is finance scope; unchanged College/GameCast evidence reused.

Run `40449012912` stopped before tests/packaging because Git normalized the preserved evidence bytes on Linux. Run `40449212040` was cancelled immediately to finish re-indexing the original bytes. Hash now matches in working tree and Git index. **Build 50 run [40449248836](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40449248836) at `61022d9` passed 155 scoped Pro cases, the source validator, helpers and packaging. [Exact APK 50](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40449248836/artifacts/12710150676). Device checks passed new budgets/source cash/themes/reinstall, then the synthetic library tap raced loading: captured XML shows the slot moved from y1470 to y1313 after the progress row disappeared. Fix harness readiness to require enabled creation and absent loading/progress, recapture before tap; 14 helper regressions pass. Reuse the exact APK with the source guard. Superseded by the successful Pro 50 exact-APK verification above.** Complete/fix it and inspect screenshots before reporting acceptance. Application edits would invalidate exact-APK reuse; keep source stable during any harness-only retry.

## Completed recent features

- Four-year drafted rookie contracts with explicit first-round fifth-year exercise/decline after season three through the next draft; unresolved controlled decisions block leaving draft. Exercise guarantees fourth salary and full fifth salary, decline remains four years, no repeat or duplicate source options. Negotiated extensions and trade identity preserved.
- Additive observed rookie participation, per-unit denominators, replay/final deduplication and unknown imported history. Frozen game-generated positional tender samples/usage/Pro Bowl tiers. Missing historical inputs marked estimated in developer metadata; not announced real NFL tender prices.
- CPU/GM/staff option decisions follow same window/cap rules; optional delegation and Front Office/inbox desk. Fixed option salary protected from rookie contract complaints.
- Pro position/age-aware ceiling/headroom, veteran growth outlook, annual rating deltas. Canonical shared 19 attributes/overall unchanged; College potential interpretation unchanged.
- Shared between-play official and ball at LOS, clear of huddles; live/replay/kickoff handling separate. Pro end zones uppercase city names.
- Pro landscape pinned controls, three compact quick calls, collapse after selection and field expansion, reopen after snap; verified in Pro 37 then full Pro 44.
- Seasonal Top-51 exclusions preserve all required bonus/other charges and non-top salary excess; full opening-day compliance, CPU planning against full obligations, legacy counting retained.

## Financial source limits / next priority

Read [pro-contract-reconciliation.md](pro-contract-reconciliation.md), [pro-contract-source-audit.md](pro-contract-source-audit.md) and [pro-completion-checklist.md](pro-completion-checklist.md).

Contract Parquet source SHA-256 `3f6f8906a20d48c688f5727e76856997bc01ad2d9e1ac0ed83b41559f4a12a9f`: 2,012 sourced matches, 519 generated fallbacks. Real salaries remain optional/experimental until source roster status, commitments, guarantees/vesting, departed-player dead money and historical cash reconcile. Do not fabricate liabilities or force headline room. Preserve negotiated saves with safe additive/unchanged-source migrations. Existing source/cash/void-year fixes and realistic generated remaining terms are implemented, but full source default promotion is unfinished.

After current APK acceptance continue this reconciliation plus remaining authorized roadmap:

1. Autonomous CPU trades and reviewable user offers/counters, ownership/expiry/future-cap/role legality. Design: [pro-trade-ai-plan.md](pro-trade-ai-plan.md). Current trading block/two-team manual workspace/future pick valuations and coordinator GM guard already exist; do not rebuild them.
2. Career/opposite-unit staff management, deeper offseason/camp/scouting/waiver rules and financial model validation.
3. League-relative analytics expansion, growth/scatter/draft outcomes and factual weekly/playoff/record news.
4. Full NFL division/wild-card multi-team/common-opponent tiebreak audit. Current ProStandings still documents foundation ordering; do not claim full league-rule parity.
5. Audit/split combined test categories into Core, College and Pro without falsely relabeling legacy coverage.
6. **Last:** audit/archive obsolete Initial Base Upload Java files, preserve C#-referenced assets, rename repository `antdroidx/Football-Sim-Engine`, update remotes/links and verify both apps. Do not perform before implementation is finished. No stable-main merge or release/branch purge yet.

## Working practices

Use `.github/workflows/pro-android.yml` finance/live/full scopes. Harness-only retries may reuse the exact prior artifact after source/build-input equality check. Report original APK plus verifying run. Do not repeat unchanged College/GameCast/long simulations for documentation/harness/display-only changes. Avoid concurrent local builds/tests writing the same DLLs. Update this handoff and checklist after every implementation/verification batch.

## Parallel read-only reconciliation audit

`build/audit-otc-opening-obligations.py` observed all 32 OTC pages; offline replay passed with zero errors and exact reported section-to-liability sums. Aggregate URLs/hashes/totals are in `docs/data/otc-obligations-observation-2026-10-26.csv`; raw/full audit remains `.artifacts/otc-2026-10-26`. 157 unique same-team roster/dead-name candidates require identity/status review; both debts and new deals can legitimately coexist. Do not remove players or deduplicate debt by name. Dead totals exceed displayed named rows for every club; preserve explicit unallocated source residuals without inventing individual liabilities. No app/save data changes were made by this audit. Detailed next-source-pack constraints are in pro-contract-reconciliation.md.

## Active implementation after Pro 50 acceptance — trade AI

Uncommitted ProTradeAi, additive offer/market-tick/history fields, CPU processing hook, Front Office offer desk/inbox link and eight new core cases are being implemented. Core compiles; 24 affected trade/GM cases passed, including specific line/kicking roles, full-buyer surplus release, future-budget rollback, total bonus conservation, stale offers, counters, deadlines and restrictions. Android local build passed with zero warnings/errors before two core-only guard/performance follow-ups. Full annual/regression run is in progress; a new save-store case and fixture-export helper need targeted verification afterward. Not accepted or released yet; latest accepted remains Pro 50. Still finish future-budget/offer changes and edge cases, run annual simulations, add device offer-desk coverage, package and verify exact APK. Do not mistake developer audit/this implementation for real-contract default promotion.

Trade device acceptance now uses a compressed synthetic fixture produced by the actual offer-generator regression test (only when PRO_TRADE_UI_FIXTURE is set), staged beside the APK as test evidence. It is not embedded in the application or a real source roster. The device script counter/accepts one offer and declines another, checking persisted player/pick ownership, depth and transaction history. Full source-finance QA franchise remains unmodified. Finish/run these checks before accepting the new trade batch.


Trade batch validation checkpoint: all **168 Pro cases passed in 6m47s**, including twenty generated-contract and five sourced seasons. After the non-semantic pick-projection index optimization, new save-store case and conditional fixture export, all **40 affected trade/GM/cap/comparison cases passed in 25s**, including the three-season sourced-cap lifecycle replay. The replay recorded 11/1/4 CPU trades in 2026/2027/2028, no silent user trades, 53 minimum active players and $2,283,589 minimum cap room at its final checkpoint; future first-round holdings across the next three classes range 1–5. Full developer metrics are in docs/data/pro-trade-three-season-calibration.json. Fourteen helpers and the 32-pair cap validator pass. Generated synthetic offer fixtures are under 1MB compressed and verified; test-output environment paths must be absolute because testhost's working directory is bin/. Android local build passed before core-only follow-ups; next CI must package/verify final source. No new trade APK is accepted yet. Reuse Pro50 finance baseline, College390/full Pro44 unchanged evidence.

## Strict identifier crosswalk observation

The public obligation audit now retains each row's OTC player URL/ID. `build/audit-nfl-contract-identities.py` joins OTC ID to the dated Parquet GSIS identity, then to the game roster's ExternalId; name-only candidates are never accepted automatically. On all 32 cached pages it identifies 2,507 of 2,539 current source rows with the same game-team identity, finds 10 dated team conflicts and 22 unresolved rows, with no ambiguous identifiers. Of 703 displayed dead-money rows, 476 have a game-player identifier; former/unrostered players and source residuals remain valid separate debt inputs rather than invented roster entries. Summary URLs/hashes/conflicts are in docs/data/nfl-contract-identity-audit-2026-10-26.json; full audit is .artifacts/otc-2026-10-26/identity-audit.json. No franchise data/status/contract/debt changes occur. Identity match does not prove contract cash, guarantee vesting or period alignment; retain those independent reconciliation requirements. This provides a strict basis for the next versioned source-obligations pack while isolating the remaining manual identity/date review.


Next coordinator code-audit checkpoint: docs/pro-coordinator-staff-plan.md records the automatic opposite-unit retention/roster gap and former-team unemployment retention gap. The live control and fifth-year staff features are already complete. Implement incrementally after the current trade APK acceptance; do not let a coordinator's uncontrolled players silently expire just because own-unit delegation is off.


Pro 52 packaging/scoped tests (167 cases) passed in run 40453905151 at e899094; APK artifact 12711701918 / pro-football-apk-52. Device finance/options checks passed, then the first synthetic trade save was below the fold: library predicate required the target visible before scrolling. Captured PNG/XML confirms three earlier slots fill the viewport. Harness now waits for library completion independently, then scrolls to the exact known fixture label; regression covers the offscreen fourth slot. Retry the same APK with input equality guard; do not claim Pro52 accepted yet. Failed-run evidence 12711303597 is .artifacts/pro52-ui. Application source is frozen for reuse.

Scoped retry optimization: Build52's original device run completed every finance/source-budget/old-save/option assertion before entering trade fixtures. The retry now uses validation_scope=trade and build/pro-trade-smoke.py on that exact APK; the application-input guard remains mandatory. Finance acceptance calls the same reusable trade flow at its end, avoiding two implementations. A trade-only run creates its disposable save folder, injects generated fixtures, and verifies offer counter/accept/decline and persisted ownership/depth/history. Keep original finance assertion evidence and later trade evidence distinct; do not call a trade-only run a new full GameCast/finance walkthrough. Run40455378521 was superseded/cancelled before repeating the full finance flow. Fifteen synchronization helpers pass.


Focused trade retry40455576333 at ec8611a passed input equality/helpers and reached actual counter/accept, then caught a real phone presentation issue: the recent-offer Result column was horizontally offscreen. Fix the app history to compact Year / stacked Player+Team / Result columns (295dp total). This app change requires a new APK; do not reuse52 afterward. The trade transaction completed far enough to show 0 open offers/history, but the failing invocation did not reach final persisted-ownership assertions, so acceptance remains pending. Evidence12711504486 / pro-ui-evidence-54. Preserve the earlier annual40/168-case evidence for unchanged transaction logic; run scoped CI and the corrected trade UI.


After the compact-history app fix, run40456011770 was immediately superseded/cancelled to avoid repeating unaffected finance device checks. Current new APK build is40456067627 (trade scope) at0e72ba4; it runs all fast Pro regressions, source validator/helpers, packaging and focused offer UI. Reuse Build52's finance/option assertions and earlier annual evidence because only offer-history presentation changed. Do not accept until new exact-APK counter/accept/decline ownership checks and screenshots pass. Source obligations/market pricing and pending-bid minimum/remainder audit remain future financial work.

Coordinator retention checkpoint: all **179 scoped Pro cases passed locally in 51s**, including three coordinator seasons and imported-cap lifecycle, plus **40 targeted cases** in22s and15 UI helpers. Source-only staff-unit authority reuses the existing GM evaluator; no global market/contract-lock/HC CPU policy change. OC/DC synthetic fixture exports are valid (about376KB each), isolated from production. New `retention` device scope validates the delegation explanation, advances the stage with own-unit auto negotiation off, and checks saved opposite-unit renewal, manual own-unit expiry, job identity and GM decision. Finance scope invokes this same flow after its existing checks; helpers can select saved slots below the fold. Package and verify this batch; do not report a new accepted APK yet.
