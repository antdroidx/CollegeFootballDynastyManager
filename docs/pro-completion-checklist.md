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
