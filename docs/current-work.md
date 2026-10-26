# Current work — College / Pro football monorepo

Updated October 26, 2026 (Pacific). This file is the current handoff; Git history preserves earlier checkpoints.

## Branch and verified downloads

- Work only on `feature/pro-franchise-foundation`. Stable `main` remains `fdd3d4898b5bd07eeec8dba346024d61f5289474` (remote verified October 26).
- Latest Pro application source: `6faa77f` (Build 35 in device verification). Last fully verified Pro source: `e32c1e2`; prior scoped QA harness: `1ab25f0`. College/shared application source remains `51c5cef`. Later documentation/label-only commits do not change the APK.
- **Pro APK Build 33 verified:** [download](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40365289405/artifacts/12669723503).
  Original run `40365289405` passed 106 finance/transaction regressions, 12 helper cases and packaging. Its first device invocation failed on the workflow shell selector before app tests began.
  **Verification run [40366831239](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40366831239) passed**, reusing that exact APK after a source/build-input equality check. Helper tests and scoped financial device flow passed; build was deliberately skipped. [Device evidence](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40366831239/artifacts/12669654799).
- Exact downloaded Pro 33 also installed locally and loaded an older 2026 save without silently importing cap allowances. Local new-save allocation/reinstall/theme checks passed; all 108 Pro cases passed locally once, including longer financial simulations.
- **College Build 389: all jobs passed**, including 682 combined Core/College cases and broad management/theme + career device flows. No College/shared app changes occurred in the subsequent Pro-only batches, so no redundant rebuild was run.
  APK: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40352915636/artifacts/12665525718
  Broad evidence: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40352915636/artifacts/12664439444
  Career evidence: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40352915636/artifacts/12664754017
- Last full Pro GameCast walkthrough: Pro 31 at `51c5cef`; subsequent 33 verification was deliberately financial in scope. Do not claim a newly repeated full GameCast acceptance run.
- Never merge unfinished Pro work or clean up branches/releases as part of this batch. `sources/` remains read-only.

## Active contract accounting batch — not yet released

Work in progress in the D: checkout. New source pack matches 2,012 contracts (October 26 Parquet SHA-256 `3f6f8906a20d48c688f5727e76856997bc01ad2d9e1ac0ed83b41559f4a12a9f`). Added playing/void distinction, separate bonus cash, annual cash and conservative guaranteed-base classification, cap-preserving guarded source migration, known generated remaining terms, and compact contract details with expandable accounting. Expiration now moves remaining void bonus debt to the following season; extension checks use net replacement commitments. CPU replacement budgets/retry and retention depth planning were necessary follow-ups to sourced multi-year regression failures. All **118 Pro tests passed locally** in 2m57s, including ten new accounting cases, twenty generated-contract seasons, five sourced-contract seasons and the three-season imported-cap scenario. The final optimization removed repeated contract scans and cached replacement savings without changing rules. 12 UI-helper tests passed. Current implementation commit: `6faa77f`. Finance-scoped CI Build 35 run `40405152646` passed all jobs: 116 financial tests, packaging and exact-APK financial/contract/theme/reinstall acceptance. APK artifact `12690317621`; evidence `12689689721`. Visual review found the accounting year column truncated the Void label; a 110dp column and improved compact-table screenshot are the final UI-only follow-up in `7687443`, currently being verified in Build 36 run `40406332938`; do not claim a new tested APK. Default remains unchanged until source reconciliation is complete.

Final cleanup is separately authorized for the end: audit/archive obsolete Java files from `Initial Base Upload`, rename the GitHub repository to `antdroidx/Football-Sim-Engine`, update remotes/links, and verify both application build paths. Do not perform this while current implementation/verification is unfinished.

User also reported Pro landscape quick calls requiring scrolling. Add College-style collapse-after-selection and dynamic field height as the next live-view UI batch; do not interrupt the accounting verification to mix in unrelated layout changes. See checklist.

## Next authorized phase — real contracts and simpler presentation

