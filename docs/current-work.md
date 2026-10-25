# Current work — Pro Football Franchise Manager

Updated: October 25, 2026 (America/Los_Angeles).

This is the primary handoff/status document. Update it with each implementation batch and its final validation results. It is manually maintained; Actions does not regenerate it. Detailed requirements live in [the Pro completion checklist](pro-completion-checklist.md). Preserve this file as the current status rather than replacing it with a single completed feature's report.

## Repository and latest verified build

- Active branch: `feature/pro-franchise-foundation`.
- Latest Pro device-verified application source: `186e699cf4f2eb5bb0bfd6fea0301d0d1c37b02b`. Later documentation-only commits do not require a new APK.
- [Pro Build 22 APK](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40238640473/artifacts/12616450302).
- [Pro build and Android checks: passed](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40238640473).
- [Pro Android screenshots/evidence](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40238640473/artifacts/12616940698).
- [Previous College build and both Android checks: passed](https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40220240848).
- Stable `main` remains `fdd3d4898b5bd07eeec8dba346024d61f5289474`. Do not merge unfinished Pro work or change College package/save identity.

## Implemented and verified

- Shared football simulation and canonical 19 player attributes/overall calculation; shared GameCast animation, field, sprites, playbooks, cameras and box scores.
- Player/staff portraits and profiles; complete rating cards and optional roster attribute columns. No Madden branding in Pro UI; import provenance remains in data/documentation.
- HC/OC/DC careers, firing toggle, job offers, own-unit management restrictions and GM decision inbox. Coordinators automatically watch CPU staff handle the opposite unit, then wait for their own play calls; CPU-unit pause/resume is available.
- Multiple save slots, rolling backups and named deletion confirmation. Cancel preserves the save; deletion removes its backups and prevents delayed autosaves resurrecting it.
- College-family green/teal/gold Pro branding, with independent package/version workflow.
- Multi-player/pick trade packages, three future draft classes, position/age/potential/contract/need valuation and completed-trade history. Ownership survives draft-order finalization.
- Saved factual news with categories, unread/filter state, results, trades, transactions, injuries and winning streaks. Franchise/player charts show saved roster, statistics, cap commitments and draft inventory.
- Build 21: shared college coin-toss ceremony, Receive/Defer choice, explicit opening kickoff and kickoff formation. Playbooks are restricted to the controlled team, possession, role and scrimmage state. Non-user Play and Watch takeover both support team selection.
- Build 21: compact free-agent rows, position filters, name search and offer/claim actions.

## Validation categories and actual counts

Use **Core**, **College**, and **Pro** in reports. Classify by what a test exercises, not its current folder or underlying assembly name. Label device flows **College Android** and **Pro Android**.

| Category | Latest evidence |
|---|---|
| Pro | 45 automated cases passed, including the twenty-season stability simulation and new kickoff/controlled-team permission coverage. |
| Core / College legacy combined suite | 678 fast cases passed in the existing `DynastyManager.Core.Tests` project. These have not yet been individually classified; do not report all 678 as exclusively College or exclusively Core. |
| Pro Android | Passed full CI device flow: creation, attributes/profiles, dense free-agent filtering, coin toss/kickoff, watch/takeover/replay, playbooks, save/update/reload, week advancement, light/Fold layouts, analytics/news/career, deletion/cancel and coordinator own-unit pacing. Additional local checks verified non-user Play and Watch takeover team selection. |
| College Android | Both career-options and broad launch/management UI jobs passed after the shared coin-toss extraction. |

## Active expanded request (October 25)

The user authorized the expanded management parity pass recorded at the end of [the checklist](pro-completion-checklist.md). Implement College-inspired sidebar/drawer, three quick-call cards, one-line sortable grids, category statistics, full-team schedules, two-team trading/block, precise contract negotiations/restructures, 90-to-53 roster handling, awards/all-star support and weekly personality-based morale. Also add College coach naming, non-user team selection and visible deletion. Both games should use small news thumbnails without large stadium/logo illustrations. Work is in progress; Build 22 is verified for Pro; the next College run must verify the navigation test fix and shared SearchBar theme-crash workaround.

## Current implementation batch (Android verification pending)

