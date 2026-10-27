# Current work — College / Pro football monorepo

Updated October 26, 2026 (Pacific). This file is the current handoff; Git history preserves earlier checkpoints.

## Branch and verified downloads

- Work only on `feature/pro-franchise-foundation`. Stable `main` remains `fdd3d4898b5bd07eeec8dba346024d61f5289474` (remote verified October 26).
- Latest verified Pro application source: `c9cddbf`, APK 45 (finance/management scope). Full GameCast/aging/field device evidence: Pro 44 at `4f6bb2c`. College/shared source `2acfcc1`, APK 390. Later documentation/label-only commits do not change the APK.
- **Pro APK Build 37 verified:** [download](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40408864223/artifacts/12691773553). [All-green run](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40408864223), [device evidence](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40408864223/artifacts/12693410970). 116 scoped Pro cases, 12 helpers, packaging and full Pro device walkthrough passed. Landscape assertions and screenshot review confirm three cards/Next snap visible without scrolling, cards collapse, field grows, choices return after a snap and More exposes secondary controls. Broad watch/replay, saved final, reinstall/reload/advance, themes/Fold, coach/coordinator flows also passed. Prior 118-case long financial validation is reused because this batch changes no financial or shared engine code.
- **Pro APK Build 36 verified:** [download](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40406332938/artifacts/12690204175). [All-green run](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40406332938), [device evidence](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40406332938/artifacts/12691288745). 116 scoped Pro cases, 12 UI helper cases, packaging and exact-APK financial/contract/theme/reinstall checks passed. All 118 Pro cases also passed locally, including longer annual simulations. Screenshot review led to a final wider Void-year column; Build 36 includes it. This was finance scope, not a new full GameCast/College walkthrough.
- **Pro APK Build 33 verified:** [download](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40365289405/artifacts/12669723503).
  Original run `40365289405` passed 106 finance/transaction regressions, 12 helper cases and packaging. Its first device invocation failed on the workflow shell selector before app tests began.
  **Verification run [40366831239](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40366831239) passed**, reusing that exact APK after a source/build-input equality check. Helper tests and scoped financial device flow passed; build was deliberately skipped. [Device evidence](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40366831239/artifacts/12669654799).
- Exact downloaded Pro 33 also installed locally and loaded an older 2026 save without silently importing cap allowances. Local new-save allocation/reinstall/theme checks passed; all 108 Pro cases passed locally once, including longer financial simulations.
- **College Build 389: all jobs passed**, including 682 combined Core/College cases and broad management/theme + career device flows. No College/shared app changes occurred in the subsequent Pro-only batches, so no redundant rebuild was run.
  APK: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40352915636/artifacts/12665525718
  Broad evidence: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40352915636/artifacts/12664439444
  Career evidence: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40352915636/artifacts/12664754017
- Latest full Pro GameCast walkthrough: Pro 37 at `a2c74e8`. College remains verified at 389 / `51c5cef`; no College/shared code changed in the Pro-only layout batch.
- Never merge unfinished Pro work or clean up branches/releases as part of this batch. `sources/` remains read-only.

## Completed accounting batch — Pro 36