Implementation pending. See [pro-contract-reconciliation.md](pro-contract-reconciliation.md) for the confirmed importer/default/term defects, ordered implementation and acceptance criteria. User approved real salaries as the default after necessary fixes, realistic generated remaining terms, drafted four-year rookie contracts with separate first-round fifth-year options, and simpler primary financial wording. Do not switch the default before correcting cash/void-year accounting. Do not overwrite negotiated saves or invent missing source liabilities. The existing hourly continuation is requested to resume this phase when usage is available. No new application code or APK is included in this documentation checkpoint; Build 33 remains the verified download.

## Completed batch — supplied 2026 cap initialization

- User supplied `C:/Users/ahngu/Desktop/2026-salarycap.csv`; copied unchanged to `src/ProFootball.Core/nfl_2026_salary_caps.csv`. Exactly 32 unique teams and 28 negative adjustment rows; every cap equation reconciles. WAS/WSH alias explicit.
- New Pro 2026 saves get exact integer base/rollover/other-adjustment/adjusted allowances. All cap budgets now use adjusted team allowances. Contract obligations, allocated bonus, other cap charges, dead money, reserved rollover and available game budget remain separate. No missing source commitments/debt/opening balances are inferred. Old saves are not re-budgeted.
- New games measure/elect rollover after final regular-season games, reserve it against closing-year room, then credit once next year. Retention bonus/restructure timing follows the applicable next league year and change lock. Annual cap growth remains league-base driven; 2026 adjustments are not repeated in later years.
- CPU depth budgets quote amounts its own negotiation evaluator will accept (avoids repeatedly bidding below a rookie's acceptance floor); no cap forgiveness or invented players.
- User requested efficient scoped QA. Added `validation_scope=finance` and `build/pro-finance-smoke.py`; see [testing-scope.md](testing-scope.md). Reuse College 389: no College/shared simulation/application changes.
- Local validation: **108 Pro tests passed**, including 20 estimated-contract seasons, five sourced-contract seasons and three new imported-cap seasons. **12 UI-helper cases passed**; Pro packaging passed with zero warnings/errors. Targeted financial device flow passed locally: all allocations persisted/reconciled, signed adjustments displayed, reinstall/save and light theme checks passed.
- Pro 32's full device job failed because a partially clipped appearance button was tapped over Android navigation. Captured XML/screenshot confirmed it; helper now rejects that clipped target while retaining fixed footer buttons. Both cases are unit tested. Pro 32 remains locally installed/update-tested, not a fully green CI release.
- Do not report Pro 32 device CI as passing. Game source committed as `e32c1e2`. Pro 33 is now verified by successful QA-only run `40366831239` after the shell selector fix. Both exact APK and evidence links are above. No redundant compilation/storage or College run was required. No routine full GameCast/College rerun for this Pro-only financial batch.

## October 26 implementation batch

Canonical working checkout (confirmed by the user): `D:/AppProjects/Codex/Football Simulation Engine`. It was fast-forwarded to all verified updates on October 26. The temporary checkout under `C:/Users/ahngu/.codex/visualizations/2026/10/24/01a16ca7-5745-7053-846f-b6fa2f023af1/repo` is redundant and must not be used or recreated for future development. Use the D: checkout explicitly; QA artifacts can live outside the repository in a user-approved project location. Branch unchanged. Pro Build 31 and College Build 389 now passed all GitHub jobs at `51c5cef`. Pro APK: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40352915349/artifacts/12665286547 · College APK: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40352915636/artifacts/12665525718. A subsequent Pro-only CPU budget fix is being validated; no further College/shared changes.

- Rookie/new contract concern protection, rare mid-contract dissatisfaction and legacy complaint cleanup; new deals are Content rather than automatically happier.
- Unified offer/current-deal/position-market workspace, annual bonus/cap/guarantee rows and release consequences by season.
- Adjusted cap rollover election, tracked cash floor/settlements, credited-season minimums, reproducible revenue/cap inflation, future cap plan/history and year-specific bonus cash. See [financial model and limitations](pro-financial-model.md).
- Future pick estimates use original-team roster age/talent/expiry uncertainty and played results, with distance regression; actual draft order remains fixed during the draft.
- All transaction history with season/week/search and Overview link; late-season-only Overview bracket shortcut; permanent league bracket remains.
- Staff coordinator changes allowed during the season for employed head coaches; correct disabled permissions and empty candidate feedback. Awards & All-Pros contains league-wide first/second teams; Pro Bowl is named and opens after conference championships.
- Front Office capitalization; ordinary source/development disclaimers removed in Pro and College. Useful gameplay explanations and attribution remain.
- Photo-reference skin overlay for 1,944 bundled players, shared portrait/sprite color, existing/custom edits preserved. Other players remain editable; source photos/lighting are not assumed perfect.
- Local validation: **98 Pro cases passed**, including twenty complete seasons and seven new regression cases. Pro Android packaging passed. 682 combined legacy Core/College tests and 10 UI-helper tests passed; Both APKs packaged without warnings/errors; Pro device flow and both College device flows passed. Exact Pro 31 APK also installed over a local build, loaded its saved franchise and passed current/offer/market tabs and cancelable annual release preview on a disposable emulator.

### CPU follow-up discovered by sourced-mode stress

- First sourced-mode diagnostic failed: one CPU over cap, unsigned rookies and thin rosters after the first offseason. No cap forgiveness was applied.
- CPU now attempts legal, once-per-season restructures before cutting core starters, preserves future budget, reserves inflated depth money and signs pending rookies before free-agent bids. Five sourced seasons (fixed franchise ID `53120000-0000-0000-0000-000000000001`) then completed 2027–31 with zero over-cap teams/unsigned rookies, minimum active rosters 47/53/53/53/53 and draft mean ratings 63.9/64.4/64.8/63.9/63.5. Camp rosters may exceed 53, capped at 90 until final cuts.
- This proves continuity for this seed, not source reconciliation. The imported opening snapshot still has eight teams above the modeled base cap because authoritative adjustments/rollover/departed-player debt remain missing. Keep full source-contract accounting experimental. The supplied 2026 allowances are now imported; do not report all opening commitments/debt/history reconciled.
- Full 100-case Pro regression passed locally and in Pro 32 CI, including twenty estimated-contract seasons and five sourced-contract seasons. Pro 32 packaging/server tests and local update/save checks passed; full CI device flow failed on the clipped appearance-button test tap; the current scoped financial build supersedes it. College/shared source is unchanged from verified 389; retain its verification without rebuilding it solely for a Pro-only follow-up.

## Prior implemented refinements

- Franchise analytics compare your team with league averages, leaders and tied ranks for roster/positions, age, current/future cap, draft inventory and regular-season per-game performance. Preseason has a clear empty state.
- Contract salary, bonus and restructure controls use $250K steps; terms open at market/agent estimates using age, comparable recent deals and competing CPU bids. Existing financial amounts retain exact accounting.
- Both trade panels expose draft-year filters and the next three future classes.
- Regular-season entry, game launch and advancement enforce user cap/53-player eligibility, including old illegal saves. Blocked advancement does not consume calendar/morale time.
- The playoff bracket shows projected seeds, first-round byes, saved results, current matchups, historical seasons and reseeded divisional rounds. Future opponents stay TBD.
- Overview/inbox alerts cover cap, unsigned rookies, expiring deals, contract dissatisfaction, trade requests, injuries, GM decisions and job offers. Read status persists; resolved alerts disappear.
- The expiring-contract retention desk supports individual negotiation, let-expire choices and optional staff negotiations. Staff respect role/cap limits; one-shot delegation and automatic-on-advance are separate options.
- Draft/scouting uses a dense sortable/paginated, searchable, position-filtered grid. Scout reports run automatically within the annual 40-player budget; projected grades retain uncertainty.
- Draft selection is separate from signing. CPU cap/roster problems cannot stop the draft: selected players remain reserved/unsigned until their four-year rookie contracts fit. No cap forgiveness or playable unsigned rookies. Full zero-cap regression completes all 224 picks.
- Rookie-year/draft metadata fixes short estimated starting rookie terms and repairs legacy saves without changing the current cap charge. Sourced future rookie financial projections are explicitly estimated; negotiated deals are retained.
- User chose illustrated portraits with editing. Player cards edit skin tone, hair and facial hair with preview/save. Portraits and live sprites share the seven-tone palette and identity fallback. No appearance is inferred from a real player's name.
- Library loading runs off the UI thread with a visible spinner, protected controls, cached rookie metadata and indexed migration lookups.

## Existing implemented foundation

- Shared canonical 19 attributes/overall, simulation, GameCast/sprites/animations, field, playbooks, coin toss/kickoff, cameras/replay/box scores and three quick recommended calls. Control respects team, possession and HC/OC/DC role. Non-user games offer team selection.
- College-style responsive Pro UI with a distinct palette, Fold sidebar, themes, profiles/portraits, sortable grids, compact FA/stat categories, branded full-season/weekly schedules, saves/backups and visible deletion.
- Two-team trades, player/pick selection, trading block, value/need/contract assessment, preliminary interest, pick suggestions, ownership/history and atomic cap/roster checks.
- Negotiation/rejection/counters, extensions, cash-conserving restructures and one completed contract change per season. Coordinator GM review protects current/future unit and shared team commitments, including pending bids, while permitting non-worsening repairs.
- Weekly positive/negative talks with visible cooldown, signing reactions/personality decay, contract concern and severe trade requests.
- 90 total camp players, final 53 cutdown, saved awards/all-star selections and optional shared-engine all-star exhibition isolated from league statistics/cap/injuries. Factual news retains small thumbnails only.
- College custom coach names, visible save deletion and non-user team selection. Shared Android SearchBar tint workaround preserves themed text while avoiding Android 16 vector-icon crashes.

## Prior baseline validation (Build 30 / College 388)

- **Pro:** 91 cases passed locally and in Build 30, including twenty-season roster/cap/draft/aging/honors stability, zero-cap draft completion, migration cap conservation, scouting, retention, inbox and bracket checks. Long-term simulation uses estimated game contracts.
- **Core / College legacy combined:** 682 fast cases passed locally and in CI. This suite has not been individually classified; do not label all 682 exclusively Core or College. Slow health/calibration classes are excluded from this local fast command.
- **Pro Android Build 30:** full CI flow passed: creation, profiles/ratings, appearance editing, FA/trade/contract UI, coin toss/kickoff, playbooks, watch/takeover/replay, saved final, reinstall/reload/advance, light/Fold, analytics/inbox/bracket/career/news, deletion and coordinator control.
- **Targeted disposable emulator:** saved draft grid/filter/40 reports, retention/inbox, 35 staff renewals while retaining one let-expire choice, blocked cap advance with unchanged week/tick, saved reseeded bracket and persisted skin choice verified. Synthetic fixtures exercise UI behavior, not financial calibration.
- **College Android Build 388:** both career and broad flows passed: branding/options, custom names and roles, permissions/firing, management layouts/themes, profiles/recruiting, shared GameCast/cameras/speeds/talks/replay/takeover, saved final/advance, departures and history.
- Both apps package locally; UI helper tests: 10 passed; packaged-resource check passed.
- An extra local install of the downloaded CI APK encountered emulator storage pressure. CI's independent install/reinstall/save flow supplies the exact APK update evidence. No franchise saves were deleted to free space; a College QA DB backup is outside the repo.

## Remaining work — do not call the full Pro roadmap complete

1. CPU autonomous trade proposals/counteroffers, valuation calibration and coordinator GM review of shared future pick value.
2. **Sourced contract accounting remains experimental:** complete opening commitments/departed-player debt/historical cash and long-term sourced-ledger reconciliation. Missing financial data is not zero; projected rookie future terms are estimates.
3. First-round fifth-year option decisions, deeper rookie/waiver rules, richer scouting/combine/camp decisions, CPU roster/contract calibration, automatic management of a coordinator’s opposite unit and career/job-market behavior.
4. Further ratings/morale/statistical calibration, growth/scatter/draft-outcome analytics and richer factual record/playoff-race/weekly-roundup news.
5. Audit individual tests before splitting the combined suite into Core and College projects/counts.

## Continuation

Hourly `resume-pro-football-development` remains active because work remains, including after usage resets. Read this handoff, checklist, chat and repository state first; do not duplicate active work. Keep source accounting experimental and preserve main, College package identity/save compatibility and separate Pro identity. Do not disable the follow-up until all authorized work is complete.

Historical management UI handoff: [Build 309](current-work-build309.md).

Final checkpoint: Pro 30 and College 388 are fully verified at the source above. Main is unchanged. The continuation stays active for the remaining acceptance work; no merge, release publication or repository cleanup was performed.
