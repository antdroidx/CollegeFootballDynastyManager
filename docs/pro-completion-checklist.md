# Pro football completion checklist

Stable college main remains untouched. All work is on `feature/pro-franchise-foundation`.
This is an implementation ledger, not a claim that every system is release-ready.

| Requirement | Current implementation | Remaining acceptance work |
|---|---|---|
| Shared engine / GameCast | Same simulation, choreography, field, sprites, playbook and box-score presenters as college | Continued device coverage for both apps |
| Canonical ratings | Shared 19 attributes, overall formula, one-time migration, complete cards and columns | Broader progression calibration |
| Saves | Multiple slots, rolling backups, deletion confirmation and anti-resurrection tombstone | Device delete/cancel coverage |
| Careers | HC/OC/DC setup, firing toggle, reviews, job offers, unit restrictions, GM inbox | Opposite-unit autonomous play pacing; editable coach appearance; deeper job market |
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

Local validation: 38 pro tests passed including twenty-season stability; 678 fast college regression tests passed. Android code compiled and packaged successfully before the final presentation refinements; CI device evidence is still required for this batch.