- October 26 Parquet source SHA-256 `3f6f8906a20d48c688f5727e76856997bc01ad2d9e1ac0ed83b41559f4a12a9f`; 2,012 sourced roster matches and 519 generated fallbacks. [32-team developer audit](pro-contract-source-audit.md) preserves the unresolved real-world counting/coverage gaps. Seattle modeled room remains -$9,422,775; do not report its official opening space reconciled.
- Playing years, inferred cap-only years, annual cash, bonus cash and guarantee-base/source-total fields are distinct. Safe source migration requires identical annual cap obligations and non-void salaries, preserving negotiated/mutated deals. Known source playing dates override rookie guesses; legitimate later short re-signings are retained. Generated unknown terms use explicit estimates rather than GUID randomness.
- Natural expiration moves residual bonus debt into the following season; extensions use net replacement budgets; void proration/acceleration stays separate from employment. Trade cash and release cutoff regressions preserve ownership/timing. Main contract view shows pay, cap and guaranteed salary with optional accounting; Void labels are readable.
- Sourced annual simulations exposed CPU replacement-cost/retry and retention depth-budget weaknesses. Fixed without cap forgiveness, weakening tests or inventing players. Cached lookups avoid repeated scans. **118 local Pro cases passed in 2m57s**, including ten new cases, twenty generated-contract seasons, five sourced-contract seasons and three imported-cap seasons. Final CI runs **116** scoped cases, 12 helpers, packaging and the targeted financial device flow.
- Build 35 passed initially; visual review prompted a wider accounting Year column and better compact-table screenshot. **Build 36 is the final verified artifact**, linked above. App source `7687443`, core `6faa77f`. College/shared code is unchanged; retain College 389 and prior Pro GameCast evidence.

## Completed Pro-only landscape layout — Pro 37

ProLivePage now uses a fixed viewport with a flexible field row, compact landscape header and pinned primary controls. Landscape secondary controls move to More; portrait retains its actions. Shared quick-call cards use horizontal layout in landscape, collapse after quick/playbook selection, and return after the next snap. The field reclaims space automatically; animation/replay also hide the quick tray. No simulation, College or financial logic changed. Source `a2c74e8`; local packaging passed with zero warnings/errors. Build 37 run `40408864223` passed all jobs. Exact APK and evidence links are above; both before/after landscape screenshots were visually reviewed. The user confirmed the build had completed, and its final status/evidence were independently checked.

## Seasonal cap-counting batch — verified in Build 39; history below

New 2026 franchises now use cap-rules version 2: offseason top-51 contract valuation, required non-top-51 Paragraph 5 excess, and all bonus/other charges retained. Excluded salary is separate from charged salary. Existing saves keep version 0/1/full-roster behavior. Opening-season eligibility explicitly uses full obligations; CPU financial planning is conservatively against opening-day obligations even in camp, and final CPU compliance uses regular-season counting. Inbox warns when preseason room is legal but opening room is negative. No source balancing amounts were invented and no College/shared code changed.

Seven new rule/save cases and all 123 scoped Pro cases passed. An earlier full 123-case run passed before the final source-credit guard and extra save case; the full 125-case annual regression passed in 3m49s before the final-cut timing correction, and all eight targeted seasonal/save/final-cut cases passed afterward. Final Android finance verification subsequently passed in 40416144714 (see acceptance below). Source audit now shows Seattle camp room $5,672,598 with $15,095,373 excluded salaries, versus full-roster room -$9,422,775. This is still not reconciled to official source space/departed-player debt. Do not claim a new verified APK or enable the sourced default yet. Initial seasonal-cap source `3014edf`; Build 38 run `40413381332` passed scoped regressions but was canceled during packaging to include a discovered final-cut timing fix. The final-cut follow-up uses regular-season week 1 only for CPU final-cut full-budget planning, preventing preseason week-three salary from being treated as earned; the original preseason week is restored. Added a regression. The targeted final-cut regression passed. Corrected finance Build 39 run `40414202682`, source `561dc4f`, passed all server tests/packaging but device QA failed on a test tap: Back was partially under the status bar (bounds 42,50–181,166, center 108 vs status bar 128). App stayed on the profile. Screenshot/XML confirmed the harness missed it; extend safe-target guard to the top edge, retain bottom/footer cases, add regression, and rerun device QA on the same artifact `12694921409` / `pro-football-apk-39` after the application-source equality guard. Do not rebuild unchanged app source; retain the earlier annual result and repeat affected fast/three-season checks. Do not report Build 38 as accepted.

## Newly queued field changes

