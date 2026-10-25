# Current work — Pro Football Franchise Manager

Updated: October 25, 2026 (America/Los_Angeles). This manually maintained handoff is updated with every implementation batch. Detailed acceptance gaps live in [the completion checklist](pro-completion-checklist.md).

## Repository and verified downloads

- Branch: `feature/pro-franchise-foundation`.
- Latest device-verified source: `8a5d1c7f9a50a9dee9ed933d04f27e65dc7a2b2c`.
- [Pro Build 24 APK](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40240371421/artifacts/12616896838): build and Pro Android checks passed. [Workflow](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40240371421), [device evidence](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40240371421/artifacts/12616008938).
- [College Build 382 APK](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40240371410/artifacts/12616797849): build, career Android and broad management/theme Android checks passed. [Workflow](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40240371410).
- Remote stable `main` verified unchanged at `fdd3d4898b5bd07eeec8dba346024d61f5289474`. No unfinished Pro work is merged there.
- College package/save identity and Pro independent package `antdroid.profootball.franchise` remain unchanged. CI uses increasing version codes and the existing stable development signing key.

## Implemented and device verified through Build 24 / College 382

- Shared simulation, canonical 19 attributes/overall, GameCast, sprites/animations/field, playbooks, coin toss/kickoff, cameras, replay and box scores. Play calling respects controlled team, possession and coaching role. Non-user Play/Watch takeover supports team selection.
- Player/staff portraits and profiles; full rating cards and roster attribute columns; HC/OC/DC careers, firing preference, unit restrictions and GM decisions; save slots, rolling backups and deletion confirmation.
- College-family Pro palette/branding, responsive Fold sidebar, loading indicator, compact sortable/paginated grids, one-line searchable position-filtered free agency, separate category statistics/analytics and branded whole-team/league schedule filters with current-week marker.
- Three shared College-style quick-call cards, refreshed after each snap.
- Two-team player/pick trade workspace, persisted user/CPU surplus trading block, preliminary interest, value-gap pick suggestions, future-pick ownership and trade history. Position/age/potential/contract/need valuation and cap/roster checks remain enforced.
- Exact-dollar salary/bonus/term negotiation form, rejection/counters, one completed contract change per season, extensions and cash/cap-conserving restructures. Veteran negotiation does not override rookie salary-scale contracts.
- College custom coach names, visible whole-dynasty deletion and non-user Play team selection. Both news views retain small thumbnails without large stadium/logo art.
- Android 16 native SearchBar theme-tint crash worked around in shared presentation; text/hint colors stay themed while icon vectors remain platform-owned. College system dark/light checks passed. Reference: https://github.com/dotnet/maui/issues/39208.

## Current follow-on batch — final Android acceptance pending

- Weekly team/unit talks reuse College positive/negative reactions. One talk per calendar advance; used buttons are visibly disabled. Signing reactions fade by simulated personality; sustained severe contract dissatisfaction can lead to trade requests, and agreements/trades can improve morale.
- Camp capacity is 90 total active/practice players (reserve excluded), then 53 active plus practice squad for regular season. Final games remain saved while the user cuts down; repeated blocked cutdown advancement cannot advance morale/cooldown time.
- Saved regular-season awards and conference all-star selection snapshots feed factual news. Optional watch/play/simulate traditional all-star exhibition uses the shared engine/GameCast. Its result, injuries and statistics are separate from league players/games/cap. Honors has Awards / All-stars / Showcase tabs.
- Efficiency statistics include NFL passer rating and per-attempt/carry/catch/punt/return averages. Pro box-score grids also use the shared sortable grid.
- College Android career checks now enter and reload custom coach names and assert visible deletion; names are limited to 60 characters in both forms.

## Validation — report Core, College and Pro honestly

| Category | Evidence |
|---|---|
| Pro | Build 24: 57 cases plus Android passed. Current follow-on: **64 automated cases passed**, including twenty-season roster/cap/draft/aging and honors-history checks, weekly talk cooldown/reactions, personality effect decay, contract concern, exhibition isolation and 90-player camp capacity. |
| Core / College legacy combined | **682 fast cases passed** in the existing `DynastyManager.Core.Tests` project. They have not been individually classified; do not label all 682 as exclusively Core or College. Four new coach-name cases are College-specific. |
| Pro Android | Build 24 CI and the local broad emulator flow passed trade/block/negotiation, ratings/profiles, FA filters, coin toss/kickoff, playbooks, watch/takeover/replay, save/reload/advance, light/Fold, careers/news/analytics and deletion. |
| Targeted follow-on Android | Disposable emulator verified weekly reaction screen, visibly disabled cooldown, saved honors, exhibition simulation/save and its final box score. Synthetic fixture only on emulator-5554. The extra script expected the wrong final heading; captured XML correctly shows `Live box score` / `Final statistics`. New tab layout and final batch still need CI acceptance. |
| College Android | Build 382 career and broad management/theme flows passed. Final follow-on run must also verify custom-name entry/reload. |

## Remaining acceptance work

Do not represent the full Pro roadmap as complete.

1. Complete final Pro/College APK builds and Android checks for the current follow-on source, fix failures, and replace verified download links above with exact tested artifacts.
2. CPU autonomous trade offers/counteroffers and further valuation calibration; GM review of shared future picks and future unit commitments.
3. Authoritative sourced cap ledger: rollover, team adjustments and departed-player dead-money reconciliation. **Sourced contract accounting remains experimental.** Preserve estimate/coverage labels; unavailable amounts are not zero.
4. Richer scouting/combine/camp/offseason decisions, waiver contract assumptions, editable coaching appearance and deeper career/job-market behavior.
5. Further progression/morale/statistical calibration; ratings-growth/scatter/draft-outcome analytics and record/playoff-race/weekly-roundup news.
6. Audit individual test classifications before splitting projects/counts. CI labels now distinguish College/Pro Android and the combined Core/College suite; existing coverage remains intact.

## Continuation

The hourly `resume-pro-football-development` follow-up remains active, including after usage resets. Read this file, the checklist, chat and repository state; never duplicate active work. Keep both status files current. Preserve main and College identities/saves. Keep `sources/` read-only. Do not delete branches/artifacts or merge unfinished work as part of this batch.

Historical management-UI handoff: [Build 309 history](current-work-build309.md).