- College: custom new/existing coach names, prominent whole-dynasty deletion, non-user Play team choice, small news thumbnails without large artwork.
- Pro: shared College quick-call cards, compact sortable/paginated grids, one-row free agency with position/search filters, category statistics separated from analytics, full-team and league schedule filters with badges/current-week marker, Fold sidebar, working indicator and small news thumbnails. Repetitive rating-scale captions removed.
- Camp rosters may hold 90 through preseason. CPU final cuts happen after the finale; an oversized user roster retains completed games and waits for cuts before regular-season advancement.
- Validation: **50 Pro cases passed**, including twenty-season simulation and final-cutdown coverage. **682 Core/College legacy combined fast cases passed** (four new College naming cases). Both local Android builds passed before the last small UI edits; final build/device evidence pending. Build 22 subsequently passed Pro Android checks; College 380 failed as detailed below.
- Still outstanding: trade workspace/block/CPU offers, precise negotiation/restructure and cap ledger, weekly personality/morale, awards/all-star support. Do not mark the expanded request complete.

## Outstanding work and next priorities

The full Pro plan is not complete. See the checklist for acceptance gaps; do not represent prototypes as finished systems.

1. Tighten career/GM decision rules for shared picks and future financial commitments; editable coach appearance and deeper job-market behavior.
2. CPU trade proposals/counteroffers, trade block and broader valuation calibration.
3. Authoritative cap ledger, team rollover/adjustments and departed-player dead money reconciliation. **Sourced contract accounting remains experimental.** Preserve coverage/estimate labels; do not silently treat unavailable source amounts as zero.
4. Richer scouting/combine/camp/offseason calendar and correct waiver contract assumptions; continue AI and long-term validation.
5. Ratings-growth history, efficiency/scatter/draft-outcome analytics; draft/record/playoff-race news and weekly roundups.
6. Continue College-to-Pro page/interaction parity and broader progression calibration.
7. Audit test classifications before reorganizing projects or reporting separate Core/College counts. Keep existing CI coverage during any later test cleanup.

## Continuation and safeguards

- Hourly `resume-pro-football-development` follow-up is active. Read this document, the checklist, current chat and repository state before resuming. Do not duplicate active work.
- Keep stable main, College Android identity, shared save compatibility and existing College workflows intact.
- Run appropriate automated and Android checks for changes; report exact tested APK links. A build compiling is not equivalent to device validation.
- Keep `sources/` read-only. Preserve unfinished work across usage resets/restarts.
- Update this file and the checklist together. Use documentation-only commits without rebuilding unchanged applications when appropriate.

## Historical handoff

The previous completed management-UI status is preserved in [Build 309 history](current-work-build309.md). Its branch/PR/build statements are historical, not current instructions or the current development status.

### Follow-on trade/contract batch (awaiting Android acceptance)

Two-team selectable trade workspace and a persisted user/CPU surplus trading block are implemented, with value-gap pick suggestions, position filters, preliminary interest and existing role/cap/roster review. A single exact-dollar contract workspace includes counters, rejection feedback and salary/bonus/year controls. Extensions/restructures lock after one completed change per season; restructures preserve total cash and cap charges while moving proration. Rookie draft salaries bypass veteran market negotiation. **57 Pro tests passed**, including twenty-season validation. Android source compiles; final packaging/device checks are pending. The existing Pro Build 22 and College Build 380 CI runs cover source `186e699`; College career smoke needs the navigation/name disambiguation fix `b2f232f` in the next run. Remaining weekly morale/personality, awards/all-star and source cap reconciliation are not complete.

### CI regression follow-up

Pro Build 22 passed both jobs. College 380 built successfully, but career smoke hit an ambiguous Coach name/navigation label (test fix `b2f232f`), and broad UI smoke captured a native Android 16 SearchBar vector-tint crash on system light-mode switching. The shared AndroidSearchAppearance mapper workaround avoids native vector tint callbacks while retaining game text/hint colors; both hosts enable it. Upstream reference: https://github.com/dotnet/maui/issues/39208. Final Pro/College builds and device checks must pass before declaring this follow-on batch verified. No changes to stable main or package/save identities.