After the current financial batch: use uppercase city names in Pro end zones (SEATTLE, LOS ANGELES, etc.); correct between-play referee/ball placement at the line of scrimmage in both College and Pro instead of inside the huddle. Audit found the shared GameFieldDrawable paints its central official 12 yards behind the line (`YardLine - 12`), overlapping the offense huddle centered 9 yards behind. Next field batch should distinguish between-play spotting from live/replay official positioning, place the official/ball at projected LOS when awaiting a scrimmage snap, and verify both directions, goal-line cases and kickoff separation. Pro end-zone text is set by ProLivePage.HomeName and can use uppercase home.City while retaining HomeInitial/badge abbreviations. Verify both games for shared presentation changes. Do not mix these into the seasonal cap-counting APK.

## Newly queued Pro aging / development changes

User reports aging veterans retain 4–5 potential stars. ProOffseason.Advance already applies annual position-dependent overall/skill regression, but the potential ceiling stays fixed. Audit and make future growth outlook age/position-aware without changing College's canonical attributes or falsely treating current elite ability as years of future development. Add the College-style season-start OVR delta in Pro roster/player views and test aging trajectories across positions. Keep this separate from the current seasonal cap counting batch; details are in the completion checklist.

## Next authorized work

Continue [pro-contract-reconciliation.md](pro-contract-reconciliation.md): complete opening source commitments/departed-player debt, seasonal Top-51 counting, option/vesting provenance and first-round fifth-year decisions before enabling the real-contract default. The default remains unchanged. Do not fabricate missing liabilities or force headline cap balances. Preserve negotiated saves and all College identity/save behavior.

The requested landscape quick-call correction is complete and verified in Build 37. Continue the financial/source/option work listed above; repository cleanup remains last.

Final cleanup is separately authorized for the end: audit/archive obsolete Java files from `Initial Base Upload`, rename the repository to `antdroidx/Football-Sim-Engine`, update remotes/links and verify both app build paths. Do not perform it while current implementation work is unfinished. Existing hourly continuation remains useful; do not disable it or mark the full roadmap complete.

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

## Source identity follow-up audit (read-only, not in current APK)

The October 26 Parquet has ten additional unique same-team, exact normalized-name matches missing source GSIS IDs. Examples: Federico Maranges (SEA, OTC12944), Valentin Senn (ARI), David Olajiga (BAL), Mapalo Mwansa (CAR), Vershon Lee (CLE), Ahmed Hassanein (DET), Thomas Yassmin (GB), Max Pircher (HOU), Paschal Ekeji (NYJ). Candidate report is ignored local `.artifacts/contract-name-candidates.json`; regenerate from the saved Parquet, never assume the file exists elsewhere. For a later data batch, require unique team/name and compatible position, retain source player URL/match method and reject ambiguities. Do not alter the current finance APK source while it is in acceptance. These candidates mostly concern exempt/young deals; they do not reconcile all missing contracts or official balances.


Financial QA retry 40415642843 passed the exact-APK source guard and all 13 helper cases, then stopped because the contract term was visible at the bottom while the adjacent APY remained below the viewport. Evidence confirmed correct profile/term; the harness now explicitly scrolls to the APY before asserting it. This does not change application inputs. Reuse Build 39 again; it is not accepted until the complete financial flow passes.


### Aging implementation audit checkpoint

Confirmed `ProOffseason.Advance` shifts skills and speed annually, then calls `ProPlayerRatingService.Refresh(resetBaseline:true)`. That recalculates the canonical overall, leaves the old potential ceiling intact, and resets the baseline to the new overall, hiding annual regression in a delta view. `FranchisePage.Profiles.cs` and the roster grid currently display absolute shared PotentialStars with no delta.

Next aging batch: retain the shared 19-attribute overall calculation; add Pro-only positional peak/decline profiles, shrink remaining headroom after prime without forcing potential below current ability, and distinguish projected ability from remaining growth. Preserve a previous-season rating separately from the existing season-start baseline so offseason decline remains visible and does not masquerade as in-season change. Legacy saves with no recorded prior-year rating must show an unknown/neutral baseline, never invented historical change. Test RB/CB/DE vs QB/OL/K trajectories, veteran ceiling conservation, generated prospects, migration idempotence and multi-season roster quality. Do not change College potential/star interpretation. Current financial APK acceptance is separate from this queued work.


