# Current work — shared Core, College and Pro

Updated October 26, 2026 (Pacific). This is the current handoff; Git history retains prior build narratives.

## Workspace and safeguards

- Work only in `D:/AppProjects/Codex/Football Simulation Engine`, branch `feature/pro-franchise-foundation`.
- Stable main remains `fdd3d4898b5bd07eeec8dba346024d61f5289474`. Do not merge unfinished Pro work.
- Preserve College package identity/save compatibility, independent Pro package/saves and update signing.
- Do not duplicate active implementation/builds. Use scoped tests per [testing-scope.md](testing-scope.md).
- Continue all authorized roadmap work autonomously. Hourly continuation `resume-pro-football-development` is active again; do not disable until everything is complete.

## Latest verified APKs

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
