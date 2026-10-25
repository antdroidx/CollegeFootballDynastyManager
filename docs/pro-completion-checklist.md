# Pro football completion checklist

Current handoff, verified APK links and validation naming: [current-work.md](current-work.md).

Stable college main remains untouched. All work is on `feature/pro-franchise-foundation`.
This is an implementation ledger, not a claim that every system is release-ready.

| Requirement | Current implementation | Remaining acceptance work |
|---|---|---|
| Shared engine / GameCast | Same simulation, choreography, field, sprites, playbook and box-score presenters as college | Continued device coverage for both apps |
| Canonical ratings | Shared 19 attributes, overall formula, one-time migration, complete cards and columns | Broader progression calibration |
| Saves | Multiple slots, rolling backups, deletion confirmation and anti-resurrection tombstone | Verified delete/cancel flow in Build 18 device tests |
| Careers | HC/OC/DC setup, firing toggle, reviews, job offers, unit restrictions, GM inbox | Opposite-unit automatic play/pause verified in Build 21; editable coach appearance; deeper job market |
| Branding | College-family green/teal/gold badge for start, launcher and splash | Visual acceptance on Android masks |
| Trades | Multi-player/pick UI, three future classes, positional/age/potential/contract/need value, cap/roster checks, history | CPU offers/counters, trade block, richer GM pick budget and historical comparable calibration |
| Analytics | Shared accessible bars: roster ratings/age, position cap, future commitments, scoring margin, league cap, pick inventory, player game logs/career season charts/position comparison | Ratings-growth history, scatter charts, draft outcomes and more efficiency views |
| News | Persistent stable IDs, categories, unread/filter state, results, trades, injuries, winning streaks and transactions, report links | Record/playoff-race templates and featured weekly roundups |
| Contracts / cap | Integer-dollar contracts, bonuses, guarantees, dead money, pending bids, source snapshot optional | Team rollover/adjustments, full ledger, source reconciliation and long-run calibration; source accounting stays experimental |
| NFL calendar | Preseason, 17-game schedule, standings, playoffs, four-stage offseason, draft and FA | Richer scouting/combine/camp calendar and waiver contract assumptions |
| College parity | Player/staff portraits and cards, shared theme/dialogs/navigation patterns, live options | Continue page-by-page coverage against college |

## Validation for this batch

- New focused tests cover save/backups deletion and stale autosave rejection, coordinator authority, GM refusal, firing toggle, job moves, future-pick ownership and news idempotence.
- Full pro suite includes a twenty-season stability test.
- APK and device evidence are published by separate pro Actions workflow after commit.
- College workflow remains independent; shared presentation changes require college build verification.

Latest validation (Build 21, application source `58eda84`): 45 Pro cases passed including twenty-season stability. The existing combined Core/College suite passed 678 fast cases; individual Core/College classification is pending. Pro Android and both College Android jobs passed. Exact APK/workflow/evidence links are maintained in [current-work.md](current-work.md).

## Current live-view / free-agency correction

- College's coin-toss ceremony is extracted into shared presentation and used by both apps. Pro opening kickoffs have an explicit Kickoff action/formation and no scrimmage play choices.
- Playbooks are restricted by controlled team, possession, coaching role and kickoff state; non-user Play already asks for a team, and taking over a non-user Watch game now asks too.
- Free agents use compact rows with annual market value and offer/claim actions, name search and position filters.
- Added permission/kickoff regression coverage and Android checks for toss, kickoff, possession-specific playbooks and free-agent filtering.

## October 25 expanded management parity request — authorized

- [ ] Pro: College-style three recommended quick-call cards, same shared card UI.
- [ ] Pro: one-line free-agency grid; all grids sortable, including contracts.
- [ ] Pro: two-team trade workspace, selectable player/pick assets, CPU interest, trading block and partner discovery.
- [ ] Pro: visible loading/progress states for operations; sidebar/drawer navigation matching College at equivalent widths.
- [ ] Pro: separate analytics from sortable complete Passing/Rushing/Receiving/Defense/Kicking/Punting/Returns statistics views.
- [ ] Pro: 90-player offseason/preseason squads; enforce final 53-player cutdown before regular season.
- [ ] Pro: persisted season awards and Pro Bowl/all-star selection/game support.
- [ ] Pro: dense branded schedule grid, team selector and entire-team season schedule view.
- [ ] Pro: remove repetitive rating-scale captions everywhere.
- [ ] Pro: single negotiation workspace with precise salary/bonus/term offers, rejection/counteroffers; one completed contract change per player/season, restructures, improved cap ledger and AI parity.
- [ ] Pro: weekly scoped coach talks, positive/negative personality reactions, decaying signing/contract morale effects and severe trade requests.
- [ ] College: custom coach name for new/existing careers, non-user game team choice, visible save deletion.
- [ ] Both: news cards/details retain small team/player thumbnails; remove large stadium/logo illustrations (user clarified request).

All work remains on the monorepo development branch with separate app commits/build evidence; stable main stays untouched. Keep current-work.md synchronized and resume after usage resets. No claim of complete financial realism until accounting and long-term checks pass.

Implementation batch checkpoint: quick-call cards, sortable/paginated grids, category stats, branded full-season schedule, sidebar/loading, College naming/deletion/team choice and thumbnail-only news are implemented; Android acceptance is pending. Camp 90-to-53 transitions pass focused and twenty-season coverage. Automated results: 50 Pro and 682 legacy combined Core/College fast cases. Remaining major systems above stay unchecked until implemented and verified.

Follow-on checkpoint: trade/block workspace and exact-dollar negotiation/restructure paths are implemented with 57 passing Pro cases (including twenty seasons), pending final Android acceptance. Counter suggestions are advisory, not automatic completed trades. Full CPU autonomous trade offers and broader valuation calibration remain. Weekly morale, awards/all-star and authoritative sourced cap reconciliation remain outstanding.

Pro Build 22 Android passed: https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40238640473/artifacts/12616450302. College 380 device failures require rerun with coach-navigation disambiguation and shared Android SearchBar native-tint workaround. Treat the follow-on trade/contract source as pending device validation.

Pro Build 24 verified the trade/contract batch (57 automated Pro cases plus Android): https://github.com/antdroidx/CollegeFootballDynastyManager/actions/runs/40240371421/artifacts/12616896838. The next uncommitted morale/honors/efficiency batch has 63 locally passing Pro cases and targeted emulator evidence; final full run and Android publication are pending. See current-work for exact state and known remaining financial/AI calibration gaps.

## Final follow-on checkpoint (October 25)

Current-work.md is the authoritative current handoff; prior checkpoint paragraphs above are historical. Pro Build 24 and College Build 382 both passed all device checks. The follow-on morale/honors/camp/efficiency implementation passes **64 Pro cases**, including twenty seasons. Final Android source/build/device publication is pending.

Implemented in the follow-on: weekly scoped positive/negative talks with gray cooldown state; signing/personality decay and severe contract trade requests; 90 total active/practice camp capacity and final 53 cutdown; saved awards/all-star snapshots, factual award news and isolated shared-engine exhibition; efficiency stats and sortable box-score grids. College device checks now cover custom-name entry/reload and visible deletion. See current-work for exact verified download links and remaining financial/AI acceptance gaps.