## Financial Build 39 accepted

The exact [Build 39 APK](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40414202682/artifacts/12694921409), application source `561dc4f`, passed financial device acceptance in [QA-only run 40416144714](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40416144714). [Evidence 12695832869](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40416144714/artifacts/12695832869). Source/build-input equality passed, 13 helper regressions passed, 124 scoped Pro cases and packaging passed in the original run. The complete financial flow checked imported terms/cash, compact/void details, all 32 cap allocations, signed adjustments, reinstall/save and light theme. Screenshots reviewed. Prior 125-case annual run and eight final-cut regressions remain as recorded above; College/shared code unchanged, reuse College 389. Seattle modeled camp room is $5,672,598 vs full-season room -$9,422,775, not a claim of official $13.442M reconciliation. No balancing credits invented; sourced default remains experimental.

## Active Pro aging batch — not in Build 39

Added Pro-only position-specific peak ages, earlier athletic than technical/mental decline, shrinking future headroom, and persistent previous-season overall. The canonical College 19-attribute overall formula remains unchanged. Growth outlook is separate from present ability/projected ceiling; elite veterans no longer carry misleading young-prospect growth stars. Roster delta and profile annual change expose offseason decline; old saves without previous-season history use an existing baseline or unknown, never fabricated history. Trade/pick forecasts use the same age-aware headroom. Fifteen targeted development/shared-rating cases passed; broad annual/Android validation remains pending. Do not call this batch accepted until its build/device checks pass.

Aging batch: local Android compilation passed with zero warnings/errors; 15 targeted tests passed. Full annual regression started; Android acceptance pending.


## Active shared field batch

Pro end zones now use uppercase home city, retaining abbreviated scoreboard/center badges. Both hosts mark idle scrimmage huddles for ball spotting: central official uses projected LOS, offset across the field to clear both huddles, and a ball is drawn at the spot. Shared animation disables idle spotting before live/replay movement; kickoff huddles and coin ceremony remain separate. Eight goal-line/midfield/both-direction geometry cases were added; 36 affected shared presentation/live cases passed in Release. App device/visual acceptance remains pending. Pro aging APK source stays `56e7d1b`; this field batch is later and must be verified in both apps. Do not claim College 389 covers these new shared changes.


Aging validation at `56e7d1b`: all **136 Pro cases passed locally in 4m03s**, including twenty generated-contract seasons, five sourced-contract seasons and imported-cap lifecycle coverage. Local Android build passed, zero warnings/errors. Run 40438135849 reached APK packaging; supersede it with the combined aging/shared-field build at `2acfcc1` to avoid a redundant interim APK/device walkthrough. The next Pro `live` scope reuses the completed annual evidence, reruns affected fast cases and performs the full Pro UI/GameCast flow. College run 40438515279 verifies the shared field batch with both device flows, without repeating unrelated long career/calibration simulations.

Draft-generation review found template players could carry their previous-season OVR into newly generated prospects. Clear that additive history field at creation; generated draft players have no prior pro season. Fifteen affected tests passed, including the new assertion. Supersede 40438635362 before packaging to include this correction in the final combined APK; College source/verification is unaffected.


## Current verification queue

- Accepted finance: Build 39, exact APK/artifact and evidence above.
- Annual aging at `56e7d1b`: all 136 local cases passed, plus 15 affected cases after clearing prospect history at `4f6bb2c`.
- Pro combined aging/city/referee: run 40438801865, `live` scope at `4f6bb2c` (full device flow + fast regressions). Runs 40438135849 and 40438635362 were superseded to consolidate fixes; neither is an accepted release.
- College shared referee: run 40438515279, application source `2acfcc1`, both Android flows requested; unrelated long career/calibration excluded. Later changes are Pro-only/docs.
- Remaining financial source reconciliation, fifth-year choices, trade AI, wider roadmap and final archive/rename remain pending. Keep automation active. No main merge or cleanup performed.


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
